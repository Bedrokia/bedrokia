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
/// Fired when a player enchants an item at an enchanting table.
/// </summary>
public sealed class EnchantItemEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public ItemStack Item { get; }
    public int OptionIndex { get; }
    public int CostLevels { get; set; }

    public EnchantItemEvent(BedrockConnection player, ItemStack item, int optionIndex, int costLevels)
    {
        Player = player;
        Item = item;
        OptionIndex = optionIndex;
        CostLevels = costLevels;
    }
}
