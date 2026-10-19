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

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a mob attacks a target entity.
/// </summary>
public sealed class MobAttackEvent : CancellableEvent
{
    public ulong EntityId { get; }
    public string Identifier { get; }
    public ulong TargetEntityId { get; }
    public float Damage { get; set; }

    public MobAttackEvent(ulong entityId, string identifier, ulong targetEntityId, float damage)
    {
        EntityId = entityId;
        Identifier = identifier;
        TargetEntityId = targetEntityId;
        Damage = damage;
    }
}
