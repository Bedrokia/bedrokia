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
/// Fired when a minecart activates or deactivates a detector rail.
/// </summary>
public sealed class DetectorRailEvent : CancellableEvent
{
    public ulong MinecartEntityId { get; }
    public BlockPos Pos { get; }
    public bool Powered { get; }

    public DetectorRailEvent(ulong minecartEntityId, BlockPos pos, bool powered)
    {
        MinecartEntityId = minecartEntityId;
        Pos = pos;
        Powered = powered;
    }
}
