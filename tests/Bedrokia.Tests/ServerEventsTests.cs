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
        var (server, conn, _) = CreateTestEnvironmentWithTransport();
        return (server, conn);
    }

    private static (BedrokiaServer Server, BedrockConnection Player, FakeTransport Transport) CreateTestEnvironmentWithTransport()
    {
        var logger = new Logger(LogLevel.Error, null);
        var server = new BedrokiaServer(new ServerConfig { WorldsDirectory = TestWorlds.NewDirectory(), Port = 0, OnlineMode = false }, logger);
        server.InitWorldForTesting();
        var transport = new FakeTransport();
        var conn = new BedrockConnection(server, transport, 100);
        return (server, conn, transport);
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
        server.Events.Clear();
        server.Events.PlayerJoin += e =>
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
        Assert.Single(server.World.DroppedItems.All);

        var pickupFired = false;
        server.Events.PlayerPickupItem += e =>
        {
            pickupFired = true;
            Assert.Equal(player, e.Player);
            e.Cancel();
        };

        server.TickDroppedItems(server.World);

        Assert.True(pickupFired);
        Assert.Single(server.World.DroppedItems.All);
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
        Assert.Contains(server.World.DroppedItems.All, item => item.Stack.Type.Name == "minecraft:diamond" && item.Stack.Count == 5);
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

    [Fact]
    public void TntPrime_FuseTicks_Editable_And_Cancellable()
    {
        var (server, _) = CreateTestEnvironment();
        var tntFired = false;
        server.Events.TntPrime += e =>
        {
            tntFired = true;
            Assert.Equal(80, e.FuseTicks);
            e.FuseTicks = 20; // Shorten fuse
        };

        server.PrimeTnt(10, 64, 10, 80);
        Assert.True(tntFired);

        // Test cancel
        var cancelFired = false;
        void OnPrimeCancel(TntPrimeEvent e)
        {
            cancelFired = true;
            e.Cancel();
        }
        server.Events.TntPrime += OnPrimeCancel;
        server.PrimeTnt(20, 64, 20, 80);
        Assert.True(cancelFired);
        server.Events.TntPrime -= OnPrimeCancel;
    }

    [Fact]
    public void ItemCook_And_FuelBurn_Editable_And_Cancellable()
    {
        var (server, _) = CreateTestEnvironment();
        var furnaceBlock = Blocks.ByName("furnace")!;
        server.World.SetBlock(5, 64, 5, furnaceBlock);
        var entity = BlockEntity.For(furnaceBlock)!;
        server.World.SetBlockEntity(5, 64, 5, entity);

        // Put beef in input and coal in fuel
        entity.Items![0] = ItemStack.Of("beef", 1);
        entity.Items[1] = ItemStack.Of("coal", 1);
        server.WatchFurnace((5, 64, 5), server.World);

        var fuelBurnFired = false;
        server.Events.FuelBurn += e =>
        {
            fuelBurnFired = true;
            Assert.Equal("furnace", e.Station);
            Assert.True(e.BurnTicks > 0);
            e.BurnTicks = 1600; // Custom burn time
        };

        var itemCookFired = false;
        server.Events.ItemCook += e =>
        {
            itemCookFired = true;
            Assert.Equal("furnace", e.Station);
            Assert.Equal("minecraft:cooked_beef", e.Output.Type.Name);
            // Change output to diamond
            e.Output = ItemStack.Of("diamond", 2)!;
        };

        // Tick furnaces to start burning fuel
        server.TickFurnaces();
        Assert.True(fuelBurnFired);

        // Fast forward cook ticks
        entity.CookTicks = Smelting.FurnaceCookTicks - 1;
        server.TickFurnaces();

        Assert.True(itemCookFired);
        Assert.NotNull(entity.Items[2]);
        Assert.Equal("minecraft:diamond", entity.Items[2]!.Type.Name);
        Assert.Equal(2, entity.Items[2]!.Count);
    }

    [Fact]
    public void BlockLand_RollbackToDroppedItem_OnCancel()
    {
        var (server, _) = CreateTestEnvironment();
        var sand = Blocks.ByName("sand")!;
        server.World.SetBlock(15, 60, 15, Blocks.Stone); // solid floor
        server.World.SetBlock(15, 61, 15, Blocks.Air);

        var fallingBlocks = new FallingBlocks(server, server.World);
        fallingBlocks.Spawn(15, 62, 15, sand);

        var landFired = false;
        server.Events.BlockLand += e =>
        {
            landFired = true;
            e.Cancel(); // Cancel landing! Must drop as item entity instead of placing block
        };

        // Tick falling block to reach the floor
        for (int i = 0; i < 30; i++)
            fallingBlocks.Tick();

        Assert.True(landFired);
        // Block was NOT placed
        Assert.Equal(Blocks.Air, server.World.GetBlock(15, 61, 15));
        // Item entity WAS spawned
        Assert.Contains(server.World.DroppedItems.All, item => item.Stack.Type.Name == "minecraft:sand");
    }

    [Fact]
    public void EntityKilled_RollbackTo1Hp_OnCancel_And_CustomDrops()
    {
        var (server, player) = CreateTestEnvironment();
        var zombie = server.Mobs.Spawn(MobKind.Zombie, 10, 64, 10);
        Assert.NotNull(zombie);

        // 1. Cancelled killed event: mob stays alive with 1 HP
        var killedFired = false;
        void OnKilledCancel(EntityKilledEvent e)
        {
            killedFired = true;
            e.Cancel();
        }
        server.Events.EntityKilled += OnKilledCancel;

        server.Mobs.Damage(zombie, 100, (10, 10), player);

        Assert.True(killedFired);
        Assert.False(zombie.Dead);
        Assert.Equal(1f, zombie.Health);
        server.Events.EntityKilled -= OnKilledCancel;

        // 2. Not cancelled: custom experience and drop items
        server.Events.EntityKilled += e =>
        {
            e.Experience = 99;
            e.DropItems = false; // suppress drops
        };

        zombie.HurtTicks = 0; // reset invulnerability ticks before next hit
        server.Mobs.Damage(zombie, 100, (10, 10), player);
        Assert.True(zombie.Dead);
        Assert.True(player.ExperienceLevel > 0 || player.ExperienceProgress > 0);
    }

    [Fact]
    public void ContainerOpen_Cancelled_SendsContainerClose()
    {
        var (server, player, fakeTransport) = CreateTestEnvironmentWithTransport();
        fakeTransport.Sent.Clear();

        var chest = Blocks.ByName("chest")!;
        server.World.SetBlock(10, 64, 10, chest);

        var openFired = false;
        server.Events.ContainerOpen += e =>
        {
            openFired = true;
            e.Cancel();
        };

        player.TryOpenContainer(chest, 10, 64, 10);

        Assert.True(openFired);
        Assert.Null(player.Inventory.OpenContainer);
        // Verify ContainerClose packet (ID 47) was sent to client
        Assert.Contains(fakeTransport.Sent, packet =>
        {
            var reader = new BedrockReader(packet);
            var len = reader.VarUInt32();
            return reader.VarUInt32() == InventoryPackets.ContainerCloseId;
        });
    }

    [Fact]
    public void PlayerFoodChange_EditableAndCancellable()
    {
        var (server, player) = CreateTestEnvironment();
        player.Position = (0f, 64f, 0f);
        player.IsInGame = true;

        var foodFired = false;
        server.Events.PlayerFoodChange += e =>
        {
            foodFired = true;
            e.Food = 15;
            e.Saturation = 4f;
        };

        // Lower food then add
        player.SetFood(10, 2);
        player.AddFood(2, 1);

        Assert.True(foodFired);
        Assert.Equal(15, player.Food);
        Assert.Equal(4f, player.Saturation);
    }

    [Fact]
    public void PlayerMove_EditableTo_And_RollbackOnCancel()
    {
        var (server, player) = CreateTestEnvironment();
        player.Position = (0f, 64f, 0f);
        player.IsInGame = true;

        // 1. Rollback on cancel
        void OnMoveCancel(PlayerMoveEvent e) => e.Cancel();
        server.Events.PlayerMove += OnMoveCancel;

        var moveWriter = new BedrockWriter();
        moveWriter.Float32(0); // pitch
        moveWriter.Float32(0); // yaw
        moveWriter.Float32(10f); // x
        moveWriter.Float32(64f); // y
        moveWriter.Float32(10f); // z
        var moveReader = new BedrockReader(moveWriter.ToArray());

        player.OnPlayerAuthInput(ref moveReader);

        // Player remained at (0, 64, 0)
        Assert.Equal(0f, player.Position.X);
        server.Events.PlayerMove -= OnMoveCancel;

        // 2. Editable To position
        server.Events.PlayerMove += e =>
        {
            e.To = new System.Numerics.Vector3(50f, 70f, 50f);
        };

        var moveWriter2 = new BedrockWriter();
        moveWriter2.Float32(0);
        moveWriter2.Float32(0);
        moveWriter2.Float32(10f);
        moveWriter2.Float32(64f);
        moveWriter2.Float32(10f);
        var moveReader2 = new BedrockReader(moveWriter2.ToArray());

        player.OnPlayerAuthInput(ref moveReader2);

        // Player moved to modified destination (50, 70, 50)
        Assert.Equal(50f, player.Position.X);
        Assert.Equal(70f, player.Position.Y);
        Assert.Equal(50f, player.Position.Z);
    }
}

public class LiquidFlowListenerTests
{
    [Fact]
    public void Water_stays_water_when_a_plugin_listens_to_liquid_flow()
    {
        var server = new BedrokiaServer(new ServerConfig { WorldsDirectory = TestWorlds.NewDirectory(), Port = 0, OnlineMode = false },
            new Logger(LogLevel.Error, null));
        server.InitWorldForTesting();
        var seen = 0;
        server.Events.LiquidFlow += _ => seen++;
        var world = server.World;
        const int y = 200; // open air: the water spreads sideways and down
        world.SetBlock(0, y, 0, Bedrokia.Server.Game.Fluids.Source("water"));
        world.Fluids.Wake(0, y, 0);
        for (var i = 0; i < 40; i++)
            server.Tick();
        Assert.True(seen > 0);
        Assert.Equal("minecraft:flowing_water", world.GetBlock(1, y, 0)!.Name); // not a random block
    }
}
