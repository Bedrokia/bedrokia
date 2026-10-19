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
/// Broad cause category for damage dealt to entities and players.
/// </summary>
public enum DamageCause
{
    EntityAttack,
    PlayerAttack,
    Fall,
    Void,
    Starvation,
    Fire,
    Lava,
    Drowning,
    Suffocation,
    Explosion,
    Magic,
    Custom
}

/// <summary>
/// Describes what caused damage to an entity or player.
/// </summary>
public sealed record DamageSource(DamageCause Cause, object? Attacker = null, string? Description = null)
{
    /// <summary>Returns the attacking player if the damage was inflicted by a player.</summary>
    public BedrockConnection? AttackingPlayer => Attacker as BedrockConnection;

    /// <summary>Returns the attacking mob if the damage was inflicted by a mob.</summary>
    public Mob? AttackingMob => Attacker as Mob;

    public static DamageSource EntityAttack(Mob mob) => new(DamageCause.EntityAttack, mob, $"Attacked by {mob.Kind.Id}");
    public static DamageSource PlayerAttack(BedrockConnection player) => new(DamageCause.PlayerAttack, player, $"Attacked by {player.Name}");
    public static DamageSource Fall(float distance = 0) => new(DamageCause.Fall, null, $"Fell from {distance:0.#} blocks");
    public static DamageSource Void() => new(DamageCause.Void, null, "Fell into the void");
    public static DamageSource Starvation() => new(DamageCause.Starvation, null, "Starved to death");
    public static DamageSource Fire() => new(DamageCause.Fire, null, "Burned");
    public static DamageSource Lava() => new(DamageCause.Lava, null, "Tried to swim in lava");
    public static DamageSource Drowning() => new(DamageCause.Drowning, null, "Drowned");
    public static DamageSource Explosion() => new(DamageCause.Explosion, null, "Blown up");
    public static DamageSource Custom(string name = "custom") => new(DamageCause.Custom, null, name);
}
