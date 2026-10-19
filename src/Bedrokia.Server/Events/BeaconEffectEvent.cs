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
/// Fired when a beacon applies a status effect to a player.
/// </summary>
public sealed class BeaconEffectEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public BlockPos Pos { get; }
    public int EffectId { get; set; }
    public int Amplifier { get; set; }
    public int DurationTicks { get; set; }

    public BeaconEffectEvent(BedrockConnection player, BlockPos pos, int effectId, int amplifier, int durationTicks)
    {
        Player = player;
        Pos = pos;
        EffectId = effectId;
        Amplifier = amplifier;
        DurationTicks = durationTicks;
    }
}
