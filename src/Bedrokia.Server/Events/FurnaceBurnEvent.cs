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
/// Fired when fuel is about to be consumed by a furnace.
/// If cancelled, the fuel is not consumed and the furnace does not start burning it.
/// </summary>
public sealed class FurnaceBurnEvent : CancellableEvent
{
    public BlockPos Position { get; }
    public ItemStack Fuel { get; }
    public int BurnDuration { get; set; }

    public FurnaceBurnEvent(BlockPos position, ItemStack fuel, int burnDuration)
    {
        Position = position;
        Fuel = fuel;
        BurnDuration = burnDuration;
    }
}
