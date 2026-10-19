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
/// Fired when a player enchants an item in an enchanting table.
/// If cancelled, the enchantment is aborted.
/// </summary>
public sealed class PlayerEnchantItemEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public ItemStack Item { get; }
    public int LevelCost { get; set; }
    public int LapisCost { get; set; }

    public PlayerEnchantItemEvent(BedrockConnection player, ItemStack item, int levelCost, int lapisCost)
    {
        Player = player;
        Item = item;
        LevelCost = levelCost;
        LapisCost = lapisCost;
    }
}
