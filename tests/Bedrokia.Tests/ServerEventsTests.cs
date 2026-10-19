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
        server.InitWorldForTesting();
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

    [Fact]
    public void PlayerJoin_CanModifyOrSilenceBroadcast()
    {
        var (server, player) = CreateTestEnvironment();
        player.Info = new PlayerInfo(Guid.NewGuid(), player.EntityId, "Steve", "123", 1, []);
        server.AddPlayer(player);

        var fired = false;
        server.Events.PlayerJoin += e =>
        {
            fired = true;
            Assert.Equal("§eSteve joined the game", e.BroadcastMessage);
            e.BroadcastMessage = "Welcome Steve!";
        };

        server.OnPlayerJoined(player);
        Assert.True(fired);

        var silencedFired = false;
        server.Events.PlayerJoin = e =>
        {
            silencedFired = true;
            e.BroadcastMessage = null;
        };

        server.OnPlayerJoined(player);
        Assert.True(silencedFired);
    }

    [Fact]
    public void PlayerMove_CanBeCancelled_RollsBackPosition()
    {
        var (server, player) = CreateTestEnvironment();
        player.Position = (10f, 64f, 10f);
        player.Pitch = 0f;
        player.Yaw = 0f;

        var moveFired = false;
        server.Events.PlayerMove += e =>
        {
            moveFired = true;
            Assert.Equal(10f, e.From.X);
            Assert.Equal(20f, e.To.X);
            Assert.True(e.IsBlockChange);
            e.Cancel();
        };

        var writer = new BedrockWriter();
        writer.Float32(0); // pitch
        writer.Float32(0); // yaw
        writer.Float32(20f); // x
        writer.Float32(64f); // y
        writer.Float32(10f); // z
        var reader = new BedrockReader(writer.ToArray());

        player.OnPlayerAuthInput(ref reader);

        Assert.True(moveFired);
        Assert.Equal(10f, player.Position.X);
    }

    [Fact]
    public void PlayerMove_UnchangedPosition_DoesNotFireEvent()
    {
        var (server, player) = CreateTestEnvironment();
        player.Position = (10f, 64f, 10f);
        player.Pitch = 0f;
        player.Yaw = 0f;

        var moveFired = false;
        server.Events.PlayerMove += _ => moveFired = true;

        var writer = new BedrockWriter();
        writer.Float32(0); // pitch
        writer.Float32(0); // yaw
        writer.Float32(10f); // x
        writer.Float32(64f); // y
        writer.Float32(10f); // z
        var reader = new BedrockReader(writer.ToArray());

        player.OnPlayerAuthInput(ref reader);

        Assert.False(moveFired);
    }

    [Fact]
    public void PlayerCommand_CanBeCancelled_And_CanModifyCommandLine()
    {
        var (server, player) = CreateTestEnvironment();
        var executed = "";
        server.Commands.Register("test", "Test command", "", null, (sender, args) => executed = "test");
        server.Commands.Register("modified", "Modified command", "", null, (sender, args) => executed = "modified");

        server.Events.PlayerCommand += e =>
        {
            if (e.CommandLine == "test")
                e.Cancel();
            else if (e.CommandLine == "change")
                e.CommandLine = "modified";
        };

        server.Commands.Dispatch(player.CommandSender, "test");
        Assert.Equal("", executed);

        server.Commands.Dispatch(player.CommandSender, "change");
        Assert.Equal("modified", executed);
    }

    [Fact]
    public void PlayerPickupItem_CanBeCancelled_ItemRemainsOnGround()
    {
        var (server, player) = CreateTestEnvironment();
        player.Position = (10f, 65.62f, 10f);
        player.IsInGame = true;
        server.AddPlayer(player);

        var stack = ItemStack.Of("diamond", 1);
        Assert.NotNull(stack);
        server.DropItem(stack, 10f, 64f, 10f, pickupDelay: 0);
        Assert.Single(server.DroppedItems.All);

        var pickupFired = false;
        server.Events.PlayerPickupItem += e =>
        {
            pickupFired = true;
            Assert.Equal(player, e.Player);
            e.Cancel();
        };

        server.TickDroppedItems();

        Assert.True(pickupFired);
        Assert.Single(server.DroppedItems.All);
        Assert.Null(player.Inventory.Main[0]);
    }

    [Fact]
    public void ContainerOpen_CanBeCancelled()
    {
        var (server, player) = CreateTestEnvironment();
        var chest = Blocks.ByName("chest")!;
        server.World.SetBlock(10, 64, 10, chest);

        var openFired = false;
        server.Events.ContainerOpen += e =>
        {
            openFired = true;
            Assert.Equal("Chest", e.ContainerKind);
            e.Cancel();
        };

        player.TryOpenContainer(chest, 10, 64, 10);

        Assert.True(openFired);
        Assert.Null(player.Inventory.OpenContainer);
        Assert.Null(player.OpenContainerAt);
    }

    [Fact]
    public void EntityDeath_CanModifyDropsAndXp()
    {
        var (server, player) = CreateTestEnvironment();
        var cow = server.Mobs.Spawn(MobKind.Cow, 10, 64, 10);
        Assert.NotNull(cow);

        var deathFired = false;
        server.Events.EntityDeath += e =>
        {
            deathFired = true;
            Assert.Equal(cow, e.Entity);
            e.Drops.Clear();
            var customItem = ItemStack.Of("diamond", 5);
            Assert.NotNull(customItem);
            e.Drops.Add(customItem);
            e.DroppedXp = 50;
        };

        server.Mobs.Damage(cow, 100, (10, 10), player);

        Assert.True(deathFired);
        Assert.Contains(server.DroppedItems.All, item => item.Stack.Type.Name == "minecraft:diamond" && item.Stack.Count == 5);
        Assert.True(player.ExperienceLevel > 0 || player.ExperienceProgress > 0);
    }

    [Fact]
    public void Explosion_CanBeCancelled_And_CanClearAffectedBlocks()
    {
        var (server, player) = CreateTestEnvironment();
        player.Position = (10f, 65.62f, 10f);
        player.IsInGame = true;
        server.AddPlayer(player);

        var dirt = Blocks.ByName("dirt")!;
        server.World.SetBlock(10, 63, 10, dirt);

        // 1. Cancelled explosion
        var cancelledFired = false;
        void OnExplosionCancel(ExplosionEvent e)
        {
            cancelledFired = true;
            e.Cancel();
        }
        server.Events.Explosion += OnExplosionCancel;
        server.Explode(10, 64, 10, 3);
        Assert.True(cancelledFired);
        Assert.Equal(dirt, server.World.GetBlock(10, 63, 10));
        server.Events.Explosion -= OnExplosionCancel;

        // 2. Clear affected blocks: blocks are spared, but damage is still dealt!
        player.SetFood(10, 0);
        var initialHealth = player.Health;
        server.Events.Explosion += e =>
        {
            e.AffectedBlocks.Clear();
        };
        server.Explode(10, 64, 10, 3);
        Assert.Equal(dirt, server.World.GetBlock(10, 63, 10)); // Blocks spared!
        Assert.True(player.Health < initialHealth); // Player damaged!
    }

    [Fact]
    public void Security_InvalidOrRejectedPackets_DoNotTriggerEvents()
    {
        var (server, player) = CreateTestEnvironment();
        player.Position = (0f, 64f, 0f);
        player.IsInGame = true;

        var moveFired = false;
        server.Events.PlayerMove += _ => moveFired = true;

        // Non-finite coordinates in PlayerAuthInput must throw / be rejected without firing PlayerMoveEvent
        var invalidWriter = new BedrockWriter();
        invalidWriter.Float32(0); // pitch
        invalidWriter.Float32(0); // yaw
        invalidWriter.Float32(float.NaN); // invalid x
        invalidWriter.Float32(64f);
        invalidWriter.Float32(0f);
        var invalidReader = new BedrockReader(invalidWriter.ToArray());

        var thrown = false;
        try
        {
            player.OnPlayerAuthInput(ref invalidReader);
        }
        catch (InvalidDataException)
        {
            thrown = true;
        }
        Assert.True(thrown);
        Assert.False(moveFired);

        // Out-of-reach attack (> 6 blocks) must be rejected without damaging target
        var targetMob = server.Mobs.Spawn(MobKind.Cow, 100, 64, 100);
        Assert.NotNull(targetMob);
        var initialMobHealth = targetMob.Health;

        var damageFired = false;
        server.Events.EntityDamage += _ => damageFired = true;

        player.AttackEntity(targetMob.EntityId);

        Assert.False(damageFired);
        Assert.Equal(initialMobHealth, targetMob.Health);
    }
}
