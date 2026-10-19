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
/// Fired when liquid (water or lava) flows into a block.
/// If cancelled, the flow is blocked.
/// </summary>
public sealed class FluidFlowEvent : CancellableEvent
{
    public BlockPos From { get; }
    public BlockPos To { get; }
    public BlockType FluidBlock { get; set; }

    public FluidFlowEvent(BlockPos from, BlockPos to, BlockType fluidBlock)
    {
        From = from;
        To = to;
        FluidBlock = fluidBlock;
    }
}
