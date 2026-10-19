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
/// Fired when a crop, sapling, sugar cane, or cactus grows naturally or via bonemeal.
/// If cancelled, the block does not grow.
/// </summary>
public sealed class BlockGrowEvent : CancellableEvent
{
    public BlockPos Position { get; }
    public BlockType OldBlock { get; }
    public BlockType NewBlock { get; set; }

    public BlockGrowEvent(BlockPos position, BlockType oldBlock, BlockType newBlock)
    {
        Position = position;
        OldBlock = oldBlock;
        NewBlock = newBlock;
    }
}
