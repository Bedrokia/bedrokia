using Bedrokia.Server.Core;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player's game mode is about to change.
/// If cancelled, the game mode remains unchanged.
/// </summary>
public sealed class PlayerGameModeChangeEvent(BedrockConnection player, GameMode oldGameMode, GameMode newGameMode) : CancellableEvent, IPlayerEvent
{
    /// <summary>Creates a new game mode change event with previous mode inferred from the player.</summary>
    public PlayerGameModeChangeEvent(BedrockConnection player, GameMode newGameMode) : this(player, player.GameMode, newGameMode) { }

    /// <summary>The player whose game mode is changing.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>The current game mode before the change.</summary>
    public GameMode OldGameMode { get; } = oldGameMode;

    /// <summary>The target game mode; can be modified.</summary>
    public GameMode NewGameMode { get; set; } = newGameMode;
}
