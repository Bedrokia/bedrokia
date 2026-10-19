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
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a tripwire hook or string is triggered or untriggered.
/// </summary>
public sealed class TripwireEvent : CancellableEvent
{
    public BedrockConnection? Player { get; }
    public ulong EntityId { get; }
    public BlockPos Pos { get; }
    public bool Tripped { get; }

    public TripwireEvent(BedrockConnection? player, ulong entityId, BlockPos pos, bool tripped)
    {
        Player = player;
        EntityId = entityId;
        Pos = pos;
        Tripped = tripped;
    }
}
