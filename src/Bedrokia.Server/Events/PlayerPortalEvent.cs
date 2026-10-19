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
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player enters a nether or end portal to change dimension.
/// </summary>
public sealed class PlayerPortalEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public string PortalType { get; }
    public Vector3 From { get; }
    public Vector3 Destination { get; set; }

    public PlayerPortalEvent(BedrockConnection player, string portalType, Vector3 from, Vector3 destination)
    {
        Player = player;
        PortalType = portalType;
        From = from;
        Destination = destination;
    }
}
