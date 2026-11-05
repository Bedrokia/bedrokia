namespace Bedrokia.Server.Events;

using Bedrokia.Server.Network;

/// <summary>
/// Uniform interface implemented by all events involving a player.
/// </summary>
public interface IPlayerEvent
{
    /// <summary>The player associated with this event.</summary>
    BedrockConnection Player { get; }

    /// <summary>Retrieves the player instance uniformly.</summary>
    BedrockConnection player() => Player;
}

/// <summary>
/// Uniform extension methods for player events.
/// </summary>
public static class PlayerEventExtensions
{
    /// <summary>Retrieves the player instance uniformly.</summary>
    public static BedrockConnection player(this IPlayerEvent ev) => ev.Player;
}
