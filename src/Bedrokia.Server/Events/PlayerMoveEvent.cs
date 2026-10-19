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
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player moves or rotates in the world.
/// If cancelled, the player is returned to <see cref="From"/>.
/// </summary>
public sealed class PlayerMoveEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public Vector3 From { get; }
    public Vector3 To { get; set; }
    public float Yaw { get; }
    public float Pitch { get; }
    public bool IsBlockChange { get; }

    public PlayerMoveEvent(BedrockConnection player, Vector3 from, Vector3 to, float yaw, float pitch, bool isBlockChange)
    {
        Player = player;
        From = from;
        To = to;
        Yaw = yaw;
        Pitch = pitch;
        IsBlockChange = isBlockChange;
    }

    public static implicit operator BedrockConnection(PlayerMoveEvent e) => e.Player;
}
