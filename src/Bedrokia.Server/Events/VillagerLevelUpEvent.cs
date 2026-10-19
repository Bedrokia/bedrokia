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
/// Fired when a villager gains enough trading experience to advance to the next level.
/// </summary>
public sealed class VillagerLevelUpEvent : CancellableEvent
{
    public ulong EntityId { get; }
    public int Level { get; }
    public string Profession { get; }

    public VillagerLevelUpEvent(ulong entityId, int level, string profession)
    {
        EntityId = entityId;
        Level = level;
        Profession = profession;
    }
}
