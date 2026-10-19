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
using Bedrokia.Server.Game;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when an explosion occurs (TNT, creeper, etc.).
/// If cancelled, the explosion (both block damage and entity damage) is aborted.
/// Plugins may clear or modify <see cref="AffectedBlocks"/> to prevent block destruction while keeping entity damage.
/// </summary>
public sealed class ExplosionEvent : CancellableEvent
{
    public Vector3 Position { get; }
    public float Radius { get; set; }
    public List<BlockPos> AffectedBlocks { get; }

    public ExplosionEvent(Vector3 position, float radius, List<BlockPos> affectedBlocks)
    {
        Position = position;
        Radius = radius;
        AffectedBlocks = affectedBlocks;
    }
}
