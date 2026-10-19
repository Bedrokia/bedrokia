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
/// Fired when an armor stand is equipped or unequipped by a player.
/// </summary>
public sealed class ArmorStandChangeEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public ulong EntityId { get; }
    public int Slot { get; }
    public ItemStack Item { get; }

    public ArmorStandChangeEvent(BedrockConnection player, ulong entityId, int slot, ItemStack item)
    {
        Player = player;
        EntityId = entityId;
        Slot = slot;
        Item = item;
    }
}
