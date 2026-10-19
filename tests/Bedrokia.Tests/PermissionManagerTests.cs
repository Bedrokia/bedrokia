using Bedrokia.Server.Permissions;
using Xunit;

namespace Bedrokia.Tests;

public class PermissionManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _filePath;

    public PermissionManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "bedrokia_perm_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _filePath = Path.Combine(_tempDir, "permissions.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Default_group_permissions_apply_to_new_players()
    {
        var manager = new PermissionManager(_filePath);
        manager.CreateGroup("default", prefix: "", priority: 0);
        manager.AddGroupPermission("default", "bedrokia.command.spawn");

        Assert.True(manager.HasPermission("steve", "bedrokia.command.spawn"));
        Assert.False(manager.HasPermission("steve", "bedrokia.command.tp"));
    }

    [Fact]
    public void Group_inheritance_resolves_correctly()
    {
        var manager = new PermissionManager(_filePath);
        manager.CreateGroup("default", prefix: "§7[Member] ", priority: 0);
        manager.AddGroupPermission("default", "bedrokia.command.spawn");

        manager.CreateGroup("vip", prefix: "§a[VIP] ", priority: 10, parent: "default");
        manager.AddGroupPermission("vip", "bedrokia.command.fly");

        manager.SetPlayerGroup("alice", "vip");

        Assert.True(manager.HasPermission("alice", "bedrokia.command.spawn"));
        Assert.True(manager.HasPermission("alice", "bedrokia.command.fly"));
        Assert.Equal("§a[VIP] ", manager.GetPlayerPrefix("alice"));
    }

    [Fact]
    public void Inheritance_cycle_does_not_infinite_loop()
    {
        var manager = new PermissionManager(_filePath);
        manager.CreateGroup("group_a", prefix: "", priority: 1, parent: "group_b");
        manager.AddGroupPermission("group_a", "perm.a");

        manager.CreateGroup("group_b", prefix: "", priority: 2, parent: "group_a");
        manager.AddGroupPermission("group_b", "perm.b");

        manager.SetPlayerGroup("bob", "group_a");

        // Should resolve without StackOverflowException
        Assert.True(manager.HasPermission("bob", "perm.a"));
        Assert.True(manager.HasPermission("bob", "perm.b"));
        Assert.False(manager.HasPermission("bob", "perm.c"));
    }

    [Fact]
    public void Direct_player_permission_overrides_group()
    {
        var manager = new PermissionManager(_filePath);
        manager.CreateGroup("vip", prefix: "§a[VIP] ", priority: 10);
        manager.AddGroupPermission("vip", "bedrokia.command.fly");

        manager.SetPlayerGroup("charlie", "vip");
        Assert.True(manager.HasPermission("charlie", "bedrokia.command.fly"));

        manager.AddPlayerPermission("charlie", "-bedrokia.command.fly");
        Assert.False(manager.HasPermission("charlie", "bedrokia.command.fly"));
    }

    [Fact]
    public void Persistence_saves_and_reloads_state()
    {
        {
            var manager = new PermissionManager(_filePath);
            manager.CreateGroup("mod", prefix: "§9[Mod] ", priority: 50);
            manager.AddGroupPermission("mod", "bedrokia.command.kick");
            manager.SetPlayerGroup("david", "mod");
            manager.AddPlayerPermission("david", "custom.special");
            manager.Save();
        }

        Assert.True(File.Exists(_filePath));

        {
            var manager2 = new PermissionManager(_filePath);
            Assert.True(manager2.HasPermission("david", "bedrokia.command.kick"));
            Assert.True(manager2.HasPermission("david", "custom.special"));
            Assert.Equal("§9[Mod] ", manager2.GetPlayerPrefix("david"));
        }
    }

    [Fact]
    public void Sanitizes_player_and_group_names()
    {
        var manager = new PermissionManager(_filePath);
        // Player names with path traversal or strange characters are normalized
        manager.AddPlayerPermission("../bad_user", "test.node");
        Assert.True(manager.HasPermission("bad_user", "test.node"));
        Assert.True(manager.HasPermission("BAD_USER", "test.node"));
    }
}
