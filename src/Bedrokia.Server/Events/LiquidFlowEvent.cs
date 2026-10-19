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
/// Fired when liquid (water/lava) spreads or flows from one block position to another.
/// </summary>
public sealed class LiquidFlowEvent : CancellableEvent
{
    public BlockPos FromPos { get; }
    public BlockPos ToPos { get; }
    public uint LiquidBlockId { get; set; }

    public LiquidFlowEvent(BlockPos fromPos, BlockPos toPos, uint liquidBlockId)
    {
        FromPos = fromPos;
        ToPos = toPos;
        LiquidBlockId = liquidBlockId;
    }
}
