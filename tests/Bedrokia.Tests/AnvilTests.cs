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

public class AnvilTests
{
    [Fact]
    public void Anvil_recognizes_all_anvil_variants()
    {
        Assert.True(Anvil.IsAnvilBlock("minecraft:anvil"));
        Assert.True(Anvil.IsAnvilBlock("minecraft:chipped_anvil"));
        Assert.True(Anvil.IsAnvilBlock("minecraft:damaged_anvil"));
        Assert.False(Anvil.IsAnvilBlock("minecraft:crafting_table"));
        Assert.False(Anvil.IsAnvilBlock("minecraft:stone"));
    }

    [Fact]
    public void Anvil_degrades_through_correct_damage_states()
    {
        var anvil = Blocks.ByName("anvil")!;
        var chipped = Anvil.NextDamageState(anvil);
        Assert.NotNull(chipped);
        Assert.Equal("minecraft:chipped_anvil", chipped.Name);

        var damaged = Anvil.NextDamageState(chipped);
        Assert.NotNull(damaged);
        Assert.Equal("minecraft:damaged_anvil", damaged.Name);

        var broken = Anvil.NextDamageState(damaged);
        Assert.NotNull(broken);
        Assert.Equal(Blocks.Air, broken);
    }

    [Fact]
    public void Anvil_checks_repair_materials_correctly()
    {
        var sword = ItemStack.Of("diamond_sword")!;
        var diamond = ItemStack.Of("diamond")!;
        var iron = ItemStack.Of("iron_ingot")!;

        Assert.True(Anvil.IsRepairMaterial(sword, diamond));
        Assert.False(Anvil.IsRepairMaterial(sword, iron));

        var ironChest = ItemStack.Of("iron_chestplate")!;
        Assert.True(Anvil.IsRepairMaterial(ironChest, iron));
    }

    [Fact]
    public void Anvil_repairs_durable_items_with_raw_materials()
    {
        var sword = ItemStack.Of("diamond_sword")!.WithDamage(1000);
        var diamonds = ItemStack.Of("diamond", 2)!;

        var result = Anvil.Calculate(sword, diamonds, null, isCreative: false);
        Assert.NotNull(result);

        var (repaired, cost, consumed) = result.Value;
        Assert.True(repaired.Damage < 1000);
        Assert.True(consumed > 0 && consumed <= 2);
        Assert.True(cost > 0);
        Assert.Equal(1, repaired.GetRepairCost()); // initial 0 -> next 1
    }

    [Fact]
    public void Anvil_combines_same_items_with_durability_bonus()
    {
        var sword1 = ItemStack.Of("diamond_sword")!.WithDamage(1000);
        var sword2 = ItemStack.Of("diamond_sword")!.WithDamage(500);

        var result = Anvil.Calculate(sword1, sword2, null, isCreative: false);
        Assert.NotNull(result);

        var (repaired, cost, consumed) = result.Value;
        Assert.Equal(1, consumed);
        // Durability should include 12% bonus
        var bonus = (int)(sword1.MaxDurability * 0.12);
        var expectedDamage = Math.Max(0, sword1.Damage - (sword2.MaxDurability - sword2.Damage) - bonus);
        Assert.Equal(expectedDamage, repaired.Damage);
    }

    [Fact]
    public void Anvil_merges_enchantments_correctly()
    {
        // 1. Same level combines to level + 1 (up to max level)
        var sword1 = Enchanting.With(ItemStack.Of("diamond_sword")!, [(9, 1)]); // Sharpness 1
        var sword2 = Enchanting.With(ItemStack.Of("diamond_sword")!, [(9, 1)]); // Sharpness 1

        var result = Anvil.Calculate(sword1, sword2, null, isCreative: false);
        Assert.NotNull(result);
        Assert.Equal(2, Enchanting.Level(result.Value.Result, "sharpness"));

        // 2. Upgrading to a higher level from material
        var sword3 = Enchanting.With(ItemStack.Of("diamond_sword")!, [(9, 1)]); // Sharpness 1
        var sword4 = Enchanting.With(ItemStack.Of("diamond_sword")!, [(9, 2)]); // Sharpness 2

        var result2 = Anvil.Calculate(sword3, sword4, null, isCreative: false);
        Assert.NotNull(result2);
        Assert.Equal(2, Enchanting.Level(result2.Value.Result, "sharpness"));

        // 3. Damaged item keeps higher enchantment when combined with lower enchantment item
        var sword5 = Enchanting.With(ItemStack.Of("diamond_sword")!, [(9, 2)]).WithDamage(500); // Sharpness 2
        var sword6 = Enchanting.With(ItemStack.Of("diamond_sword")!, [(9, 1)]).WithDamage(200); // Sharpness 1

        var resultKept = Anvil.Calculate(sword5, sword6, null, isCreative: false);
        Assert.NotNull(resultKept);
        Assert.Equal(2, Enchanting.Level(resultKept.Value.Result, "sharpness"));

        // 4. Incompatible enchantments are rejected (e.g. Silk Touch + Fortune)
        var pick1 = Enchanting.With(ItemStack.Of("diamond_pickaxe")!, [(16, 1)]); // Silk Touch
        var pick2 = Enchanting.With(ItemStack.Of("diamond_pickaxe")!, [(18, 1)]); // Fortune 1

        var result3 = Anvil.Calculate(pick1, pick2, null, isCreative: false);
        Assert.NotNull(result3);
        Assert.Equal(1, Enchanting.Level(result3.Value.Result, "silk_touch"));
        Assert.Equal(0, Enchanting.Level(result3.Value.Result, "fortune")); // rejected
    }

    [Fact]
    public void Anvil_renames_item_and_applies_cost()
    {
        var sword = ItemStack.Of("iron_sword")!;
        var result = Anvil.Calculate(sword, null, "Excalibur", isCreative: false);
        Assert.NotNull(result);

        var (renamed, cost, consumed) = result.Value;
        Assert.Equal("Excalibur", renamed.CustomName);
        Assert.Equal(1, cost);
        Assert.Equal(0, consumed);
    }

    [Fact]
    public void Anvil_respects_survival_limit_and_creative_bypass()
    {
        // Item with high prior work penalty
        var sword = ItemStack.Of("diamond_sword")!.WithRepairCost(35);
        var book = Enchanting.With(ItemStack.Of("enchanted_book")!, [(9, 5)]); // Sharpness 5

        // In survival, total cost exceeds 39 ("Too Expensive!")
        var survivalResult = Anvil.Calculate(sword, book, null, isCreative: false);
        Assert.Null(survivalResult);

        // In creative, player can still combine
        var creativeResult = Anvil.Calculate(sword, book, null, isCreative: true);
        Assert.NotNull(creativeResult);
        Assert.True(creativeResult.Value.Cost > 39);
    }
}
