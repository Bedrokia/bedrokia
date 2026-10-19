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
/// Fired when an entity is set on fire.
/// </summary>
public sealed class EntityCombustEvent : CancellableEvent
{
    public ulong EntityId { get; }
    public string Identifier { get; }
    public string Cause { get; }
    public int DurationTicks { get; set; }

    public EntityCombustEvent(ulong entityId, string identifier, string cause, int durationTicks)
    {
        EntityId = entityId;
        Identifier = identifier;
        Cause = cause;
        DurationTicks = durationTicks;
    }
}
