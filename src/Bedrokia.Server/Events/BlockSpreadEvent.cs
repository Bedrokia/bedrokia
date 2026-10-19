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
/// Fired when a block spreads to another position (e.g. grass spreading, fruit stems growing adjacent fruits, mycelium).
/// If cancelled, the block does not spread.
/// </summary>
public sealed class BlockSpreadEvent : CancellableEvent
{
    public BlockPos Source { get; }
    public BlockPos Target { get; }
    public BlockType NewBlock { get; set; }

    public BlockSpreadEvent(BlockPos source, BlockPos target, BlockType newBlock)
    {
        Source = source;
        Target = target;
        NewBlock = newBlock;
    }
}
