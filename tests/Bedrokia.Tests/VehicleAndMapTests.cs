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
using Bedrokia.Protocol;
using Bedrokia.Server;
using Bedrokia.Server.Core;
using Bedrokia.Server.Entities;
using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Tests;

/// <summary>Boats, minecarts and rails, saddles and leads, maps, crossbows and fireworks, on a stone floor in the sky.</summary>
public class VehicleAndMapTests
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
            Position = (8f, Floor + BedrockConnection.EyeHeight, 2f),
            IsInGame = true,
        };
        server.AddPlayer(player);
        return (server, player);
    }

    private static void Hold(BedrockConnection player, string item) =>
        player.Inventory.Main[player.Inventory.SelectedSlot] = ItemStack.Of(item);

    private static void Rail(BedrokiaServer server, int x, int z)
    {
        server.World.SetBlock(x, Floor, z, Blocks.ByName("rail")!);
        Rails.Connect(server.World, x, Floor, z, (rx, ry, rz, b) => server.World.SetBlock(rx, ry, rz, b));
    }

    [Fact]
    public void Rails_bend_and_a_minecart_follows_them()
    {
        var (server, _) = Arena();
        for (var x = 2; x <= 8; x++)
            Rail(server, x, 4);
        for (var z = 5; z <= 12; z++)
            Rail(server, 8, z);
        Assert.Equal(1, Rails.Shape(server.World.GetBlock(4, Floor, 4)!)); // east-west
        Assert.Equal(7, Rails.Shape(server.World.GetBlock(8, Floor, 4)!)); // corner: south and west
        Assert.Equal(0, Rails.Shape(server.World.GetBlock(8, Floor, 9)!)); // north-south

        var cart = server.Vehicles.Place("minecraft:minecart", server.World, 2.5, Floor, 4.5, -90)!; // looking east
        Assert.Equal((1, 0), cart.Heading);
        cart.Speed = 0.4;
        for (var t = 0; t < 40; t++)
            server.Vehicles.MoveCart(cart);
        Assert.Equal(8.5, cart.X, 2); // turned at the corner
        Assert.True(cart.Z > 6, $"went south after the curve (z {cart.Z})");
        Assert.Equal(Floor, cart.Y, 2);
    }

    [Fact]
    public void Minecarts_climb_slopes_and_stop_against_walls()
    {
        var (server, _) = Arena();
        server.World.SetBlock(6, Floor, 4, Blocks.Stone); // a step, with a rail on it
        server.World.SetBlock(7, Floor + 1, 4, Blocks.Stone); // and a wall after it
        for (var x = 2; x <= 5; x++)
            Rail(server, x, 4);
        server.World.SetBlock(6, Floor + 1, 4, Blocks.ByName("rail")!);
        Rails.Connect(server.World, 6, Floor + 1, 4, (rx, ry, rz, b) => server.World.SetBlock(rx, ry, rz, b));
        Assert.Equal(2, Rails.Shape(server.World.GetBlock(5, Floor, 4)!)); // ascending east

        var cart = server.Vehicles.Place("minecraft:minecart", server.World, 3.5, Floor, 4.5, -90)!;
        cart.Speed = 0.4;
        for (var t = 0; t < 30; t++)
            server.Vehicles.MoveCart(cart);
        Assert.True(cart.Y > Floor + 0.5, $"climbed the slope (y {cart.Y})");
        Assert.InRange(cart.X, 6, 7); // stopped by the wall
        Assert.Equal(0, cart.Speed);
    }

    [Fact]
    public void Boats_float_and_carry_their_rider()
    {
        var (server, player) = Arena();
        for (var x = 2; x <= 10; x++)
        for (var z = 2; z <= 10; z++)
            server.World.SetBlock(x, Floor, z, Blocks.Water);
        Assert.Equal(("minecraft:boat", 8), VehicleManager.ForItem("minecraft:cherry_boat"));
        var boat = server.Vehicles.Place("minecraft:oak_boat", server.World, 5.5, Floor + 0.9, 5.5, 0)!;
        for (var t = 0; t < 40; t++)
            server.Vehicles.Tick(t);
        Assert.InRange(boat.Y, Floor + 0.5, Floor + 1.3); // floats at the surface

        Assert.True(server.Vehicles.Mount(player, boat));
        Assert.Same(boat, player.Riding);
        server.Vehicles.RiderInput(player, 7.5f, Floor + 1 + VehicleManager.BoatSeat, 6.5f, 90, 0, 1);
        Assert.Equal(7.5, boat.X, 3);
        server.Vehicles.Dismount(player, teleport: false);
        Assert.Null(player.Riding);
        Assert.Null(boat.Rider);

        // Three hits break it; its item drops.
        for (var i = 0; i < 3; i++)
        {
            server.Vehicles.Hit(player, boat);
            for (var t = 0; t < 11; t++)
                server.Vehicles.Tick(100 + i * 20 + t);
        }
        Assert.DoesNotContain(boat, server.Vehicles.All);
        Assert.Contains(server.World.DroppedItems.All, d => d.Stack.Type.Name == "minecraft:oak_boat");
    }

    [Fact]
    public void Saddled_pigs_are_ridden_and_leads_pull_animals()
    {
        var (server, player) = Arena();
        var pig = server.Mobs.Spawn(MobKind.Pig, 8.5, Floor, 4.5)!;
        Hold(player, "saddle");
        Assert.True(server.Mobs.Interact(player, pig));
        Assert.True(pig.Saddled);
        Hold(player, "stick");
        Assert.True(server.Mobs.Interact(player, pig)); // rides it
        Assert.Same(pig, player.Riding);
        Assert.Same(player, pig.Rider);
        server.Vehicles.Dismount(player, teleport: false);

        var cow = server.Mobs.Spawn(MobKind.Cow, 4.5, Floor, 4.5)!;
        Hold(player, "lead");
        Assert.True(server.Mobs.Interact(player, cow));
        Assert.Same(player, cow.LeashHolder);
        player.Position = (11f, Floor + BedrockConnection.EyeHeight, 11f); // walks away (about 9 blocks)
        for (var t = 1; t < 200; t++)
            server.Mobs.Tick(t);
        Assert.Same(player, cow.LeashHolder);
        Assert.True(Math.Abs(cow.X - 11) + Math.Abs(cow.Z - 11) < 8, $"the cow followed ({cow.X:0.0}, {cow.Z:0.0})");

        server.World.SetBlock(12, Floor, 12, Blocks.ByName("oak_fence")!);
        Assert.True(server.Mobs.TieToFence(player, 12, Floor, 12));
        Assert.IsType<LeashKnot>(cow.LeashHolder);
        server.Mobs.BreakKnot(server.Mobs.Knots[0]);
        Assert.Null(cow.LeashHolder);
    }

    [Fact]
    public void Maps_are_drawn_from_the_world()
    {
        var (server, player) = Arena();
        var item = server.Maps.Create(player)!;
        Assert.Equal("minecraft:filled_map", item.Type.Name);
        var map = server.Maps.Of(item)!;
        Assert.Equal(0, map.CenterX); // player at x 8: the map around the origin
        // Pixel of block (8, 2): chunk (0, 0) is loaded, its top is the stone floor.
        Assert.NotEqual(0, map.Pixels[(2 + 64) * MapData.Size + 8 + 64]);
        Assert.Equal(0, map.Pixels[0]); // far corner: not loaded, unexplored
        var packet = MapManager.Packet(map, player);
        Assert.True(packet.Length > MapData.Size * MapData.Size * 4);

        server.Maps.Save();
        Assert.True(File.Exists(Path.Combine(server.World.Folder, "maps", $"{map.Id}.json")));
    }

    [Fact]
    public void Firework_recipes_and_crossbows()
    {
        var star = SpecialRecipes.Craft([ItemStack.Of("gunpowder")!, ItemStack.Of("red_dye")!, ItemStack.Of("gold_nugget")!])!;
        Assert.Equal("minecraft:firework_star", star.Type.Name);
        var explosion = (OrderedDictionary<string, object>)star.Nbt!["FireworkExplosion"];
        Assert.Equal((byte)2, explosion["FireworkType"]); // star shape
        var rocket = SpecialRecipes.Craft([ItemStack.Of("paper")!, ItemStack.Of("gunpowder")!, ItemStack.Of("gunpowder")!, star])!;
        Assert.Equal(3, rocket.Count);
        var fireworks = (OrderedDictionary<string, object>)rocket.Nbt!["Fireworks"];
        Assert.Equal((byte)2, fireworks["Flight"]);
        Assert.Single(((NbtList)fireworks["Explosions"]).Values);
        Assert.Null(SpecialRecipes.Craft([ItemStack.Of("paper")!, ItemStack.Of("stick")!]));

        var (server, player) = Arena();
        Hold(player, "crossbow");
        player.Inventory.Main[5] = ItemStack.Of("arrow", 2);
        typeof(BedrockConnection).GetField("_chargeSince", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(player, (long?)(server.CurrentTick - 30));
        player.OnReleaseItem();
        Assert.True(player.Inventory.Held!.Nbt!.ContainsKey("chargedItem")); // loaded, not fired
        Assert.Empty(server.Projectiles.All);
        typeof(BedrockConnection).GetMethod("UseHeldInAir", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(player, null);
        Assert.Single(server.Projectiles.All); // the next click fires
        Assert.False(player.Inventory.Held?.Nbt?.ContainsKey("chargedItem") ?? false);
    }
}
