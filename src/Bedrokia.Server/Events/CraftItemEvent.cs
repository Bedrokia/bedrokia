// ┌───────────────────────────────────────────────────────────────────┐
// │  ____  _____ ____  ____   ___  _  __ ___    _                     │
// │ | __ )| ____|  _ \|  _ \ / _ \| |/ /|_ _|  / \                    │
// │ |  _ \|  _| | | | | |_) | | | | ' /  | |  / _ \                   │
// │ | |_) | |___| |_| |  _ <| |_| | . \  | | / ___ \                  │
// │ |____/|_____|____/|_| \_\\___/|_|\_\|___/_/   \_\                 │
// │                                                                   │
// │  Bedrokia - Minecraft: Bedrock Edition server software            │
// │  Licensed under the Apache License 2.0 - see LICENSE              │
// └───────────────────────────────────────────────────────────────────┘

using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player crafts an item at a crafting table or 2x2 grid.
/// </summary>
public sealed class CraftItemEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public string Station { get; }
    public uint RecipeId { get; }
    public ItemStack Result { get; set; }

    public CraftItemEvent(BedrockConnection player, string station, uint recipeId, ItemStack result)
    {
        Player = player;
        Station = station;
        RecipeId = recipeId;
        Result = result;
    }
}
