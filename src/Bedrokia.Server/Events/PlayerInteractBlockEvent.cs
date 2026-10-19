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
/// Fired when a player right-clicks an interactive block (door, chest, lever, bed, button, etc.).
/// If cancelled, the block is not activated and its state is resynchronized with the client.
/// </summary>
public sealed class PlayerInteractBlockEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public BlockPos Position { get; }
    public BlockType Block { get; }
    public int Face { get; }

    public PlayerInteractBlockEvent(BedrockConnection player, BlockPos position, BlockType block, int face)
    {
        Player = player;
        Position = position;
        Block = block;
        Face = face;
    }
}
