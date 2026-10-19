// ┌───────────────────────────────────────────────────────────────────┐
// │  ____  _____ ____  ____   ___  _  __ ___    _                     │
// │ | __ )| ____|  _ \|  _ \ / _ \| |/ /|_ _|  / \                    │
// │ |  _ \|  _| | | | | |_) | | | | ' /  | |  / _ \                   │
// │ | |_) | |___| |_| |  _ <| |_| | . \  | | / ___ \                  │
// │ |____/|_____|____/|_| \_\\___/|_|\_\|___/_/   \_\                 │
// │                                                                   │
// │  Bedrokia - Minecraft: Bedrock Edition server software            │
// │  Licensed under the Apache License 2.0 - see LICENSE              │
// └───────────────────────────────────────────────────────────────────┘


using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Bedrokia.Server.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Bedrokia.Server.Plugins;

/// <summary>A plugin that is running, and where it came from ("folder", "source" or "dll").</summary>
public sealed record LoadedPlugin(Plugin Plugin, string File, string Kind);

/// <summary>
/// Loads the plugins at startup, each in its own load context (Bedrokia and .NET are shared with the server):
/// <list type="bullet">
/// <item>plugins/Name/ with a plugin.yml - the organised form (see <see cref="Plugin"/>), code in src/ or a DLL, and the
/// libraries the code uses (a database driver...) in libs/;</item>
/// <item>plugins/Name.cs - a single C# file;</item>
/// <item>plugins/Name.dll - a compiled class library.</item>
/// </list>
/// C# is compiled at startup with Roslyn (MIT). Plugins load after their dependencies; one that does not compile,
/// misses a dependency or crashes while enabling is reported with its error and skipped.
/// </summary>
public sealed class PluginManager(BedrokiaServer server)
{
    private readonly List<LoadedPlugin> _loaded = [];

    public IReadOnlyList<Plugin> All => [.. _loaded.Select(l => l.Plugin)];

    public IReadOnlyList<LoadedPlugin> Loaded => _loaded;

    public Plugin? Get(string name) => _loaded.FirstOrDefault(l => l.Plugin.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Plugin;

    /// <summary>Plugins that could not be loaded at startup, with the reason.</summary>
    public List<(string File, string Error)> Failed { get; } = [];

    private sealed record Candidate(string Path, PluginDescription? Description)
    {
        public string Name => Description?.Name ?? System.IO.Path.GetFileNameWithoutExtension(Path);
        public IEnumerable<string> Depend => Description?.Depend ?? [];
        public IEnumerable<string> SoftDepend => Description?.Softdepend ?? [];
    }

    internal void LoadAll(string directory, string dataDirectory)
    {
        if (!Directory.Exists(directory))
            return;
        var timer = Stopwatch.StartNew();
        var candidates = new List<Candidate>();
        foreach (var path in Directory.GetDirectories(directory).Concat(Directory.GetFiles(directory, "*.cs"))
                     .Concat(Directory.GetFiles(directory, "*.dll")).Order())
        {
            try
            {
                var yml = Path.Combine(path, "plugin.yml");
                if (Directory.Exists(path) && !File.Exists(yml))
                {
                    if (SourceFiles(path).Any() || Directory.GetFiles(path, "*.dll").Length > 0)
                        Fail(path, "the folder has no plugin.yml (see the example plugins).");
                    continue; // other folders (libraries, disabled plugins...) are ignored
                }
                candidates.Add(new Candidate(path, Directory.Exists(path) ? PluginDescription.Load(yml) : null));
            }
            catch (InvalidDataException e)
            {
                Fail(path, e.Message);
            }
        }
        if (candidates.Count == 0 && Failed.Count == 0)
            return;
        server.Logger.Info($"Loading {candidates.Count + Failed.Count} plugin(s) from {directory}/...");

        // Dependencies first: each pass loads the plugins whose dependencies are all loaded.
        var waiting = candidates;
        while (waiting.Count > 0)
        {
            var names = waiting.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var ready = waiting.Where(c => c.Depend.All(d => Get(d) is not null)
                                           && c.SoftDepend.All(d => Get(d) is not null || !names.Contains(d))).ToList();
            if (ready.Count == 0)
            {
                foreach (var c in waiting)
                {
                    var missing = c.Depend.Concat(c.SoftDepend).Where(d => Get(d) is null).ToList();
                    Fail(c.Path, $"missing or broken dependencies: {string.Join(", ", missing)}"
                                 + (missing.Any(names.Contains) ? " (or a dependency loop)" : "") + ".");
                }
                break;
            }
            foreach (var candidate in ready)
                Load(candidate, dataDirectory);
            waiting = [.. waiting.Except(ready)];
        }

        foreach (var (plugin, file, kind) in _loaded)
            server.Logger.Info($"  [OK]     {plugin.Name} v{plugin.Version}"
                               + (plugin.Info.Author.Length > 0 ? $" by {plugin.Info.Author}" : "")
                               + $" ({kind}: {Path.GetFileName(file)})" + (plugin.Description.Length > 0 ? $" - {plugin.Description}" : ""));
        foreach (var (file, _) in Failed)
            server.Logger.Error($"  [FAILED] {Path.GetFileName(file)} (see the error above)");
        var summary = $"Plugins: {_loaded.Count} loaded, {Failed.Count} failed ({timer.Elapsed.TotalSeconds:0.0} s).";
        if (Failed.Count > 0)
            server.Logger.Warn(summary);
        else
            server.Logger.Info(summary);
    }

    private void Load(Candidate candidate, string dataDirectory)
    {
        var (path, description) = candidate;
        var kind = description is not null ? "folder" : path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? "dll" : "source";
        try
        {
            if (Get(candidate.Name) is not null)
                throw new PluginLoadException($"another plugin is already named '{candidate.Name}'.");
            var folder = Directory.Exists(path) ? path : Path.GetDirectoryName(Path.GetFullPath(path))!;
            var libs = Directory.Exists(path) && Directory.Exists(Path.Combine(path, "libs"))
                ? Directory.GetFiles(Path.Combine(path, "libs"), "*.dll") : [];
            var context = new PluginLoadContext(candidate.Name, folder);
            var assembly = kind switch
            {
                "dll" => context.LoadFromAssemblyPath(Path.GetFullPath(path)),
                "source" => Compile(candidate.Name, [path], context, []),
                _ when Directory.Exists(Path.Combine(path, "src")) => Compile(candidate.Name, [.. SourceFiles(Path.Combine(path, "src"))], context, libs),
                _ when Directory.GetFiles(path, "*.dll") is [var dll] => context.LoadFromAssemblyPath(Path.GetFullPath(dll)),
                _ => throw new PluginLoadException("no code: put the .cs files in src/ (or a single DLL next to plugin.yml)."),
            };

            var type = description is not null
                ? assembly.GetType(description.Main) ?? throw new PluginLoadException($"main class '{description.Main}' (plugin.yml) was not found.")
                : assembly.GetExportedTypes().FirstOrDefault(t => t.IsSubclassOf(typeof(Plugin)) && !t.IsAbstract);
            if (type is null)
            {
                if (kind == "source")
                    throw new PluginLoadException("no public class deriving from Bedrokia.Server.Plugins.Plugin.");
                return; // a DLL without a plugin class is a dependency of another plugin
            }
            if (!type.IsSubclassOf(typeof(Plugin)) || type.IsAbstract)
                throw new PluginLoadException($"main class '{type.FullName}' must be a non-abstract class deriving from Bedrokia.Server.Plugins.Plugin.");

            var plugin = (Plugin)Activator.CreateInstance(type)!;
            plugin.Info = description ?? new PluginDescription
            {
                Name = candidate.Name,
                Main = type.FullName!,
                Version = assembly.GetName().Version is { } v && v > new Version(0, 0, 0, 0) ? v.ToString(3) : "1.0.0",
            };
            plugin.Server = server;
            plugin.DataFolder = Path.GetFullPath(Path.Combine(dataDirectory, plugin.Name));
            plugin.ResourceFolder = description is not null && Directory.Exists(Path.Combine(path, "resources"))
                ? Path.GetFullPath(Path.Combine(path, "resources")) : null;
            Directory.CreateDirectory(plugin.DataFolder);
            plugin.SaveResource("config.yml"); // default config; never overwrites the server owner's edits
            plugin.OnEnable();
            RegisterCommands(plugin);
            _loaded.Add(new LoadedPlugin(plugin, path, kind));
        }
        catch (Exception e)
        {
            Fail(path, e switch
            {
                TargetInvocationException { InnerException: { } inner } => inner.ToString(),
                PluginLoadException or InvalidDataException => e.Message,
                _ => e.ToString(),
            });
        }
    }

    // Commands declared in plugin.yml call Plugin.OnCommand.
    private void RegisterCommands(Plugin plugin)
    {
        foreach (var (name, info) in plugin.Info.Commands)
        {
            var perm = info.Permission.ToLowerInvariant() switch
            {
                "everyone" or "all" or "true" => "",
                "op" => "op",
                _ => info.Permission
            };
            void Run(CommandSender sender, string[] args)
            {
                try
                {
                    if (!plugin.OnCommand(sender, name, args))
                        sender.Reply($"§cUsage: /{name} {CommandManager.DisplayUsage(info.Usage)}");
                }
                catch (Exception e)
                {
                    server.Logger.Error($"Plugin '{plugin.Name}' crashed in command '{name}': {e}");
                    sender.Reply("§cAn internal error occurred while running this command.");
                }
            }
            server.Commands.Register(name, info.Description, info.Usage, perm, Run);
            foreach (var alias in info.Aliases)
                server.Commands.Register(alias, $"Alias of /{name}.", info.Usage, perm, Run);
        }
    }

    private sealed class PluginLoadException(string message) : Exception(message);

    private void Fail(string path, string error)
    {
        Failed.Add((path, error));
        server.Logger.Error($"Plugin '{Path.GetFileName(path)}' could not be loaded: {error}");
    }

    internal void DisableAll()
    {
        foreach (var (plugin, _, _) in Enumerable.Reverse(_loaded))
        {
            try
            {
                plugin.OnDisable();
            }
            catch (Exception e)
            {
                server.Logger.Error($"Plugin '{plugin.Name}' crashed while disabling: {e}");
            }
            server.Scheduler.CancelAll(plugin);
        }
        _loaded.Clear();
    }

    private static IEnumerable<string> SourceFiles(string directory) =>
        Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

    // --- Source plugins ---

    private const string GlobalUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        global using System.Threading.Tasks;
        global using Bedrokia.Server;
        global using Bedrokia.Server.Commands;
        global using Bedrokia.Server.Game;
        global using Bedrokia.Server.Network;
        global using Bedrokia.Server.Permissions;
        global using Bedrokia.Server.Plugins;
        global using Bedrokia.Server.Events;
        """;



    private static Assembly Compile(string name, string[] files, PluginLoadContext context, string[] libs)
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var trees = files.Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), options, f, Encoding.UTF8))
            .Append(CSharpSyntaxTree.ParseText(GlobalUsings, options)).ToList();
        var compilation = CSharpCompilation.Create(name, trees, References.Value.Concat(libs.Select(l => MetadataReference.CreateFromImage(File.ReadAllBytes(l)))),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release,
                nullableContextOptions: NullableContextOptions.Enable));
        using var image = new MemoryStream();
        var result = compilation.Emit(image);
        if (!result.Success)
        {
            var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            throw new PluginLoadException($"{errors.Count} compile error(s):{Environment.NewLine}    "
                                       + string.Join(Environment.NewLine + "    ", errors.Take(10)));
        }
        image.Position = 0;
        return context.LoadFromStream(image);
    }

    // The server's own assemblies, read from memory: they have no file in a single-file build.
    private static readonly Lazy<MetadataReference[]> References = new(() =>
    {
        foreach (var name in new[] { "System.Runtime", "System.Collections", "System.Linq", "System.Console", "System.Text.Json",
                     "System.Collections.Concurrent", "System.Text.RegularExpressions", "System.Net.Http", "System.Memory", "netstandard" })
        {
            try
            {
                Assembly.Load(name);
            }
            catch (IOException)
            {
                // not in this runtime: plugins simply cannot use it
            }
        }
        return [.. AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && AssemblyLoadContext.GetLoadContext(a) == AssemblyLoadContext.Default)
            .DistinctBy(a => a.GetName().Name)
            .Select(Reference).OfType<MetadataReference>()];
    });

    private static unsafe MetadataReference? Reference(Assembly assembly) =>
        System.Reflection.Metadata.AssemblyExtensions.TryGetRawMetadata(assembly, out var blob, out var length)
            ? AssemblyMetadata.Create(ModuleMetadata.CreateFromMetadata((IntPtr)blob, length)).GetReference()
            : null;

    private sealed class PluginLoadContext(string name, string directory) : AssemblyLoadContext(name)
    {
        protected override Assembly? Load(AssemblyName assembly)
        {
            // Shared with the server so that Plugin, BedrockConnection... are the same types.
            if (Default.Assemblies.Any(a => a.GetName().Name == assembly.Name))
                return null;
            var path = Path.Combine(directory, assembly.Name + ".dll");
            if (File.Exists(path))
                return LoadFromAssemblyPath(path);
            // Read into memory: the file stays free, so it can be replaced while the server runs.
            var lib = Path.Combine(directory, "libs", assembly.Name + ".dll");
            return File.Exists(lib) ? LoadFromStream(new MemoryStream(File.ReadAllBytes(lib))) : null;
        }
    }
}
