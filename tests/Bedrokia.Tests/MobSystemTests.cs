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

using System.Net;
using System.Security.Cryptography;
using Bedrokia.Server;
using Bedrokia.Server.Core;
using Bedrokia.Server.Entities;
using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Tests;

/// <summary>The mob system driven tick by tick on a test world: a stone floor high in the sky of chunk (0, 0).</summary>
public class MobSystemTests
{
    private const int Floor = 150;

    private sealed class FakeTransport : IGameTransport
    {
        public IPEndPoint RemoteEndPoint { get; } = new(IPAddress.Loopback, 19132);
        public ECDsa? PeerPublicKey => null;
        public void Send(byte[] batch) { }
        public void Disconnect(string reason) { }
    }

    private static (BedrokiaServer Server, BedrockConnection Player) Arena()
    {
        var server = new BedrokiaServer(new ServerConfig { WorldsDirectory = TestWorlds.NewDirectory(), Port = 0, OnlineMode = false },
            new Logger(LogLevel.Error, null));
        server.InitWorldForTesting();
        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
        {
            server.World.SetBlock(x, Floor - 1, z, Blocks.Stone);
            for (var y = Floor; y < Floor + 4; y++)
                server.World.SetBlock(x, y, z, Blocks.Air);
        }
        var player = new BedrockConnection(server, new FakeTransport(), 100)
        {
            Position = (8f, Floor + BedrockConnection.EyeHeight, 14f),
            IsInGame = true,
        };
        server.AddPlayer(player);
        return (server, player);
    }

    private static void Run(BedrokiaServer server, int ticks, long from = 1)
    {
        for (var t = from; t < from + ticks; t++)
            server.Mobs.Tick(t);
    }

    [Fact]
    public void Paths_go_around_a_wall()
    {
        var (server, _) = Arena();
        for (var z = 0; z <= 12; z++)
        for (var y = Floor; y < Floor + 3; y++)
            server.World.SetBlock(8, y, z, Blocks.Stone);
        var zombie = server.Mobs.Spawn(MobKind.Zombie, 3.5, Floor, 3.5)!;

        var path = server.Mobs.FindPath(zombie, (12, Floor, 3));

        Assert.NotNull(path);
        Assert.DoesNotContain(path, n => n.X == 8 && n.Z <= 12); // no walking through the wall
        Assert.Contains(path, n => n.Z >= 13); // around its end
        var last = path[^1];
        Assert.True(Math.Abs(last.X - 12) <= 1 && Math.Abs(last.Z - 3) <= 1, $"path ends at {last}");
    }

    [Fact]
    public void Fed_animals_breed_a_baby_that_grows_up()
    {
        var (server, player) = Arena();
        var a = server.Mobs.Spawn(MobKind.Cow, 6.5, Floor, 8.5)!;
        var b = server.Mobs.Spawn(MobKind.Cow, 9.5, Floor, 8.5)!;
        player.Inventory.Main[player.Inventory.SelectedSlot] = ItemStack.Of("wheat", 2);

        Assert.True(server.Mobs.Interact(player, a));
        Assert.True(server.Mobs.Interact(player, b));
        Assert.Null(player.Inventory.Held); // both wheat eaten (survival)

        Run(server, 200);

        var baby = Assert.Single(server.Mobs.All, m => m.Baby);
        Assert.Equal(MobKind.Cow, baby.Kind);
        Assert.False(server.Mobs.Interact(player, a)); // nothing in hand any more
        Assert.Equal(0, a.LoveTicks);

        Run(server, 24000, from: 201);
        Assert.False(baby.Baby);
    }

    [Fact]
    public void Shears_and_buckets_work_on_sheep_and_cows()
    {
        var (server, player) = Arena();
        var sheep = server.Mobs.Spawn(MobKind.Sheep, 8.5, Floor, 12.5)!;
        var cow = server.Mobs.Spawn(MobKind.Cow, 7.5, Floor, 12.5)!;

        player.Inventory.Main[player.Inventory.SelectedSlot] = ItemStack.Of("shears");
        Assert.True(server.Mobs.Interact(player, sheep));
        Assert.True(sheep.Sheared);
        Assert.False(server.Mobs.Interact(player, sheep)); // already sheared
        Assert.NotEmpty(server.World.DroppedItems.All);

        player.Inventory.Main[player.Inventory.SelectedSlot] = ItemStack.Of("bucket");
        Assert.True(server.Mobs.Interact(player, cow));
        Assert.Contains(Enumerable.Range(0, player.Inventory.Main.Size), i => player.Inventory.Main[i]?.Type.Name == "minecraft:milk_bucket");
    }

    [Fact]
    public void Mobs_off_removes_them_and_saved_animals_come_back()
    {
        var (server, _) = Arena();
        var cow = server.Mobs.Spawn(MobKind.Cow, 8.5, Floor, 8.5)!;
        server.Mobs.Spawn(MobKind.Zombie, 4.5, Floor, 4.5);

        server.Config.Mobs = false;
        Assert.Null(server.Mobs.Spawn(MobKind.Pig, 5.5, Floor, 5.5));
        Run(server, 1);
        Assert.Empty(server.Mobs.All);
        server.Mobs.Save(server.World);
        Assert.Contains("minecraft:cow", File.ReadAllText(Path.Combine(server.World.Folder, "entities.json")));

        server.Config.Mobs = true;
        Run(server, 1, from: 20); // dormant mobs wake every 20 ticks
        var back = Assert.Single(server.Mobs.All); // the cow; the zombie was not kept
        Assert.Equal(MobKind.Cow, back.Kind);
        Assert.InRange(back.X, cow.X - 1, cow.X + 1); // where it was (it may have taken a step)
    }

    [Fact]
    public void A_long_fall_hurts()
    {
        var (server, _) = Arena();
        var cow = server.Mobs.Spawn(MobKind.Cow, 8.5, Floor + 12, 8.5)!;
        Run(server, 100);
        Assert.True(cow.OnGround);
        Assert.InRange(cow.Health, 0.5f, cow.Kind.MaxHealth - 5); // about 8 blocks of damage beyond the 3 safe ones
    }

    [Fact]
    public void Monsters_need_the_dark()
    {
        var (server, _) = Arena();
        var nether = false;
        // Midday under the open sky: no monster. Under a roof, far from any light: yes. Next to a torch: no.
        server.World.Time = 6000;
        Assert.False(Dark(server.World, 8, Floor, 8, night: false, nether));
        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
            server.World.SetBlock(x, Floor + 4, z, Blocks.Stone);
        Assert.True(Dark(server.World, 8, Floor, 8, night: false, nether));
        server.World.SetBlock(10, Floor, 8, Blocks.ByName("torch")!);
        Assert.False(Dark(server.World, 8, Floor, 8, night: false, nether));
    }

    private static bool Dark(World world, int x, int y, int z, bool night, bool nether) =>
        (bool)typeof(MobManager).GetMethod("Dark", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, [world, x, y, z, night, nether])!;
}
