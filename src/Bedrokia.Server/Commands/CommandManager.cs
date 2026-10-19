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

    /// <summary>Parses and runs one input line. Errors are reported to the sender, never thrown.</summary>
    public void Dispatch(CommandSender sender, string line)
    {
        // Piped stdin (hosting panels, PowerShell) may start with a UTF-8 BOM.
        var parts = line.Trim().Trim((char)0xFEFF).TrimStart('/').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return;
        if (!_commands.TryGetValue(parts[0], out var command))
        {
            sender.Reply($"§cUnknown command '{parts[0]}'. Type help for the list of commands.");
            return;
        }
        if (!sender.HasPermission(command.Permission))
        {
            sender.Reply("§cYou do not have permission to use this command.");
            return;
        }
        if (sender.Player is not null)
            logger.Info($"{sender.Name} issued command: {line.Trim()}");
        try
        {
            command.Execute(sender, parts[1..]);
        }
        catch (CommandUsageException)
        {
            sender.Reply($"§cUsage: {command.Name} {command.Usage}");
        }
        catch (Exception e)
        {
            var owner = command.Execute.Method.DeclaringType?.Assembly.GetName().Name;
            logger.Error(owner is not null && !owner.StartsWith("Bedrokia.", StringComparison.Ordinal)
                ? $"Plugin '{owner}' crashed in command '{command.Name}': {e}"
                : $"Command '{command.Name}' failed: {e}");
            sender.Reply("§cAn internal error occurred while running this command.");
        }
    }

    /// <summary>
    /// AvailableCommands packet (gophertunnel packet/available_commands.go, protocol 2193) with the commands this
    /// sender may run, so the client lists and auto-completes them. Every parameter is sent as a plain string.
    /// </summary>
    public byte[] AvailableCommandsPacket(bool isOp) =>
        AvailableCommandsPacket(perm => isOp || string.IsNullOrWhiteSpace(perm) || perm.Equals("everyone", StringComparison.OrdinalIgnoreCase));

    /// <summary>Sends commands filtered by the sender's permissions.</summary>
    public byte[] AvailableCommandsPacket(CommandSender sender) =>
        AvailableCommandsPacket(sender.HasPermission);

    /// <summary>Sends commands filtered by a predicate.</summary>
    public byte[] AvailableCommandsPacket(Func<string?, bool> canRun)
    {
        const uint valid = 0x100000, typeString = 56, typeRawText = 70;
        var visible = All.Where(c => canRun(c.Permission)).ToList();
        var w = new BedrockWriter().VarUInt32(AvailableCommandsId)
            .VarUInt32(0).VarUInt32(0).VarUInt32(0).VarUInt32(0).VarUInt32(0) // enum values, chained values, suffixes, enums, chained
            .VarUInt32((uint)visible.Count);
        foreach (var command in visible)
        {
            var parameters = command.Usage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            w.String(command.Name).String(command.Description).UInt16(0).String("any")
                .UInt32(uint.MaxValue) // no aliases
                .VarUInt32(0) // chained subcommand offsets
                .VarUInt32(1).Bool(false).VarUInt32((uint)parameters.Length);
            foreach (var parameter in parameters)
            {
                var name = parameter.Trim('<', '>', '[', ']');
                var rest = name.EndsWith("...");
                w.String(rest ? name[..^3] : name).UInt32(valid | (rest ? typeRawText : typeString))
                    .Bool(parameter.StartsWith('[')).Byte(0);
            }
        }
        return w.VarUInt32(0).VarUInt32(0).ToArray(); // dynamic enums, constraints
    }
}

/// <summary>Thrown by a command whose arguments are wrong; the manager replies with the usage.</summary>
public sealed class CommandUsageException : Exception;
