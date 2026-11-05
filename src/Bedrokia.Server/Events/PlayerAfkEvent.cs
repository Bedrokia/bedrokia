using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player's AFK (away from keyboard) status changes.
/// If cancelled, the status change is rejected.
/// </summary>
public sealed class PlayerAfkEvent(BedrockConnection player, bool newState) : CancellableEvent, IPlayerEvent
{
    /// <summary>The player whose AFK status changed.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>The new AFK state (true = AFK, false = active).</summary>
    public bool NewState { get; set; } = newState;

    /// <summary>Alias for NewState.</summary>
    public bool IsAfk { get => NewState; set => NewState = value; }
}
