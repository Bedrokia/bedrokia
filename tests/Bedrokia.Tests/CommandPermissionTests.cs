using Bedrokia.Server.Commands;
using Bedrokia.Server.Core;
using Bedrokia.Server.Permissions;
using Xunit;

namespace Bedrokia.Tests;

public class CommandPermissionTests
{
    [Fact]
    public void CommandSender_checks_permission_via_haspermission_delegate()
    {
        var grantedPerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "test.cmd" };
        var sender = new CommandSender("player1", isOp: false, _ => { }, player: null, hasPermission: perm => perm is not null && grantedPerms.Contains(perm));

        Assert.True(sender.HasPermission("test.cmd"));
        Assert.False(sender.HasPermission("admin.cmd"));
    }

    [Fact]
    public void Console_sender_has_all_permissions()
    {
        using var logger = new Logger(LogLevel.Warn, null);
        var manager = new CommandManager(logger);
        Assert.True(manager.Console.HasPermission("any.random.perm"));
        Assert.True(manager.Console.IsOp);
    }

    [Fact]
    public void Dispatch_rejects_command_when_sender_lacks_granular_permission()
    {
        using var logger = new Logger(LogLevel.Warn, null);
        var manager = new CommandManager(logger);
        var executed = false;
        string? replyMsg = null;

        manager.Register("warp", "Teleport to warp", "<name>", "bedrokia.warp", (sender, args) => executed = true);

        var senderWithoutPerm = new CommandSender("bob", isOp: false, msg => replyMsg = msg, player: null, hasPermission: _ => false);
        manager.Dispatch(senderWithoutPerm, "warp spawn");

        Assert.False(executed);
        Assert.Equal("§cYou do not have permission to use this command.", replyMsg);

        var senderWithPerm = new CommandSender("alice", isOp: false, msg => replyMsg = msg, player: null, hasPermission: perm => perm == "bedrokia.warp");
        manager.Dispatch(senderWithPerm, "warp spawn");

        Assert.True(executed);
    }

    [Fact]
    public void Available_commands_packet_filters_by_sender_permissions()
    {
        using var logger = new Logger(LogLevel.Warn, null);
        var manager = new CommandManager(logger);

        manager.Register("regular", "Regular command", "", false, (_, _) => { });
        manager.Register("moderator", "Mod command", "", "bedrokia.mod", (_, _) => { });
        manager.Register("admin", "Admin command", "", "bedrokia.admin", (_, _) => { });

        var modSender = new CommandSender("modUser", isOp: false, _ => { }, player: null, hasPermission: perm => perm is "" or "bedrokia.mod");

        var packet = manager.AvailableCommandsPacket(modSender);
        Assert.NotNull(packet);
        Assert.True(packet.Length > 0);
    }
}

public class CommandUsageTests
{
    [Fact]
    public void Usages_become_typed_overloads_for_tab_completion()
    {
        var overloads = CommandManager.ParseUsage("add <joueur:player> <montant:float> [raison...] || mode <mode:survival|creative>");
        Assert.Equal(2, overloads.Count);
        Assert.Equal(["add"], overloads[0][0].Options!); // subcommand word
        Assert.Equal(8u, overloads[0][1].Type); // target: online players on Tab
        Assert.Equal(3u, overloads[0][2].Type);
        Assert.True(overloads[0][3].Optional);
        Assert.Equal(70u, overloads[0][3].Type); // rest of the line
        Assert.Equal(["survival", "creative"], overloads[1][1].Options!);
        Assert.Equal("add <joueur> <montant> [raison...] | mode <mode>",
            CommandManager.DisplayUsage("add <joueur:player> <montant:float> [raison...] || mode <mode:survival|creative>"));

        using var logger = new Logger(LogLevel.Warn, null);
        var manager = new CommandManager(logger);
        manager.Register("gm", "", "<mode:survival|creative> [player:player]", false, (_, _) => { });
        var packet = System.Text.Encoding.UTF8.GetString(manager.AvailableCommandsPacket(true));
        Assert.Contains("survival", packet); // the choices travel as enum values
    }

    [Fact]
    public void Selectors_in_player_arguments_run_the_command_for_each_match()
    {
        using var logger = new Logger(LogLevel.Warn, null);
        var manager = new CommandManager(logger)
        {
            SelectorResolver = (_, word) => word switch { "@a" => ["Alice", "Bob"], "@s" => ["Alice"], "@r" => [], _ => null },
        };
        var runs = new List<string>();
        manager.Register("hit", "", "<target:player> <message...>", false, (_, args) => runs.Add(string.Join(' ', args)));
        var replies = new List<string>();
        var sender = new CommandSender("Alice", true, replies.Add);

        manager.Dispatch(sender, "hit @a hello @a");
        Assert.Equal(["Alice hello @a", "Bob hello @a"], runs); // a message keeps its @a
        runs.Clear();
        manager.Dispatch(sender, "hit @s hi");
        Assert.Equal(["Alice hi"], runs);
        runs.Clear();
        manager.Dispatch(sender, "hit @r hi");
        Assert.Empty(runs);
        Assert.Contains(replies, r => r.Contains("No player matches @r"));
    }

    [Fact]
    public void Enums_with_the_same_name_get_distinct_names()
    {
        using var logger = new Logger(LogLevel.Warn, null);
        var manager = new CommandManager(logger);
        manager.Register("time", "", "<action:set|add>", false, (_, _) => { });
        manager.Register("whitelist", "", "<action:on|off>", false, (_, _) => { });
        var packet = System.Text.Encoding.UTF8.GetString(manager.AvailableCommandsPacket(true));
        // Two enums both named "action" crashed the client when it showed the second command.
        Assert.Contains("action2", packet);
    }
}
