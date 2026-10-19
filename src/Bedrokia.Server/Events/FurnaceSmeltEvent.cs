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
/// Fired when an item finishes smelting in a furnace, blast furnace, or smoker.
/// If cancelled, the ingredient is not consumed and the product is not produced.
/// </summary>
public sealed class FurnaceSmeltEvent : CancellableEvent
{
    public BlockPos Position { get; }
    public ItemStack Source { get; }
    public ItemStack Result { get; set; }

    public FurnaceSmeltEvent(BlockPos position, ItemStack source, ItemStack result)
    {
        Position = position;
        Source = source;
        Result = result;
    }
}
