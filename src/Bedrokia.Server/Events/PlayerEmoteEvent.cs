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
/// Fired when a player triggers an emote animation.
/// If cancelled, the emote is not broadcast to other players.
/// </summary>
public sealed class PlayerEmoteEvent(BedrockConnection player, string emoteId, byte flags) : CancellableEvent, IPlayerEvent
{
    /// <summary>The player performing the emote.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>The unique ID / UUID of the emote.</summary>
    public string EmoteId { get; } = emoteId;

    /// <summary>Emote flags sent by the client.</summary>
    public byte Flags { get; } = flags;
}

