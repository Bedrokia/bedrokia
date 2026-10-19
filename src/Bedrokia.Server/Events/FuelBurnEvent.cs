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
/// Fired when fuel starts burning in a cooking station (furnace, blast_furnace, smoker, campfire).
/// </summary>
public sealed class FuelBurnEvent : CancellableEvent
{
    public BlockPos Pos { get; }
    public string Station { get; }
    public ItemStack Fuel { get; }
    public int BurnTicks { get; set; }

    public FuelBurnEvent(BlockPos pos, string station, ItemStack fuel, int burnTicks)
    {
        Pos = pos;
        Station = station;
        Fuel = fuel;
        BurnTicks = burnTicks;
    }
}
