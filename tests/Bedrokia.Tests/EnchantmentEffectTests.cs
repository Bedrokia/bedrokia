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

namespace Bedrokia.Tests;

public class EnchantmentEffectTests
{
    [Fact]
    public void Silk_touch_drops_unbroken_block_form()
    {
        var pickaxe = Enchanting.With(ItemStack.Of("diamond_pickaxe")!, [(16, 1)]); // Silk Touch 1
        var stone = Blocks.ByName("stone")!;
        var diamondOre = Blocks.ByName("diamond_ore")!;

        var stoneDrops = DropRules.Drops(stone, pickaxe, Random.Shared);
        Assert.Single(stoneDrops);
        Assert.Equal("minecraft:stone", stoneDrops[0].Type.Name);

        var oreDrops = DropRules.Drops(diamondOre, pickaxe, Random.Shared);
        Assert.Single(oreDrops);
        Assert.Equal("minecraft:diamond_ore", oreDrops[0].Type.Name);
    }

    [Fact]
    public void Fortune_drops_mined_ores_and_gravel_flint()
    {
        var fortunePick = Enchanting.With(ItemStack.Of("diamond_pickaxe")!, [(18, 3)]); // Fortune 3
        var diamondOre = Blocks.ByName("diamond_ore")!;

        var drops = DropRules.Drops(diamondOre, fortunePick, Random.Shared);
        Assert.Single(drops);
        Assert.Equal("minecraft:diamond", drops[0].Type.Name);
        Assert.InRange(drops[0].Count, 1, 4);

        // Fortune 3 guarantees 100% flint from gravel
        var gravel = Blocks.ByName("gravel")!;
        var gravelDrops = DropRules.Drops(gravel, fortunePick, Random.Shared);
        Assert.Single(gravelDrops);
        Assert.Equal("minecraft:flint", gravelDrops[0].Type.Name);
    }

    [Fact]
    public void Normal_mining_without_silk_touch_drops_expected_items()
    {
        var ironPick = ItemStack.Of("iron_pickaxe")!;
        var stone = Blocks.ByName("stone")!;
        var diamondOre = Blocks.ByName("diamond_ore")!;

        var stoneDrops = DropRules.Drops(stone, ironPick, Random.Shared);
        Assert.Single(stoneDrops);
        Assert.Equal("minecraft:cobblestone", stoneDrops[0].Type.Name);

        var oreDrops = DropRules.Drops(diamondOre, ironPick, Random.Shared);
        Assert.Single(oreDrops);
        Assert.Equal("minecraft:diamond", oreDrops[0].Type.Name);
    }
}
