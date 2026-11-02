using Bedrokia.Server.Entities;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player dismounts or exits a vehicle.
/// If cancelled, the player remains in the vehicle.
/// </summary>
public sealed class VehicleExitEvent(object vehicle, BedrockConnection player) : CancellableEvent, IPlayerEvent
{
    /// <summary>The vehicle being exited.</summary>
    public object Vehicle { get; } = vehicle;

    /// <summary>The player exiting the vehicle.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;
}
