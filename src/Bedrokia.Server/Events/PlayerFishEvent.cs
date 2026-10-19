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
/// Fired when a player uses a fishing rod (cast, reel_in, catch_item, catch_entity).
/// </summary>
public sealed class PlayerFishEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public string Action { get; }
    public ulong HookedEntityId { get; }
    public ItemStack CaughtItem { get; set; }
    public int Experience { get; set; }

    public PlayerFishEvent(BedrockConnection player, string action, ulong hookedEntityId, ItemStack caughtItem, int experience)
    {
        Player = player;
        Action = action;
        HookedEntityId = hookedEntityId;
        CaughtItem = caughtItem;
        Experience = experience;
    }
}
