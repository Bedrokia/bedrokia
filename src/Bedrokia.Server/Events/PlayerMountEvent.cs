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

using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player mounts or dismounts a vehicle/entity (horse, boat, minecart).
/// </summary>
public sealed class PlayerMountEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public ulong VehicleEntityId { get; }
    public bool Mounted { get; }

    public PlayerMountEvent(BedrockConnection player, ulong vehicleEntityId, bool mounted)
    {
        Player = player;
        VehicleEntityId = vehicleEntityId;
        Mounted = mounted;
    }
}
