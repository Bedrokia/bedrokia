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

namespace Bedrokia.Server.Game.Custom.Entities;

/// <summary>
/// Official Minecraft Bedrock entity component and behavior identifiers (in <c>minecraft:snake_case</c> format).
/// Used in entity definition JSON files and server-side NBT data sent via <see cref="Bedrokia.Protocol.PacketId.AvailableActorIdentifiers"/>.
/// Source: Bedrock Entity Documentation &amp; Dragonfly (MIT) entity definitions.
/// </summary>
public static class EntityComponentIds
{
    public const string AddRider = "minecraft:addrider";
    public const string Ageable = "minecraft:ageable";
    public const string Attack = "minecraft:attack";
    public const string BehaviorAvoidMobType = "minecraft:behavior.avoid_mob_type";
    public const string BehaviorBreed = "minecraft:behavior.breed";
    public const string BehaviorFleeSun = "minecraft:behavior.flee_sun";
    public const string BehaviorFloat = "minecraft:behavior.float";
    public const string BehaviorFollowOwner = "minecraft:behavior.follow_owner";
    public const string BehaviorFollowParent = "minecraft:behavior.follow_parent";
    public const string BehaviorHurtByTarget = "minecraft:behavior.hurt_by_target";
    public const string BehaviorLeapAtTarget = "minecraft:behavior.leap_at_target";
    public const string BehaviorLookAtPlayer = "minecraft:behavior.look_at_player";
    public const string BehaviorMeleeAttack = "minecraft:behavior.melee_attack";
    public const string BehaviorNearestAttackableTarget = "minecraft:behavior.nearest_attackable_target";
    public const string BehaviorOpenDoor = "minecraft:behavior.open_door";
    public const string BehaviorOwnerHurtByTarget = "minecraft:behavior.owner_hurt_by_target";
    public const string BehaviorOwnerHurtTarget = "minecraft:behavior.owner_hurt_target";
    public const string BehaviorPanic = "minecraft:behavior.panic";
    public const string BehaviorRandomFly = "minecraft:behavior.random_fly";
    public const string BehaviorRandomLookAround = "minecraft:behavior.random_look_around";
    public const string BehaviorRandomStroll = "minecraft:behavior.random_stroll";
    public const string BehaviorRangedAttack = "minecraft:behavior.ranged_attack";
    public const string BehaviorStayWhileSitting = "minecraft:behavior.stay_while_sitting";
    public const string BehaviorSummonEntity = "minecraft:behavior.summon_entity";
    public const string BehaviorSwell = "minecraft:behavior.swell";
    public const string BehaviorTempt = "minecraft:behavior.tempt";
    public const string Boostable = "minecraft:boostable";
    public const string Boss = "minecraft:boss";
    public const string Breathable = "minecraft:breathable";
    public const string Breedable = "minecraft:breedable";
    public const string CanClimb = "minecraft:can_climb";
    public const string CanFly = "minecraft:can_fly";
    public const string CanPowerJump = "minecraft:can_power_jump";
    public const string CollisionBox = "minecraft:collision_box";
    public const string DamageSensor = "minecraft:damage_sensor";
    public const string Despawn = "minecraft:despawn";
    public const string EnvironmentSensor = "minecraft:environment_sensor";
    public const string EquipItem = "minecraft:equip_item";
    public const string Equipment = "minecraft:equipment";
    public const string Equippable = "minecraft:equippable";
    public const string Explode = "minecraft:explode";
    public const string FireImmune = "minecraft:fire_immune";
    public const string FloatsInLiquid = "minecraft:floats_in_liquid";
    public const string FlyingSpeed = "minecraft:flying_speed";
    public const string Healable = "minecraft:healable";
    public const string Health = "minecraft:health";
    public const string InputGroundControlled = "minecraft:input_ground_controlled";
    public const string Interact = "minecraft:interact";
    public const string Inventory = "minecraft:inventory";
    public const string IsBaby = "minecraft:is_baby";
    public const string IsCharged = "minecraft:is_charged";
    public const string IsChested = "minecraft:is_chested";
    public const string IsIgnited = "minecraft:is_ignited";
    public const string IsSaddled = "minecraft:is_saddled";
    public const string IsSheared = "minecraft:is_sheared";
    public const string IsTamed = "minecraft:is_tamed";
    public const string KnockbackResistance = "minecraft:knockback_resistance";
    public const string Leashable = "minecraft:leashable";
    public const string LookAt = "minecraft:lookat";
    public const string Loot = "minecraft:loot";
    public const string MarkVariant = "minecraft:mark_variant";
    public const string MobEffect = "minecraft:mob_effect";
    public const string Movement = "minecraft:movement";
    public const string Nameable = "minecraft:nameable";
    public const string NavigationClimb = "minecraft:navigation.climb";
    public const string NavigationFly = "minecraft:navigation.fly";
    public const string NavigationGeneric = "minecraft:navigation.generic";
    public const string NavigationSwim = "minecraft:navigation.swim";
    public const string NavigationWalk = "minecraft:navigation.walk";
    public const string Npc = "minecraft:npc";
    public const string Persistent = "minecraft:persistent";
    public const string Physics = "minecraft:physics";
    public const string Projectile = "minecraft:projectile";
    public const string Pushable = "minecraft:pushable";
    public const string PushThrough = "minecraft:push_through";
    public const string Rideable = "minecraft:rideable";
    public const string Scale = "minecraft:scale";
    public const string Shooter = "minecraft:shooter";
    public const string SkinId = "minecraft:skin_id";
    public const string Tameable = "minecraft:tameable";
    public const string Teleport = "minecraft:teleport";
    public const string Timer = "minecraft:timer";
    public const string TradeTable = "minecraft:trade_table";
    public const string Transformation = "minecraft:transformation";
    public const string TypeFamily = "minecraft:type_family";
    public const string UnderwaterMovement = "minecraft:underwater_movement";
    public const string Variant = "minecraft:variant";
}
