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
/// Fired when an Ender Dragon changes combat phase (circling, strafing, perched, charging).
/// </summary>
public sealed class DragonPhaseEvent : CancellableEvent
{
    public ulong DragonEntityId { get; }
    public int OldPhase { get; }
    public int NewPhase { get; set; }

    public DragonPhaseEvent(ulong dragonEntityId, int oldPhase, int newPhase)
    {
        DragonEntityId = dragonEntityId;
        OldPhase = oldPhase;
        NewPhase = newPhase;
    }
}
