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

using Bedrokia.Server.Game;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when the world spawn point coordinates are changed.
/// </summary>
public sealed class SpawnChangeEvent(World world, (int X, int Y, int Z) oldSpawn, (int X, int Y, int Z) newSpawn) : CancellableEvent
{
    /// <summary>The world whose spawn is changing.</summary>
    public World World { get; } = world;

    /// <summary>Previous spawn coordinates.</summary>
    public (int X, int Y, int Z) OldSpawn { get; } = oldSpawn;

    /// <summary>New spawn coordinates; can be modified.</summary>
    public (int X, int Y, int Z) NewSpawn { get; set; } = newSpawn;
}

