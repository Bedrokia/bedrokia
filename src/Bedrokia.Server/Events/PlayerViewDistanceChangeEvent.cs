using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player requests a new view distance (chunk radius).
/// </summary>
public sealed class PlayerViewDistanceChangeEvent(BedrockConnection player, int oldDistance, int newDistance) : IPlayerEvent
{
    /// <summary>Creates a new view distance change event with unknown previous distance.</summary>
    public PlayerViewDistanceChangeEvent(BedrockConnection player, int newDistance) : this(player, 0, newDistance) { }

    /// <summary>The player requesting the radius change.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>The previous view distance in chunks.</summary>
    public int OldDistance { get; } = oldDistance;

    /// <summary>The requested view distance in chunks; can be clamped or modified.</summary>
    public int NewDistance { get; set; } = newDistance;
}
