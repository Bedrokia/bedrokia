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
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player changes the primary or secondary effect of a beacon.
/// </summary>
public sealed class BeaconChangeEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public BlockPos Pos { get; }
    public int PrimaryEffect { get; }
    public int SecondaryEffect { get; }

    public BeaconChangeEvent(BedrockConnection player, BlockPos pos, int primaryEffect, int secondaryEffect)
    {
        Player = player;
        Pos = pos;
        PrimaryEffect = primaryEffect;
        SecondaryEffect = secondaryEffect;
    }
}
