using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player leaves a bed.
/// </summary>
public sealed class PlayerBedLeaveEvent(BedrockConnection player, BlockType? bed = null, int x = 0, int y = 0, int z = 0) : IPlayerEvent
{
    /// <summary>The player leaving the bed.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>The bed block left, if known.</summary>
    public BlockType? Bed { get; } = bed;

    /// <summary>X coordinate of the bed.</summary>
    public int X { get; } = x;

    /// <summary>Y coordinate of the bed.</summary>
    public int Y { get; } = y;

    /// <summary>Z coordinate of the bed.</summary>
    public int Z { get; } = z;
}
