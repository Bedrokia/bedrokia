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
/// Fired when a dispenser or dropper dispenses an item into the world.
/// </summary>
public sealed class BlockDispenseEvent : CancellableEvent
{
    public BlockPos Pos { get; }
    public ItemStack Item { get; set; }
    public Vector3 Velocity { get; set; }

    public BlockDispenseEvent(BlockPos pos, ItemStack item, Vector3 velocity)
    {
        Pos = pos;
        Item = item;
        Velocity = velocity;
    }
}
