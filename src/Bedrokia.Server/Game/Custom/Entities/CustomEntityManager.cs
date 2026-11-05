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
using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Game.Custom.Entities;

/// <summary>
/// Manages live custom entity instances in worlds: handles spawning, per-tick updates,
/// physics, AI routines, viewer synchronization, and despawning.
/// Source: Dragonfly (MIT) server/world entity management.
/// </summary>
public sealed class CustomEntityManager
{
    private readonly BedrokiaServer _server;
    private readonly List<CustomEntityInstance> _entities = [];
    private readonly List<CustomEntityInstance> _scratch = [];
    private readonly List<BedrockConnection> _playerScratch = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomEntityManager"/> class.
    /// </summary>
    /// <param name="server">The Bedrokia server instance.</param>
    public CustomEntityManager(BedrokiaServer server)
    {
        _server = server;
    }

    /// <summary>Gets all active custom entity instances across all worlds.</summary>
    public IReadOnlyList<CustomEntityInstance> All => _entities;

    /// <summary>Returns an enumerable of all living custom entities in the specified world.</summary>
    public IEnumerable<CustomEntityInstance> InWorld(World world) =>
        _entities.Where(e => e.World == world && !e.Dead);

    /// <summary>Finds an active, non-dead custom entity instance by its unique entity ID.</summary>
    public CustomEntityInstance? Find(long entityId) =>
        _entities.FirstOrDefault(e => e.EntityId == entityId && !e.Dead);

    /// <summary>
    /// Spawns a custom entity instance of the given type at the specified world coordinates.
    /// </summary>
    /// <param name="identifier">Registered entity identifier (e.g. <c>custom:fire_dragon</c>).</param>
    /// <param name="world">The target world.</param>
    /// <param name="x">Initial X position.</param>
    /// <param name="y">Initial Y position.</param>
    /// <param name="z">Initial Z position.</param>
    /// <returns>The newly spawned entity instance, or null if the identifier is unknown.</returns>
    public CustomEntityInstance? Spawn(string identifier, World world, double x, double y, double z)
    {
        var definition = CustomEntityRegistry.Get(identifier);
        if (definition is null)
        {
            _server.Logger.Warn($"Cannot spawn unknown custom entity: '{identifier}'");
            return null;
        }

        var entityId = _server.NextEntityId();
        var instance = new CustomEntityInstance(_server, entityId, definition, world, x, y, z);
        _entities.Add(instance);

        _server.Logger.Debug($"Spawned custom entity {definition.Identifier} #{instance.EntityId} at ({x:0.#}, {y:0.#}, {z:0.#}); {_entities.Count} custom entities.");
        return instance;
    }

    /// <summary>
    /// Removes a custom entity instance from the world and sends despawn packets to all tracking viewers.
    /// </summary>
    /// <param name="entity">The entity instance to remove.</param>
    public void Remove(CustomEntityInstance entity)
    {
        if (!_entities.Remove(entity))
            return;

        entity.Dismount();
        foreach (var viewer in entity.Viewers)
            viewer.Send(Packets.RemoveActor(entity.EntityId));
        entity.Viewers.Clear();

        foreach (var viewer in entity.BossViewers)
            viewer.Send(Packets.BossEventHide(entity.EntityId));
        entity.BossViewers.Clear();
    }

    /// <summary>
    /// Cleans up tracking state and dismounts when a player disconnects from the server.
    /// </summary>
    /// <param name="player">The player connection that disconnected.</param>
    public void Forget(BedrockConnection player)
    {
        foreach (var entity in _entities)
        {
            entity.Viewers.Remove(player);
            entity.BossViewers.Remove(player);
            if (entity.Rider == player)
                entity.Dismount();
        }
    }

    /// <summary>
    /// Advances physics, AI behaviors, viewer updates, and death animations for all active entities.
    /// </summary>
    /// <param name="tick">The current server tick counter.</param>
    public void Tick(long tick)
    {
        _playerScratch.Clear();
        var allPlayers = _server.Players;
        for (var i = 0; i < allPlayers.Count; i++)
        {
            var p = allPlayers[i];
            if (p.IsInGame)
                _playerScratch.Add(p);
        }

        _scratch.Clear();
        _scratch.AddRange(_entities);

        for (var i = 0; i < _scratch.Count; i++)
        {
            var entity = _scratch[i];
            if (entity.Dead)
            {
                if (++entity.DeathTicks >= 20)
                {
                    Remove(entity);
                    continue;
                }
            }

            entity.Tick(tick, _playerScratch);
        }
    }
}
