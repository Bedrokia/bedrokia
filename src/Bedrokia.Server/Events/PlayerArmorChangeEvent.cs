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
/// Fired when an armor slot of a player changes (equipped or unequipped).
/// If cancelled, the armor piece is not changed.
/// </summary>
public sealed class PlayerArmorChangeEvent(BedrockConnection player, int slot, ItemStack? oldItem, ItemStack? newItem) : CancellableEvent, IPlayerEvent
{
    /// <summary>The player changing armor.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>Armor slot index (0 = helmet, 1 = chestplate, 2 = leggings, 3 = boots).</summary>
    public int Slot { get; } = slot;

    /// <summary>The previous item in this slot.</summary>
    public ItemStack? OldItem { get; } = oldItem;

    /// <summary>The new item being equipped in this slot.</summary>
    public ItemStack? NewItem { get; set; } = newItem;
}

