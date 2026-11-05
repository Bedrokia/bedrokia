using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when sleeping players cause the night to be skipped to morning.
/// </summary>
public sealed class PlayerSleepSkipNightEvent(BedrockConnection player, World? world = null, IReadOnlyList<BedrockConnection>? sleepingPlayers = null) : IPlayerEvent
{
    /// <summary>The player whose sleeping triggered or participated in the skip.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>The world where night was skipped.</summary>
    public World World { get; } = world ?? player.World;

    /// <summary>The list of players who were sleeping to skip the night.</summary>
    public IReadOnlyList<BedrockConnection> SleepingPlayers { get; } = sleepingPlayers ?? [player];
}
