using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player empties a liquid bucket into the world.
/// If cancelled, the liquid is not placed.
/// </summary>
public sealed class PlayerBucketEmptyEvent(BedrockConnection player, int x, int y, int z, string bucketItem = "", BlockType? liquidToPlace = null) : CancellableEvent, IPlayerEvent
{
    /// <summary>The player emptying the bucket.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>Target block X coordinate.</summary>
    public int X { get; } = x;

    /// <summary>Target block Y coordinate.</summary>
    public int Y { get; } = y;

    /// <summary>Target block Z coordinate.</summary>
    public int Z { get; } = z;

    /// <summary>The bucket item used (water_bucket, lava_bucket, etc.).</summary>
    public string BucketItem { get; } = bucketItem;

    /// <summary>The liquid block type placed in the world; can be modified.</summary>
    public BlockType? LiquidToPlace { get; set; } = liquidToPlace;
}
