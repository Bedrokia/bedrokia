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

public class GrindstoneTests
{
    [Fact]
    public void Grindstone_identifies_curses_correctly()
    {
        Assert.True(Grindstone.IsCurse(Grindstone.BindingCurseId));
        Assert.True(Grindstone.IsCurse(Grindstone.VanishingCurseId));
        Assert.True(Grindstone.IsCurse("binding_curse"));
        Assert.True(Grindstone.IsCurse("minecraft:vanishing_curse"));
        Assert.False(Grindstone.IsCurse(9)); // sharpness
        Assert.False(Grindstone.IsCurse("sharpness"));
    }

    [Fact]
    public void Grindstone_removes_normal_enchantments_and_resets_repair_cost()
    {
        var sword = Enchanting.With(ItemStack.Of("diamond_sword")!, [(9, 3), (17, 3)]) // Sharpness 3, Unbreaking 3
            .WithRepairCost(15);

        var result = Grindstone.Calculate(sword, null);
        Assert.NotNull(result);

        var (disenchanted, xp) = result.Value;
        Assert.Empty(disenchanted.GetEnchantments());
        Assert.Equal(0, disenchanted.GetRepairCost());
        Assert.True(xp > 0);
    }

    [Fact]
    public void Grindstone_preserves_curses_while_removing_other_enchantments()
    {
        var helm = Enchanting.With(ItemStack.Of("diamond_helmet")!, [
            (0, 4), // Protection 4
            (Grindstone.BindingCurseId, 1), // Curse of Binding
            (Grindstone.VanishingCurseId, 1) // Curse of Vanishing
        ]);

        var result = Grindstone.Calculate(helm, null);
        Assert.NotNull(result);

        var (disenchanted, xp) = result.Value;
        Assert.Equal(0, Enchanting.Level(disenchanted, "protection"));
        Assert.Equal(1, Enchanting.Level(disenchanted, "binding_curse"));
        Assert.Equal(1, Enchanting.Level(disenchanted, "vanishing_curse"));
        Assert.True(xp > 0); // XP refunded from Protection 4
    }

    [Fact]
    public void Grindstone_converts_enchanted_book_to_regular_book()
    {
        var book = Enchanting.With(ItemStack.Of("enchanted_book")!, [(18, 3)]) // Fortune 3
            .WithRepairCost(7);

        var result = Grindstone.Calculate(book, null);
        Assert.NotNull(result);

        var (reverted, xp) = result.Value;
        Assert.Equal("minecraft:book", reverted.Type.Name);
        Assert.Empty(reverted.GetEnchantments());
        Assert.Equal(0, reverted.GetRepairCost());
        Assert.True(xp > 0);
    }

    [Fact]
    public void Grindstone_combines_two_damaged_items_with_durability_bonus()
    {
        var sword1 = ItemStack.Of("diamond_sword")!.WithDamage(1000);
        var sword2 = ItemStack.Of("diamond_sword")!.WithDamage(500);

        var result = Grindstone.Calculate(sword1, sword2);
        Assert.NotNull(result);

        var (combined, _) = result.Value;
        var bonus = (int)(sword1.MaxDurability * 0.05);
        var expectedDamage = Math.Max(0, sword1.Damage - (sword2.MaxDurability - sword2.Damage) - bonus);
        Assert.Equal(expectedDamage, combined.Damage);
    }
}
