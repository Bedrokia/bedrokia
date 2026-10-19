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
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player uses an End Gateway portal to travel to the outer end islands or back.
/// </summary>
public sealed class EndGatewayEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public BlockPos Pos { get; }
    public Vector3 Destination { get; set; }

    public EndGatewayEvent(BedrockConnection player, BlockPos pos, Vector3 destination)
    {
        Player = player;
        Pos = pos;
        Destination = destination;
    }
}
