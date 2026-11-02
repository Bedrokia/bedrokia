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

/// <summary>
/// Unit tests for all newly implemented Bedrokia events and hooks.
/// </summary>
public sealed class NewEventSystemTests
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
        var transport = new FakeTransport();
        var conn = new BedrockConnection(server, transport, 100);
        return (server, conn);
    }

    /// <summary>Tests server lifecycle events ServerStartedEvent and ServerStopEvent.</summary>
    [Fact]
    public void ServerLifecycleEvents_FireProperly()
    {
        var (server, _) = CreateTestEnvironment();
        var startedFired = false;
        var stopFired = false;

        server.Events.ServerStarted += e =>
        {
            startedFired = true;
            Assert.Equal(server, e.Server);
        };
        server.Events.ServerStop += e =>
        {
            stopFired = true;
            Assert.Equal(server, e.Server);
        };

        server.Events.OnServerStarted(new ServerStartedEvent(server));
        server.Events.OnServerStop(new ServerStopEvent(server));

        Assert.True(startedFired);
        Assert.True(stopFired);
    }

    /// <summary>Tests ConsoleCommandEvent execution and cancellation.</summary>
    [Fact]
    public void ConsoleCommandEvent_CancellableAndModifiable()
    {
        var (server, _) = CreateTestEnvironment();
        var fired = false;

        server.Events.ConsoleCommand += e =>
        {
            fired = true;
            Assert.Equal("help", e.CommandLine);
            e.CommandLine = "version";
            e.Cancel();
        };

        var ev = new ConsoleCommandEvent("help");
        var proceed = server.Events.OnConsoleCommand(ev);

        Assert.True(fired);
        Assert.False(proceed);
        Assert.Equal("version", ev.CommandLine);
    }

    /// <summary>Tests DataPacketReceiveEvent and DataPacketSendEvent with player uniform accessor.</summary>
    [Fact]
    public void DataPacketEvents_UniformPlayerAndCancellable()
    {
        var (server, player) = CreateTestEnvironment();
        var rcvFired = false;
        var sendFired = false;

        server.Events.DataPacketReceive += e =>
        {
            rcvFired = true;
            Assert.Equal(player, e.player());
            Assert.Equal((uint)1, e.PacketId);
            e.Cancel();
        };

        server.Events.DataPacketSend += e =>
        {
            sendFired = true;
            Assert.Equal(player, e.player());
            Assert.Equal((uint)2, e.PacketId);
            e.Cancel();
        };

        var rcvEv = new DataPacketReceiveEvent(player, 1, [0x01, 0x02]);
        var sendEv = new DataPacketSendEvent(player, 2, [0x03, 0x04]);

        Assert.False(server.Events.OnDataPacketReceive(rcvEv));
        Assert.False(server.Events.OnDataPacketSend(sendEv));
        Assert.True(rcvFired);
        Assert.True(sendFired);
    }

    /// <summary>Tests PlayerPreLoginEvent cancellation and kick reason.</summary>
    [Fact]
    public void PlayerPreLoginEvent_CanBeCancelled()
    {
        var (server, player) = CreateTestEnvironment();
        var fired = false;

        server.Events.PlayerPreLogin += e =>
        {
            fired = true;
            Assert.Equal(player, e.player());
            e.DisconnectReason = "Server is under maintenance";
            e.Cancel();
        };

        var ev = new PlayerPreLoginEvent(player, "Server is full");
        Assert.False(server.Events.OnPlayerPreLogin(ev));
        Assert.True(fired);
        Assert.Equal("Server is under maintenance", ev.DisconnectReason);
    }

    /// <summary>Tests PlayerKickEvent cancellation and reason modification.</summary>
    [Fact]
    public void PlayerKickEvent_CanBeCancelledAndModified()
    {
        var (server, player) = CreateTestEnvironment();
        var fired = false;

        server.Events.PlayerKick += e =>
        {
            fired = true;
            Assert.Equal(player, e.player());
            e.Reason = "Custom kick reason";
            e.Cancel();
        };

        var ev = new PlayerKickEvent(player, "Default kick reason");
        Assert.False(server.Events.OnPlayerKick(ev));
        Assert.True(fired);
        Assert.Equal("Custom kick reason", ev.Reason);
    }

    /// <summary>Tests bed events: PlayerBedEnterEvent, PlayerBedLeaveEvent, and PlayerSleepSkipNightEvent.</summary>
    [Fact]
    public void BedEvents_UniformPlayerAndCancellable()
    {
        var (server, player) = CreateTestEnvironment();
        var enterFired = false;
        var leaveFired = false;
        var skipFired = false;

        server.Events.PlayerBedEnter += e =>
        {
            enterFired = true;
            Assert.Equal(player, e.player());
            Assert.Equal(10, e.X);
            e.Cancel();
        };

        server.Events.PlayerBedLeave += e =>
        {
            leaveFired = true;
            Assert.Equal(player, e.player());
        };

        server.Events.PlayerSleepSkipNight += e =>
        {
            skipFired = true;
            Assert.Equal(player, e.player());
            Assert.Contains(player, e.SleepingPlayers);
        };

        var enterEv = new PlayerBedEnterEvent(player, 10, 64, 20);
        Assert.False(server.Events.OnPlayerBedEnter(enterEv));
        Assert.True(enterFired);

        var leaveEv = new PlayerBedLeaveEvent(player);
        server.Events.OnPlayerBedLeave(leaveEv);
        Assert.True(leaveFired);

        var skipEv = new PlayerSleepSkipNightEvent(player);
        server.Events.OnPlayerSleepSkipNight(skipEv);
        Assert.True(skipFired);
    }

    /// <summary>Tests PlayerToggleFlightEvent, PlayerToggleCrawlEvent, and snake_case hooks.</summary>
    [Fact]
    public void PlayerMovementToggleEvents_AndSnakeCaseHooks()
    {
        var (server, player) = CreateTestEnvironment();
        var flyFired = false;
        var crawlFired = false;

        server.Events.on_player_fly_toggle(e =>
        {
            flyFired = true;
            Assert.Equal(player, e.player());
            Assert.True(e.IsFlying);
            e.Cancel();
        });

        server.Events.on_player_crawl(e =>
        {
            crawlFired = true;
            Assert.Equal(player, e.player());
            Assert.True(e.IsCrawling);
            e.Cancel();
        });

        var flyEv = new PlayerToggleFlightEvent(player, true);
        Assert.False(server.Events.OnPlayerToggleFlight(flyEv));
        Assert.True(flyFired);

        var crawlEv = new PlayerToggleCrawlEvent(player, true);
        Assert.False(server.Events.OnPlayerToggleCrawl(crawlEv));
        Assert.True(crawlFired);
    }

    /// <summary>Tests PlayerAttackEvent with snake_case hook on_player_attack.</summary>
    [Fact]
    public void PlayerAttackEvent_CanBeCancelledAndModified()
    {
        var (server, player) = CreateTestEnvironment();
        var (_, targetPlayer) = CreateTestEnvironment();
        var fired = false;

        server.Events.on_player_attack(e =>
        {
            fired = true;
            Assert.Equal(player, e.player());
            Assert.Equal(targetPlayer.EntityId, e.TargetEntityId);
            e.Damage = 15f;
            e.Cancel();
        });

        var ev = new PlayerAttackEvent(player, targetPlayer.EntityId, 5f);
        Assert.False(server.Events.OnPlayerAttack(ev));
        Assert.True(fired);
        Assert.Equal(15f, ev.Damage);
    }

    /// <summary>Tests PlayerFallEvent with snake_case hook on_player_fall.</summary>
    [Fact]
    public void PlayerFallEvent_CanBeCancelledAndModified()
    {
        var (server, player) = CreateTestEnvironment();
        var fired = false;

        server.Events.on_player_fall(e =>
        {
            fired = true;
            Assert.Equal(player, e.player());
            Assert.Equal(10f, e.FallDistance);
            Assert.Equal(10f, e.Distance);
            e.Damage = 0f;
            e.Cancel();
        });

        var ev = new PlayerFallEvent(player, 10f, 7f);
        Assert.False(server.Events.OnPlayerFall(ev));
        Assert.True(fired);
        Assert.Equal(0f, ev.Damage);
    }

    /// <summary>Tests PlayerWorldChangeEvent and PlayerChunkEnterEvent with snake_case hooks.</summary>
    [Fact]
    public void PlayerWorldAndChunkEvents_FireProperly()
    {
        var (server, player) = CreateTestEnvironment();
        var worldFired = false;
        var chunkFired = false;

        server.Events.on_player_world_change(e =>
        {
            worldFired = true;
            Assert.Equal(player, e.player());
            Assert.Equal(server.World, e.ToWorld);
        });

        server.Events.on_player_chunk_enter(e =>
        {
            chunkFired = true;
            Assert.Equal(player, e.player());
            Assert.Equal(3, e.ChunkX);
            Assert.Equal(5, e.ChunkZ);
        });

        server.Events.OnPlayerWorldChange(new PlayerWorldChangeEvent(player, server.World, server.World));
        server.Events.OnPlayerChunkEnter(new PlayerChunkEnterEvent(player, 3, 5, 0, 0));

        Assert.True(worldFired);
        Assert.True(chunkFired);
    }

    /// <summary>Tests PlayerAfkEvent with snake_case hook on_player_afk.</summary>
    [Fact]
    public void PlayerAfkEvent_CanBeCancelled()
    {
        var (server, player) = CreateTestEnvironment();
        var fired = false;

        server.Events.on_player_afk(e =>
        {
            fired = true;
            Assert.Equal(player, e.player());
            Assert.True(e.NewState);
            Assert.True(e.IsAfk);
            e.Cancel();
        });

        var ev = new PlayerAfkEvent(player, true);
        Assert.False(server.Events.OnPlayerAfk(ev));
        Assert.True(fired);
    }

    /// <summary>Tests PlayerMissSwingEvent with snake_case hook on_player_swing_miss.</summary>
    [Fact]
    public void PlayerMissSwingEvent_CanBeCancelled()
    {
        var (server, player) = CreateTestEnvironment();
        var fired = false;

        server.Events.on_player_swing_miss(e =>
        {
            fired = true;
            Assert.Equal(player, e.player());
            e.Cancel();
        });

        var ev = new PlayerMissSwingEvent(player);
        Assert.False(server.Events.OnPlayerMissSwing(ev));
        Assert.True(fired);
    }

    /// <summary>Tests ResourcePackClientResponseEvent with hook on_resource_pack_status.</summary>
    [Fact]
    public void ResourcePackStatus_FiresProperly()
    {
        var (server, player) = CreateTestEnvironment();
        var fired = false;

        server.Events.on_resource_pack_status(e =>
        {
            fired = true;
            Assert.Equal(player, e.player());
            Assert.Equal(3, e.ResponseStatus);
            Assert.Contains("pack_a", e.PackIds);
        });

        server.Events.OnResourcePackClientResponse(new ResourcePackClientResponseEvent(player, 3, ["pack_a"]));
        Assert.True(fired);
    }

    /// <summary>Tests PlayerViewDistanceChangeEvent modification.</summary>
    [Fact]
    public void PlayerViewDistanceChangeEvent_Modifiable()
    {
        var (server, player) = CreateTestEnvironment();
        var fired = false;

        server.Events.PlayerViewDistanceChange += e =>
        {
            fired = true;
            Assert.Equal(player, e.player());
            Assert.Equal(8, e.NewDistance);
            e.NewDistance = 12;
        };

        var ev = new PlayerViewDistanceChangeEvent(player, 8);
        server.Events.OnPlayerViewDistanceChange(ev);
        Assert.True(fired);
        Assert.Equal(12, ev.NewDistance);
    }

    /// <summary>Tests PlayerGameModeChangeEvent cancellation and modification.</summary>
    [Fact]
    public void PlayerGameModeChangeEvent_CanBeCancelledAndModified()
    {
        var (server, player) = CreateTestEnvironment();
        var fired = false;

        server.Events.PlayerGameModeChange += e =>
        {
            fired = true;
            Assert.Equal(player, e.player());
            Assert.Equal(GameMode.Survival, e.OldGameMode);
            Assert.Equal(GameMode.Creative, e.NewGameMode);
            e.NewGameMode = GameMode.Adventure;
            e.Cancel();
        };

        var ev = new PlayerGameModeChangeEvent(player, GameMode.Survival, GameMode.Creative);
        Assert.False(server.Events.OnPlayerGameModeChange(ev));
        Assert.True(fired);
        Assert.Equal(GameMode.Adventure, ev.NewGameMode);
    }

    /// <summary>Tests PlayerChangeSkinEvent and PlayerEmoteEvent cancellation.</summary>
    [Fact]
    public void SkinAndEmoteEvents_CanBeCancelled()
    {
        var (server, player) = CreateTestEnvironment();
        var skinFired = false;
        var emoteFired = false;

        server.Events.PlayerChangeSkin += e =>
        {
            skinFired = true;
            Assert.Equal(player, e.player());
            Assert.Equal(2, e.SkinData.Length);
            e.Cancel();
        };

        server.Events.PlayerEmote += e =>
        {
            emoteFired = true;
            Assert.Equal(player, e.player());
            Assert.Equal("emote_piece", e.EmoteId);
            e.Cancel();
        };

        var skinEv = new PlayerChangeSkinEvent(player, [0x01, 0x02]);
        var emoteEv = new PlayerEmoteEvent(player, "emote_piece", 0);

        Assert.False(server.Events.OnPlayerChangeSkin(skinEv));
        Assert.False(server.Events.OnPlayerEmote(emoteEv));
        Assert.True(skinFired);
        Assert.True(emoteFired);
    }

    /// <summary>Tests PlayerTransferEvent cancellation and modification.</summary>
    [Fact]
    public void PlayerTransferEvent_CanBeCancelledAndModified()
    {
        var (server, player) = CreateTestEnvironment();
        var fired = false;

        server.Events.PlayerTransfer += e =>
        {
            fired = true;
            Assert.Equal(player, e.player());
            e.Address = "hub.bedrokia.net";
            e.Port = 19133;
            e.Cancel();
        };

        var ev = new PlayerTransferEvent(player, "old.server.com", 19132);
        Assert.False(server.Events.OnPlayerTransfer(ev));
        Assert.True(fired);
        Assert.Equal("hub.bedrokia.net", ev.Address);
        Assert.Equal((ushort)19133, ev.Port);
    }

    /// <summary>Tests PlayerFormResponseEvent cancellation and payload inspection.</summary>
    [Fact]
    public void PlayerFormResponseEvent_CanBeCancelled()
    {
        var (server, player) = CreateTestEnvironment();
        var fired = false;

        server.Events.PlayerFormResponse += e =>
        {
            fired = true;
            Assert.Equal(player, e.player());
            Assert.Equal((uint)42, e.FormId);
            Assert.Equal("{\"test\":1}", e.ResponseJson);
            e.Cancel();
        };

        var ev = new PlayerFormResponseEvent(player, 42, "{\"test\":1}", false, null);
        Assert.False(server.Events.OnPlayerFormResponse(ev));
        Assert.True(fired);
    }

    /// <summary>Tests PlayerInputModeChangeEvent and PlayerExhaustEvent.</summary>
    [Fact]
    public void InputModeAndExhaustEvents_FireProperly()
    {
        var (server, player) = CreateTestEnvironment();
        var inputFired = false;
        var exhaustFired = false;

        server.Events.PlayerInputModeChange += e =>
        {
            inputFired = true;
            Assert.Equal(player, e.player());
            Assert.Equal((uint)0, e.OldMode);
            Assert.Equal((uint)1, e.NewMode);
        };

        server.Events.PlayerExhaust += e =>
        {
            exhaustFired = true;
            Assert.Equal(player, e.player());
            Assert.Equal(0.05f, e.Amount);
            e.Amount = 0.1f;
            e.Cancel();
        };

        server.Events.OnPlayerInputModeChange(new PlayerInputModeChangeEvent(player, 0, 1));
        var exhaustEv = new PlayerExhaustEvent(player, 0.05f);
        Assert.False(server.Events.OnPlayerExhaust(exhaustEv));

        Assert.True(inputFired);
        Assert.True(exhaustFired);
        Assert.Equal(0.1f, exhaustEv.Amount);
    }

    /// <summary>Tests PlayerArmorChangeEvent slot and item modification.</summary>
    [Fact]
    public void PlayerArmorChangeEvent_CanBeCancelledAndModified()
    {
        var (server, player) = CreateTestEnvironment();
        var helmet = ItemStack.Of("diamond_helmet");
        var chest = ItemStack.Of("netherite_chestplate");
        var fired = false;

        server.Events.PlayerArmorChange += e =>
        {
            fired = true;
            Assert.Equal(player, e.player());
            Assert.Equal(0, e.Slot);
            Assert.Equal(helmet, e.NewItem);
            e.NewItem = chest;
            e.Cancel();
        };

        var ev = new PlayerArmorChangeEvent(player, 0, null, helmet);
        Assert.False(server.Events.OnPlayerArmorChange(ev));
        Assert.True(fired);
        Assert.Equal(chest, ev.NewItem);
    }

    /// <summary>Tests PlayerBucketFillEvent and PlayerBucketEmptyEvent cancellation.</summary>
    [Fact]
    public void BucketEvents_CanBeCancelled()
    {
        var (server, player) = CreateTestEnvironment();
        var fillFired = false;
        var emptyFired = false;

        server.Events.PlayerBucketFill += e =>
        {
            fillFired = true;
            Assert.Equal(player, e.player());
            Assert.Equal(5, e.X);
            e.Cancel();
        };

        server.Events.PlayerBucketEmpty += e =>
        {
            emptyFired = true;
            Assert.Equal(player, e.player());
            Assert.Equal("water_bucket", e.BucketItem);
            e.Cancel();
        };

        var fillEv = new PlayerBucketFillEvent(player, 5, 60, 5, Blocks.Water);
        var emptyEv = new PlayerBucketEmptyEvent(player, 5, 60, 5, "water_bucket");

        Assert.False(server.Events.OnPlayerBucketFill(fillEv));
        Assert.False(server.Events.OnPlayerBucketEmpty(emptyEv));
        Assert.True(fillFired);
        Assert.True(emptyFired);
    }

    /// <summary>Tests PlayerDimensionChangeEvent cancellation.</summary>
    [Fact]
    public void PlayerDimensionChangeEvent_CanBeCancelled()
    {
        var (server, player) = CreateTestEnvironment();
        var fired = false;

        server.Events.PlayerDimensionChange += e =>
        {
            fired = true;
            Assert.Equal(player, e.player());
            Assert.Equal(0, e.FromDimension);
            Assert.Equal(1, e.ToDimension);
            e.Cancel();
        };

        var ev = new PlayerDimensionChangeEvent(player, 0, 1);
        Assert.False(server.Events.OnPlayerDimensionChange(ev));
        Assert.True(fired);
    }

    /// <summary>Tests World and Chunk lifecycle events.</summary>
    [Fact]
    public void WorldAndChunkEvents_FireProperly()
    {
        var (server, _) = CreateTestEnvironment();
        var worldLoadFired = false;
        var worldSaveFired = false;
        var spawnChangeFired = false;
        var chunkLoadFired = false;
        var chunkPopFired = false;
        var timeChangeFired = false;

        server.Events.WorldLoad += e => { worldLoadFired = true; Assert.Equal(server.World, e.World); };
        server.Events.WorldSave += e => { worldSaveFired = true; Assert.Equal(server.World, e.World); };
        server.Events.SpawnChange += e => { spawnChangeFired = true; Assert.Equal(100, e.NewSpawn.X); e.Cancel(); };
        server.Events.ChunkLoad += e => { chunkLoadFired = true; Assert.Equal(1, e.Chunk.X); };
        server.Events.ChunkPopulate += e => { chunkPopFired = true; Assert.Equal(2, e.Chunk.Z); };
        server.Events.TimeChange += e => { timeChangeFired = true; Assert.Equal(1000L, e.NewTime); e.Cancel(); };

        server.Events.OnWorldLoad(new WorldLoadEvent(server.World));
        server.Events.OnWorldSave(new WorldSaveEvent(server.World));
        var spawnEv = new SpawnChangeEvent(server.World, (0, 64, 0), (100, 70, 100));
        Assert.False(server.Events.OnSpawnChange(spawnEv));
        var chunk = new Chunk(1, 2);
        server.Events.OnChunkLoad(new ChunkLoadEvent(server.World, chunk));
        server.Events.OnChunkPopulate(new ChunkPopulateEvent(server.World, chunk));
        var timeEv = new TimeChangeEvent(server.World, 0, 1000L);
        Assert.False(server.Events.OnTimeChange(timeEv));

        Assert.True(worldLoadFired);
        Assert.True(worldSaveFired);
        Assert.True(spawnChangeFired);
        Assert.True(chunkLoadFired);
        Assert.True(chunkPopFired);
        Assert.True(timeChangeFired);
    }

    /// <summary>Tests StonecutterUseEvent, LeavesDecayEvent, and MoistureChangeEvent.</summary>
    [Fact]
    public void BlockAndCraftEvents_CanBeCancelled()
    {
        var (server, player) = CreateTestEnvironment();
        var stone = ItemStack.Of("stone");
        var slab = ItemStack.Of("stone", 2);
        Assert.NotNull(stone);
        Assert.NotNull(slab);

        var scFired = false;
        var leafFired = false;
        var moistFired = false;

        server.Events.StonecutterUse += e =>
        {
            scFired = true;
            Assert.Equal(player, e.player());
            Assert.Equal(stone, e.Input);
            e.Cancel();
        };

        server.Events.LeavesDecay += e =>
        {
            leafFired = true;
            Assert.Equal(10, e.X);
            e.Cancel();
        };

        server.Events.MoistureChange += e =>
        {
            moistFired = true;
            Assert.Equal(0, e.OldMoisture);
            Assert.Equal(7, e.NewMoisture);
            e.Cancel();
        };

        var scEv = new StonecutterUseEvent(player, stone, slab);
        Assert.False(server.Events.OnStonecutterUse(scEv));
        var leafEv = new LeavesDecayEvent(server.World, 10, 70, 10, Blocks.OakLeaves);
        Assert.False(server.Events.OnLeavesDecay(leafEv));
        var moistEv = new MoistureChangeEvent(server.World, 10, 64, 10, 0, 7);
        Assert.False(server.Events.OnMoistureChange(moistEv));

        Assert.True(scFired);
        Assert.True(leafFired);
        Assert.True(moistFired);
    }

    /// <summary>Tests Vehicle lifecycle events (Create, Destroy, Enter, Exit, Damage).</summary>
    [Fact]
    public void VehicleEvents_CanBeCancelledAndExposePlayer()
    {
        var (server, player) = CreateTestEnvironment();
        var vehicle = new Vehicle(1, "minecraft:boat", "oak_boat", server.World, 0, 64, 0);

        var createFired = false;
        var destroyFired = false;
        var enterFired = false;
        var exitFired = false;
        var damageFired = false;

        server.Events.VehicleCreate += e => { createFired = true; Assert.Equal(vehicle, e.Vehicle); e.Cancel(); };
        server.Events.VehicleDestroy += e => { destroyFired = true; Assert.Equal(vehicle, e.Vehicle); e.Cancel(); };
        server.Events.VehicleEnter += e => { enterFired = true; Assert.Equal(player, e.player()); e.Cancel(); };
        server.Events.VehicleExit += e => { exitFired = true; Assert.Equal(player, e.player()); e.Cancel(); };
        server.Events.VehicleDamage += e => { damageFired = true; Assert.Equal(player, e.player()); Assert.Equal(5f, e.Damage); e.Cancel(); };

        server.Events.OnVehicleCreate(new VehicleCreateEvent(vehicle));
        server.Events.OnVehicleDestroy(new VehicleDestroyEvent(vehicle));
        Assert.False(server.Events.OnVehicleEnter(new VehicleEnterEvent(vehicle, player)));
        Assert.False(server.Events.OnVehicleExit(new VehicleExitEvent(vehicle, player)));
        Assert.False(server.Events.OnVehicleDamage(new VehicleDamageEvent(vehicle, player, 5f)));

        Assert.True(createFired);
        Assert.True(destroyFired);
        Assert.True(enterFired);
        Assert.True(exitFired);
        Assert.True(damageFired);
    }

    /// <summary>Tests SheepShearEvent, SheepRegrowWoolEvent, and FireworkExplodeEvent.</summary>
    [Fact]
    public void EntityAndMobEvents_CanBeCancelled()
    {
        var (server, player) = CreateTestEnvironment();
        var mob = new Mob(1, MobKind.Sheep, 0, 64, 0) { World = server.World };

        var shearFired = false;
        var regrowFired = false;
        var fireworkFired = false;

        server.Events.SheepShear += e => { shearFired = true; Assert.Equal(player, e.player()); Assert.Equal(mob, e.Sheep); e.Cancel(); };
        server.Events.SheepRegrowWool += e => { regrowFired = true; Assert.Equal(mob, e.Sheep); e.Cancel(); };
        server.Events.FireworkExplode += e => { fireworkFired = true; Assert.Equal(100.0, e.X); e.Cancel(); };

        Assert.False(server.Events.OnSheepShear(new SheepShearEvent(player, mob)));
        Assert.False(server.Events.OnSheepRegrowWool(new SheepRegrowWoolEvent(mob)));
        Assert.False(server.Events.OnFireworkExplode(new FireworkExplodeEvent(server.World, 100.0, 70.0, 100.0)));

        Assert.True(shearFired);
        Assert.True(regrowFired);
        Assert.True(fireworkFired);
    }
}

