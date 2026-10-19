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

namespace Bedrokia.Server.Game;

/// <summary>
/// Status effect types with official Bedrock IDs.
/// </summary>
public enum EffectType
{
    Speed = 1,
    Slowness = 2,
    Haste = 3,
    MiningFatigue = 4,
    Strength = 5,
    InstantHealth = 6,
    InstantDamage = 7,
    JumpBoost = 8,
    Nausea = 9,
    Regeneration = 10,
    Resistance = 11,
    FireResistance = 12,
    WaterBreathing = 13,
    Invisibility = 14,
    Blindness = 15,
    NightVision = 16,
    Hunger = 17,
    Weakness = 18,
    Poison = 19,
    Wither = 20,
    HealthBoost = 21,
    Absorption = 22,
    Saturation = 23,
    Levitation = 24,
    FatalPoison = 25,
    SlowFalling = 27,
}

/// <summary>
/// Represents an active or pending instance of a status effect.
/// </summary>
public sealed class EffectInstance
{
    public EffectType Type { get; }
    public int Amplifier { get; }
    public int DurationTicks { get; set; }
    public bool ShowParticles { get; }
    public bool IsAmbient { get; }

    public EffectInstance(EffectType type, int amplifier = 0, int durationTicks = 600, bool showParticles = true, bool isAmbient = false)
    {
        Type = type;
        Amplifier = Math.Max(0, amplifier);
        DurationTicks = durationTicks;
        ShowParticles = showParticles;
        IsAmbient = isAmbient;
    }

    /// <summary>Returns true if this effect is instantaneous (applied immediately without persistent duration).</summary>
    public bool IsInstant => Type is EffectType.InstantHealth or EffectType.InstantDamage;

    /// <summary>Copies this effect instance.</summary>
    public EffectInstance Clone() => new(Type, Amplifier, DurationTicks, ShowParticles, IsAmbient);
}

/// <summary>
/// Helpers for potion metadata and vanilla effect conversions.
/// </summary>
public static class StatusEffects
{
    /// <summary>
    /// Resolves an EffectInstance from Bedrock potion item metadata (aux value).
    /// </summary>
    public static EffectInstance? FromPotionMeta(short meta) => meta switch
    {
        5 => new EffectInstance(EffectType.NightVision, 0, 3600),
        6 => new EffectInstance(EffectType.NightVision, 0, 9600),
        7 => new EffectInstance(EffectType.Invisibility, 0, 3600),
        8 => new EffectInstance(EffectType.Invisibility, 0, 9600),
        9 => new EffectInstance(EffectType.JumpBoost, 0, 3600),
        10 => new EffectInstance(EffectType.JumpBoost, 0, 9600),
        11 => new EffectInstance(EffectType.JumpBoost, 1, 1800),
        12 => new EffectInstance(EffectType.FireResistance, 0, 3600),
        13 => new EffectInstance(EffectType.FireResistance, 0, 9600),
        14 => new EffectInstance(EffectType.Speed, 0, 3600),
        15 => new EffectInstance(EffectType.Speed, 0, 9600),
        16 => new EffectInstance(EffectType.Speed, 1, 1800),
        17 => new EffectInstance(EffectType.Slowness, 0, 1800),
        18 => new EffectInstance(EffectType.Slowness, 0, 4800),
        19 => new EffectInstance(EffectType.WaterBreathing, 0, 3600),
        20 => new EffectInstance(EffectType.WaterBreathing, 0, 9600),
        21 => new EffectInstance(EffectType.InstantHealth, 0, 1),
        22 => new EffectInstance(EffectType.InstantHealth, 1, 1),
        23 => new EffectInstance(EffectType.InstantDamage, 0, 1),
        24 => new EffectInstance(EffectType.InstantDamage, 1, 1),
        25 => new EffectInstance(EffectType.Poison, 0, 900),
        26 => new EffectInstance(EffectType.Poison, 0, 1800),
        27 => new EffectInstance(EffectType.Poison, 1, 420),
        28 => new EffectInstance(EffectType.Regeneration, 0, 900),
        29 => new EffectInstance(EffectType.Regeneration, 0, 1800),
        30 => new EffectInstance(EffectType.Regeneration, 1, 440),
        31 => new EffectInstance(EffectType.Strength, 0, 3600),
        32 => new EffectInstance(EffectType.Strength, 0, 9600),
        33 => new EffectInstance(EffectType.Strength, 1, 1800),
        34 => new EffectInstance(EffectType.Weakness, 0, 1800),
        35 => new EffectInstance(EffectType.Weakness, 0, 4800),
        36 => new EffectInstance(EffectType.Wither, 0, 800),
        40 => new EffectInstance(EffectType.SlowFalling, 0, 1800),
        41 => new EffectInstance(EffectType.SlowFalling, 0, 4800),
        _ => null,
    };
}
