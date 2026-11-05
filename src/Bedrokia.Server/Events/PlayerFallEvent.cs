using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player lands and takes fall damage.
/// If cancelled, fall damage is prevented.
/// </summary>
public sealed class PlayerFallEvent(BedrockConnection player, float fallDistance, float damage) : CancellableEvent, IPlayerEvent
{
    /// <summary>The falling player.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>The distance fallen in blocks; can be modified.</summary>
    public float FallDistance { get; set; } = fallDistance;

    /// <summary>Alias for FallDistance.</summary>
    public float Distance { get => FallDistance; set => FallDistance = value; }

    /// <summary>The damage inflicted by the fall; can be modified.</summary>
    public float Damage { get; set; } = damage;
}
