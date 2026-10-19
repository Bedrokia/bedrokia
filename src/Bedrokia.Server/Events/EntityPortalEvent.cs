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
/// Fired when a non-player entity travels through a nether or end portal.
/// </summary>
public sealed class EntityPortalEvent : CancellableEvent
{
    public ulong EntityId { get; }
    public string PortalType { get; }
    public Vector3 From { get; }
    public Vector3 Destination { get; set; }

    public EntityPortalEvent(ulong entityId, string portalType, Vector3 from, Vector3 destination)
    {
        EntityId = entityId;
        PortalType = portalType;
        From = from;
        Destination = destination;
    }
}
