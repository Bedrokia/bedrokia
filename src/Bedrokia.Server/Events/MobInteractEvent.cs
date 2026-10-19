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
/// Fired when a player interacts with a mob (feed, shear, milk, dye).
/// </summary>
public sealed class MobInteractEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public ulong EntityId { get; }
    public string Identifier { get; }
    public ItemStack Item { get; }
    public string Action { get; }

    public MobInteractEvent(BedrockConnection player, ulong entityId, string identifier, ItemStack item, string action)
    {
        Player = player;
        EntityId = entityId;
        Identifier = identifier;
        Item = item;
        Action = action;
    }
}
