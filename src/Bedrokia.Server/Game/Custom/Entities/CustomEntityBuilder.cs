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
using Bedrokia.Server.Entities;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Game.Custom.Entities;

/// <summary>
/// Fluent builder for constructing and registering custom Bedrock entity definitions.
/// Mirrors the architecture and ergonomics of <see cref="Bedrokia.Server.Game.Custom.Blocks.CustomBlockBuilder"/>
/// and <see cref="Bedrokia.Server.Game.Custom.Items.CustomItemBuilder"/>.
/// Source: Dragonfly (MIT) entity definition builder pattern &amp; Bedrock Entity JSON specs.
/// </summary>
public sealed class CustomEntityBuilder
{
    private readonly string _identifier;
    private string _displayName = "";
    private string _runtimeIdentifier = "";
    private bool _summonable = true;
    private bool _hasSpawnEgg = true;

    private readonly Dictionary<string, IEntityComponent> _components = [];
    private readonly Dictionary<string, EntityProperty> _properties = [];
    private readonly Dictionary<string, EntityComponentGroup> _componentGroups = [];

    private Action<CustomEntityInstance>? _onSpawn;
    private Action<CustomEntityInstance>? _onTick;
    private Action<BedrockConnection, CustomEntityInstance>? _onInteract;
    private Action<CustomEntityInstance, DamageSource>? _onHurt;
    private Action<CustomEntityInstance>? _onDeath;

    /// <summary>
    /// Initializes a new builder instance with default sane physics and collision box.
    /// </summary>
    /// <param name="identifier">Entity identifier (auto-prefixed with <c>custom:</c> if unqualified).</param>
    public CustomEntityBuilder(string identifier)
    {
        _identifier = identifier.Contains(':') ? identifier : "custom:" + identifier;

        // Valeurs par défaut saines (Dragonfly / gophertunnel conventions)
        AddComponent(new HealthEntityComponent(20, 20));
        AddComponent(new MovementEntityComponent(0.25f));
        AddComponent(new CollisionBoxEntityComponent(0.6f, 1.8f));
        AddComponent(new PhysicsEntityComponent(HasGravity: true, HasCollision: true));
        AddComponent(new PushableEntityComponent(IsPushable: true, IsPushableByPiston: true));
        AddComponent(new ScaleEntityComponent(1.0f));
    }

    /// <summary>
    /// Creates a new fluent builder for a custom entity with the specified identifier.
    /// </summary>
    /// <param name="identifier">Unique entity identifier (e.g. <c>custom:fire_dragon</c> or <c>fire_dragon</c>).</param>
    public static CustomEntityBuilder Create(string identifier) => new(identifier);

    // --- Identifiant & Métadonnées d'apparition ---

    public CustomEntityBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }
    public CustomEntityBuilder SetDisplayName(string displayName) => WithDisplayName(displayName);

    public CustomEntityBuilder WithRuntimeIdentifier(string vanillaFallback)
    {
        _runtimeIdentifier = vanillaFallback;
        return this;
    }
    public CustomEntityBuilder SetRuntimeIdentifier(string vanillaFallback) => WithRuntimeIdentifier(vanillaFallback);

    public CustomEntityBuilder WithSpawnEgg(bool summonable = true, bool hasSpawnEgg = true)
    {
        _summonable = summonable;
        _hasSpawnEgg = hasSpawnEgg;
        return this;
    }
    public CustomEntityBuilder SetSpawnEgg(bool summonable = true, bool hasSpawnEgg = true) => WithSpawnEgg(summonable, hasSpawnEgg);

    // --- Attributs & Métriques ---

    public CustomEntityBuilder WithHealth(int value, int max) => AddComponent(new HealthEntityComponent(value, max));
    public CustomEntityBuilder SetHealth(int value, int max) => WithHealth(value, max);

    public CustomEntityBuilder WithMovement(float speed) => AddComponent(new MovementEntityComponent(speed));
    public CustomEntityBuilder SetMovement(float speed) => WithMovement(speed);

    public CustomEntityBuilder WithFlyingSpeed(float speed) => AddComponent(new FlyingSpeedEntityComponent(speed));
    public CustomEntityBuilder SetFlyingSpeed(float speed) => WithFlyingSpeed(speed);

    public CustomEntityBuilder WithUnderwaterMovement(float speed) => AddComponent(new UnderwaterMovementEntityComponent(speed));
    public CustomEntityBuilder SetUnderwaterMovement(float speed) => WithUnderwaterMovement(speed);

    public CustomEntityBuilder WithKnockbackResistance(float resistance) => AddComponent(new KnockbackResistanceEntityComponent(resistance));
    public CustomEntityBuilder SetKnockbackResistance(float resistance) => WithKnockbackResistance(resistance);

    public CustomEntityBuilder WithScale(float scale) => AddComponent(new ScaleEntityComponent(scale));
    public CustomEntityBuilder SetScale(float scale) => WithScale(scale);

    public CustomEntityBuilder WithCollisionBox(float width, float height) => AddComponent(new CollisionBoxEntityComponent(width, height));
    public CustomEntityBuilder SetCollisionBox(float width, float height) => WithCollisionBox(width, height);

    // --- Combat & Attaque ---

    public CustomEntityBuilder WithAttack(float damage, string? effectName = null, int effectDuration = 0) =>
        AddComponent(new AttackEntityComponent(damage, effectName, effectDuration));
    public CustomEntityBuilder SetAttack(float damage, string? effectName = null, int effectDuration = 0) =>
        WithAttack(damage, effectName, effectDuration);

    public CustomEntityBuilder WithShooter(string projectileDef = "minecraft:arrow", float power = 1.2f) =>
        AddComponent(new ShooterEntityComponent(projectileDef, power));
    public CustomEntityBuilder SetShooter(string projectileDef = "minecraft:arrow", float power = 1.2f) =>
        WithShooter(projectileDef, power);

    public CustomEntityBuilder WithEquipment(
        string? mainHand = null,
        string? offHand = null,
        string? helmet = null,
        string? chestplate = null,
        string? leggings = null,
        string? boots = null) => AddComponent(new EquipmentEntityComponent(mainHand, offHand, helmet, chestplate, leggings, boots));
    public CustomEntityBuilder SetEquipment(
        string? mainHand = null,
        string? offHand = null,
        string? helmet = null,
        string? chestplate = null,
        string? leggings = null,
        string? boots = null) => WithEquipment(mainHand, offHand, helmet, chestplate, leggings, boots);

    // --- Physique & Environnement ---

    public CustomEntityBuilder WithPhysics(bool hasGravity = true, bool hasCollision = true) =>
        AddComponent(new PhysicsEntityComponent(hasGravity, hasCollision));
    public CustomEntityBuilder SetPhysics(bool hasGravity = true, bool hasCollision = true) =>
        WithPhysics(hasGravity, hasCollision);

    public CustomEntityBuilder WithPushable(bool isPushable = true, bool isPushableByPiston = true) =>
        AddComponent(new PushableEntityComponent(isPushable, isPushableByPiston));
    public CustomEntityBuilder SetPushable(bool isPushable = true, bool isPushableByPiston = true) =>
        WithPushable(isPushable, isPushableByPiston);

    public CustomEntityBuilder WithBreathable(int totalSupply = 15, int suffocateTime = -20, bool breatheAir = true, bool breatheWater = false) =>
        AddComponent(new BreathableEntityComponent(totalSupply, suffocateTime, breatheAir, breatheWater));
    public CustomEntityBuilder SetBreathable(int totalSupply = 15, int suffocateTime = -20, bool breatheAir = true, bool breatheWater = false) =>
        WithBreathable(totalSupply, suffocateTime, breatheAir, breatheWater);

    public CustomEntityBuilder WithNameable(bool allowRenaming = true, bool alwaysShow = false) =>
        AddComponent(new NameableEntityComponent(allowRenaming, alwaysShow));
    public CustomEntityBuilder SetNameable(bool allowRenaming = true, bool alwaysShow = false) =>
        WithNameable(allowRenaming, alwaysShow);

    public CustomEntityBuilder WithBoss(string name, int hudRange = 55, bool shouldDarkenSky = false) =>
        AddComponent(new BossEntityComponent(name, hudRange, shouldDarkenSky));
    public CustomEntityBuilder SetBoss(string name, int hudRange = 55, bool shouldDarkenSky = false) =>
        WithBoss(name, hudRange, shouldDarkenSky);

    public CustomEntityBuilder WithRideable(int seatCount = 1, bool inputGroundControlled = false, Vector3[]? seatPositions = null, params string[] familyTypes) =>
        AddComponent(new RideableEntityComponent(seatCount, inputGroundControlled, seatPositions, familyTypes));
    public CustomEntityBuilder SetRideable(int seatCount = 1, bool inputGroundControlled = false, Vector3[]? seatPositions = null, params string[] familyTypes) =>
        WithRideable(seatCount, inputGroundControlled, seatPositions, familyTypes);

    public CustomEntityBuilder WithTameable(float probability = 0.33f, params string[] tameItems) =>
        AddComponent(new TameableEntityComponent(probability, tameItems));
    public CustomEntityBuilder SetTameable(float probability = 0.33f, params string[] tameItems) =>
        WithTameable(probability, tameItems);

    public CustomEntityBuilder WithHealable(params (string Item, int HealAmount)[] items) =>
        AddComponent(new HealableEntityComponent(items));
    public CustomEntityBuilder SetHealable(params (string Item, int HealAmount)[] items) =>
        WithHealable(items);

    public CustomEntityBuilder WithAgeable(float duration = 1200f, params string[] feedItems) =>
        AddComponent(new AgeableEntityComponent(duration, feedItems));
    public CustomEntityBuilder SetAgeable(float duration = 1200f, params string[] feedItems) =>
        WithAgeable(duration, feedItems);

    public CustomEntityBuilder WithExplode(float power = 3.0f, float fuseLength = 1.5f, bool causesFire = false, bool breaksBlocks = true) =>
        AddComponent(new ExplodeEntityComponent(power, fuseLength, causesFire, breaksBlocks));
    public CustomEntityBuilder SetExplode(float power = 3.0f, float fuseLength = 1.5f, bool causesFire = false, bool breaksBlocks = true) =>
        WithExplode(power, fuseLength, causesFire, breaksBlocks);

    public CustomEntityBuilder WithTeleport(float maxRandomTeleportTime = 20f, Vector3? randomTeleportCube = null) =>
        AddComponent(new TeleportEntityComponent(maxRandomTeleportTime, randomTeleportCube));
    public CustomEntityBuilder SetTeleport(float maxRandomTeleportTime = 20f, Vector3? randomTeleportCube = null) =>
        WithTeleport(maxRandomTeleportTime, randomTeleportCube);

    public CustomEntityBuilder WithInventory(int inventorySize = 27, string containerType = "minecart_chest", bool isPrivate = false) =>
        AddComponent(new InventoryEntityComponent(inventorySize, containerType, isPrivate));
    public CustomEntityBuilder SetInventory(int inventorySize = 27, string containerType = "minecart_chest", bool isPrivate = false) =>
        WithInventory(inventorySize, containerType, isPrivate);

    public CustomEntityBuilder WithInteract(string buttonText = "Interact", string? requiredItem = null, Action<BedrockConnection, CustomEntityInstance>? callback = null) =>
        AddComponent(new InteractEntityComponent(buttonText, requiredItem, callback));
    public CustomEntityBuilder SetInteract(string buttonText = "Interact", string? requiredItem = null, Action<BedrockConnection, CustomEntityInstance>? callback = null) =>
        WithInteract(buttonText, requiredItem, callback);

    public CustomEntityBuilder WithTypeFamily(params string[] families) => AddComponent(new TypeFamilyEntityComponent(families));
    public CustomEntityBuilder SetTypeFamily(params string[] families) => WithTypeFamily(families);

    public CustomEntityBuilder WithLoot(string table) => AddComponent(new LootEntityComponent(table));
    public CustomEntityBuilder SetLoot(string table) => WithLoot(table);

    public CustomEntityBuilder WithVariant(int value) => AddComponent(new VariantEntityComponent(value));
    public CustomEntityBuilder SetVariant(int value) => WithVariant(value);

    public CustomEntityBuilder WithMarkVariant(int value) => AddComponent(new MarkVariantEntityComponent(value));
    public CustomEntityBuilder SetMarkVariant(int value) => WithMarkVariant(value);

    public CustomEntityBuilder WithSkinId(int value) => AddComponent(new SkinIdEntityComponent(value));
    public CustomEntityBuilder SetSkinId(int value) => WithSkinId(value);

    // --- Composants Marqueurs Booléens ---

    public CustomEntityBuilder WithFireImmune(bool immune = true)
    {
        if (immune) AddComponent(new FireImmuneEntityComponent());
        else _components.Remove(EntityComponentIds.FireImmune);
        return this;
    }
    public CustomEntityBuilder SetFireImmune(bool immune = true) => WithFireImmune(immune);

    public CustomEntityBuilder WithPersistent(bool persistent = true)
    {
        if (persistent) AddComponent(new PersistentEntityComponent());
        else _components.Remove(EntityComponentIds.Persistent);
        return this;
    }
    public CustomEntityBuilder SetPersistent(bool persistent = true) => WithPersistent(persistent);

    public CustomEntityBuilder WithBaby(bool baby = true)
    {
        if (baby) AddComponent(new IsBabyEntityComponent());
        else _components.Remove(EntityComponentIds.IsBaby);
        return this;
    }
    public CustomEntityBuilder SetBaby(bool baby = true) => WithBaby(baby);

    public CustomEntityBuilder WithTamed(bool tamed = true)
    {
        if (tamed) AddComponent(new IsTamedEntityComponent());
        else _components.Remove(EntityComponentIds.IsTamed);
        return this;
    }
    public CustomEntityBuilder SetTamed(bool tamed = true) => WithTamed(tamed);

    public CustomEntityBuilder WithSaddled(bool saddled = true)
    {
        if (saddled) AddComponent(new IsSaddledEntityComponent());
        else _components.Remove(EntityComponentIds.IsSaddled);
        return this;
    }
    public CustomEntityBuilder SetSaddled(bool saddled = true) => WithSaddled(saddled);

    public CustomEntityBuilder WithCharged(bool charged = true)
    {
        if (charged) AddComponent(new IsChargedEntityComponent());
        else _components.Remove(EntityComponentIds.IsCharged);
        return this;
    }
    public CustomEntityBuilder SetCharged(bool charged = true) => WithCharged(charged);

    public CustomEntityBuilder WithCanClimb(bool climb = true)
    {
        if (climb) AddComponent(new CanClimbEntityComponent());
        else _components.Remove(EntityComponentIds.CanClimb);
        return this;
    }
    public CustomEntityBuilder SetCanClimb(bool climb = true) => WithCanClimb(climb);

    public CustomEntityBuilder WithCanFly(bool fly = true)
    {
        if (fly) AddComponent(new CanFlyEntityComponent());
        else _components.Remove(EntityComponentIds.CanFly);
        return this;
    }
    public CustomEntityBuilder SetCanFly(bool fly = true) => WithCanFly(fly);

    public CustomEntityBuilder WithNpc(bool npc = true)
    {
        if (npc) AddComponent(new NpcEntityComponent());
        else _components.Remove(EntityComponentIds.Npc);
        return this;
    }
    public CustomEntityBuilder SetNpc(bool npc = true) => WithNpc(npc);

    // --- Navigation & Comportements IA ---

    public CustomEntityBuilder WithWalkNavigation(bool canPassDoors = true, bool canOpenDoors = false) =>
        AddComponent(new WalkNavigationComponent(canPassDoors, canOpenDoors));
    public CustomEntityBuilder WithFlyNavigation(bool canPathOverWater = true) =>
        AddComponent(new FlyNavigationComponent(canPathOverWater));
    public CustomEntityBuilder WithSwimNavigation() =>
        AddComponent(new SwimNavigationComponent());

    public CustomEntityBuilder WithBehavior(IEntityComponent behavior) => AddComponent(behavior);
    public CustomEntityBuilder AddBehavior(IEntityComponent behavior) => WithBehavior(behavior);

    public CustomEntityBuilder WithFloatBehavior(int priority = 0) => AddComponent(new FloatBehaviorComponent(priority));
    public CustomEntityBuilder WithPanicBehavior(int priority = 1, float speedMultiplier = 1.25f) =>
        AddComponent(new PanicBehaviorComponent(priority, speedMultiplier));
    public CustomEntityBuilder WithRandomStroll(int priority = 6, float speedMultiplier = 1.0f) =>
        AddComponent(new RandomStrollBehaviorComponent(priority, speedMultiplier));
    public CustomEntityBuilder WithRandomFly(int priority = 6, float speedMultiplier = 1.0f) =>
        AddComponent(new RandomFlyBehaviorComponent(priority, speedMultiplier));
    public CustomEntityBuilder WithLookAtPlayer(int priority = 7, float distance = 8.0f, float probability = 0.02f) =>
        AddComponent(new LookAtPlayerBehaviorComponent(priority, distance, probability));
    public CustomEntityBuilder WithRandomLookAround(int priority = 8) => AddComponent(new RandomLookAroundBehaviorComponent(priority));
    public CustomEntityBuilder WithMeleeAttack(int priority = 2, float speedMultiplier = 1.25f, bool trackTarget = true) =>
        AddComponent(new MeleeAttackBehaviorComponent(priority, speedMultiplier, trackTarget));
    public CustomEntityBuilder WithRangedAttack(int priority = 2, float intervalMin = 1.0f, float intervalMax = 3.0f, float radius = 16.0f) =>
        AddComponent(new RangedAttackBehaviorComponent(priority, intervalMin, intervalMax, radius));
    public CustomEntityBuilder WithLeapAtTarget(int priority = 4, float jumpHeight = 0.4f) =>
        AddComponent(new LeapAtTargetBehaviorComponent(priority, jumpHeight));
    public CustomEntityBuilder WithSwell(int priority = 2, float startDist = 2.5f, float stopDist = 5.0f) =>
        AddComponent(new SwellBehaviorComponent(priority, startDist, stopDist));
    public CustomEntityBuilder WithNearestAttackableTarget(int priority = 1, string targetType = "player", bool mustSee = true) =>
        AddComponent(new NearestAttackableTargetBehaviorComponent(priority, targetType, mustSee));
    public CustomEntityBuilder WithHurtByTarget(int priority = 1, bool alertSameType = false) =>
        AddComponent(new HurtByTargetBehaviorComponent(priority, alertSameType));
    public CustomEntityBuilder WithFollowOwner(int priority = 5, float startDist = 10f, float stopDist = 2f) =>
        AddComponent(new FollowOwnerBehaviorComponent(priority, startDist, stopDist));
    public CustomEntityBuilder WithStayWhileSitting(int priority = 2) => AddComponent(new StayWhileSittingBehaviorComponent(priority));
    public CustomEntityBuilder WithTempt(int priority = 3, float speedMultiplier = 1.0f, params string[] items) =>
        AddComponent(new TemptBehaviorComponent(priority, speedMultiplier, items));
    public CustomEntityBuilder WithAvoidMobType(int priority = 4, string entityType = "player", float maxDist = 6.0f, float walkSpeed = 1.0f, float sprintSpeed = 1.2f) =>
        AddComponent(new AvoidMobTypeBehaviorComponent(priority, entityType, maxDist, walkSpeed, sprintSpeed));

    // --- Entity Properties & Component Groups ---

    public CustomEntityBuilder WithProperty(EntityProperty property)
    {
        _properties[property.Name] = property;
        return this;
    }
    public CustomEntityBuilder AddProperty(EntityProperty property) => WithProperty(property);

    public CustomEntityBuilder WithIntProperty(string name, int min, int max, int defaultValue = 0, bool clientSync = true) =>
        WithProperty(EntityProperty.CreateInt(name, defaultValue, min, max, clientSync));
    public CustomEntityBuilder SetIntProperty(string name, int min, int max, int defaultValue = 0, bool clientSync = true) =>
        WithIntProperty(name, min, max, defaultValue, clientSync);

    public CustomEntityBuilder WithFloatProperty(string name, float min, float max, float defaultValue = 0f, bool clientSync = true) =>
        WithProperty(EntityProperty.CreateFloat(name, defaultValue, min, max, clientSync));
    public CustomEntityBuilder SetFloatProperty(string name, float min, float max, float defaultValue = 0f, bool clientSync = true) =>
        WithFloatProperty(name, min, max, defaultValue, clientSync);

    public CustomEntityBuilder WithBoolProperty(string name, bool defaultValue = false, bool clientSync = true) =>
        WithProperty(EntityProperty.CreateBool(name, defaultValue, clientSync));
    public CustomEntityBuilder SetBoolProperty(string name, bool defaultValue = false, bool clientSync = true) =>
        WithBoolProperty(name, defaultValue, clientSync);

    public CustomEntityBuilder WithEnumProperty(string name, string[] values, string defaultValue, bool clientSync = true) =>
        WithProperty(EntityProperty.CreateEnum(name, values, defaultValue, clientSync));
    public CustomEntityBuilder SetEnumProperty(string name, string[] values, string defaultValue, bool clientSync = true) =>
        WithEnumProperty(name, values, defaultValue, clientSync);

    public CustomEntityBuilder WithComponentGroup(string name, params IEntityComponent[] components)
    {
        _componentGroups[name] = new EntityComponentGroup(name, components);
        return this;
    }
    public CustomEntityBuilder AddComponentGroup(EntityComponentGroup group)
    {
        _componentGroups[group.Name] = group;
        return this;
    }

    public CustomEntityBuilder AddComponent(IEntityComponent component)
    {
        _components[component.Name] = component;
        return this;
    }

    // --- Callbacks C# directs ---

    public CustomEntityBuilder OnSpawn(Action<CustomEntityInstance> onSpawn)
    {
        _onSpawn = onSpawn;
        return this;
    }

    public CustomEntityBuilder OnTick(Action<CustomEntityInstance> onTick)
    {
        _onTick = onTick;
        return this;
    }

    public CustomEntityBuilder OnInteract(Action<BedrockConnection, CustomEntityInstance> onInteract)
    {
        _onInteract = onInteract;
        return this;
    }

    public CustomEntityBuilder OnHurt(Action<CustomEntityInstance, DamageSource> onHurt)
    {
        _onHurt = onHurt;
        return this;
    }

    public CustomEntityBuilder OnDeath(Action<CustomEntityInstance> onDeath)
    {
        _onDeath = onDeath;
        return this;
    }

    // --- Presets statiques ---

    /// <summary>
    /// Creates a pre-configured passive mob builder (e.g. animal/critter) with panic, stroll, and look-around AI.
    /// </summary>
    /// <param name="identifier">Unique entity identifier.</param>
    /// <param name="displayName">Display name.</param>
    /// <param name="health">Initial and maximum health.</param>
    /// <param name="speed">Walking movement speed.</param>
    public static CustomEntityBuilder CreatePassiveMob(string identifier, string displayName, int health = 10, float speed = 0.25f)
    {
        return Create(identifier)
            .WithDisplayName(displayName)
            .WithNameable(allowRenaming: true, alwaysShow: false)
            .WithHealth(health, health)
            .WithMovement(speed)
            .WithTypeFamily("mob", "passive")
            .WithFloatBehavior(0)
            .WithPanicBehavior(1, 1.25f)
            .WithRandomStroll(6, 1.0f)
            .WithLookAtPlayer(7, 8.0f)
            .WithRandomLookAround(8);
    }

    /// <summary>
    /// Creates a simple mob builder (alias for <see cref="CreatePassiveMob"/>).
    /// </summary>
    public static CustomEntityBuilder CreateSimpleMob(string identifier, string displayName, int health = 10, float speed = 0.25f) =>
        CreatePassiveMob(identifier, displayName, health, speed);

    /// <summary>
    /// Creates a pre-configured hostile monster mob builder with player targeting, melee attack, and retaliatory AI.
    /// </summary>
    /// <param name="identifier">Unique entity identifier.</param>
    /// <param name="displayName">Display name.</param>
    /// <param name="health">Initial and maximum health.</param>
    /// <param name="damage">Base melee attack damage.</param>
    /// <param name="speed">Walking movement speed.</param>
    public static CustomEntityBuilder CreateHostileMob(string identifier, string displayName, int health = 20, float damage = 3f, float speed = 0.28f)
    {
        return Create(identifier)
            .WithDisplayName(displayName)
            .WithNameable(allowRenaming: true, alwaysShow: false)
            .WithHealth(health, health)
            .WithMovement(speed)
            .WithAttack(damage)
            .WithTypeFamily("mob", "monster")
            .WithFloatBehavior(0)
            .WithNearestAttackableTarget(1, "player")
            .WithMeleeAttack(2, 1.25f)
            .WithHurtByTarget(3)
            .WithRandomStroll(6, 1.0f)
            .WithLookAtPlayer(7, 8.0f)
            .WithRandomLookAround(8);
    }

    /// <summary>
    /// Creates a pre-configured stationary NPC entity builder with persistent tag and look-around AI.
    /// </summary>
    /// <param name="identifier">Unique entity identifier.</param>
    /// <param name="displayName">Display name (always visible above head).</param>
    public static CustomEntityBuilder CreateNpc(string identifier, string displayName)
    {
        return Create(identifier)
            .WithDisplayName(displayName)
            .WithNameable(allowRenaming: false, alwaysShow: true)
            .WithPersistent(true)
            .WithNpc(true)
            .WithMovement(0.0f)
            .WithLookAtPlayer(1, 8.0f)
            .WithRandomLookAround(2);
    }

    /// <summary>
    /// Creates a pre-configured boss entity builder with boss bar HUD, high health, scale, and combat AI.
    /// </summary>
    /// <param name="identifier">Unique entity identifier.</param>
    /// <param name="bossBarTitle">Title displayed on the boss health bar.</param>
    /// <param name="health">Initial and maximum health.</param>
    /// <param name="damage">Base melee attack damage.</param>
    /// <param name="scale">Scale multiplier (default 1.5x).</param>
    public static CustomEntityBuilder CreateBoss(string identifier, string bossBarTitle, int health = 200, float damage = 8f, float scale = 1.5f)
    {
        return Create(identifier)
            .WithDisplayName(bossBarTitle)
            .WithBoss(bossBarTitle, hudRange: 64, shouldDarkenSky: true)
            .WithNameable(allowRenaming: false, alwaysShow: true)
            .WithHealth(health, health)
            .WithAttack(damage)
            .WithMovement(0.3f)
            .WithScale(scale)
            .WithKnockbackResistance(0.8f)
            .WithTypeFamily("mob", "monster", "boss")
            .WithFloatBehavior(0)
            .WithNearestAttackableTarget(1, "player")
            .WithMeleeAttack(2, 1.3f)
            .WithHurtByTarget(3)
            .WithRandomStroll(6, 1.0f);
    }

    /// <summary>
    /// Compiles all configured components, properties, groups, and callbacks into an immutable <see cref="CustomEntity"/>.
    /// </summary>
    public CustomEntity Build() => new(
        _identifier,
        _displayName,
        _runtimeIdentifier,
        _summonable,
        _hasSpawnEgg,
        _components,
        _properties,
        _componentGroups,
        _onSpawn,
        _onTick,
        _onInteract,
        _onHurt,
        _onDeath);

    /// <summary>
    /// Builds and immediately registers the custom entity in <see cref="CustomEntityRegistry"/>.
    /// </summary>
    /// <returns>The registered <see cref="CustomEntity"/> instance.</returns>
    public CustomEntity Register()
    {
        var entity = Build();
        CustomEntityRegistry.Register(entity);
        return entity;
    }
}
