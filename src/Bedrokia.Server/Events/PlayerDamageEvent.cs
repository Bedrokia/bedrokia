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
/// Fired when a player takes damage (combat, fall, void, starvation, etc.).
/// Handlers can modify the damage amount, adjust knockback, or cancel the damage entirely.
/// </summary>
public sealed class PlayerDamageEvent(BedrockConnection player, DamageSource source, float damage, (double X, double Z) knockbackOrigin) : CancellableEvent, IPlayerEvent
{
    public BedrockConnection Player => player;
    public DamageSource Source => source;
    public float Damage { get; set; } = damage;
    public (double X, double Z) KnockbackOrigin { get; set; } = knockbackOrigin;
    /// <summary>Only hits from a player or a mob push back (falls, fire, void... do not).</summary>
    public bool ApplyKnockback { get; set; } = source.Attacker is not null;

    /// <summary>Horizontal push (0.4 = a normal hit, + 0.5 per Knockback level); the upward push is capped at 0.4.</summary>
    public float Knockback { get; set; } = 0.4f;
}
