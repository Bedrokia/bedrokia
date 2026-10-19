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
/// Fired when a block forms or transforms due to environmental conditions (concrete powder touching water, obsidian/cobble forming, sponge absorbing).
/// </summary>
public sealed class BlockFormEvent : CancellableEvent
{
    public BlockPos Pos { get; }
    public uint OldBlockId { get; }
    public uint NewBlockId { get; set; }

    public BlockFormEvent(BlockPos pos, uint oldBlockId, uint newBlockId)
    {
        Pos = pos;
        OldBlockId = oldBlockId;
        NewBlockId = newBlockId;
    }
}
