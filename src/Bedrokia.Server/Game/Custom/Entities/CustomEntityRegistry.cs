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
using Bedrokia.Server.Game.Custom.Items;

namespace Bedrokia.Server.Game.Custom.Entities;

/// <summary>
/// Global registry storing custom entity definitions and allocating protocol network IDs.
/// Automatically creates and registers corresponding spawn egg items in <see cref="CustomItemRegistry"/>.
/// Source: Dragonfly (MIT) server/entity registry &amp; Bedrock AvailableActorIdentifiers architecture.
/// </summary>
public static class CustomEntityRegistry
{
    private static int _nextNetworkId = 10000;
    private static readonly Dictionary<string, CustomEntity> ByIdentifier = [];
    private static readonly Dictionary<int, CustomEntity> ByNetworkId = [];
    private static readonly Dictionary<string, int> NetworkIdByIdentifier = [];

    /// <summary>
    /// Resets the registry (useful for testing and hot reloading).
    /// </summary>
    public static void Reset()
    {
        ByIdentifier.Clear();
        ByNetworkId.Clear();
        NetworkIdByIdentifier.Clear();
        _nextNetworkId = 10000;
    }

    /// <summary>Gets the total count of registered custom entity types.</summary>
    public static int Count => ByIdentifier.Count;

    /// <summary>Gets all registered custom entity definitions.</summary>
    public static IReadOnlyCollection<CustomEntity> All => ByIdentifier.Values;

    /// <summary>
    /// Registers a custom entity definition and its associated spawn egg item.
    /// </summary>
    /// <param name="entity">The custom entity definition to register.</param>
    /// <returns>The registered custom entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown if entity is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if an entity with the same identifier is already registered.</exception>
    public static CustomEntity Register(CustomEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (ByIdentifier.ContainsKey(entity.Identifier))
            throw new InvalidOperationException($"Custom entity '{entity.Identifier}' is already registered.");

        var networkId = Interlocked.Increment(ref _nextNetworkId);
        ByIdentifier[entity.Identifier] = entity;
        ByNetworkId[networkId] = entity;
        NetworkIdByIdentifier[entity.Identifier] = networkId;

        // Si l'entité dispose d'un œuf d'apparition, on enregistre automatiquement un CustomItem pour le spawn egg
        if (entity.HasSpawnEgg)
        {
            var eggIdentifier = entity.Identifier + "_spawn_egg";
            if (CustomItemRegistry.Get(eggIdentifier) is null)
            {
                var cleanName = entity.Identifier.Contains(':')
                    ? entity.Identifier[(entity.Identifier.IndexOf(':') + 1)..]
                    : entity.Identifier;

                var displayName = !string.IsNullOrEmpty(entity.DisplayName)
                    ? $"Spawn {entity.DisplayName}"
                    : (entity.Components.TryGetValue(EntityComponentIds.Nameable, out var nameComp)
                       && nameComp is NameableEntityComponent nc
                        ? $"Spawn {nc.Name}"
                        : $"Spawn {cleanName}");

                CustomItemBuilder.Create(eggIdentifier)
                    .WithDisplayName(displayName)
                    .WithIcon("spawn_egg")
                    .WithCreative(CreativeCategory.Items, "itemGroup.name.mobEgg")
                    .Register();
            }
        }

        return entity;
    }

    /// <summary>
    /// Retrieves a custom entity definition by its identifier (e.g. <c>custom:fire_dragon</c>).
    /// </summary>
    /// <param name="identifier">Entity identifier (auto-prefixed with <c>custom:</c> if unqualified).</param>
    /// <returns>The entity definition if found; otherwise, null.</returns>
    public static CustomEntity? Get(string identifier)
    {
        var key = identifier.Contains(':') ? identifier : "custom:" + identifier;
        return ByIdentifier.GetValueOrDefault(key);
    }

    /// <summary>
    /// Retrieves a custom entity definition by its assigned protocol network ID.
    /// </summary>
    /// <param name="networkId">Unique protocol integer ID.</param>
    public static CustomEntity? GetByNetworkId(int networkId) => ByNetworkId.GetValueOrDefault(networkId);

    /// <summary>
    /// Gets the allocated protocol network ID for an entity identifier.
    /// </summary>
    /// <param name="identifier">Entity identifier.</param>
    /// <returns>Network ID if found; otherwise 0.</returns>
    public static int GetNetworkId(string identifier)
    {
        var key = identifier.Contains(':') ? identifier : "custom:" + identifier;
        return NetworkIdByIdentifier.GetValueOrDefault(key, 0);
    }

    /// <summary>
    /// Generates the NBT compound for the Bedrock <see cref="Bedrokia.Protocol.PacketId.AvailableActorIdentifiers"/> packet (<c>idlist</c>).
    /// Source: gophertunnel (MIT) minecraft/protocol/packet/available_actor_identifiers.go.
    /// </summary>
    public static OrderedDictionary<string, object> GetIdentifiersNbt()
    {
        var idlist = new List<object>();
        foreach (var (networkId, entity) in ByNetworkId)
        {
            idlist.Add(entity.ToIdentifierNbt(networkId));
        }

        return new OrderedDictionary<string, object>
        {
            ["idlist"] = new NbtList(Nbt.Compound, idlist)
        };
    }

    /// <summary>
    /// Generates the NBT compound for the Bedrock <see cref="Bedrokia.Protocol.PacketId.SyncActorProperty"/> packet.
    /// Source: gophertunnel (MIT) minecraft/protocol/packet/sync_actor_property.go.
    /// </summary>
    public static OrderedDictionary<string, object> GetSyncPropertiesNbt()
    {
        var root = new OrderedDictionary<string, object>();
        foreach (var entity in ByIdentifier.Values)
        {
            if (entity.Properties.Count > 0)
            {
                var propDict = new OrderedDictionary<string, object>();
                foreach (var (_, prop) in entity.Properties)
                    propDict[prop.Name] = prop.BuildNbt();

                root[entity.Identifier] = new OrderedDictionary<string, object>
                {
                    ["properties"] = propDict
                };
            }
        }
        return root;
    }

    /// <summary>
    /// Resets all registered custom entities and IDs for unit testing isolation.
    /// </summary>
    public static void ResetForTesting()
    {
        ByIdentifier.Clear();
        ByNetworkId.Clear();
        NetworkIdByIdentifier.Clear();
        _nextNetworkId = 10000;
    }
}
