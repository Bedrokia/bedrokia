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

            // Handle durability combination if same type
            if (isSameType)
            {
                var inputDura = input.MaxDurability - input.Damage;
                var matDura = material.MaxDurability - material.Damage;
                var bonus = (int)(input.MaxDurability * 0.12);
                var combined = Math.Min(input.MaxDurability, inputDura + matDura + bonus);
                if (combined > inputDura)
                {
                    result = result.WithDamage(Math.Max(0, input.MaxDurability - combined));
                    actionCost += 2;
                }
                materialConsumed = 1;
            }
            else if (isRepairMat)
            {
                if (input.Damage <= 0)
                    return null; // Already full durability

                var repairDelta = Math.Max(1, input.MaxDurability / 4);
                while (result.Damage > 0 && materialConsumed < material.Count)
                {
                    result = result.WithDamage(Math.Max(0, result.Damage - repairDelta));
                    materialConsumed++;
                    actionCost++;
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

    private static string CleanName(string name) =>
        name.StartsWith("minecraft:") ? name["minecraft:".Length..] : name;
}
