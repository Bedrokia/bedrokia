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
using System.Reflection;
using System.Security.Cryptography;
using Bedrokia.Server;
using Bedrokia.Server.Core;
using Bedrokia.Server.Entities;
using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Tests;

/// <summary>Vanilla details: what stands on a broken block falls, snow stacks, items merge, flyers float.</summary>
public class BlockDetailsTests
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
            server.World.SetBlock(x, Floor - 1, z, Blocks.GrassBlock);
            for (var y = Floor; y < Floor + 6; y++)
                server.World.SetBlock(x, y, z, Blocks.Air);
        }
        var player = new BedrockConnection(server, new FakeTransport(), 100)
        {
            Position = (8f, Floor + BedrockConnection.EyeHeight, 2f),
            IsInGame = true,
        };
        server.AddPlayer(player);
        return (server, player);
    }

    [Fact]
    public void A_sugar_cane_column_breaks_from_its_base()
    {
        var (server, _) = Arena();
        var cane = Blocks.ByName("reeds")!;
        for (var y = Floor; y < Floor + 3; y++)
            server.World.SetBlock(4, y, 4, cane);
        server.World.SetBlock(6, Floor, 4, Blocks.ByName("poppy")!);

        server.SetBlockAndUpdate(4, Floor - 1, 4, Blocks.Air); // the ground under the cane goes
        server.SetBlockAndUpdate(6, Floor - 1, 4, Blocks.Air); // and under the flower

        for (var y = Floor; y < Floor + 3; y++)
            Assert.True(server.World.GetBlock(4, y, 4)!.IsAir, $"cane left at y {y}");
        Assert.True(server.World.GetBlock(6, Floor, 4)!.IsAir);
        Assert.Equal(4, server.World.DroppedItems.All.Sum(i => i.Stack.Count)); // 3 sugar canes and a poppy
    }

    [Fact]
    public void Snow_layers_stack_up_to_eight()
    {
        var (server, player) = Arena();
        server.Config.SpawnProtection = 0; // the arena is next to the world spawn
        server.World.SetBlock(4, Floor, 4, Blocks.SnowLayer);
        player.Inventory.Main[player.Inventory.SelectedSlot] = ItemStack.Of("snow_layer", 10);
        var use = typeof(BedrockConnection).GetMethod("UseSpecialItemOn", BindingFlags.NonPublic | BindingFlags.Instance)!;
        for (var i = 0; i < 9; i++)
            use.Invoke(player, ["snow_layer", server.World.GetBlock(4, Floor, 4)!, 4, Floor, 4, 1]);
        Assert.Equal(7, Convert.ToInt32(server.World.GetBlock(4, Floor, 4)!.States["height"])); // 8 layers, not more
        Assert.Equal(3, player.Inventory.Held!.Count);
    }

    [Fact]
    public void Items_on_the_ground_merge()
    {
        var (server, _) = Arena();
        for (var i = 0; i < 5; i++)
            server.DropItem(ItemStack.Of("dirt")!, 4.5f + i * 0.1f, Floor, 4.5f, velocity: (0, 0, 0));
        for (var t = 0; t < 40; t++)
            server.World.DroppedItems.Tick();
        server.World.DroppedItems.Merge(0);
        var item = Assert.Single(server.World.DroppedItems.All);
        Assert.Equal(5, item.Stack.Count);
    }

    [Fact]
    public void Flying_mobs_are_sent_without_gravity()
    {
        var (server, _) = Arena();
        var bat = server.Mobs.Spawn(MobKind.Bat, 4.5, Floor + 3, 4.5)!;
        var cow = server.Mobs.Spawn(MobKind.Cow, 6.5, Floor, 4.5)!;
        Assert.Equal(0, bat.Flags & EntityPackets.FlagGravity);
        Assert.NotEqual(0, cow.Flags & EntityPackets.FlagGravity);
    }

    [Fact]
    public void Breaking_in_the_air_is_five_times_slower_as_in_PocketMine()
    {
        var (_, player) = Arena();
        var dirt = Blocks.ByName("dirt")!;
        var onGround = player.BreakProgressPerTick(dirt);
        player.Position = (8f, Floor + 3 + BedrockConnection.EyeHeight, 2f); // jumping: air under the feet
        Assert.Equal(onGround / 5, player.BreakProgressPerTick(dirt), 5);
    }

    [Fact]
    public void Tnt_in_water_breaks_nothing_and_obsidian_resists()
    {
        var (server, _) = Arena();
        var dirt = Blocks.ByName("dirt")!;
        server.World.SetBlock(4, Floor, 4, Blocks.Water);
        server.World.SetBlock(5, Floor, 4, dirt);
        server.Explode(4.5, Floor + 0.1, 4.5, 4);
        Assert.Equal(dirt, server.World.GetBlock(5, Floor, 4)); // the water took the blast

        var obsidian = Blocks.ByName("obsidian")!;
        server.World.SetBlock(10, Floor, 10, obsidian);
        server.World.SetBlock(12, Floor, 10, dirt);
        server.Explode(11.5, Floor + 0.1, 10.5, 4);
        Assert.Equal(obsidian, server.World.GetBlock(10, Floor, 10));
        Assert.True(server.World.GetBlock(12, Floor, 10)!.IsAir);
    }

    [Fact]
    public void Water_flows_from_the_source_towards_its_spread()
    {
        var (server, _) = Arena();
        var flowing = Blocks.ByKey(new BlockType(0, "minecraft:flowing_water", new OrderedDictionary<string, object> { ["liquid_depth"] = 2 }).Key)!;
        server.World.SetBlock(4, Floor, 4, Fluids.Source("water"));
        server.World.SetBlock(5, Floor, 4, flowing);
        var (x, _, z) = Fluids.Flow(server.World, 4, Floor, 4);
        Assert.True(x > 0.9 && Math.Abs(z) < 0.1, $"flow {x} {z}");
        Assert.Equal((0, 0, 0), Fluids.Flow(server.World, 8, Floor, 8)); // no water
    }

    [Fact]
    public void A_firework_with_a_star_bursts_up_high_and_hurts_who_is_close()
    {
        var (server, player) = Arena();
        var star = new OrderedDictionary<string, object> { ["FireworkType"] = (byte)0, ["FireworkColor"] = new byte[] { 1 } };
        var rocket = new ItemStack(ItemStack.Of("firework_rocket")!.Type, 1, 0, 0, new OrderedDictionary<string, object>
        {
            ["Fireworks"] = new OrderedDictionary<string, object> { ["Flight"] = (byte)1, ["Explosions"] = new Bedrokia.Protocol.NbtList(10, [star]) },
        });
        server.World.SetBlock(8, Floor + 4, 2, Blocks.Stone); // a ceiling: it bursts right above the player
        server.LaunchFirework(8.5, Floor, 2.5, rocket);
        var health = player.Health;
        for (var i = 0; i < 40; i++)
            server.Tick();
        Assert.True(player.Health < health, "burst within 5 blocks");

        player.SetHealth();
        server.LaunchFirework(14.5, Floor, 14.5, rocket); // far away
        for (var i = 0; i < 40; i++)
            server.Tick();
        Assert.Equal(player.CurrentMaxHealth, player.Health);
    }

    [Theory]
    [InlineData("water", false)]
    [InlineData("slime", false)]
    [InlineData("grass_block", true)]
    public void Falling_hurts_on_the_ground_but_not_into_water_or_onto_slime(string landing, bool hurts)
    {
        var (server, player) = Arena();
        server.World.SetBlock(8, Floor - 1, 2, Blocks.ByName(landing)!);
        if (landing == "water")
            server.World.SetBlock(8, Floor, 2, Blocks.Water); // landing in the water
        void Move(float feet)
        {
            var w = new Bedrokia.Protocol.BedrockWriter().Float32(0).Float32(0).Float32(8.5f).Float32(feet + BedrockConnection.EyeHeight).Float32(2.5f);
            var reader = new Bedrokia.Protocol.BedrockReader(w.ToArray());
            player.OnPlayerAuthInput(ref reader);
        }
        var health = player.Health;
        for (var y = Floor + 12; y >= Floor; y--)
            Move(y);
        Move(Floor); // still: landed
        Assert.Equal(hurts, player.Health < health);
    }

    [Fact]
    public void Obsidian_breaks_after_obsidian_durability_explosions()
    {
        var (server, _) = Arena();
        server.Config.ObsidianDurability = 3;
        var obsidian = Blocks.ByName("obsidian")!;
        server.World.SetBlock(10, Floor, 10, obsidian);
        server.Explode(11.5, Floor + 0.1, 10.5, 4);
        server.Explode(11.5, Floor + 0.1, 10.5, 4);
        Assert.Equal(obsidian, server.World.GetBlock(10, Floor, 10));
        server.Explode(11.5, Floor + 0.1, 10.5, 4);
        Assert.True(server.World.GetBlock(10, Floor, 10)!.IsAir);
    }

    [Fact]
    public void Pvp_reach_refuses_far_hits_but_counts_where_the_victim_just_was()
    {
        var (server, attacker) = Arena();
        var victim = new BedrockConnection(server, new FakeTransport(), 101) { Position = (8f, Floor + BedrockConnection.EyeHeight, 5f), IsInGame = true };
        server.AddPlayer(victim);
        Assert.True(attacker.InReach(victim)); // 3 blocks
        victim.Position = (8f, Floor + BedrockConnection.EyeHeight, 9f);
        Assert.False(attacker.InReach(victim)); // 7 blocks, never closer
        victim.Position = (8f, Floor + BedrockConnection.EyeHeight, 5f);
        victim.TickHealth(); // remembered at 3 blocks...
        victim.Position = (8f, Floor + BedrockConnection.EyeHeight, 9f);
        Assert.True(attacker.InReach(victim)); // ...what a laggy attacker still sees
    }

    [Fact]
    public void Splash_potions_are_thrown_slower_than_snowballs()
    {
        var (server, player) = Arena();
        var potion = server.Projectiles.Launch(player, ProjectileManager.SplashPotion, potion: 21)!;
        var snowball = server.Projectiles.Launch(player, ProjectileManager.Snowball)!;
        Assert.Equal(0.5f, potion.Velocity.Length(), 3);
        Assert.Equal(1.5f, snowball.Velocity.Length(), 3);
    }

    [Theory]
    [InlineData("anvil", 0f, "east")] // looking south: across the look, as the client predicts (Dragonfly RotateLeft)
    [InlineData("damaged_anvil", 90f, "south")]
    [InlineData("furnace", 0f, "north")] // faces the player
    public void Placed_blocks_face_as_the_client_predicts(string block, float yaw, string facing)
    {
        var placed = BlockPlacement.Place(Blocks.ByName(block)!, 0, 64, 0, 1, 0, yaw, 0, (_, _, _) => true);
        Assert.Equal(facing, placed[0].Block.States["minecraft:cardinal_direction"]);
    }

    [Fact]
    public void Custom_blocks_with_a_rotation_face_the_player_as_with_Customies()
    {
        var id = "test:rotating_" + Guid.NewGuid().ToString("N")[..8];
        Bedrokia.Server.Game.Custom.Blocks.CustomBlockBuilder.Create(id).WithProperty("test:rotation", 2, 3, 4, 5).Register();
        var placed = BlockPlacement.Place(Blocks.ByName(id)!, 0, 64, 0, 1, 0, 0f, 0, (_, _, _) => true); // looking south
        Assert.Equal(2, Convert.ToInt32(placed[0].Block.States["test:rotation"])); // its front looks north, at the player
    }

    [Fact]
    public void A_custom_furnace_cooks_like_a_furnace_but_faster()
    {
        var (server, _) = Arena();
        var id = "test:fast_furnace_" + Guid.NewGuid().ToString("N")[..8];
        Bedrokia.Server.Game.Custom.Blocks.CustomBlockBuilder.Create(id).WithProperty("test:lit", 0, 1).Register();
        server.RegisterFurnace(id, 6); // AntaCore's netherite furnace
        var block = Blocks.ByName(id)!;
        server.World.SetBlock(4, Floor, 4, block);
        var furnace = BlockEntity.For(block)!;
        server.World.SetBlockEntity(4, Floor, 4, furnace);
        furnace.Items![0] = ItemStack.Of("raw_iron");
        furnace.Items[1] = ItemStack.Of("coal");
        server.WatchFurnace(4, Floor, 4, server.World);
        for (var i = 0; i < 25; i++) // a vanilla furnace takes 200 ticks
            server.TickFurnaces();
        Assert.Equal("minecraft:iron_ingot", furnace.Items[2]?.Type.Name);
        Assert.Equal(1, Convert.ToInt32(server.World.GetBlock(4, Floor, 4)!.States["test:lit"])); // lit while it burns
    }
}
