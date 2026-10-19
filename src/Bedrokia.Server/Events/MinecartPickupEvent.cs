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
/// Fired when a hopper minecart picks up an item from the ground.
/// </summary>
public sealed class MinecartPickupEvent : CancellableEvent
{
    public ulong MinecartEntityId { get; }
    public ulong ItemEntityId { get; }
    public ItemStack Item { get; set; }

    public MinecartPickupEvent(ulong minecartEntityId, ulong itemEntityId, ItemStack item)
    {
        MinecartEntityId = minecartEntityId;
        ItemEntityId = itemEntityId;
        Item = item;
    }
}
