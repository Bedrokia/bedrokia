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

public enum HealCause
{
    Regeneration,
    Effect,
    Food,
    Custom,
}

/// <summary>
/// Fired when a player is healed.
/// Can be cancelled to prevent healing or modify the amount.
/// </summary>
public sealed class PlayerHealEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public float Amount { get; set; }
    public HealCause Cause { get; }

    public PlayerHealEvent(BedrockConnection player, float amount, HealCause cause)
    {
        Player = player;
        Amount = amount;
        Cause = cause;
    }
}
