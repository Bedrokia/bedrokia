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
/// Plugins may clear or modify <see cref="AffectedBlocks"/> or set <see cref="BreaksBlocks"/> to false to prevent block destruction while keeping entity damage.
/// </summary>
public sealed class ExplosionEvent : CancellableEvent
{
    public Vector3 Position { get; }
    public ulong OwnerEntityId { get; }
    public float Power
    {
        get => Radius;
        set => Radius = value;
    }
    public float Radius { get; set; }
    public bool Fire { get; set; }
    public bool BreaksBlocks { get; set; }
    public List<BlockPos> AffectedBlocks { get; }

    public ExplosionEvent(Vector3 position, float radius, List<BlockPos> affectedBlocks, ulong ownerEntityId = 0, bool fire = false, bool breaksBlocks = true)
    {
        Position = position;
        Radius = radius;
        AffectedBlocks = affectedBlocks;
        OwnerEntityId = ownerEntityId;
        Fire = fire;
        BreaksBlocks = breaksBlocks;
    }

    public ExplosionEvent(Vector3 position, ulong ownerEntityId, float power, bool fire = false, bool breaksBlocks = true)
    {
        Position = position;
        OwnerEntityId = ownerEntityId;
        Radius = power;
        Fire = fire;
        BreaksBlocks = breaksBlocks;
        AffectedBlocks = [];
    }
}
