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
/// Fired when an Ender Dragon spawns or is respawned.
/// </summary>
public sealed class DragonSpawnEvent : CancellableEvent
{
    public ulong DragonEntityId { get; }
    public Vector3 Position { get; }

    public DragonSpawnEvent(ulong dragonEntityId, Vector3 position)
    {
        DragonEntityId = dragonEntityId;
        Position = position;
    }
}
