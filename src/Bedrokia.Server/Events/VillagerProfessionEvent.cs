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

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a villager acquires or changes its profession.
/// </summary>
public sealed class VillagerProfessionEvent : CancellableEvent
{
    public ulong EntityId { get; }
    public string Profession { get; }
    public BlockPos JobSitePos { get; }

    public VillagerProfessionEvent(ulong entityId, string profession, BlockPos jobSitePos)
    {
        EntityId = entityId;
        Profession = profession;
        JobSitePos = jobSitePos;
    }
}
