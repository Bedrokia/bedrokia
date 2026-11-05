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
/// Fired when farmland block moisture level changes.
/// </summary>
public sealed class MoistureChangeEvent(World world, int x, int y, int z, int oldMoisture, int newMoisture) : CancellableEvent
{
    /// <summary>The world containing the farmland.</summary>
    public World World { get; } = world;

    /// <summary>X coordinate of farmland block.</summary>
    public int X { get; } = x;

    /// <summary>Y coordinate of farmland block.</summary>
    public int Y { get; } = y;

    /// <summary>Z coordinate of farmland block.</summary>
    public int Z { get; } = z;

    /// <summary>Previous moisture level (0-7).</summary>
    public int OldMoisture { get; } = oldMoisture;

    /// <summary>New moisture level; can be modified.</summary>
    public int NewMoisture { get; set; } = newMoisture;
}

