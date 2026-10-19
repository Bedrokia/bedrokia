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

using Bedrokia.Server.Entities;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a mob/entity takes damage (player attacks, environment, fire, void, etc.).
/// Handlers can modify the damage or cancel the damage entirely.
/// </summary>
public sealed class EntityDamageEvent(
    Mob entity,
    DamageSource source,
    float damage,
    (double X, double Z)? knockbackOrigin = null,
    BedrockConnection? killer = null) : CancellableEvent
{
    public Mob Entity => entity;
    public DamageSource Source => source;
    public float Damage { get; set; } = damage;
    public (double X, double Z)? KnockbackOrigin { get; set; } = knockbackOrigin;
    public BedrockConnection? Killer => killer;
}
