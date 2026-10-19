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
/// Fired when a player uses or interacts with a block (open, till, strip, compost, cauldron, bell, cake, pot, interact).
/// </summary>
public sealed class BlockUseEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public BlockPos Pos { get; }
    public uint BlockRuntimeId { get; }
    public ItemStack Item { get; }
    public string Action { get; }

    public BlockUseEvent(BedrockConnection player, BlockPos pos, uint blockRuntimeId, ItemStack item, string action)
    {
        Player = player;
        Pos = pos;
        BlockRuntimeId = blockRuntimeId;
        Item = item;
        Action = action;
    }
}
