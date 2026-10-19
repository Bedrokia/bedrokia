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

using System.Numerics;
using Bedrokia.Server.Game;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when an explosion selects the list of blocks to destroy.
/// Plugins may cancel this event or modify the block list and item drop behavior.
/// </summary>
public sealed class ExplosionBlocksEvent : CancellableEvent
{
    public Vector3 Position { get; }
    public List<BlockPos> Blocks { get; }
    public bool DropItems { get; set; }

    public ExplosionBlocksEvent(Vector3 position, List<BlockPos> blocks, bool dropItems = true)
    {
        Position = position;
        Blocks = blocks;
        DropItems = dropItems;
    }
}
