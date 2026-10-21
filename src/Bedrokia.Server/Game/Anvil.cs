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
/// The anvil, ported from Dragonfly (MIT) session/handler_anvil.go: rename, repair with the material (a quarter of
/// the durability per item), repair by combining two of the same item (+12 %), and merge enchantments (from the
/// other item or an enchanted book). The cost in levels adds the items' repair costs (NBT "RepairCost"), which
/// grow with each use; 40 levels or more is "too expensive" (renaming alone stops at 39).
/// </summary>
public static class Anvil
{
    /// <summary>The result, its cost in levels and how many materials it uses; null when nothing can be done.</summary>
    public static (ItemStack Result, int Cost, int MaterialUsed)? Combine(ItemStack input, ItemStack? material, string? rename)
    {
        var result = input;
        var anvilCost = RepairCost(input) + RepairCost(material);
        int actionCost = 0, renameCost = 0, used = 0;
        if (material is not null)
        {
            if (RepairableBy(input, material))
            {
                var max = Durability.Max(input);
                var delta = Math.Min(Durability.Damage(result), max / 4);
                if (delta <= 0)
                    return null; // already fully repaired
                while (delta > 0 && used < material.Count)
                {
                    result = WithDamage(result, Durability.Damage(result) - delta);
                    actionCost++;
                    used++;
                    delta = Math.Min(Durability.Damage(result), max / 4);
                }
            }
            else
            {
                var book = material.Type.Name == "minecraft:enchanted_book" && Enchanting.Of(material).Count > 0;
                var durable = Durability.Max(input) > 0;
                if (!book && (material.Type != input.Type || !durable))
                    return null;
                if (durable && !book)
                {
                    var max = Durability.Max(input);
                    var remaining = Math.Min(max, max - Durability.Damage(input) + max - Durability.Damage(material) + max * 12 / 100);
                    if (remaining > max - Durability.Damage(input))
                    {
                        result = WithDamage(result, max - remaining);
                        actionCost += 2;
                    }
                }
                var (merged, compatible, incompatible, cost) = Merge(input, material, result, actionCost, book);
                if (!durable && incompatible && !compatible)
                    return null;
                result = merged;
                actionCost = cost;
                used = 1;
            }
        }
        if (!string.IsNullOrEmpty(rename) && rename != input.CustomName)
        {
            renameCost = 1;
            actionCost += renameCost;
            result = result.WithName(rename);
        }
        var total = actionCost + anvilCost;
        if (actionCost <= 0)
            return null;
        if (renameCost == actionCost && total >= 40)
            total = 39;
        var newRepairCost = Math.Max(RepairCost(result), RepairCost(material));
        if (renameCost != actionCost)
            newRepairCost = newRepairCost * 2 + 1;
        return (WithRepairCost(result, newRepairCost), total, used);
    }

    // Tools and armour are repaired with what they are made of (vanilla).
    private static bool RepairableBy(ItemStack input, ItemStack material)
    {
        var name = input.Type.Name.Replace("minecraft:", "");
        var with = material.Type.Name.Replace("minecraft:", "");
        var tier = name.Split('_')[0];
        return (tier, with) switch
        {
            ("wooden", _) => with.EndsWith("_planks"),
            ("stone", _) => with is "cobblestone" or "cobbled_deepslate" or "blackstone",
            ("iron" or "chainmail", "iron_ingot") => true,
            ("golden", "gold_ingot") => true,
            ("diamond", "diamond") => true,
            ("netherite", "netherite_ingot") => true,
            ("leather", "leather") => true,
            ("turtle", "turtle_scute" or "scute") => true,
            _ => name switch
            {
                "elytra" => with == "phantom_membrane",
                "shield" => with.EndsWith("_planks"),
                "mace" => with == "breeze_rod",
                _ => false,
            },
        };
    }

    // Dragonfly mergeEnchantments: compatible enchantments are added (same level twice: one level up), each costs
    // its rarity (half for a book) times the levels gained; incompatible ones cost 1 each.
    private static (ItemStack Result, bool Compatible, bool Incompatible, int Cost) Merge(ItemStack input, ItemStack material,
        ItemStack result, int cost, bool book)
    {
        bool hasCompatible = false, hasIncompatible = false;
        var enchantments = Enchanting.Of(result).ToDictionary(e => e.Id, e => e.Level);
        var inputEnchantments = Enchanting.Of(input);
        var inputName = input.Type.Name.Replace("minecraft:", "");
        foreach (var (id, level) in Enchanting.Of(material))
        {
            if (Enchanting.ById(id) is not { } type)
                continue;
            var compatible = type.Fits(inputName) || input.Type.Name == "minecraft:enchanted_book";
            foreach (var (otherId, _) in inputEnchantments)
                if (otherId != id && Enchanting.ById(otherId) is { Group: { } group } && group == type.Group)
                {
                    compatible = false;
                    cost++;
                }
            if (!compatible)
            {
                hasIncompatible = true;
                continue;
            }
            hasCompatible = true;
            var resultLevel = level;
            var levelCost = level;
            if (inputEnchantments.FirstOrDefault(e => e.Id == id) is { Level: > 0 } existing)
            {
                if (existing.Level > level || existing.Level == level && level == type.MaxLevel)
                {
                    hasIncompatible = true;
                    continue;
                }
                if (existing.Level == level)
                    resultLevel++;
                levelCost = resultLevel - existing.Level;
            }
            var rarityCost = type.Weight switch { >= 10 => 1, >= 5 => 2, >= 2 => 4, _ => 8 };
            if (book)
                rarityCost = Math.Max(1, rarityCost / 2);
            enchantments[id] = resultLevel;
            cost += rarityCost * levelCost;
            if (input.Count > 1)
                cost = 40;
        }
        if (hasCompatible)
            result = Enchanting.With(result, enchantments.Select(e => (e.Key, e.Value)));
        return (result, hasCompatible, hasIncompatible, cost);
    }

    /// <summary>Levels this item adds to anvil costs (NBT "RepairCost"), 0 when never used on an anvil.</summary>
    public static int RepairCost(ItemStack? stack) => stack?.Nbt?.GetValueOrDefault("RepairCost") is int cost ? cost : 0;

    private static ItemStack WithRepairCost(ItemStack stack, int cost) => WithTag(stack, "RepairCost", cost);

    private static ItemStack WithDamage(ItemStack stack, int damage) => WithTag(stack, "Damage", Math.Max(0, damage));

    private static ItemStack WithTag(ItemStack stack, string key, int value)
    {
        var nbt = stack.Nbt is null ? new OrderedDictionary<string, object>() : new OrderedDictionary<string, object>(stack.Nbt);
        nbt[key] = value;
        return new ItemStack(stack.Type, stack.Count, stack.Meta, stack.BlockRuntimeId, nbt);
    }
}
