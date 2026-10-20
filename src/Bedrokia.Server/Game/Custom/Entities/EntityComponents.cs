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
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Game.Custom.Entities;

// --- Attributs & Métriques de Base ---

/// <summary>Defines base and maximum health attributes (<c>minecraft:health</c>).</summary>
public sealed record HealthEntityComponent(int Value = 20, int Max = 20) : IEntityComponent
{
    public string Name => EntityComponentIds.Health;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["value"] = Value,
        ["max"] = Max
    };
}

/// <summary>Defines base walking movement speed (<c>minecraft:movement</c>).</summary>
public sealed record MovementEntityComponent(float Value = 0.25f) : IEntityComponent
{
    public string Name => EntityComponentIds.Movement;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["value"] = Value
    };
}

/// <summary>Defines flying movement speed (<c>minecraft:flying_speed</c>).</summary>
public sealed record FlyingSpeedEntityComponent(float Value = 0.4f) : IEntityComponent
{
    public string Name => EntityComponentIds.FlyingSpeed;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["value"] = Value
    };
}

/// <summary>Defines underwater swimming speed (<c>minecraft:underwater_movement</c>).</summary>
public sealed record UnderwaterMovementEntityComponent(float Value = 0.02f) : IEntityComponent
{
    public string Name => EntityComponentIds.UnderwaterMovement;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["value"] = Value
    };
}

/// <summary>Defines knockback resistance ratio from 0.0 (full knockback) to 1.0 (no knockback) (<c>minecraft:knockback_resistance</c>).</summary>
public sealed record KnockbackResistanceEntityComponent(float Value = 0.0f) : IEntityComponent
{
    public string Name => EntityComponentIds.KnockbackResistance;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["value"] = Value
    };
}

/// <summary>Defines rendering and collision scale multiplier (<c>minecraft:scale</c>).</summary>
public sealed record ScaleEntityComponent(float Value = 1.0f) : IEntityComponent
{
    public string Name => EntityComponentIds.Scale;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["value"] = Value
    };
}

/// <summary>Defines entity collision bounding box width and height (<c>minecraft:collision_box</c>).</summary>
public sealed record CollisionBoxEntityComponent(float Width = 0.6f, float Height = 1.8f) : IEntityComponent
{
    public string Name => EntityComponentIds.CollisionBox;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["width"] = Width,
        ["height"] = Height
    };
}

// --- Combat, Attaque, Équipement & Dégâts ---

/// <summary>Defines melee attack damage and on-hit potion effects (<c>minecraft:attack</c>).</summary>
public sealed record AttackEntityComponent(float Damage = 3.0f, string? EffectName = null, int EffectDuration = 0) : IEntityComponent
{
    public string Name => EntityComponentIds.Attack;

    public OrderedDictionary<string, object> BuildNbt()
    {
        var dict = new OrderedDictionary<string, object> { ["damage"] = Damage };
        if (!string.IsNullOrEmpty(EffectName))
        {
            dict["effect_name"] = EffectName;
            dict["effect_duration"] = EffectDuration;
        }
        return dict;
    }
}

/// <summary>Defines ranged projectile shooting behavior (<c>minecraft:shooter</c>).</summary>
public sealed record ShooterEntityComponent(string ProjectileDef = "minecraft:arrow", float Power = 1.2f) : IEntityComponent
{
    public string Name => EntityComponentIds.Shooter;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["def"] = ProjectileDef,
        ["power"] = Power
    };
}

/// <summary>Defines default equipped armor and weapons in hands and armor slots (<c>minecraft:equipment</c>).</summary>
public sealed record EquipmentEntityComponent(
    string? MainHand = null,
    string? OffHand = null,
    string? Helmet = null,
    string? Chestplate = null,
    string? Leggings = null,
    string? Boots = null) : IEntityComponent
{
    public string Name => EntityComponentIds.Equipment;

    public OrderedDictionary<string, object> BuildNbt()
    {
        var dict = new OrderedDictionary<string, object>();
        var slotDropChance = new OrderedDictionary<string, object>();
        if (MainHand is not null) { dict["mainhand"] = MainHand; slotDropChance["slot.weapon.mainhand"] = 0.085f; }
        if (OffHand is not null) { dict["offhand"] = OffHand; slotDropChance["slot.weapon.offhand"] = 0.085f; }
        if (Helmet is not null) { dict["helmet"] = Helmet; slotDropChance["slot.armor.head"] = 0.085f; }
        if (Chestplate is not null) { dict["chestplate"] = Chestplate; slotDropChance["slot.armor.chest"] = 0.085f; }
        if (Leggings is not null) { dict["leggings"] = Leggings; slotDropChance["slot.armor.legs"] = 0.085f; }
        if (Boots is not null) { dict["boots"] = Boots; slotDropChance["slot.armor.feet"] = 0.085f; }
        dict["slot_drop_chance"] = slotDropChance;
        return dict;
    }
}

// --- Physique & Mouvement ---

/// <summary>Defines gravity simulation and block collision properties (<c>minecraft:physics</c>).</summary>
public sealed record PhysicsEntityComponent(bool HasGravity = true, bool HasCollision = true) : IEntityComponent
{
    public string Name => EntityComponentIds.Physics;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["has_gravity"] = HasGravity,
        ["has_collision"] = HasCollision
    };
}

/// <summary>Defines pushability by other entities or pistons (<c>minecraft:pushable</c>).</summary>
public sealed record PushableEntityComponent(bool IsPushable = true, bool IsPushableByPiston = true) : IEntityComponent
{
    public string Name => EntityComponentIds.Pushable;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["is_pushable"] = IsPushable,
        ["is_pushable_by_piston"] = IsPushableByPiston
    };
}

/// <summary>Defines suffocation and breathing rules in air and water (<c>minecraft:breathable</c>).</summary>
public sealed record BreathableEntityComponent(
    int TotalSupply = 15,
    int SuffocateTime = -20,
    bool BreatheAir = true,
    bool BreatheWater = false) : IEntityComponent
{
    public string Name => EntityComponentIds.Breathable;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["total_supply"] = TotalSupply,
        ["suffocate_time"] = SuffocateTime,
        ["breathe_air"] = BreatheAir,
        ["breathe_water"] = BreatheWater
    };
}

/// <summary>Defines custom nametag display and renaming behavior (<c>minecraft:nameable</c>).</summary>
public sealed record NameableEntityComponent(bool AllowNameTagRenaming = true, bool AlwaysShow = false) : IEntityComponent
{
    public string Name => EntityComponentIds.Nameable;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["allow_name_tag_renaming"] = AllowNameTagRenaming,
        ["always_show"] = AlwaysShow
    };
}

/// <summary>Defines boss bar HUD display, title, range, and sky darkening (<c>minecraft:boss</c>).</summary>
public sealed record BossEntityComponent(string Name, int HudRange = 55, bool ShouldDarkenSky = false) : IEntityComponent
{
    string IEntityComponent.Name => EntityComponentIds.Boss;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["name"] = Name,
        ["hud_range"] = HudRange,
        ["should_darken_sky"] = ShouldDarkenSky
    };
}

/// <summary>Defines mount seats, passenger capacity, and rider controls (<c>minecraft:rideable</c>).</summary>
public sealed record RideableEntityComponent(
    int SeatCount = 1,
    bool InputGroundControlled = false,
    Vector3[]? SeatPositions = null,
    params string[] FamilyTypes) : IEntityComponent
{
    public string Name => EntityComponentIds.Rideable;

    public OrderedDictionary<string, object> BuildNbt()
    {
        var seats = new List<object>();
        if (SeatPositions is { Length: > 0 } positions)
        {
            foreach (var pos in positions)
            {
                seats.Add(new OrderedDictionary<string, object>
                {
                    ["position"] = new NbtList(Nbt.Float, [pos.X, pos.Y, pos.Z])
                });
            }
        }
        else
        {
            for (var i = 0; i < SeatCount; i++)
            {
                seats.Add(new OrderedDictionary<string, object>
                {
                    ["position"] = new NbtList(Nbt.Float, [0f, 0.5f, 0f])
                });
            }
        }

        var dict = new OrderedDictionary<string, object>
        {
            ["seat_count"] = SeatCount,
            ["seats"] = new NbtList(Nbt.Compound, seats)
        };

        if (FamilyTypes is { Length: > 0 })
            dict["family_types"] = new NbtList(Nbt.String, [.. FamilyTypes.Cast<object>()]);

        return dict;
    }
}

public sealed record TameableEntityComponent(float Probability = 0.33f, params string[] TameItems) : IEntityComponent
{
    public string Name => EntityComponentIds.Tameable;

    public OrderedDictionary<string, object> BuildNbt()
    {
        var dict = new OrderedDictionary<string, object>
        {
            ["probability"] = Probability
        };
        if (TameItems is { Length: > 0 })
            dict["tame_items"] = new NbtList(Nbt.String, [.. TameItems.Cast<object>()]);
        return dict;
    }
}

public sealed record HealableEntityComponent(params (string Item, int HealAmount)[] Items) : IEntityComponent
{
    public string Name => EntityComponentIds.Healable;

    public OrderedDictionary<string, object> BuildNbt()
    {
        var itemsList = new List<object>();
        foreach (var (item, healAmount) in Items)
        {
            itemsList.Add(new OrderedDictionary<string, object>
            {
                ["item"] = item,
                ["heal_amount"] = healAmount
            });
        }
        return new OrderedDictionary<string, object>
        {
            ["items"] = new NbtList(Nbt.Compound, itemsList)
        };
    }
}

public sealed record AgeableEntityComponent(float Duration = 1200f, params string[] FeedItems) : IEntityComponent
{
    public string Name => EntityComponentIds.Ageable;

    public OrderedDictionary<string, object> BuildNbt()
    {
        var dict = new OrderedDictionary<string, object> { ["duration"] = Duration };
        if (FeedItems is { Length: > 0 })
            dict["feed_items"] = new NbtList(Nbt.String, [.. FeedItems.Cast<object>()]);
        return dict;
    }
}

public sealed record ExplodeEntityComponent(
    float Power = 3.0f,
    float FuseLength = 1.5f,
    bool CausesFire = false,
    bool BreaksBlocks = true) : IEntityComponent
{
    public string Name => EntityComponentIds.Explode;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["power"] = Power,
        ["fuse_length"] = FuseLength,
        ["causes_fire"] = CausesFire,
        ["breaks_blocks"] = BreaksBlocks
    };
}

public sealed record TeleportEntityComponent(
    float MaxRandomTeleportTime = 20f,
    Vector3? RandomTeleportCube = null) : IEntityComponent
{
    public string Name => EntityComponentIds.Teleport;

    public OrderedDictionary<string, object> BuildNbt()
    {
        var dict = new OrderedDictionary<string, object>
        {
            ["max_random_teleport_time"] = MaxRandomTeleportTime
        };
        if (RandomTeleportCube is { } cube)
            dict["random_teleport_cube"] = new NbtList(Nbt.Float, [cube.X, cube.Y, cube.Z]);
        return dict;
    }
}

public sealed record InventoryEntityComponent(
    int InventorySize = 27,
    string ContainerType = "minecart_chest",
    bool Private = false) : IEntityComponent
{
    public string Name => EntityComponentIds.Inventory;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["inventory_size"] = InventorySize,
        ["container_type"] = ContainerType,
        ["private"] = Private
    };
}

public sealed class InteractEntityComponent : IEntityComponent
{
    public InteractEntityComponent(
        string buttonText = "Interact",
        string? requiredItem = null,
        Action<BedrockConnection, CustomEntityInstance>? onInteractCallback = null)
    {
        ButtonText = buttonText;
        RequiredItem = requiredItem;
        OnInteractCallback = onInteractCallback;
    }

    public string ButtonText { get; }
    public string? RequiredItem { get; }
    public Action<BedrockConnection, CustomEntityInstance>? OnInteractCallback { get; }
    public Action<BedrockConnection, CustomEntityInstance>? OnInteract => OnInteractCallback;

    public string Name => EntityComponentIds.Interact;

    public OrderedDictionary<string, object> BuildNbt()
    {
        var dict = new OrderedDictionary<string, object>();
        var interactions = new List<object>
        {
            new OrderedDictionary<string, object>
            {
                ["interact_text"] = ButtonText
            }
        };
        dict["interactions"] = new NbtList(Nbt.Compound, interactions);
        return dict;
    }
}

public sealed record TypeFamilyEntityComponent(params string[] Families) : IEntityComponent
{
    public string Name => EntityComponentIds.TypeFamily;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["family"] = new NbtList(Nbt.String, [.. Families.Cast<object>()])
    };
}

public sealed record LootEntityComponent(string Table) : IEntityComponent
{
    public string Name => EntityComponentIds.Loot;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["table"] = Table
    };
}

public sealed record VariantEntityComponent(int Value = 0) : IEntityComponent
{
    public string Name => EntityComponentIds.Variant;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["value"] = Value
    };
}

public sealed record MarkVariantEntityComponent(int Value = 0) : IEntityComponent
{
    public string Name => EntityComponentIds.MarkVariant;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["value"] = Value
    };
}

public sealed record SkinIdEntityComponent(int Value = 0) : IEntityComponent
{
    public string Name => EntityComponentIds.SkinId;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["value"] = Value
    };
}

// --- Composants Marqueurs / Booléens ---

/// <summary>
/// Base record for empty marker components that signify entity states or abilities with an empty NBT compound.
/// </summary>
public abstract record MarkerEntityComponent(string ComponentName) : IEntityComponent
{
    public string Name => ComponentName;
    public OrderedDictionary<string, object> BuildNbt() => [];
}

public sealed record FireImmuneEntityComponent() : MarkerEntityComponent(EntityComponentIds.FireImmune);
public sealed record PersistentEntityComponent() : MarkerEntityComponent(EntityComponentIds.Persistent);
public sealed record IsBabyEntityComponent() : MarkerEntityComponent(EntityComponentIds.IsBaby);
public sealed record IsTamedEntityComponent() : MarkerEntityComponent(EntityComponentIds.IsTamed);
public sealed record IsSaddledEntityComponent() : MarkerEntityComponent(EntityComponentIds.IsSaddled);
public sealed record IsChargedEntityComponent() : MarkerEntityComponent(EntityComponentIds.IsCharged);
public sealed record CanClimbEntityComponent() : MarkerEntityComponent(EntityComponentIds.CanClimb);
public sealed record CanFlyEntityComponent() : MarkerEntityComponent(EntityComponentIds.CanFly);
public sealed record NpcEntityComponent() : MarkerEntityComponent(EntityComponentIds.Npc);

// --- Navigation ---

/// <summary>Configures ground walking pathfinding and door handling (<c>minecraft:navigation.walk</c>).</summary>
public sealed record WalkNavigationComponent(bool CanPassDoors = true, bool CanOpenDoors = false) : IEntityComponent
{
    public string Name => EntityComponentIds.NavigationWalk;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["can_pass_doors"] = CanPassDoors,
        ["can_open_doors"] = CanOpenDoors
    };
}

/// <summary>Configures airborne flying pathfinding (<c>minecraft:navigation.fly</c>).</summary>
public sealed record FlyNavigationComponent(bool CanPathOverWater = true) : IEntityComponent
{
    public string Name => EntityComponentIds.NavigationFly;

    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["can_path_over_water"] = CanPathOverWater
    };
}

/// <summary>Configures aquatic swimming pathfinding (<c>minecraft:navigation.swim</c>).</summary>
public sealed record SwimNavigationComponent() : MarkerEntityComponent(EntityComponentIds.NavigationSwim);

// --- Comportements IA (minecraft:behavior.*) ---

/// <summary>
/// Base record for all Bedrock entity AI behaviors (<c>minecraft:behavior.*</c>) with priority ordering.
/// </summary>
public abstract record BehaviorEntityComponent(string BehaviorName, int Priority = 0) : IEntityComponent
{
    public string Name => BehaviorName;

    public virtual OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["priority"] = Priority
    };
}

public sealed record FloatBehaviorComponent(int Priority = 0) : BehaviorEntityComponent(EntityComponentIds.BehaviorFloat, Priority);

public sealed record PanicBehaviorComponent(int Priority = 1, float SpeedMultiplier = 1.25f)
    : BehaviorEntityComponent(EntityComponentIds.BehaviorPanic, Priority)
{
    public override OrderedDictionary<string, object> BuildNbt()
    {
        var dict = base.BuildNbt();
        dict["speed_multiplier"] = SpeedMultiplier;
        return dict;
    }
}

public sealed record RandomStrollBehaviorComponent(int Priority = 6, float SpeedMultiplier = 1.0f)
    : BehaviorEntityComponent(EntityComponentIds.BehaviorRandomStroll, Priority)
{
    public override OrderedDictionary<string, object> BuildNbt()
    {
        var dict = base.BuildNbt();
        dict["speed_multiplier"] = SpeedMultiplier;
        return dict;
    }
}

public sealed record RandomFlyBehaviorComponent(int Priority = 6, float SpeedMultiplier = 1.0f)
    : BehaviorEntityComponent(EntityComponentIds.BehaviorRandomFly, Priority)
{
    public override OrderedDictionary<string, object> BuildNbt()
    {
        var dict = base.BuildNbt();
        dict["speed_multiplier"] = SpeedMultiplier;
        return dict;
    }
}

public sealed record LookAtPlayerBehaviorComponent(int Priority = 7, float LookDistance = 8.0f, float Probability = 0.02f)
    : BehaviorEntityComponent(EntityComponentIds.BehaviorLookAtPlayer, Priority)
{
    public override OrderedDictionary<string, object> BuildNbt()
    {
        var dict = base.BuildNbt();
        dict["look_distance"] = LookDistance;
        dict["probability"] = Probability;
        return dict;
    }
}

public sealed record RandomLookAroundBehaviorComponent(int Priority = 8)
    : BehaviorEntityComponent(EntityComponentIds.BehaviorRandomLookAround, Priority);

public sealed record MeleeAttackBehaviorComponent(int Priority = 2, float SpeedMultiplier = 1.25f, bool TrackTarget = true)
    : BehaviorEntityComponent(EntityComponentIds.BehaviorMeleeAttack, Priority)
{
    public override OrderedDictionary<string, object> BuildNbt()
    {
        var dict = base.BuildNbt();
        dict["speed_multiplier"] = SpeedMultiplier;
        dict["track_target"] = TrackTarget;
        return dict;
    }
}

public sealed record RangedAttackBehaviorComponent(
    int Priority = 2,
    float AttackIntervalMin = 1.0f,
    float AttackIntervalMax = 3.0f,
    float AttackRadius = 16.0f) : BehaviorEntityComponent(EntityComponentIds.BehaviorRangedAttack, Priority)
{
    public override OrderedDictionary<string, object> BuildNbt()
    {
        var dict = base.BuildNbt();
        dict["attack_interval_min"] = AttackIntervalMin;
        dict["attack_interval_max"] = AttackIntervalMax;
        dict["attack_radius"] = AttackRadius;
        return dict;
    }
}

public sealed record LeapAtTargetBehaviorComponent(int Priority = 4, float JumpHeight = 0.4f)
    : BehaviorEntityComponent(EntityComponentIds.BehaviorLeapAtTarget, Priority)
{
    public override OrderedDictionary<string, object> BuildNbt()
    {
        var dict = base.BuildNbt();
        dict["yd"] = JumpHeight;
        return dict;
    }
}

public sealed record SwellBehaviorComponent(int Priority = 2, float StartDistance = 2.5f, float StopDistance = 5.0f)
    : BehaviorEntityComponent(EntityComponentIds.BehaviorSwell, Priority)
{
    public override OrderedDictionary<string, object> BuildNbt()
    {
        var dict = base.BuildNbt();
        dict["start_distance"] = StartDistance;
        dict["stop_distance"] = StopDistance;
        return dict;
    }
}

public sealed record SummonEntityBehaviorComponent(
    int Priority = 2,
    string SummonEntityDef = "",
    int MinDuration = 20,
    int MaxDuration = 40) : BehaviorEntityComponent(EntityComponentIds.BehaviorSummonEntity, Priority)
{
    public override OrderedDictionary<string, object> BuildNbt()
    {
        var dict = base.BuildNbt();
        dict["summon_entity_def"] = SummonEntityDef;
        dict["min_duration"] = MinDuration;
        dict["max_duration"] = MaxDuration;
        return dict;
    }
}

public sealed record NearestAttackableTargetBehaviorComponent(
    int Priority = 1,
    string TargetType = "player",
    bool MustSee = true) : BehaviorEntityComponent(EntityComponentIds.BehaviorNearestAttackableTarget, Priority)
{
    public override OrderedDictionary<string, object> BuildNbt()
    {
        var dict = base.BuildNbt();
        dict["target_types"] = new NbtList(Nbt.Compound, [
            new OrderedDictionary<string, object>
            {
                ["filters"] = new OrderedDictionary<string, object>
                {
                    ["test"] = "is_family",
                    ["subject"] = "other",
                    ["value"] = TargetType
                },
                ["max_dist"] = 16.0f,
                ["must_see"] = MustSee
            }
        ]);
        return dict;
    }
}

public sealed record HurtByTargetBehaviorComponent(int Priority = 1, bool AlertSameType = false)
    : BehaviorEntityComponent(EntityComponentIds.BehaviorHurtByTarget, Priority)
{
    public override OrderedDictionary<string, object> BuildNbt()
    {
        var dict = base.BuildNbt();
        dict["alert_same_type"] = AlertSameType;
        return dict;
    }
}

public sealed record OwnerHurtByTargetBehaviorComponent(int Priority = 1)
    : BehaviorEntityComponent(EntityComponentIds.BehaviorOwnerHurtByTarget, Priority);

public sealed record OwnerHurtTargetBehaviorComponent(int Priority = 1)
    : BehaviorEntityComponent(EntityComponentIds.BehaviorOwnerHurtTarget, Priority);

public sealed record StayWhileSittingBehaviorComponent(int Priority = 2)
    : BehaviorEntityComponent(EntityComponentIds.BehaviorStayWhileSitting, Priority);

public sealed record TemptBehaviorComponent(int Priority = 3, float SpeedMultiplier = 1.0f, params string[] Items)
    : BehaviorEntityComponent(EntityComponentIds.BehaviorTempt, Priority)
{
    public override OrderedDictionary<string, object> BuildNbt()
    {
        var dict = base.BuildNbt();
        dict["speed_multiplier"] = SpeedMultiplier;
        if (Items is { Length: > 0 })
            dict["items"] = new NbtList(Nbt.String, [.. Items.Cast<object>()]);
        return dict;
    }
}

public sealed record BreedBehaviorComponent(int Priority = 3, float SpeedMultiplier = 1.0f)
    : BehaviorEntityComponent(EntityComponentIds.BehaviorBreed, Priority)
{
    public override OrderedDictionary<string, object> BuildNbt()
    {
        var dict = base.BuildNbt();
        dict["speed_multiplier"] = SpeedMultiplier;
        return dict;
    }
}

public sealed record FollowOwnerBehaviorComponent(int Priority = 5, float StartDistance = 10.0f, float StopDistance = 2.0f)
    : BehaviorEntityComponent(EntityComponentIds.BehaviorFollowOwner, Priority)
{
    public override OrderedDictionary<string, object> BuildNbt()
    {
        var dict = base.BuildNbt();
        dict["start_distance"] = StartDistance;
        dict["stop_distance"] = StopDistance;
        return dict;
    }
}

public sealed record FollowParentBehaviorComponent(int Priority = 5, float SpeedMultiplier = 1.1f)
    : BehaviorEntityComponent(EntityComponentIds.BehaviorFollowParent, Priority)
{
    public override OrderedDictionary<string, object> BuildNbt()
    {
        var dict = base.BuildNbt();
        dict["speed_multiplier"] = SpeedMultiplier;
        return dict;
    }
}

public sealed record AvoidMobTypeBehaviorComponent(
    int Priority = 4,
    string EntityType = "player",
    float MaxDist = 6.0f,
    float WalkSpeed = 1.0f,
    float SprintSpeed = 1.2f) : BehaviorEntityComponent(EntityComponentIds.BehaviorAvoidMobType, Priority)
{
    public override OrderedDictionary<string, object> BuildNbt()
    {
        var dict = base.BuildNbt();
        dict["entity_types"] = new NbtList(Nbt.Compound, [
            new OrderedDictionary<string, object>
            {
                ["filters"] = new OrderedDictionary<string, object>
                {
                    ["test"] = "is_family",
                    ["subject"] = "other",
                    ["value"] = EntityType
                },
                ["max_dist"] = MaxDist,
                ["walk_speed_multiplier"] = WalkSpeed,
                ["sprint_speed_multiplier"] = SprintSpeed
            }
        ]);
        return dict;
    }
}

public sealed record FleeSunBehaviorComponent(int Priority = 2, float SpeedMultiplier = 1.2f)
    : BehaviorEntityComponent(EntityComponentIds.BehaviorFleeSun, Priority)
{
    public override OrderedDictionary<string, object> BuildNbt()
    {
        var dict = base.BuildNbt();
        dict["speed_multiplier"] = SpeedMultiplier;
        return dict;
    }
}

public sealed record OpenDoorBehaviorComponent(int Priority = 5, bool CloseDoorAfter = true)
    : BehaviorEntityComponent(EntityComponentIds.BehaviorOpenDoor, Priority)
{
    public override OrderedDictionary<string, object> BuildNbt()
    {
        var dict = base.BuildNbt();
        dict["close_door_after"] = CloseDoorAfter;
        return dict;
    }
}
