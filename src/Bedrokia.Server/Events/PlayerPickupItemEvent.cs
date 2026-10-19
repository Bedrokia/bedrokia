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
/// Fired when a player is about to pick up a dropped item from the ground.
/// If cancelled, the item is not collected and stays on the ground.
/// </summary>
public sealed class PlayerPickupItemEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public ulong ItemEntityId { get; }
    public ItemStack Item { get; set; }
    public DroppedItem? DroppedItem { get; }

    public PlayerPickupItemEvent(BedrockConnection player, DroppedItem item)
    {
        Player = player;
        DroppedItem = item;
        ItemEntityId = (ulong)item.EntityId;
        Item = item.Stack;
    }

    public PlayerPickupItemEvent(BedrockConnection player, ulong itemEntityId, ItemStack item)
    {
        Player = player;
        ItemEntityId = itemEntityId;
        Item = item;
    }
}
