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

using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player tames an animal (wolf, cat, parrot, horse).
/// </summary>
public sealed class MobTameEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public ulong EntityId { get; }
    public string Identifier { get; }

    public MobTameEvent(BedrockConnection player, ulong entityId, string identifier)
    {
        Player = player;
        EntityId = entityId;
        Identifier = identifier;
    }
}
