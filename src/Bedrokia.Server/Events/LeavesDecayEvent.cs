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
/// Fired when leaf blocks decay naturally due to missing wood support.
/// If cancelled, the leaves do not decay.
/// </summary>
public sealed class LeavesDecayEvent(World world, int x, int y, int z, BlockType leaves) : CancellableEvent
{
    /// <summary>The world containing the decaying leaves.</summary>
    public World World { get; } = world;

    /// <summary>X block coordinate.</summary>
    public int X { get; } = x;

    /// <summary>Y block coordinate.</summary>
    public int Y { get; } = y;

    /// <summary>Z block coordinate.</summary>
    public int Z { get; } = z;

    /// <summary>The decaying leaves block type.</summary>
    public BlockType Leaves { get; } = leaves;
}

