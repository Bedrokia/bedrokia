using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player attempts to enter a bed.
/// If cancelled, the player does not enter the bed.
/// </summary>
public sealed class PlayerBedEnterEvent(BedrockConnection player, int x, int y, int z, BlockType? bed = null) : CancellableEvent, IPlayerEvent
{
    /// <summary>The player entering the bed.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>X coordinate of the bed block.</summary>
    public int X { get; } = x;

    /// <summary>Y coordinate of the bed block.</summary>
    public int Y { get; } = y;

    /// <summary>Z coordinate of the bed block.</summary>
    public int Z { get; } = z;

    /// <summary>The bed block.</summary>
    public BlockType? Bed { get; } = bed;
}
