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

using System.Numerics;
using Bedrokia.Protocol;
using Bedrokia.Server;
using Bedrokia.Server.Core;
using Bedrokia.Server.Entities;
using Bedrokia.Server.Events;
using Bedrokia.Server.Game;
using Bedrokia.Server.Game.Custom.Entities;
using Bedrokia.Server.Game.Custom.Items;
using Bedrokia.Server.Network;

namespace Bedrokia.Tests;

/// <summary>
/// Unit tests for custom entity definition, building, registry, NBT serialization,
/// component groups, properties, and live runtime behavior.
/// Source: Bedrock Edition custom actors specification and Dragonfly (MIT).
/// </summary>
public sealed class CustomEntityTests
{
    private sealed class FakeTransport : IGameTransport
    {
        public System.Net.IPEndPoint RemoteEndPoint { get; } = new(System.Net.IPAddress.Loopback, 19132);
        public System.Security.Cryptography.ECDsa? PeerPublicKey => null;
        public List<byte[]> Sent { get; } = [];
        public void Send(byte[] batch) => Sent.Add(batch);
        public void Disconnect(string reason) { }
    }

    public CustomEntityTests()
    {
        CustomEntityRegistry.Reset();
    }

    private static BedrokiaServer CreateTestServer()
    {
        var tempWorlds = TestWorlds.NewDirectory();
        var config = new ServerConfig { WorldsDirectory = tempWorlds, Port = 0, MaxPlayers = 5 };
        var logger = new Logger(LogLevel.Warn, null);
        var server = new BedrokiaServer(config, logger);
        server.InitWorldForTesting();
        return server;
    }

    /// <summary>
    /// Verifies that CustomEntityBuilder correctly initializes default components (health, movement, collision, physics, pushable).
    /// </summary>
    [Fact]
    public void CustomEntityBuilder_HasHealthyDefaults()
    {
        var entity = CustomEntityBuilder.Create("custom:test_mob")
            .WithDisplayName("Test Mob")
            .Build();

        Assert.Equal("custom:test_mob", entity.Identifier);
        Assert.Equal("Test Mob", entity.DisplayName);
        Assert.True(entity.HasComponent(EntityComponentIds.Health));
        Assert.True(entity.HasComponent(EntityComponentIds.Movement));
        Assert.True(entity.HasComponent(EntityComponentIds.CollisionBox));
        Assert.True(entity.HasComponent(EntityComponentIds.Physics));
        Assert.True(entity.HasComponent(EntityComponentIds.Pushable));

        var health = entity.GetComponent<HealthEntityComponent>();
        Assert.NotNull(health);
        Assert.Equal(20, health.Value);
        Assert.Equal(20, health.Max);

        var movement = entity.GetComponent<MovementEntityComponent>();
        Assert.NotNull(movement);
        Assert.Equal(0.25f, movement.Value);

        var box = entity.GetComponent<CollisionBoxEntityComponent>();
        Assert.NotNull(box);
        Assert.Equal(0.6f, box.Width);
        Assert.Equal(1.8f, box.Height);
    }

    /// <summary>
    /// Verifies that identifier normalization automatically prepends "custom:" when no namespace is given.
    /// </summary>
    [Fact]
    public void CustomEntityBuilder_NormalizesIdentifier()
    {
        var entity = CustomEntityBuilder.Create("goblin")
            .Build();

        Assert.Equal("custom:goblin", entity.Identifier);
    }

    /// <summary>
    /// Verifies that preset factories create expected entities with proper components.
    /// </summary>
    [Fact]
    public void CustomEntityBuilder_Presets_CreateExpectedComponents()
    {
        // Passive mob preset
        var passive = CustomEntityBuilder.CreatePassiveMob("sheep_custom", "Custom Sheep", health: 12, speed: 0.22f)
            .Build();
        Assert.Equal(12, passive.GetComponent<HealthEntityComponent>()?.Max);
        Assert.Equal(0.22f, passive.GetComponent<MovementEntityComponent>()?.Value);
        Assert.True(passive.HasComponent(EntityComponentIds.BehaviorFloat));
        Assert.True(passive.HasComponent(EntityComponentIds.BehaviorRandomStroll));
        Assert.True(passive.HasComponent(EntityComponentIds.BehaviorPanic));

        // Hostile mob preset
        var hostile = CustomEntityBuilder.CreateHostileMob("orc_warrior", "Orc Warrior", health: 40, damage: 6f, speed: 0.30f)
            .Build();
        Assert.Equal(40, hostile.GetComponent<HealthEntityComponent>()?.Max);
        Assert.Equal(6f, hostile.GetComponent<AttackEntityComponent>()?.Damage);
        Assert.True(hostile.HasComponent(EntityComponentIds.BehaviorMeleeAttack));
        Assert.True(hostile.HasComponent(EntityComponentIds.BehaviorNearestAttackableTarget));

        // NPC preset
        var npc = CustomEntityBuilder.CreateNpc("village_elder", "Elder")
            .Build();
        Assert.True(npc.HasComponent(EntityComponentIds.Npc));
        Assert.True(npc.HasComponent(EntityComponentIds.Persistent));
        Assert.True(npc.HasComponent(EntityComponentIds.BehaviorLookAtPlayer));

        // Boss preset
        var boss = CustomEntityBuilder.CreateBoss("nether_dragon", "Nether Dragon", health: 300, damage: 12f, scale: 2.0f)
            .Build();
        Assert.Equal(300, boss.GetComponent<HealthEntityComponent>()?.Max);
        Assert.Equal(12f, boss.GetComponent<AttackEntityComponent>()?.Damage);
        Assert.Equal(2.0f, boss.GetComponent<ScaleEntityComponent>()?.Value);
        Assert.True(boss.HasComponent(EntityComponentIds.Boss));
        Assert.Equal("Nether Dragon", boss.GetComponent<BossEntityComponent>()?.Name);
    }

    /// <summary>
    /// Verifies that CustomEntityRegistry registers entities and auto-generates spawn egg item.
    /// </summary>
    [Fact]
    public void CustomEntityRegistry_RegistersAndGeneratesSpawnEgg()
    {
        var entity = CustomEntityBuilder.Create("custom:phoenix")
            .WithDisplayName("Phoenix")
            .WithSpawnEgg(summonable: true, hasSpawnEgg: true)
            .Register();

        Assert.NotNull(CustomEntityRegistry.Get("custom:phoenix"));
        Assert.NotNull(CustomEntityRegistry.Get("phoenix"));
        Assert.True(entity.HasSpawnEgg);

        // Verify spawn egg item was registered in CustomItemRegistry
        var eggItem = CustomItemRegistry.Get("custom:phoenix_spawn_egg");
        Assert.NotNull(eggItem);
        Assert.Equal("Spawn Phoenix", eggItem.DisplayName);
    }

    /// <summary>
    /// Verifies that AvailableActorIdentifiers NBT structure strictly contains 'idlist' with required keys:
    /// id, bid, rid, summonable, hasspawnegg, experimental.
    /// </summary>
    [Fact]
    public void CustomEntityRegistry_GeneratesValidIdentifiersNbt()
    {
        CustomEntityBuilder.Create("custom:shadow_knight")
            .WithDisplayName("Shadow Knight")
            .WithRuntimeIdentifier("minecraft:zombie")
            .WithSpawnEgg(summonable: true, hasSpawnEgg: true)
            .Register();

        var nbt = CustomEntityRegistry.GetIdentifiersNbt();
        Assert.True(nbt.ContainsKey("idlist"));

        var idList = nbt["idlist"] as NbtList;
        Assert.NotNull(idList);
        Assert.NotEmpty(idList.Values);

        var first = idList.Values[0] as OrderedDictionary<string, object>;
        Assert.NotNull(first);
        Assert.Equal("custom:shadow_knight", first["id"]);
        Assert.Equal("custom:shadow_knight", first["bid"]);
        Assert.Equal("minecraft:zombie", first["rid"]);
        Assert.Equal((byte)1, first["summonable"]);
        Assert.Equal((byte)1, first["hasspawnegg"]);
        Assert.Equal((byte)0, first["experimental"]);
    }

    /// <summary>
    /// Verifies that entity properties (int, float, bool, enum) are correctly serialized into SyncProperties NBT.
    /// </summary>
    [Fact]
    public void CustomEntity_Properties_SerializeCorrectly()
    {
        var entity = CustomEntityBuilder.Create("custom:elemental")
            .WithIntProperty("custom:stage", min: 1, max: 3, defaultValue: 1, clientSync: true)
            .WithFloatProperty("custom:rage", min: 0f, max: 100f, defaultValue: 0f, clientSync: true)
            .WithBoolProperty("custom:shielded", defaultValue: false, clientSync: true)
            .WithEnumProperty("custom:element", ["fire", "water", "earth", "air"], defaultValue: "fire", clientSync: true)
            .Register();

        Assert.Equal(4, entity.Properties.Count);

        var syncNbt = CustomEntityRegistry.GetSyncPropertiesNbt();
        Assert.NotNull(syncNbt);
        Assert.True(syncNbt.ContainsKey("custom:elemental"));

        var entry = syncNbt["custom:elemental"] as OrderedDictionary<string, object>;
        Assert.NotNull(entry);
        Assert.True(entry.ContainsKey("properties"));

        var propsDict = entry["properties"] as OrderedDictionary<string, object>;
        Assert.NotNull(propsDict);
        Assert.True(propsDict.ContainsKey("custom:stage"));
        Assert.True(propsDict.ContainsKey("custom:rage"));
        Assert.True(propsDict.ContainsKey("custom:shielded"));
        Assert.True(propsDict.ContainsKey("custom:element"));
    }

    /// <summary>
    /// Verifies that component groups can be defined, serialized, and dynamically added/removed on runtime instances.
    /// </summary>
    [Fact]
    public void CustomEntity_ComponentGroups_ApplyDynamically()
    {
        CustomEntityBuilder.Create("custom:golem")
            .WithHealth(50, 50)
            .WithMovement(0.2f)
            .WithComponentGroup("enraged",
                new HealthEntityComponent(80, 80),
                new MovementEntityComponent(0.45f),
                new ScaleEntityComponent(1.5f),
                new AttackEntityComponent(15f))
            .Register();

        var server = CreateTestServer();
        var world = server.World;

        var instance = server.CustomEntities.Spawn("custom:golem", world, 0, 64, 0);
        Assert.NotNull(instance);
        Assert.Equal(50, instance.Health);
        Assert.Equal(50, instance.MaxHealth);
        Assert.Equal(0.2f, instance.Speed);
        Assert.Equal(1.0f, instance.Scale);

        // Dynamically apply component group "enraged"
        instance.AddComponentGroup("enraged");
        Assert.Contains("enraged", instance.ActiveComponentGroups);
        Assert.Equal(80, instance.MaxHealth);
        Assert.Equal(0.45f, instance.Speed);
        Assert.Equal(1.5f, instance.Scale);
        Assert.Equal(15f, instance.AttackDamage);

        // Dynamically remove component group "enraged" (restoring base values)
        instance.RemoveComponentGroup("enraged");
        Assert.DoesNotContain("enraged", instance.ActiveComponentGroups);
        Assert.Equal(50, instance.MaxHealth);
        Assert.Equal(0.2f, instance.Speed);
        Assert.Equal(1.0f, instance.Scale);
    }

    /// <summary>
    /// Verifies runtime custom entity spawning, damage taking, knockback, and death lifecycle.
    /// </summary>
    [Fact]
    public void CustomEntityInstance_Spawning_Combat_And_Death()
    {
        var hurtCount = 0;
        var deathCount = 0;

        CustomEntityBuilder.CreateHostileMob("custom:skeleton_king", "Skeleton King", health: 30, damage: 5f)
            .OnHurt((ent, src) => hurtCount++)
            .OnDeath(ent => deathCount++)
            .Register();

        var server = CreateTestServer();
        var world = server.World;

        var instance = server.CustomEntities.Spawn("custom:skeleton_king", world, 10, 65, 10);
        Assert.NotNull(instance);
        Assert.Equal(30, instance.Health);
        Assert.False(instance.Dead);

        // Damage the entity
        var damaged = instance.Damage(10, (0, 0), source: DamageSource.Custom());
        Assert.True(damaged);
        Assert.Equal(20, instance.Health);
        Assert.Equal(1, hurtCount);
        Assert.False(instance.Dead);

        // Lethal damage (reset hurt ticks to allow subsequent hit)
        instance.HurtTicks = 0;
        var lethal = instance.Damage(25, (0, 0), source: DamageSource.Custom());
        Assert.True(lethal);
        Assert.Equal(0, instance.Health);
        Assert.True(instance.Dead);
        Assert.Equal(1, deathCount);
    }

    /// <summary>
    /// Verifies mount and rider mechanics including SetActorLink packet and dismount.
    /// </summary>
    [Fact]
    public void CustomEntityInstance_Mount_And_Dismount()
    {
        CustomEntityBuilder.Create("custom:warhorse")
            .WithRideable(seatCount: 1, inputGroundControlled: true)
            .Register();

        var server = CreateTestServer();
        var world = server.World;

        var instance = server.CustomEntities.Spawn("custom:warhorse", world, 0, 64, 0);
        Assert.NotNull(instance);

        var player = new BedrockConnection(server, new FakeTransport(), 100);
        player.ChangeDimension(world, 0, 64, 0);

        Assert.True(instance.Mount(player));
        Assert.Equal(player, instance.Rider);

        // Cannot mount when already ridden
        var otherPlayer = new BedrockConnection(server, new FakeTransport(), 101);
        otherPlayer.ChangeDimension(world, 0, 64, 0);
        Assert.False(instance.Mount(otherPlayer));

        // Dismount
        instance.Dismount();
        Assert.Null(instance.Rider);
    }

    /// <summary>
    /// Verifies entity properties get/set and client sync behavior.
    /// </summary>
    [Fact]
    public void CustomEntityInstance_PropertySync_WorksCorrectly()
    {
        CustomEntityBuilder.Create("custom:crystal_golem")
            .WithIntProperty("custom:state", min: 0, max: 5, defaultValue: 0, clientSync: true)
            .WithFloatProperty("custom:charge", min: 0f, max: 1f, defaultValue: 0f, clientSync: true)
            .Register();

        var server = CreateTestServer();
        var world = server.World;

        var instance = server.CustomEntities.Spawn("custom:crystal_golem", world, 0, 64, 0);
        Assert.NotNull(instance);

        Assert.Equal(0, instance.GetProperty<int>("custom:state"));
        Assert.Equal(0f, instance.GetProperty<float>("custom:charge"));

        instance.SetProperty("custom:state", 3);
        instance.SetProperty("custom:charge", 0.75f);

        Assert.Equal(3, instance.GetProperty<int>("custom:state"));
        Assert.Equal(0.75f, instance.GetProperty<float>("custom:charge"));
    }

    /// <summary>
    /// Verifies protocol packet encoders: AvailableActorIdentifiers, SyncActorProperty, SetActorLink, MobArmorEquipment, and BossEvent.
    /// </summary>
    [Fact]
    public void CustomEntity_ProtocolPackets_EncodeCorrectly()
    {
        // AvailableActorIdentifiers (Packet ID 119)
        var identNbt = new OrderedDictionary<string, object>
        {
            ["idlist"] = new NbtList(Nbt.Compound, [])
        };
        var identPacket = Packets.AvailableActorIdentifiers(identNbt);
        Assert.NotNull(identPacket);
        var identReader = new BedrockReader(identPacket);
        Assert.Equal(PacketId.AvailableActorIdentifiers, identReader.VarUInt32());

        // SyncActorProperty (Packet ID 165)
        var syncNbt = new OrderedDictionary<string, object>
        {
            ["properties"] = new NbtList(Nbt.Compound, [])
        };
        var syncPacket = Packets.SyncActorProperty(syncNbt);
        Assert.NotNull(syncPacket);
        var syncReader = new BedrockReader(syncPacket);
        Assert.Equal(PacketId.SyncActorProperty, syncReader.VarUInt32());

        // SetActorLink (Packet ID 41)
        var linkPacket = Packets.SetActorLink(vehicleId: 10, riderId: 20, linkType: 1, immediate: true, riderInitiated: true);
        Assert.NotNull(linkPacket);
        var linkReader = new BedrockReader(linkPacket);
        Assert.Equal(PacketId.SetActorLink, linkReader.VarUInt32());
        Assert.Equal(10, linkReader.VarInt64());
        Assert.Equal(20, linkReader.VarInt64());
        Assert.Equal(1, linkReader.Byte());
        Assert.True(linkReader.Bool());
        Assert.True(linkReader.Bool());

        // MobArmourEquipment (Packet ID 32)
        var armorPacket = InventoryPackets.MobArmorEquipment(entityId: 50, helmet: ItemStack.Of("diamond_helmet"));
        Assert.NotNull(armorPacket);
        var armorReader = new BedrockReader(armorPacket);
        Assert.Equal(PacketId.MobArmourEquipment, armorReader.VarUInt32());
        Assert.Equal(50UL, armorReader.VarUInt64());

        // BossEvent (Packet ID 74)
        var bossShow = Packets.BossEventShow(bossEntityId: 99, title: "Giant Slime", healthPercent: 0.8f);
        Assert.NotNull(bossShow);
        var bossReader = new BedrockReader(bossShow);
        Assert.Equal(PacketId.BossEvent, bossReader.VarUInt32());
        Assert.Equal(99, bossReader.VarInt64());
        Assert.Equal(0U, bossReader.VarUInt32()); // EventType 0 = Show
    }
}
