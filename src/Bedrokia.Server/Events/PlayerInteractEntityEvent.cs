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

using Bedrokia.Server.Entities;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player right-clicks or interacts with an entity.
/// If cancelled, the interaction is blocked.
/// </summary>
public sealed class PlayerInteractEntityEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public long EntityId { get; }
    public Mob? Mob { get; }

    public PlayerInteractEntityEvent(BedrockConnection player, long entityId, Mob? mob = null)
    {
        Player = player;
        EntityId = entityId;
        Mob = mob;
    }
}
