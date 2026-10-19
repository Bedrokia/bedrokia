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
/// Fired when a block is ignited (flint and steel, lava, lightning, fire spread).
/// </summary>
public sealed class BlockIgniteEvent : CancellableEvent
{
    public BedrockConnection? Player { get; }
    public BlockPos Pos { get; }
    public uint BlockId { get; }
    public string Cause { get; }

    public BlockIgniteEvent(BedrockConnection? player, BlockPos pos, uint blockId, string cause)
    {
        Player = player;
        Pos = pos;
        BlockId = blockId;
        Cause = cause;
    }
}
