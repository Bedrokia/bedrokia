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
}
