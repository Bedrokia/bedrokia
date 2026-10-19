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

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when lightning strikes the world.
/// </summary>
public sealed class LightningStrikeEvent : CancellableEvent
{
    public string Cause { get; }
    public ulong OwnerEntityId { get; }
    public Vector3 Position { get; set; }
    public bool Fire { get; set; }

    public LightningStrikeEvent(string cause, ulong ownerEntityId, Vector3 position, bool fire = true)
    {
        Cause = cause;
        OwnerEntityId = ownerEntityId;
        Position = position;
        Fire = fire;
    }
}
