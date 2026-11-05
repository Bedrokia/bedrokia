using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player uses an empty bucket to collect a liquid block.
/// If cancelled, the block is not collected.
/// </summary>
public sealed class PlayerBucketFillEvent(BedrockConnection player, int x, int y, int z, BlockType liquidBlock, ItemStack? resultingBucket = null) : CancellableEvent, IPlayerEvent
{
    /// <summary>The player collecting the liquid.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>X position of the collected liquid.</summary>
    public int X { get; } = x;

    /// <summary>Y position of the collected liquid.</summary>
    public int Y { get; } = y;

    /// <summary>Z position of the collected liquid.</summary>
    public int Z { get; } = z;

    /// <summary>The liquid block type collected.</summary>
    public BlockType LiquidBlock { get; } = liquidBlock;

    /// <summary>The resulting filled bucket item; can be modified.</summary>
    public ItemStack? ResultingBucket { get; set; } = resultingBucket;
}
