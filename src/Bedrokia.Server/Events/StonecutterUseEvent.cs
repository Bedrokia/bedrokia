using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player uses a stonecutter to craft a block variant.
/// If cancelled, the stonecutting craft is aborted.
/// </summary>
public sealed class StonecutterUseEvent(BedrockConnection player, ItemStack input, ItemStack? output) : CancellableEvent, IPlayerEvent
{
    /// <summary>The player using the stonecutter.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>The stone ingredient item consumed.</summary>
    public ItemStack Input { get; } = input;

    /// <summary>The resulting crafted item; can be modified.</summary>
    public ItemStack? Output { get; set; } = output;
}
