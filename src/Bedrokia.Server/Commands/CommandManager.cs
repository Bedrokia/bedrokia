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

using Bedrokia.Protocol;
using Bedrokia.Server.Core;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Commands;

/// <summary>Who runs a command: the console (always operator) or an in-game player.</summary>
public sealed class CommandSender(string name, bool isOp, Action<string> reply, BedrockConnection? player = null, Func<string?, bool>? hasPermission = null)
{
    public string Name => name;
    public bool IsOp => isOp;

    /// <summary>The player running the command; null for the console.</summary>
    public BedrockConnection? Player => player;

    public void Reply(string message) => reply(message);

    /// <summary>Checks whether this sender has the requested granular permission node.</summary>
    public bool HasPermission(string? permission)
    {
        if (string.IsNullOrWhiteSpace(permission) ||
            permission.Equals("everyone", StringComparison.OrdinalIgnoreCase) ||
            permission.Equals("all", StringComparison.OrdinalIgnoreCase))
            return true;

        if (hasPermission is not null)
            return hasPermission(permission);

        if (player is not null)
            return player.HasPermission(permission);

        if (permission.Equals("op", StringComparison.OrdinalIgnoreCase))
            return isOp;

        return isOp;
    }
}

/// <param name="Usage">Parameters, e.g. "&lt;player&gt; &lt;item&gt; [count]"; a name ending in "..." takes the rest of the line.</param>
/// <param name="Permission">Permission node required (empty or "everyone" for all, "op" for operators, or custom node).</param>
/// <param name="Execute">Receives the sender and the arguments after the command name.</param>
public sealed record Command(string Name, string Description, string Usage, string? Permission, Action<CommandSender, string[]> Execute)
{
    /// <summary>True if this command is restricted to operators or specific permissions.</summary>
    public bool OpOnly => Permission is not null && (Permission.Equals("op", StringComparison.OrdinalIgnoreCase) ||
        (Permission.Length > 0 && !Permission.Equals("everyone", StringComparison.OrdinalIgnoreCase) && !Permission.Equals("all", StringComparison.OrdinalIgnoreCase)));
}

/// <summary>Central command registry. Commands run on the game thread.</summary>
public sealed class CommandManager(Logger logger)
{
    public const uint AvailableCommandsId = 76, CommandRequestId = 77, SetCommandsEnabledId = 59;

    private readonly SortedDictionary<string, Command> _commands = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<Command> All => _commands.Values;

    /// <exception cref="InvalidOperationException">A command with this name already exists.</exception>
    public void Register(string name, string description, string usage, bool opOnly, Action<CommandSender, string[]> execute) =>
        Register(name, description, usage, opOnly ? "op" : "", execute);

    /// <summary>Registers a command requiring a specific permission node.</summary>
    public void Register(string name, string description, string usage, string? permission, Action<CommandSender, string[]> execute)
    {
        if (!_commands.TryAdd(name, new Command(name, description, usage, permission ?? "", execute)))
            throw new InvalidOperationException($"Command '{name}' is already registered.");
    }

    /// <summary>Removes a command (e.g. a built-in one a plugin replaces); false if there is none.</summary>
    public bool Unregister(string name) => _commands.Remove(name);

    /// <summary>Operator-only command that ignores who runs it (console style).</summary>
    public void Register(string name, string description, Action<string[]> execute) =>
        Register(name, description, "[args...]", "op", (_, args) => execute(args));

    /// <summary>Console sender: operator, replies go to the log.</summary>
    public CommandSender Console => new("Server", true, message => logger.Info(System.Text.RegularExpressions.Regex.Replace(message, "§.", "")));

    /// <summary>Runs one console line.</summary>
    public void Dispatch(string line) => Dispatch(Console, line);

    /// <summary>
    /// Turns a target selector into player names for <paramref name="sender"/>: @s (the sender), @p (the nearest player),
    /// @r (a random one), @a and @e (everyone). Null when the word is not a selector. Set by the server.
    /// </summary>
    public Func<CommandSender, string, IReadOnlyList<string>?>? SelectorResolver { get; set; }

    // Player arguments ("<x:player>") holding a selector are replaced by the names it matches; with several names the
    // command runs once per name, like vanilla commands do for each target. Other arguments (a message) are left as is.
    private List<string[]>? ExpandSelectors(CommandSender sender, string usage, string[] args)
    {
        var runs = new List<string[]> { args };
        if (SelectorResolver is null || !args.Any(a => a.StartsWith('@')))
            return runs;
        var playerPositions = ParseUsage(usage).SelectMany(o => o.Select((p, i) => (p, i))).Where(x => x.p.Type == 8).Select(x => x.i).ToHashSet();
        foreach (var position in playerPositions.Where(i => i < args.Length).Order())
        {
            if (SelectorResolver(sender, args[position]) is not { } names)
                continue;
            if (names.Count == 0)
            {
                sender.Reply($"§cNo player matches {args[position]}.");
                return null;
            }
            runs = [.. runs.SelectMany(run => names.Select(name =>
            {
                var copy = (string[])run.Clone();
                copy[position] = name;
                return copy;
            }))];
        }
        return runs;
    }

    /// <summary>Parses and runs one input line. Errors are reported to the sender, never thrown.</summary>
    public void Dispatch(CommandSender sender, string line)
    {
        if (sender.Player is { } player)
        {
            if (player.Server.Events.HasPlayerCommand)
            {
                var cmdEv = new PlayerCommandEvent(player, line);
                if (!player.Server.Events.OnPlayerCommand(cmdEv))
                    return;
                line = cmdEv.CommandLine;
            }
        }

        // Piped stdin (hosting panels, PowerShell) may start with a UTF-8 BOM.
        var span = line.AsSpan().Trim().Trim((char)0xFEFF).TrimStart('/');
        if (span.IsEmpty)
            return;

        var firstSpace = span.IndexOf(' ');
        string cmdName;
        ReadOnlySpan<char> argsSpan;
        if (firstSpace < 0)
        {
            cmdName = span.ToString();
            argsSpan = ReadOnlySpan<char>.Empty;
        }
        else
        {
            cmdName = span[..firstSpace].ToString();
            argsSpan = span[(firstSpace + 1)..].TrimStart();
        }

        if (!_commands.TryGetValue(cmdName, out var command))
        {
            sender.Reply($"§cUnknown command '{cmdName}'. Type help for the list of commands.");
            return;
        }
        if (!sender.HasPermission(command.Permission))
        {
            sender.Reply("§cYou do not have permission to use this command.");
            return;
        }
        if (sender.Player is not null)
            logger.Info($"{sender.Name} issued command: {line.Trim()}");

        string[] args;
        if (argsSpan.IsEmpty)
        {
            args = [];
        }
        else
        {
            var argList = new List<string>(4);
            var remaining = argsSpan;
            while (!remaining.IsEmpty)
            {
                remaining = remaining.TrimStart();
                if (remaining.IsEmpty)
                    break;
                var nextSpace = remaining.IndexOf(' ');
                if (nextSpace < 0)
                {
                    argList.Add(remaining.ToString());
                    break;
                }
                argList.Add(remaining[..nextSpace].ToString());
                remaining = remaining[(nextSpace + 1)..];
            }
            args = [.. argList];
        }

        if (ExpandSelectors(sender, command.Usage, args) is not { } runs)
            return;
        foreach (var run in runs)
        {
            try
            {
                command.Execute(sender, run);
            }
            catch (CommandUsageException)
            {
                sender.Reply($"§cUsage: {command.Name} {DisplayUsage(command.Usage)}");
                return;
            }
            catch (Exception e)
            {
                var owner = command.Execute.Method.DeclaringType?.Assembly.GetName().Name;
                logger.Error(owner is not null && !owner.StartsWith("Bedrokia.", StringComparison.Ordinal)
                    ? $"Plugin '{owner}' crashed in command '{command.Name}': {e}"
                    : $"Command '{command.Name}' failed: {e}");
                sender.Reply("§cAn internal error occurred while running this command.");
                return;
            }
        }
    }

    /// <summary>
    /// AvailableCommands packet (gophertunnel packet/available_commands.go and protocol/command.go, protocol 2193)
    /// with the commands this sender may run, so the client lists them and Tab proposes the arguments: online
    /// players, numbers, fixed choices... The parameter types come from the usage, see <see cref="ParseUsage"/>.
    /// </summary>
    public byte[] AvailableCommandsPacket(bool isOp) =>
        AvailableCommandsPacket(perm => isOp || string.IsNullOrWhiteSpace(perm) || perm.Equals("everyone", StringComparison.OrdinalIgnoreCase));

    /// <summary>Sends commands filtered by the sender's permissions.</summary>
    public byte[] AvailableCommandsPacket(CommandSender sender) =>
        AvailableCommandsPacket(sender.HasPermission);

    /// <summary>Sends commands filtered by a predicate.</summary>
    public byte[] AvailableCommandsPacket(Func<string?, bool> canRun)
    {
        const uint valid = 0x100000, enumFlag = 0x200000;
        var visible = All.Where(c => canRun(c.Permission)).ToList();
        var overloads = visible.Select(c => ParseUsage(c.Usage)).ToList();

        // Every fixed choice (subcommands, modes...) becomes an enum; identical lists share one.
        var values = new List<string>();
        var valueIndex = new Dictionary<string, int>();
        var enums = new List<(string Name, List<int> Values)>();
        var enumIndex = new Dictionary<string, int>();
        int EnumOf(string name, IReadOnlyList<string> options)
        {
            var key = string.Join('|', options);
            if (enumIndex.TryGetValue(key, out var existing))
                return existing;
            var indices = new List<int>();
            foreach (var option in options)
            {
                if (!valueIndex.TryGetValue(option, out var v))
                {
                    valueIndex[option] = v = values.Count;
                    values.Add(option);
                }
                indices.Add(v);
            }
            // The client crashes when it shows a command whose enum shares its name with another one ("action" of /time
            // and of /whitelist): every name is made unique.
            var enumName = options.Count == 1 ? options[0] : name;
            for (var n = 2; enums.Any(e => e.Name == enumName); n++)
                enumName = (options.Count == 1 ? options[0] : name) + n;
            enums.Add((enumName, indices));
            return enumIndex[key] = enums.Count - 1;
        }
        var types = overloads.Select(command => command.Select(overload => overload.Select(p =>
            p.Options is { } options ? valid | enumFlag | (uint)EnumOf(p.Name, options) : valid | p.Type).ToList()).ToList()).ToList();

        var w = new BedrockWriter().VarUInt32(AvailableCommandsId).VarUInt32((uint)values.Count);
        foreach (var value in values)
            w.String(value);
        w.VarUInt32(0).VarUInt32(0).VarUInt32((uint)enums.Count); // chained values, suffixes, enums
        foreach (var (name, indices) in enums)
        {
            w.String(name).VarUInt32((uint)indices.Count);
            foreach (var index in indices)
                w.UInt32((uint)index);
        }
        w.VarUInt32(0).VarUInt32((uint)visible.Count); // chained subcommands, commands
        for (var c = 0; c < visible.Count; c++)
        {
            w.String(visible[c].Name).String(visible[c].Description).UInt16(0).String("any")
                .UInt32(uint.MaxValue) // aliases are registered as commands of their own
                .VarUInt32(0) // chained subcommand offsets
                .VarUInt32((uint)overloads[c].Count);
            for (var o = 0; o < overloads[c].Count; o++)
            {
                w.Bool(false).VarUInt32((uint)overloads[c][o].Count);
                for (var p = 0; p < overloads[c][o].Count; p++)
                {
                    var parameter = overloads[c][o][p];
                    w.String(parameter.Name).UInt32(types[c][o][p]).Bool(parameter.Optional).Byte(0);
                }
            }
        }
        return w.VarUInt32(0).VarUInt32(0).ToArray(); // dynamic enums, constraints
    }

    /// <summary>A command parameter for the client: name, protocol type, optional, or a fixed list of choices.</summary>
    public sealed record UsageParameter(string Name, uint Type, bool Optional, IReadOnlyList<string>? Options);

    /// <summary>
    /// Reads a usage into overloads. Overloads are separated by "||"; a parameter is &lt;name&gt; (required) or
    /// [name] (optional), with an optional type after a colon: player (online players on Tab), int, float, bool,
    /// text (the rest of the line), pos (x y z), or choices "a|b|c". A bare word is a fixed subcommand.
    /// "&lt;message...&gt;" is the rest of the line too. Without a type the parameter is a string.
    /// Example: "add &lt;joueur:player&gt; &lt;montant:float&gt; || mode &lt;mode:survival|creative&gt;".
    /// </summary>
    public static List<List<UsageParameter>> ParseUsage(string usage)
    {
        const uint typeInt = 1, typeFloat = 3, typeTarget = 8, typeString = 56, typePosition = 65, typeRawText = 70;
        var result = new List<List<UsageParameter>>();
        foreach (var overload in usage.Split("||"))
        {
            var parameters = new List<UsageParameter>();
            foreach (var token in overload.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var optional = token.StartsWith('[');
                var inner = token.Trim('<', '>', '[', ']');
                if (!optional && !token.StartsWith('<'))
                {
                    parameters.Add(new UsageParameter(inner, typeString, false, [inner])); // subcommand word
                    continue;
                }
                var colon = inner.IndexOf(':');
                var name = colon < 0 ? inner : inner[..colon];
                var type = colon < 0 ? "" : inner[(colon + 1)..];
                if (name.EndsWith("..."))
                    (name, type) = (name[..^3], "text");
                else if (colon < 0 && name.Contains('|'))
                    (name, type) = ("value", name); // "<on|off>": choices without a name
                IReadOnlyList<string>? options = type.Contains('|') ? type.Split('|', StringSplitOptions.RemoveEmptyEntries)
                    : type == "bool" ? ["true", "false"] : null;
                parameters.Add(new UsageParameter(name, type switch
                {
                    "player" or "target" => typeTarget,
                    "int" => typeInt,
                    "float" or "number" => typeFloat,
                    "text" or "message" => typeRawText,
                    "pos" or "position" => typePosition,
                    _ => typeString,
                }, optional, options));
            }
            result.Add(parameters);
        }
        return result;
    }

    /// <summary>The usage as players read it: types removed, overloads one after another.</summary>
    public static string DisplayUsage(string usage) =>
        System.Text.RegularExpressions.Regex.Replace(usage, @"([<\[])([^:<>\[\]]+):[^<>\[\]]*([>\]])", "$1$2$3").Replace(" || ", " | ");
}

/// <summary>Thrown by a command whose arguments are wrong; the manager replies with the usage.</summary>
public sealed class CommandUsageException : Exception;
