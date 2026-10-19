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
/// Fired when a TNT block is ignited/primed.
/// </summary>
public sealed class TntPrimeEvent : CancellableEvent
{
    public BlockPos Pos { get; }
    public string Cause { get; }
    public ulong OwnerEntityId { get; }
    public int FuseTicks { get; set; }

    public TntPrimeEvent(BlockPos pos, string cause, ulong ownerEntityId, int fuseTicks = 80)
    {
        Pos = pos;
        Cause = cause;
        OwnerEntityId = ownerEntityId;
        FuseTicks = fuseTicks;
    }
}
