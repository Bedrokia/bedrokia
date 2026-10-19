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
/// Fired when a mob selects or updates its attack target.
/// </summary>
public sealed class MobTargetEvent : CancellableEvent
{
    public ulong EntityId { get; }
    public string Identifier { get; }
    public ulong TargetEntityId { get; set; }

    public MobTargetEvent(ulong entityId, string identifier, ulong targetEntityId)
    {
        EntityId = entityId;
        Identifier = identifier;
        TargetEntityId = targetEntityId;
    }
}
