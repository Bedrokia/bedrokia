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
/// Fired when a player interacts physically or attacks an entity.
/// </summary>
public sealed class EntityTouchEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public ulong EntityId { get; }
    public string Identifier { get; }
    public bool IsAttack { get; }

    public EntityTouchEvent(BedrockConnection player, ulong entityId, string identifier, bool isAttack)
    {
        Player = player;
        EntityId = entityId;
        Identifier = identifier;
        IsAttack = isAttack;
    }
}
