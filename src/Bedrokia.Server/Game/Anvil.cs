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
/// Anvil repair, combination, renaming and enchantment merging mechanics.
/// Ported and adapted from Dragonfly (MIT) server/session/handler_anvil.go and vanilla Bedrock specifications.
/// </summary>
public static class Anvil
{
    public const int MaxLevelCostSurvival = 39;

    public static bool IsAnvilBlock(string? blockName) =>
        blockName is "minecraft:anvil" or "minecraft:chipped_anvil" or "minecraft:damaged_anvil";

    /// <summary>Degrades an anvil block to its next worn state, or air if destroyed.</summary>
    public static BlockType? NextDamageState(BlockType anvilBlock) =>
        anvilBlock.Name switch
        {
            "minecraft:anvil" => Blocks.ByName("chipped_anvil"),
            "minecraft:chipped_anvil" => Blocks.ByName("damaged_anvil"),
            "minecraft:damaged_anvil" => Blocks.Air,
            _ => null
        };

    /// <summary>Checks whether <paramref name="material"/> is a valid raw repair resource for <paramref name="target"/>.</summary>
    public static bool IsRepairMaterial(ItemStack target, ItemStack material)
    {
        var targetName = CleanName(target.Type.Name);
        var matName = CleanName(material.Type.Name);

        if (targetName.StartsWith("wooden_") || targetName is "bow" or "crossbow" or "fishing_rod")
            return matName.EndsWith("_planks") || matName == "planks";
        if (targetName.StartsWith("stone_"))
            return matName is "cobblestone" or "cobbled_deepslate" or "blackstone";
        if (targetName.StartsWith("iron_") || targetName.StartsWith("chainmail_"))
            return matName == "iron_ingot";
        if (targetName.StartsWith("golden_"))
            return matName == "gold_ingot";
        if (targetName.StartsWith("diamond_"))
            return matName == "diamond";
        if (targetName.StartsWith("netherite_"))
            return matName == "netherite_ingot";
        if (targetName.StartsWith("leather_"))
            return matName == "leather";
        if (targetName == "elytra")
            return matName == "phantom_membrane";
        if (targetName == "turtle_helmet")
            return matName is "turtle_scute" or "scute";
        if (targetName == "shield")
            return matName.EndsWith("_planks") || matName == "planks";
        if (targetName == "mace")
            return matName == "breeze_rod";
        if (targetName == "wolf_armor")
            return matName == "armadillo_scute";

        return false;
    }

    /// <summary>
    /// Calculates the anvil operation: result item, total level cost, and number of material items consumed.
    /// Returns null if the operation is invalid or exceeds survival cost limit (> 39).
    /// </summary>
    public static (ItemStack Result, int Cost, int MaterialConsumed)? Calculate(
        ItemStack input, ItemStack? material, string? newName, bool isCreative)
    {
        if (input.Count <= 0)
            return null;

        var targetName = CleanName(input.Type.Name);
        var isTargetBook = targetName == "enchanted_book";
        var isDurable = input.MaxDurability > 0;

        var result = input.Split(input.Count);
        var actionCost = 0;
        var materialConsumed = 0;

        // 1. Process Material (Repair or Combine or Apply Enchanted Book)
        if (material is { Count: > 0 })
        {
            var matName = CleanName(material.Type.Name);
            var isMatBook = matName == "enchanted_book";
            var matEnchantments = material.GetEnchantments();

            // Material can be:
            // a) An enchanted book with at least 1 enchantment
            // b) Another durable item of the exact same type
            // c) A valid raw repair material
            bool isSameType = matName == targetName && isDurable;
            bool isRepairMat = IsRepairMaterial(input, material) && isDurable;

            if (!isMatBook && !isSameType && !isRepairMat)
                return null;

            // Handle combining durable items or repairing with materials
            if (isSameType)
            {
                var inputDmg = input.Damage;
                var matDmg = material.Damage;
                var maxDur = input.MaxDurability;

                var durabilityRestored = (maxDur - matDmg) + (maxDur * 12 / 100);
                var newDamage = Math.Max(0, inputDmg - durabilityRestored);

                if (newDamage < inputDmg)
                {
                    result = result.WithDamage(newDamage);
                    actionCost += 2;
                }
                materialConsumed = 1;
            }
            else if (isRepairMat)
            {
                var maxDur = input.MaxDurability;
                var repairDelta = Math.Min(result.Damage, maxDur / 4);
                if (repairDelta <= 0 && matEnchantments.Count == 0 && string.IsNullOrEmpty(newName))
                    return null;

                while (repairDelta > 0 && materialConsumed < material.Count)
                {
                    result = result.WithDamage(Math.Max(0, result.Damage - repairDelta));
                    materialConsumed++;
                    actionCost++;
                    repairDelta = Math.Min(result.Damage, maxDur / 4);
                }
            }

            // Handle enchantment merging (from book or same item)
            if (matEnchantments.Count > 0 && (isMatBook || isSameType))
            {
                var (mergedResult, compatibleCount, incompatibleCount, enchantCost) =
                    MergeEnchantments(input, material, result, isMatBook);

                if (!isDurable && incompatibleCount > 0 && compatibleCount == 0)
                    return null;

                result = mergedResult;
                actionCost += enchantCost;
                if (materialConsumed == 0)
                    materialConsumed = 1;
            }
        }

        // 2. Renaming
        var renameCost = 0;
        if (!string.IsNullOrEmpty(newName) && newName != (input.CustomName ?? ""))
        {
            renameCost = 1;
            actionCost += renameCost;
            result = result.WithName(newName);
        }

        if (actionCost <= 0)
            return null;

        // 3. Prior Work Penalty (RepairCost)
        var inputPenalty = input.GetRepairCost();
        var matPenalty = material?.GetRepairCost() ?? 0;
        var anvilCost = inputPenalty + matPenalty;
        var totalCost = actionCost + anvilCost;

        // If only renaming, cost cannot exceed 39
        if (renameCost == actionCost && renameCost > 0 && totalCost >= 40)
            totalCost = 39;

        // Survival level cap check
        if (!isCreative && totalCost > MaxLevelCostSurvival)
            return null;

        // 4. Calculate new penalty for result item
        var nextPenalty = Math.Max(inputPenalty, matPenalty);
        if (renameCost != actionCost || renameCost == 0)
            nextPenalty = nextPenalty * 2 + 1;
        result = result.WithRepairCost(nextPenalty);

        return (result, totalCost, materialConsumed);
    }

    private static (ItemStack Result, int CompatibleCount, int IncompatibleCount, int Cost) MergeEnchantments(
        ItemStack input, ItemStack material, ItemStack result, bool isEnchantedBook)
    {
        var inputName = CleanName(input.Type.Name);
        var isInputBook = inputName == "enchanted_book";
        var cost = 0;
        var compatibleCount = 0;
        var incompatibleCount = 0;

        var inputEnchs = input.GetEnchantments().ToDictionary(e => e.Id, e => e.Level);

        foreach (var (matId, matLevel) in material.GetEnchantments())
        {
            var def = Enchanting.ById(matId);
            if (def is null)
                continue;

            // Check item compatibility: books can take any enchant
            if (!isInputBook && !def.Fits(inputName))
            {
                incompatibleCount++;
                continue;
            }

            // Check mutual exclusivity with existing enchantments on input
            var hasConflict = false;
            if (!isInputBook && def.Group is not null)
            {
                foreach (var (existingId, _) in inputEnchs)
                {
                    if (existingId == def.Id)
                        continue;
                    var existingDef = Enchanting.ById(existingId);
                    if (existingDef?.Group is not null && existingDef.Group == def.Group)
                    {
                        hasConflict = true;
                        cost++;
                        break;
                    }
                }
            }

            if (hasConflict)
            {
                incompatibleCount++;
                continue;
            }

            compatibleCount++;
            var resultLevel = matLevel;
            var levelCost = resultLevel;

            if (inputEnchs.TryGetValue(def.Id, out var existingLevel))
            {
                if (existingLevel > resultLevel)
                {
                    incompatibleCount++;
                    continue;
                }
                if (existingLevel == resultLevel)
                {
                    resultLevel = Math.Min(def.MaxLevel, existingLevel + 1);
                }
                levelCost = resultLevel - existingLevel;
            }

            var rarityCost = def.Weight switch
            {
                10 => 1, // Common
                5 => 2,  // Uncommon
                2 => 4,  // Rare
                _ => 8   // Very Rare
            };

            if (isEnchantedBook)
                rarityCost = Math.Max(1, rarityCost / 2);

            result = result.AddEnchantment(def.Id, resultLevel);
            cost += rarityCost * levelCost;
        }

        return (result, compatibleCount, incompatibleCount, cost);
    }

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

    private static string CleanName(string name) =>
        name.StartsWith("minecraft:") ? name["minecraft:".Length..] : name;
}
