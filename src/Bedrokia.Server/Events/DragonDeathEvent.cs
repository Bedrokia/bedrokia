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

using System.Numerics;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when an Ender Dragon is defeated (non-cancellable post-action).
/// </summary>
public sealed class DragonDeathEvent
{
    public ulong DragonEntityId { get; }
    public Vector3 Position { get; }
    public int Experience { get; }

    public DragonDeathEvent(ulong dragonEntityId, Vector3 position, int experience = 12000)
    {
        DragonEntityId = dragonEntityId;
        Position = position;
        Experience = experience;
    }
}
