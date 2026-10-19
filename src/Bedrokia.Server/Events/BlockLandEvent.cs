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
/// Fired when a falling block lands.
/// If cancelled, the block is not placed and drops as an item instead.
/// </summary>
public sealed class BlockLandEvent : CancellableEvent
{
    public ulong FallingEntityId { get; }
    public BlockPos Pos { get; }
    public uint BlockId { get; set; }

    public BlockLandEvent(ulong fallingEntityId, BlockPos pos, uint blockId)
    {
        FallingEntityId = fallingEntityId;
        Pos = pos;
        BlockId = blockId;
    }
}
