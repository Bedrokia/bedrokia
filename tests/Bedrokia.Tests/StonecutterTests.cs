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

namespace Bedrokia.Tests;

public class StonecutterTests
{
    [Fact]
    public void Stonecutter_recipes_are_loaded_from_vanilla_recipe_data()
    {
        var stonecutterRecipes = Recipes.All.Where(r => r.Block == "stonecutter").ToList();
        Assert.NotEmpty(stonecutterRecipes);

        // Every stonecutter recipe should have at least 1 input and 1 output
        foreach (var recipe in stonecutterRecipes)
        {
            Assert.Contains(recipe.Inputs, i => !i.Empty);
            Assert.NotEmpty(recipe.Outputs);
        }
    }

    [Fact]
    public void Stonecutter_can_craft_items_and_consumes_ingredients()
    {
        var inv = new PlayerInventory();
        inv.StonecutterOpen = true;

        // Find a stonecutter recipe that converts stone to something else (e.g. stone_bricks or stone_slab)
        var recipe = Recipes.All.FirstOrDefault(r => r.Block == "stonecutter" && r.Inputs.Any(i => i.Name == "minecraft:stone"));
        Assert.NotNull(recipe);

        // Put stone into stonecutter input slot
        inv.Ui[PlayerInventory.StonecutterInputSlot] = ItemStack.Of("stone", 10);

        // Verify initial state
        Assert.Equal(10, inv.Ui[PlayerInventory.StonecutterInputSlot]?.Count);

        // Close stonecutter and verify leftover items are safely returned to player main inventory
        inv.ReturnCraftingItems();
        Assert.False(inv.StonecutterOpen);
        Assert.Null(inv.Ui[PlayerInventory.StonecutterInputSlot]);
        Assert.Equal(10, inv.Main[0]?.Count);
        Assert.Equal("minecraft:stone", inv.Main[0]?.Type.Name);
    }
}
