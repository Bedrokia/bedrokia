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
/// Fired when two mobs breed and produce offspring.
/// </summary>
public sealed class MobBreedEvent : CancellableEvent
{
    public ulong ParentA { get; }
    public ulong ParentB { get; }
    public string Identifier { get; }
    public int Experience { get; set; }

    public MobBreedEvent(ulong parentA, ulong parentB, string identifier, int experience = 1)
    {
        ParentA = parentA;
        ParentB = parentB;
        Identifier = identifier;
        Experience = experience;
    }
}
