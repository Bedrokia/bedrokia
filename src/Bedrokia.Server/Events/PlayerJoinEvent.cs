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
/// Fired when a player has spawned and joined the world.
/// BroadcastMessage can be modified or set to null/empty to suppress the join announcement.
/// </summary>
public sealed class PlayerJoinEvent
{
    public BedrockConnection Player { get; }
    public string? BroadcastMessage { get; set; }

    public PlayerJoinEvent(BedrockConnection player, string? broadcastMessage)
    {
        Player = player;
        BroadcastMessage = broadcastMessage;
    }

    public static implicit operator BedrockConnection(PlayerJoinEvent e) => e.Player;
}
