using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player breaks or places a block.
/// </summary>
public sealed class BlockEvent(BedrockConnection player, int x, int y, int z, BlockType block) : CancellableEvent, IPlayerEvent
{
    public BedrockConnection Player => player;
    public int X => x;
    public int Y => y;
    public int Z => z;

    /// <summary>The block broken, placed, or clicked.</summary>
    public BlockType Block => block;

    /// <summary>Break only: the items that will drop (empty outside survival); change the list to change them.</summary>
    public List<ItemStack> Drops { get; set; } = [];

    /// <summary>Break only: the experience the player gets.</summary>
    public int Xp { get; set; }
}
