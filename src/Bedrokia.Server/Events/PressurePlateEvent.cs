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
/// Fired when a pressure plate is stepped on or stepped off.
/// </summary>
public sealed class PressurePlateEvent : CancellableEvent
{
    public BedrockConnection? Player { get; }
    public ulong EntityId { get; }
    public BlockPos Pos { get; }
    public bool Pressed { get; }

    public PressurePlateEvent(BedrockConnection? player, ulong entityId, BlockPos pos, bool pressed)
    {
        Player = player;
        EntityId = entityId;
        Pos = pos;
        Pressed = pressed;
    }
}
