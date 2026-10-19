using Bedrokia.Server;
using Bedrokia.Server.Core;
using Xunit;

namespace Bedrokia.Tests;

public class PermissionCommandTests
{
    private static BedrokiaServer CreateTestServer()
    {
        var tempWorlds = TestWorlds.NewDirectory();
        var config = new ServerConfig { WorldsDirectory = tempWorlds, Port = 0, MaxPlayers = 5 };
        var logger = new Logger(LogLevel.Warn, null);
        var permPath = Path.Combine(tempWorlds, "permissions.json");
        var permissions = new Bedrokia.Server.Permissions.PermissionManager(permPath);
        return new BedrokiaServer(config, logger, permissions);
    }

    [Fact]
    public void Perm_command_is_registered_and_restricted()
    {
        var server = CreateTestServer();
        var cmd = server.Commands.All.FirstOrDefault(c => c.Name.Equals("perm", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(cmd);
        Assert.Equal("bedrokia.command.permission", cmd.Permission);
    }

    [Fact]
    public void Perm_user_setgroup_and_info_commands_work()
    {
        var server = CreateTestServer();
        var replies = new List<string>();
        var console = new Bedrokia.Server.Commands.CommandSender("Server", isOp: true, replies.Add);

        server.Commands.Dispatch(console, "perm group vip add bedrokia.command.fly");
        server.Commands.Dispatch(console, "perm group vip setprefix §a[VIP] ");
        server.Commands.Dispatch(console, "perm user steve setgroup vip");

        Assert.Equal("vip", server.Permissions.GetPlayerGroup("steve"));
        Assert.True(server.Permissions.HasPermission("steve", "bedrokia.command.fly"));
        Assert.Equal("§a[VIP] ", server.Permissions.GetPlayerPrefix("steve"));

        server.Commands.Dispatch(console, "perm user steve add -bedrokia.command.fly");
        Assert.False(server.Permissions.HasPermission("steve", "bedrokia.command.fly"));
    }

    [Fact]
    public void Builtin_commands_have_proper_permissions()
    {
        var server = CreateTestServer();
        var tpCmd = server.Commands.All.FirstOrDefault(c => c.Name.Equals("tp", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(tpCmd);
        Assert.Equal("bedrokia.command.tp", tpCmd.Permission);

        var spawnCmd = server.Commands.All.FirstOrDefault(c => c.Name.Equals("spawn", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(spawnCmd);
        Assert.Equal("bedrokia.command.spawn", spawnCmd.Permission);
    }
}
