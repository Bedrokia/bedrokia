using Bedrokia.Server.Entities;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player mounts or enters a vehicle (boat or minecart).
/// If cancelled, the player does not enter the vehicle.
/// </summary>
public sealed class VehicleEnterEvent(object vehicle, BedrockConnection player) : CancellableEvent, IPlayerEvent
{
    /// <summary>The vehicle being entered.</summary>
    public object Vehicle { get; } = vehicle;

    /// <summary>The player entering the vehicle.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;
}
