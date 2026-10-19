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
using Bedrokia.Protocol;
using Bedrokia.Server;
using Bedrokia.Server.Core;
using Bedrokia.Server.Entities;
using Bedrokia.Server.Events;
using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Tests;

public sealed class ServerEventsTests
{
    private sealed class FakeTransport : IGameTransport
    {
        public IPEndPoint RemoteEndPoint { get; } = new(IPAddress.Loopback, 19132);
        public ECDsa? PeerPublicKey => null;
        public List<byte[]> Sent { get; } = [];
        public void Send(byte[] batch) => Sent.Add(batch);
        public void Disconnect(string reason) { }
    }

    private static (BedrokiaServer Server, BedrockConnection Player) CreateTestEnvironment()
    {
        var logger = new Logger(LogLevel.Error, null);
        var server = new BedrokiaServer(new ServerConfig { WorldsDirectory = TestWorlds.NewDirectory(), Port = 0, OnlineMode = false }, logger);
        var conn = new BedrockConnection(server, new FakeTransport(), 100);
        return (server, conn);
    }

    [Fact]
    public void PlayerDamage_CanBeCancelled()
    {
        var (server, player) = CreateTestEnvironment();
        var initialHealth = player.Health;
        var fired = false;

        server.Events.PlayerDamage += e =>
        {
            fired = true;
            Assert.Equal(player, e.Player);
            Assert.Equal(DamageCause.Fall, e.Source.Cause);
            e.Cancel();
        };

        player.Damage(10, 0, 0, armour: false, source: DamageSource.Fall(13));

        Assert.True(fired);
        Assert.Equal(initialHealth, player.Health);
    }

    [Fact]
    public void PlayerDamage_CanModifyDamageAmount()
    {
        var (server, player) = CreateTestEnvironment();
        var initialHealth = player.Health;

        server.Events.PlayerDamage += e =>
        {
            e.Damage = 2; // reduce 10 damage down to 2
        };

        player.Damage(10, 0, 0, armour: false, source: DamageSource.Void());

        Assert.Equal(initialHealth - 2, player.Health);
    }

    [Fact]
    public void PlayerDeath_CustomMessage_And_CustomDrops()
    {
        var (server, player) = CreateTestEnvironment();
        var customStick = ItemStack.Of("stick", 5);
        Assert.NotNull(customStick);
        player.Inventory.Main[0] = customStick;

        var deathFired = false;
        server.Events.PlayerDeath += e =>
        {
            deathFired = true;
            Assert.Equal(player, e.Player);
            e.DeathMessage = "Custom elimination message";
            e.Drops.Clear(); // suppress dropping items
            var diamond = ItemStack.Of("diamond", 1);
            Assert.NotNull(diamond);
            e.Drops.Add(diamond);
        };

        // Deal fatal damage
        player.Damage(100, 0, 0, armour: false);

        Assert.True(deathFired);
        Assert.True(player.Dead);
    }

    [Fact]
    public void Items_in_the_crafting_grid_are_not_lost_on_death()
    {
        var (server, player) = CreateTestEnvironment();
        player.Inventory.Ui[28] = ItemStack.Of("diamond", 3); // small crafting grid
        List<ItemStack> drops = [];
        server.Events.PlayerDeath += e => drops = [.. e.Drops];

        player.Damage(100, 0, 0, armour: false);

        Assert.Contains(drops, s => s.Type.Name == "minecraft:diamond" && s.Count == 3);
        Assert.Null(player.Inventory.Ui[28]);
    }

    [Fact]
    public void PlayerRespawn_CanOverrideRespawnCoordinates()
    {
        var (server, player) = CreateTestEnvironment();
        player.Damage(100, 0, 0, armour: false);
        Assert.True(player.Dead);

        var respawnFired = false;
        server.Events.PlayerRespawn += e =>
        {
            respawnFired = true;
            Assert.Equal(player, e.Player);
            e.RespawnPosition = (123.5f, 75.0f, 456.5f);
        };

        // Send respawn packet (state 2 = client ready)
        var writer = new BedrockWriter();
        writer.Bytes(new byte[12]); // position
        writer.Byte(2); // state = 2
        var data = writer.ToArray();
        var reader = new BedrockReader(data);

        player.OnRespawnRequest(ref reader);

        Assert.True(respawnFired);
        Assert.False(player.Dead);
        Assert.Equal(123.5f, player.Position.X);
        Assert.Equal(456.5f, player.Position.Z);
    }

    [Fact]
    public void PlayerDropItem_CanBeCancelled_And_ItemPreserved()
    {
        var (server, player) = CreateTestEnvironment();
        var stack = ItemStack.Of("apple", 10);
        Assert.NotNull(stack);
        player.Inventory.Main[0] = stack;

        var dropFired = false;
        server.Events.PlayerDropItem += e =>
        {
            dropFired = true;
            Assert.Equal(player, e.Player);
            Assert.Equal("minecraft:apple", e.Item.Type.Name);
            Assert.Equal(2, e.Item.Count);
            e.Cancel();
        };

        // Simulate drop request via ItemStackRequest
        // Variant 3 = Drop
        var writer = new BedrockWriter();
        writer.VarUInt32(1); // request count
        writer.VarInt32(1); // request id
        writer.VarUInt32(1); // action count
        writer.VarUInt32(3); // action variant: drop
        writer.Byte(0); // legacy byte
        writer.Byte(2); // count to drop
        writer.Byte(28); // ContainerHotBar (28)
        writer.Bool(false); // dynamic id
        writer.Byte(0); // slot 0
        writer.Int32(stack.NetworkId); // stack net id
        writer.Bool(false); // randomly
        writer.VarUInt32(0); // filter strings
        writer.Int32(0); // filter cause

        var reader = new BedrockReader(writer.ToArray());
        player.Inventory.HandleRequests(ref reader, GameMode.Survival, _ => { });

        Assert.True(dropFired);
        // Stack should still have all 10 items because drop was cancelled
        Assert.NotNull(player.Inventory.Main[0]);
        Assert.Equal(10, player.Inventory.Main[0]!.Count);
    }

    [Fact]
    public void InventoryTransaction_CanBeCancelled_RollsBackChanges()
    {
        var (server, player) = CreateTestEnvironment();
        var apple = ItemStack.Of("apple", 5);
        Assert.NotNull(apple);
        player.Inventory.Main[0] = apple;
        player.Inventory.Main[1] = null;

        var txFired = false;
        server.Events.InventoryTransaction += e =>
        {
            txFired = true;
            Assert.Equal(InventoryTransactionType.Transfer, e.Type);
            e.Cancel();
        };

        // Transfer 2 apples from slot 0 to slot 1
        // Action 0 = Take / Place (Transfer)
        var writer = new BedrockWriter();
        writer.VarUInt32(1); // request count
        writer.VarInt32(2); // request id
        writer.VarUInt32(1); // action count
        writer.VarUInt32(0); // action variant: transfer (take)
        writer.Byte(0);
        writer.Byte(2); // count
        // From slot 0
        writer.Byte(28); // ContainerHotBar
        writer.Bool(false);
        writer.Byte(0);
        writer.Int32(apple.NetworkId);
        // To slot 1
        writer.Byte(28); // ContainerHotBar
        writer.Bool(false);
        writer.Byte(1);
        writer.Int32(0);
        writer.VarUInt32(0); // filters
        writer.Int32(0); // filter cause

        var reader = new BedrockReader(writer.ToArray());
        player.Inventory.HandleRequests(ref reader, GameMode.Survival, _ => { });

        Assert.True(txFired);
        // Transaction was cancelled: slot 0 must still have 5 apples and slot 1 must be null
        Assert.NotNull(player.Inventory.Main[0]);
        Assert.Equal(5, player.Inventory.Main[0]!.Count);
        Assert.Null(player.Inventory.Main[1]);
    }

    [Fact]
    public void EntitySpawn_CanBeCancelled_PreventsMobAddition()
    {
        var (server, _) = CreateTestEnvironment();
        var spawnFired = false;

        server.Events.EntitySpawn += e =>
        {
            spawnFired = true;
            Assert.Equal("minecraft:cow", e.Entity.Kind.Id);
            e.Cancel();
        };

        var spawned = server.Mobs.Spawn(MobKind.Cow, 10, 64, 10);

        Assert.True(spawnFired);
        Assert.Null(spawned);
        Assert.Empty(server.Mobs.All);
    }

    [Fact]
    public void EntityDamage_CanBeCancelled_And_CanModifyDamage()
    {
        var (server, player) = CreateTestEnvironment();
        var cow = server.Mobs.Spawn(MobKind.Cow, 10, 64, 10);
        Assert.NotNull(cow);

        var initialHealth = cow.Health;
        var damageFired = false;

        server.Events.EntityDamage += e =>
        {
            damageFired = true;
            Assert.Equal(cow, e.Entity);
            Assert.Equal(DamageCause.PlayerAttack, e.Source.Cause);
            Assert.Equal(player, e.Killer);
            e.Damage = 3; // change damage to 3
        };

        server.Mobs.Damage(cow, 10, (10, 10), player);

        Assert.True(damageFired);
        Assert.Equal(initialHealth - 3, cow.Health);
    }

    [Fact]
    public void EntityDespawn_FiresWithCorrectReason()
    {
        var (server, _) = CreateTestEnvironment();
        var cow = server.Mobs.Spawn(MobKind.Cow, 10, 64, 10);
        Assert.NotNull(cow);

        var despawnFired = false;
        EntityDespawnReason? receivedReason = null;

        server.Events.EntityDespawn += e =>
        {
            despawnFired = true;
            Assert.Equal(cow, e.Entity);
            receivedReason = e.Reason;
        };

        server.Mobs.Remove(cow, EntityDespawnReason.Death);

        Assert.True(despawnFired);
        Assert.Equal(EntityDespawnReason.Death, receivedReason);
    }
}
