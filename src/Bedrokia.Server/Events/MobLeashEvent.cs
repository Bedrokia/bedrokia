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
/// Fired when a mob is leashed to a player or fence post, or unleashed.
/// </summary>
public sealed class MobLeashEvent : CancellableEvent
{
    public BedrockConnection? Player { get; }
    public ulong EntityId { get; }
    public string Identifier { get; }
    public bool Leashed { get; }
    public bool Fence { get; }
    public BlockPos FencePos { get; }

    public MobLeashEvent(BedrockConnection? player, ulong entityId, string identifier, bool leashed, bool fence, BlockPos fencePos)
    {
        Player = player;
        EntityId = entityId;
        Identifier = identifier;
        Leashed = leashed;
        Fence = fence;
        FencePos = fencePos;
    }
}
