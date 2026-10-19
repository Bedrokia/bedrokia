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
/// Fired when a player crafts an item from a recipe.
/// If cancelled, the crafting action is rolled back.
/// </summary>
public sealed class PlayerCraftItemEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public ItemStack? Result { get; set; }
    public IReadOnlyList<Ingredient> Ingredients { get; }

    public PlayerCraftItemEvent(BedrockConnection player, ItemStack? result, IReadOnlyList<Ingredient> ingredients)
    {
        Player = player;
        Result = result;
        Ingredients = ingredients;
    }
}
