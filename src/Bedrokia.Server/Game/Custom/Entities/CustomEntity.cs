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

using Bedrokia.Protocol;
using Bedrokia.Server.Entities;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Game.Custom.Entities;

/// <summary>
/// Immutable definition of a custom Bedrock entity, containing its identifier, components,
/// properties, component groups, and lifecycle event callbacks.
/// Source: Dragonfly (MIT) entity architecture &amp; Bedrock Entity Definition Specification.
/// </summary>
public sealed class CustomEntity
{
    /// <summary>
    /// Initializes a new custom entity definition.
    /// </summary>
    /// <param name="identifier">Namespaced identifier (e.g. <c>custom:fire_dragon</c>).</param>
    /// <param name="displayName">User-friendly display name (e.g. <c>Fire Dragon</c>).</param>
    /// <param name="runtimeIdentifier">Optional vanilla entity fallback runtime ID (e.g. <c>minecraft:zombie</c>).</param>
    /// <param name="summonable">Whether this entity can be summoned via commands.</param>
    /// <param name="hasSpawnEgg">Whether a spawn egg item should be generated and registered automatically.</param>
    /// <param name="components">Base components defining the entity attributes and behaviors.</param>
    /// <param name="properties">Dynamic synchronized properties.</param>
    /// <param name="componentGroups">Dynamic component groups toggled at runtime.</param>
    /// <param name="onSpawn">Callback invoked when an instance of this entity is spawned.</param>
    /// <param name="onTick">Callback invoked every server tick for this entity.</param>
    /// <param name="onInteract">Callback invoked when a player interacts with this entity.</param>
    /// <param name="onHurt">Callback invoked when this entity takes damage.</param>
    /// <param name="onDeath">Callback invoked when this entity dies.</param>
    public CustomEntity(
        string identifier,
        string displayName = "",
        string runtimeIdentifier = "",
        bool summonable = true,
        bool hasSpawnEgg = true,
        IReadOnlyDictionary<string, IEntityComponent>? components = null,
        IReadOnlyDictionary<string, EntityProperty>? properties = null,
        IReadOnlyDictionary<string, EntityComponentGroup>? componentGroups = null,
        Action<CustomEntityInstance>? onSpawn = null,
        Action<CustomEntityInstance>? onTick = null,
        Action<BedrockConnection, CustomEntityInstance>? onInteract = null,
        Action<CustomEntityInstance, DamageSource>? onHurt = null,
        Action<CustomEntityInstance>? onDeath = null)
    {
        Identifier = identifier.Contains(':') ? identifier : "custom:" + identifier;
        DisplayName = string.IsNullOrEmpty(displayName)
            ? (identifier.Contains(':') ? identifier[(identifier.IndexOf(':') + 1)..] : identifier)
            : displayName;
        RuntimeIdentifier = runtimeIdentifier;
        Summonable = summonable;
        HasSpawnEgg = hasSpawnEgg;
        Components = components ?? new Dictionary<string, IEntityComponent>();
        Properties = properties ?? new Dictionary<string, EntityProperty>();
        ComponentGroups = componentGroups ?? new Dictionary<string, EntityComponentGroup>();
        OnSpawnAction = onSpawn;
        OnTickAction = onTick;
        OnInteractAction = onInteract;
        OnHurtAction = onHurt;
        OnDeathAction = onDeath;
    }

    /// <summary>Gets the unique namespaced identifier of the entity.</summary>
    public string Identifier { get; }
    /// <summary>Gets the user-friendly display name of the entity.</summary>
    public string DisplayName { get; }
    /// <summary>Gets the vanilla fallback runtime identifier if configured.</summary>
    public string RuntimeIdentifier { get; }
    /// <summary>Gets whether the entity can be summoned via commands.</summary>
    public bool Summonable { get; }
    /// <summary>Gets whether this entity has a registered spawn egg.</summary>
    public bool HasSpawnEgg { get; }
    /// <summary>Gets the dictionary of base entity components.</summary>
    public IReadOnlyDictionary<string, IEntityComponent> Components { get; }
    /// <summary>Gets the dictionary of dynamic synchronized entity properties.</summary>
    public IReadOnlyDictionary<string, EntityProperty> Properties { get; }
    /// <summary>Gets the dictionary of dynamic component groups.</summary>
    public IReadOnlyDictionary<string, EntityComponentGroup> ComponentGroups { get; }

    /// <summary>Gets the callback invoked upon entity spawn.</summary>
    public Action<CustomEntityInstance>? OnSpawnAction { get; }
    /// <summary>Gets the callback invoked every server tick.</summary>
    public Action<CustomEntityInstance>? OnTickAction { get; }
    /// <summary>Gets the callback invoked on player interaction.</summary>
    public Action<BedrockConnection, CustomEntityInstance>? OnInteractAction { get; }
    /// <summary>Gets the callback invoked when the entity takes damage.</summary>
    public Action<CustomEntityInstance, DamageSource>? OnHurtAction { get; }
    /// <summary>Gets the callback invoked upon entity death.</summary>
    public Action<CustomEntityInstance>? OnDeathAction { get; }

    /// <summary>Returns true if the entity definition includes the specified component.</summary>
    public bool HasComponent(string name) => Components.ContainsKey(name);
    /// <summary>Retrieves a typed component instance, or null if absent.</summary>
    public T? GetComponent<T>() where T : class, IEntityComponent => Components.Values.OfType<T>().FirstOrDefault();

    /// <summary>Gets base health from components (default 20).</summary>
    public float Health => GetComponent<HealthEntityComponent>()?.Value ?? 20f;
    /// <summary>Gets base max health from components (default 20).</summary>
    public float MaxHealth => GetComponent<HealthEntityComponent>()?.Max ?? 20f;
    /// <summary>Gets base movement speed from components (default 0.25).</summary>
    public float Speed => GetComponent<MovementEntityComponent>()?.Value ?? 0.25f;
    /// <summary>Gets collision bounding box width (default 0.6).</summary>
    public float Width => GetComponent<CollisionBoxEntityComponent>()?.Width ?? 0.6f;
    /// <summary>Gets collision bounding box height (default 1.8).</summary>
    public float Height => GetComponent<CollisionBoxEntityComponent>()?.Height ?? 1.8f;
    /// <summary>Gets entity visual and collision scale (default 1.0).</summary>
    public float Scale => GetComponent<ScaleEntityComponent>()?.Value ?? 1.0f;

    /// <summary>
    /// Serializes this entire entity definition into Bedrock NBT format matching entity JSON specs.
    /// </summary>
    public OrderedDictionary<string, object> ToNbt()
    {
        var compDict = new OrderedDictionary<string, object>();
        foreach (var (_, comp) in Components)
            compDict[comp.Name] = comp.BuildNbt();

        var propDict = new OrderedDictionary<string, object>();
        foreach (var (_, prop) in Properties)
            propDict[prop.Name] = prop.BuildNbt();

        var groupDict = new OrderedDictionary<string, object>();
        foreach (var (_, grp) in ComponentGroups)
            groupDict[grp.Name] = grp.BuildNbt();

        var desc = new OrderedDictionary<string, object>
        {
            ["identifier"] = Identifier,
            ["is_spawnable"] = (byte)(HasSpawnEgg ? 1 : 0),
            ["is_summonable"] = (byte)(Summonable ? 1 : 0),
        };
        if (!string.IsNullOrEmpty(RuntimeIdentifier))
            desc["runtime_identifier"] = RuntimeIdentifier;
        if (propDict.Count > 0)
            desc["properties"] = propDict;

        var entityCompound = new OrderedDictionary<string, object>
        {
            ["description"] = desc,
            ["components"] = compDict,
        };
        if (groupDict.Count > 0)
            entityCompound["component_groups"] = groupDict;

        return new OrderedDictionary<string, object>
        {
            ["format_version"] = "1.16.0",
            ["minecraft:entity"] = entityCompound
        };
    }

    /// <summary>
    /// Serializes this entity's entry for the Bedrock AvailableActorIdentifiers network packet.
    /// Source: gophertunnel (MIT) minecraft/protocol/packet/available_actor_identifiers.go
    /// </summary>
    public OrderedDictionary<string, object> ToIdentifierNbt(int networkId) => new()
    {
        ["bid"] = Identifier,
        ["experimental"] = (byte)0,
        ["hasspawnegg"] = (byte)(HasSpawnEgg ? 1 : 0),
        ["id"] = Identifier,
        ["rid"] = string.IsNullOrEmpty(RuntimeIdentifier) ? Identifier : RuntimeIdentifier,
        ["summonable"] = (byte)(Summonable ? 1 : 0),
    };
}
