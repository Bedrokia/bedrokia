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

    [Fact]
    public void Skeletons_shoot_real_arrows()
    {
        var (server, player) = Arena();
        server.Config.Difficulty = Difficulty.Hard; // little spread: this arrow cannot miss
        var skeleton = server.Mobs.Spawn(MobKind.Skeleton, 8.5, Floor, 4.5)!;
        Run(server, 3);
        var arrow = Assert.Single(server.Projectiles.All);
        Assert.Equal(ProjectileManager.Arrow, arrow.Identifier);
        Assert.Same(skeleton, arrow.Shooter);
        Assert.True(arrow.Velocity.Z > 1, "flies towards the player"); // the player is at z 14
        // The aim has a random spread: the skeleton keeps shooting until one arrow hits (a few seconds at most).
        var health = player.Health;
        for (long t = 4; t < 400 && player.Health >= health; t++)
        {
            server.Mobs.Tick(t);
            server.Projectiles.Tick();
        }
        Assert.True(player.Health < health, "an arrow hit the player");
    }

    [Fact]
    public void Fish_swim_in_water_and_suffocate_out_of_it()
    {
        var (server, _) = Arena();
        for (var x = 2; x <= 6; x++)
        for (var z = 2; z <= 6; z++)
        for (var y = Floor; y <= Floor + 2; y++)
            server.World.SetBlock(x, y, z, Blocks.Water);
        var fish = server.Mobs.Spawn(MobKind.Cod, 4.5, Floor + 1, 4.5)!;
        var dry = server.Mobs.Spawn(MobKind.Cod, 11.5, Floor, 11.5)!;

        Run(server, 300);

        Assert.InRange(fish.X, 2, 7);
        Assert.InRange(fish.Z, 2, 7);
        Assert.InRange(fish.Y, Floor, Floor + 2.5); // never jumped out
        Assert.Equal(fish.Kind.MaxHealth, fish.Health);
        Assert.True(dry.Health < dry.Kind.MaxHealth, "a fish on land suffocates");
    }

    [Fact]
    public void Flying_mobs_ignore_gravity()
    {
        var (server, player) = Arena();
        player.IsInGame = false; // no AI: only physics
        var bat = server.Mobs.Spawn(MobKind.Bat, 8.5, Floor + 3, 8.5)!;
        var cow = server.Mobs.Spawn(MobKind.Cow, 4.5, Floor + 3, 4.5)!;
        Run(server, 40);
        Assert.Equal(Floor + 3, bat.Y, 3);
        Assert.True(cow.OnGround);
    }

    [Fact]
    public void Ghast_fireballs_explode()
    {
        var (server, _) = Arena();
        var ghast = server.Mobs.Spawn(MobKind.Ghast, 8.5, Floor + 1, 2.5)!;
        server.World.SetBlock(8, Floor + 1, 12, Blocks.ByName("dirt")!); // strength 1: stone resists it, as vanilla
        server.Projectiles.Shoot(ghast, ProjectileManager.Fireball, new(8.5f, Floor + 1.5f, 6f), new(0, 0, 0.9f), 6);
        for (var i = 0; i < 20 && server.Projectiles.All.Count > 0; i++)
            server.Projectiles.Tick();
        Assert.Empty(server.Projectiles.All);
        Assert.True(server.World.GetBlock(8, Floor + 1, 12)!.IsAir, "the wall was blown up");
    }

    private static bool Dark(World world, int x, int y, int z, bool night, bool nether) =>
        (bool)typeof(MobManager).GetMethod("Dark", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, [world, x, y, z, night, nether])!;
}
