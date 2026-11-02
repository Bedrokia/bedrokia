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

using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a client has validated credentials and requests to log in, before admission or world assignment.
/// If cancelled, the player is disconnected with <see cref="DisconnectReason"/>.
/// </summary>
public sealed class PlayerPreLoginEvent(BedrockConnection player, string disconnectReason = "Login cancelled by server.") : CancellableEvent, IPlayerEvent
{
    /// <summary>The player connection attempting to log in.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>Reason shown on the client disconnection screen if cancelled.</summary>
    public string DisconnectReason { get; set; } = disconnectReason;
}

