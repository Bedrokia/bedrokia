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
using Bedrokia.Server.Events;
using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Tests;

/// <summary>Items with a use of their own and the wear of tools and armour, on a stone floor high in the sky.</summary>
public class ItemUseTests
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

    [Fact]
    public void Shown_items_reach_only_the_players_within_range()
    {
        var (server, player) = Arena();
        var near = server.ShowItem(ItemStack.Of("diamond")!, 8, Floor, 6);
        var far = server.ShowItem(ItemStack.Of("emerald")!, 8, Floor, 2 + BedrokiaServer.ShownItemRange + 5);
        for (var i = 0; i < 10; i++)
            server.Tick();
        Assert.Contains(near, player.SeenShownItems);
        Assert.DoesNotContain(far, player.SeenShownItems);

        player.Position = (8f, Floor + BedrockConnection.EyeHeight, 2 + BedrokiaServer.ShownItemRange + 5);
        for (var i = 0; i < 10; i++)
            server.Tick();
        Assert.Contains(far, player.SeenShownItems);
        Assert.DoesNotContain(near, player.SeenShownItems);

        server.HideItem(far);
        Assert.Empty(player.SeenShownItems);
    }

    private static void Hold(BedrockConnection player, string item, int count = 1) =>
        player.Inventory.Main[player.Inventory.SelectedSlot] = ItemStack.Of(item, count);

    // Draws the held bow, crossbow or trident for the given ticks, then lets go.
    private static void Draw(BedrokiaServer server, BedrockConnection player, int ticks)
    {
        typeof(BedrockConnection).GetField("_chargeSince", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(player, (long?)(server.CurrentTick - ticks));
        player.OnReleaseItem();
    }

    [Fact]
    public void Tools_wear_out_and_break()
    {
        var pickaxe = ItemStack.Of("iron_pickaxe")!;
        Assert.Equal(250, Durability.Max(pickaxe));
        var worn = Durability.Wear(pickaxe, 10, new Random(1))!;
        Assert.Equal(10, Durability.Damage(worn));
        Assert.Null(Durability.Wear(worn, 240, new Random(1)));
        Assert.Same(pickaxe, Durability.Wear(ItemStack.Of("stick")!, 1, new Random(1)) is { } stick ? pickaxe : null); // no durability
        Assert.Equal(363, Durability.Max(ItemStack.Of("diamond_helmet")));
        Assert.Equal(0, Durability.Max(ItemStack.Of("dirt")));

        var (_, player) = Arena();
        Hold(player, "golden_sword");
        player.WearHeld(31);
        Assert.Equal(31, Durability.Damage(player.Inventory.Held));
        player.WearHeld(1);
        Assert.Null(player.Inventory.Held); // 32 uses: broken
    }

    [Fact]
    public void Armour_wears_when_hit_and_a_totem_saves_from_death()
    {
        var (_, player) = Arena();
        player.Inventory.Armour[0] = ItemStack.Of("iron_helmet");
        player.Damage(8, 8, 3, source: DamageSource.Custom());
        Assert.Equal(2, Durability.Damage(player.Inventory.Armour[0])); // 8 damage: 2 points

        Hold(player, "totem_of_undying");
        typeof(BedrockConnection).GetField("_hurtTicks", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(player, 0);
        player.Damage(100, 8, 3, armour: false, source: DamageSource.Custom());
        Assert.False(player.Dead);
        Assert.Equal(1, player.Health);
        Assert.Null(player.Inventory.Held); // used up
    }

    [Fact]
    public void A_bow_shoots_an_arrow_from_the_inventory()
    {
        var (server, player) = Arena();
        Hold(player, "bow");
        player.Inventory.Main[5] = ItemStack.Of("arrow", 3);

        Draw(server, player, 1); // too short: nothing
        Assert.Empty(server.Projectiles.All);

        Draw(server, player, 20);
        var arrow = Assert.Single(server.Projectiles.All);
        Assert.Equal(ProjectileManager.Arrow, arrow.Identifier);
        Assert.Same(player, arrow.Owner);
        Assert.InRange(arrow.Velocity.Length(), 2.9f, 3.1f); // full draw
        Assert.True(arrow.Damage >= 6);
        Assert.Equal(2, player.Inventory.Main[5]!.Count);
        Assert.Equal(1, Durability.Damage(player.Inventory.Held));
    }

    [Fact]
    public void A_bow_without_arrows_does_nothing_and_a_trident_is_thrown()
    {
        var (server, player) = Arena();
        Hold(player, "bow");
        Draw(server, player, 20);
        Assert.Empty(server.Projectiles.All);

        Hold(player, "trident");
        Draw(server, player, 20);
        var trident = Assert.Single(server.Projectiles.All);
        Assert.Equal(ProjectileManager.Trident, trident.Identifier);
        Assert.Equal("minecraft:trident", trident.Item?.Type.Name);
        Assert.Null(player.Inventory.Held); // it left the hand
    }

    [Fact]
    public void Reeling_in_during_a_bite_catches_something()
    {
        var (server, player) = Arena();
        Hold(player, "fishing_rod");
        var hook = server.Projectiles.Fire(player, ProjectileManager.FishingHook, 1, 0);
        Assert.Same(hook, server.Projectiles.HookOf(player));
        hook.BiteTicks = 10;
        Assert.True(server.Projectiles.Reel(hook));
        Assert.Null(server.Projectiles.HookOf(player));
        Assert.Contains(Enumerable.Range(0, player.Inventory.Main.Size), i => i != player.Inventory.SelectedSlot && player.Inventory.Main[i] is not null);
    }

    [Fact]
    public void An_axe_strips_logs()
    {
        var (server, player) = Arena();
        server.World.SetBlock(4, Floor, 4, Blocks.Get("oak_log", ("pillar_axis", "x")));
        Hold(player, "iron_axe");
        var used = (bool)typeof(BedrockConnection).GetMethod("UseSpecialItemOn", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(player, ["iron_axe", server.World.GetBlock(4, Floor, 4)!, 4, Floor, 4, 1])!;
        Assert.True(used);
        var stripped = server.World.GetBlock(4, Floor, 4)!;
        Assert.Equal("minecraft:stripped_oak_log", stripped.Name);
        Assert.Equal("x", stripped.States["pillar_axis"]);
        Assert.Equal(1, Durability.Damage(player.Inventory.Held));
    }

    [Fact]
    public void Name_tags_and_water_buckets_on_mobs()
    {
        var (server, player) = Arena();
        var cow = server.Mobs.Spawn(MobKind.Cow, 8.5, Floor, 4.5)!;
        player.Inventory.Main[player.Inventory.SelectedSlot] = ItemStack.Of("name_tag")!.WithName("Marguerite");
        Assert.True(server.Mobs.Interact(player, cow));
        Assert.Equal("Marguerite", cow.NameTag);
        Assert.Null(player.Inventory.Held);

        for (var x = 2; x <= 4; x++)
            server.World.SetBlock(x, Floor, 8, Blocks.Water);
        var cod = server.Mobs.Spawn(MobKind.Cod, 3.5, Floor, 8.5)!;
        Hold(player, "water_bucket");
        Assert.True(server.Mobs.Interact(player, cod));
        Assert.DoesNotContain(cod, server.Mobs.All);
        Assert.Contains(Enumerable.Range(0, player.Inventory.Main.Size), i => player.Inventory.Main[i]?.Type.Name == "minecraft:cod_bucket");
    }
}
