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

namespace Bedrokia.Server.Game;

/// <summary>
/// Grindstone disenchanting, combination and experience refund mechanics.
/// Ported and adapted from Dragonfly (MIT) server/session/handler_grindstone.go and vanilla Bedrock specifications.
/// </summary>
public static class Grindstone
{
    public const int BindingCurseId = 27;
    public const int VanishingCurseId = 28;

    public static bool IsCurse(int id) => id is BindingCurseId or VanishingCurseId;

    public static bool IsCurse(string name)
    {
        name = name.ToLowerInvariant().Replace("minecraft:", "");
        return name is "binding_curse" or "vanishing_curse";
    }

    /// <summary>
    /// Processes a grindstone craft from one or two items.
    /// Returns the disenchanted/combined result and the total experience refund, or null if invalid.
    /// </summary>
    public static (ItemStack Result, int ExperienceRefund)? Calculate(ItemStack? first, ItemStack? second)
    {
        var hasFirst = first is { Count: > 0 };
        var hasSecond = second is { Count: > 0 };

        if (!hasFirst && !hasSecond)
            return null;

        ItemStack result;
        var removedEnchantments = new List<(int Id, int Level)>();

        if (hasFirst && hasSecond)
        {
            var item1 = first!;
            var item2 = second!;

            if (item1.Type.Name != item2.Type.Name || item1.MaxDurability <= 0)
                return null;

            var dura1 = item1.MaxDurability - item1.Damage;
            var dura2 = item2.MaxDurability - item2.Damage;
            var bonus = (int)(item1.MaxDurability * 0.05);
            var combined = Math.Min(item1.MaxDurability, dura1 + dura2 + bonus);
            var newDamage = Math.Max(0, item1.MaxDurability - combined);

            result = item1.Split(1).WithDamage(newDamage);

            // Collect all enchantments to strip
            foreach (var (id, lvl) in item1.GetEnchantments().Concat(item2.GetEnchantments()))
            {
                if (IsCurse(id))
                {
                    result = result.AddEnchantment(id, lvl);
                }
                else
                {
                    removedEnchantments.Add((id, lvl));
                    result = result.RemoveEnchantment(id);
                }
            }
        }
        else
        {
            var single = hasFirst ? first! : second!;
            result = single.Split(1);

            foreach (var (id, lvl) in single.GetEnchantments())
            {
                if (IsCurse(id))
                    continue;

                removedEnchantments.Add((id, lvl));
                result = result.RemoveEnchantment(id);
            }

            // If an enchanted book has all non-curses removed and no curses remain, revert to regular book
            if (result.Type.Name == "minecraft:enchanted_book" && result.GetEnchantments().Count == 0)
            {
                if (ItemStack.Of("book", 1) is { } book)
                {
                    result = book;
                    if (single.CustomName is not null)
                        result = result.WithName(single.CustomName);
                }
            }
        }

        // Reset repair penalty cost to 0
        result = result.WithRepairCost(0);

        // Calculate experience refund
        var totalEnchantCost = 0;
        foreach (var (id, level) in removedEnchantments)
        {
            var def = Enchanting.ById(id);
            var rarityCost = def?.Weight switch
            {
                10 => 1,
                5 => 2,
                2 => 4,
                _ => 8
            };
            totalEnchantCost += rarityCost * level;
        }

        var minXp = (int)Math.Ceiling(totalEnchantCost / 2.0);
        var xp = minXp > 0 ? minXp + Random.Shared.Next(minXp + 1) : 0;

        return (result, xp);
    }
}
