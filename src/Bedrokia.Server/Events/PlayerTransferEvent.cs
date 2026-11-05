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
/// Fired when a player is transferred to another Bedrock server.
/// If cancelled, the transfer packet is not sent.
/// </summary>
public sealed class PlayerTransferEvent(BedrockConnection player, string address, ushort port) : CancellableEvent, IPlayerEvent
{
    /// <summary>The player being transferred.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>Target server host address; can be modified.</summary>
    public string Address { get; set; } = address;

    /// <summary>Target server UDP port; can be modified.</summary>
    public ushort Port { get; set; } = port;
}

