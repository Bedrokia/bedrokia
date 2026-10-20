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
    public void A_provider_grants_permissions_and_is_asked_every_time()
    {
        var manager = new PermissionManager(_filePath);
        var rank = "Joueur";
        manager.AddProvider((player, permission) => player == "steve" && permission == "setrank.cmd" && rank == "Fondateur");

        Assert.False(manager.HasPermission("steve", "setrank.cmd"));
        rank = "Fondateur"; // a rank change applies at once (no cache)
        Assert.True(manager.HasPermission("steve", "setrank.cmd"));
        Assert.False(manager.HasPermission("alex", "setrank.cmd"));
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
    public void Players_with_similar_names_do_not_share_permissions()
    {
        var manager = new PermissionManager(_filePath);
        manager.AddPlayerPermission("Jean Pierre", "admin.node");
        manager.SetPlayerGroup("Élodie", "admin");
        Assert.True(manager.HasPermission("jean pierre", "admin.node")); // same player, other case
        Assert.False(manager.HasPermission("JeanPierre", "admin.node"));
        Assert.False(manager.HasPermission("../Jean Pierre", "admin.node"));
        Assert.Equal("admin", manager.GetPlayerGroup("élodie"));
        Assert.Equal(PermissionManager.DefaultGroupName, manager.GetPlayerGroup("lodie"));
    }

    [Fact]
    public void A_broken_permissions_file_is_kept_aside_instead_of_being_lost()
    {
        File.WriteAllText(_filePath, "{ \"Groups\": { \"admin\": "); // cut in the middle of an edit
        var manager = new PermissionManager(_filePath);
        Assert.NotNull(manager.LoadError);
        var backup = Assert.Single(Directory.GetFiles(_tempDir, "permissions.json.broken-*"));
        Assert.Equal("{ \"Groups\": { \"admin\": ", File.ReadAllText(backup));
        Assert.True(manager.HasPermission("Steve", "bedrokia.command.help")); // the defaults still work
    }
}
