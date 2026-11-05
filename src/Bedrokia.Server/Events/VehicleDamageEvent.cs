using Bedrokia.Server.Entities;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a vehicle is hit or damaged by a player.
/// If cancelled, the vehicle is not damaged.
/// </summary>
public sealed class VehicleDamageEvent(Vehicle vehicle, BedrockConnection player, float damage = 1f) : CancellableEvent, IPlayerEvent
{
    /// <summary>The vehicle taking damage.</summary>
    public Vehicle Vehicle { get; } = vehicle;

    /// <summary>The player damaging the vehicle.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Damage dealt to the vehicle.</summary>
    public float Damage { get; set; } = damage;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;
}
