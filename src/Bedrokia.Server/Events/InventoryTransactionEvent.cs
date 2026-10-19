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

/// <summary>Kind of inventory action in an InventoryTransactionEvent.</summary>
public enum InventoryTransactionType
{
    Transfer,
    Swap,
    Drop,
    Craft,
    CreativeCraft,
    Destroy,
    Other
}

/// <summary>
/// Fired when a player performs an action in their inventory or container (moving, swapping, crafting, dropping items).
/// If cancelled, the server rejects the request and automatically resyncs the player's inventory.
/// </summary>
public sealed class InventoryTransactionEvent(
    BedrockConnection player,
    InventoryTransactionType type,
    ItemStack? item,
    int count,
    (byte Container, int Slot) source,
    (byte Container, int Slot)? destination = null) : CancellableEvent
{
    public BedrockConnection Player => player;
    public InventoryTransactionType Type => type;
    public ItemStack? Item => item;
    public int Count => count;
    public (byte Container, int Slot) Source => source;
    public (byte Container, int Slot)? Destination => destination;
}
