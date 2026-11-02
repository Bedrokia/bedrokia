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

using Bedrokia.Protocol;
using Bedrokia.Server.Game;

namespace Bedrokia.Tests;

public class EnchantingTests
{
    [Fact]
    public void Table_offers_compatible_enchantments_with_rising_costs()
    {
        var sword = ItemStack.Of("diamond_sword")!;
        var (costs, options) = Enchanting.Options(sword, 15, 1234)!.Value;
        Assert.True(costs[0] <= costs[1] && costs[1] <= costs[2]);
        Assert.Equal(30, costs[2]); // 15 bookshelves: the bottom option costs 30 levels
        foreach (var (id, level) in options.SelectMany(o => o))
        {
            var kind = Enchanting.ById(id)!;
            Assert.True(kind.Fits("diamond_sword"), kind.Name);
            Assert.InRange(level, 1, kind.MaxLevel);
        }
        Assert.Null(Enchanting.Options(ItemStack.Of("dirt")!, 0, 1)); // not enchantable
    }

    [Fact]
    public void Enchantments_are_stored_in_item_nbt_and_survive_a_round_trip()
    {
        var pickaxe = Enchanting.With(ItemStack.Of("iron_pickaxe")!, [(15, 4), (18, 3)]);
        Assert.Equal(4, Enchanting.Level(pickaxe, "efficiency"));
        Assert.Equal(3, Enchanting.Level(pickaxe, "fortune"));

        var back = Nbt.ReadAll(Nbt.Write(pickaxe.Nbt!, network: false), network: false)[0];
        var copy = new ItemStack(pickaxe.Type, 1, 0, 0, back);
        Assert.Equal(4, Enchanting.Level(copy, "efficiency"));
        Assert.Null(Enchanting.Options(pickaxe, 15, 1)); // already enchanted
        Assert.True(DropRules.BreakTicks(Blocks.Stone, pickaxe) < DropRules.BreakTicks(Blocks.Stone, ItemStack.Of("iron_pickaxe")));
    }

    [Fact]
    public void Anvil_renames_repairs_and_applies_books()
    {
        var sword = ItemStack.Of("diamond_sword")!;
        var (named, cost, _) = Anvil.Combine(sword, null, "Excalibur")!.Value;
        Assert.Equal("Excalibur", named.CustomName);
        Assert.Equal(1, cost);
        Assert.Null(Anvil.Combine(sword, null, null)); // nothing to do

        var worn = new ItemStack(sword.Type, 1, 0, 0, new OrderedDictionary<string, object> { ["Damage"] = 1000 });
        var (repaired, _, used) = Anvil.Combine(worn, ItemStack.Of("diamond")!.Split(2), null)!.Value;
        Assert.Equal(1000 - 2 * (Durability.Max(sword) / 4), Durability.Damage(repaired)); // a quarter per diamond
        Assert.Equal(2, used);
        Assert.Null(Anvil.Combine(worn, ItemStack.Of("iron_ingot"), null)); // not its material

        var book = Enchanting.With(ItemStack.Of("enchanted_book")!, [(9, 3)]); // sharpness III
        var (sharp, bookCost, _) = Anvil.Combine(sword, book, null)!.Value;
        Assert.Equal(3, Enchanting.Level(sharp, "sharpness"));
        Assert.True(bookCost > 0);
        Assert.Equal(1, Anvil.RepairCost(sharp)); // the next use costs more
    }
}
