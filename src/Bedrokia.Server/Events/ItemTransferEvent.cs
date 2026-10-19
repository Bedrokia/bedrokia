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
/// Fired when an item is transferred from one container to another via hopper or dropper.
/// </summary>
public sealed class ItemTransferEvent : CancellableEvent
{
    public BlockPos SourcePos { get; }
    public BlockPos TargetPos { get; }
    public ItemStack Item { get; set; }

    public ItemTransferEvent(BlockPos sourcePos, BlockPos targetPos, ItemStack item)
    {
        SourcePos = sourcePos;
        TargetPos = targetPos;
        Item = item;
    }
}
