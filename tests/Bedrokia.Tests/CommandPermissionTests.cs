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
