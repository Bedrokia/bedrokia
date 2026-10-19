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
/// Fired when a player trades with a villager.
/// </summary>
public sealed class VillagerTradeEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public ulong EntityId { get; }
    public int OfferIndex { get; }
    public int Times { get; }
    public ItemStack ResultItem { get; }
    public int Experience { get; set; }

    public VillagerTradeEvent(BedrockConnection player, ulong entityId, int offerIndex, int times, ItemStack resultItem, int experience)
    {
        Player = player;
        EntityId = entityId;
        OfferIndex = offerIndex;
        Times = times;
        ResultItem = resultItem;
        Experience = experience;
    }
}
