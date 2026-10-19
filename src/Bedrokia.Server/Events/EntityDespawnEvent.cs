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

using Bedrokia.Server.Entities;

namespace Bedrokia.Server.Events;

/// <summary>Cause or reason why an entity is despawned / removed from the world.</summary>
public enum EntityDespawnReason
{
    Death,
    Distance,
    ChunkUnload,
    Removed,
    Custom
}

/// <summary>
/// Fired when an entity/mob despawns or is removed from the world.
/// </summary>
public sealed class EntityDespawnEvent(Mob entity, EntityDespawnReason reason)
{
    public Mob Entity => entity;
    public EntityDespawnReason Reason => reason;
}
