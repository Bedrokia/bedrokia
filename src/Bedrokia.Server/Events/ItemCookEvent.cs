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
/// Fired when an item finishes cooking in a cooking station (furnace, blast_furnace, smoker, campfire).
/// </summary>
public sealed class ItemCookEvent : CancellableEvent
{
    public BlockPos Pos { get; }
    public string Station { get; }
    public ItemStack Input { get; }
    public ItemStack Output { get; set; }

    public ItemCookEvent(BlockPos pos, string station, ItemStack input, ItemStack output)
    {
        Pos = pos;
        Station = station;
        Input = input;
        Output = output;
    }
}
