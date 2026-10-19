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

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when weather causes a block to change (snow layering, ice freezing/melting, lightning striking copper).
/// </summary>
public sealed class WeatherEffectEvent : CancellableEvent
{
    public BlockPos Pos { get; }
    public uint BlockId { get; }
    public string Cause { get; }
    public uint NewBlockId { get; set; }

    public WeatherEffectEvent(BlockPos pos, uint blockId, string cause, uint newBlockId)
    {
        Pos = pos;
        BlockId = blockId;
        Cause = cause;
        NewBlockId = newBlockId;
    }
}
