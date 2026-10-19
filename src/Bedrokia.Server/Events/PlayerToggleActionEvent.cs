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

public enum ToggleActionType
{
    Sneak,
    Sprint,
    Swim,
    Glide,
    Jump,
}

/// <summary>
/// Fired when a player toggles an action state (sneak, sprint, swim, glide, jump).
/// If cancelled, the state change is rejected.
/// </summary>
public sealed class PlayerToggleActionEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public ToggleActionType Action { get; }
    public bool NewState { get; }

    public PlayerToggleActionEvent(BedrockConnection player, ToggleActionType action, bool newState)
    {
        Player = player;
        Action = action;
        NewState = newState;
    }
}
