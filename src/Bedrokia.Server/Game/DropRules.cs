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
/// What a block drops when broken in survival, following vanilla rules: by default the item of the same name;
/// the exceptions below cover the common blocks (ores, stone, plants...). Fortune and Silk Touch are not handled.
/// </summary>
/// <remarks>ponytail: curated subset of vanilla loot tables; extend the tables when a block drops the wrong thing.</remarks>
public static class DropRules
{
    /// <summary>Tool tier: 0 = any pickaxe (wood), 1 = stone, 2 = iron, 3 = diamond.</summary>
    private static readonly Dictionary<string, int> PickaxeTier = new()
    {
        ["stone"] = 0, ["cobblestone"] = 0, ["deepslate"] = 0, ["cobbled_deepslate"] = 0, ["granite"] = 0, ["diorite"] = 0,
        ["andesite"] = 0, ["sandstone"] = 0, ["red_sandstone"] = 0, ["tuff"] = 0, ["calcite"] = 0, ["netherrack"] = 0,
        ["coal_ore"] = 0, ["deepslate_coal_ore"] = 0, ["nether_gold_ore"] = 0, ["nether_quartz_ore"] = 0, ["furnace"] = 0,
        ["iron_ore"] = 1, ["deepslate_iron_ore"] = 1, ["copper_ore"] = 1, ["deepslate_copper_ore"] = 1,
        ["lapis_ore"] = 1, ["deepslate_lapis_ore"] = 1, ["iron_block"] = 1, ["lapis_block"] = 1, ["copper_block"] = 1,
        ["gold_ore"] = 2, ["deepslate_gold_ore"] = 2, ["diamond_ore"] = 2, ["deepslate_diamond_ore"] = 2,
        ["emerald_ore"] = 2, ["deepslate_emerald_ore"] = 2, ["redstone_ore"] = 2, ["deepslate_redstone_ore"] = 2,
        ["lit_redstone_ore"] = 2, ["lit_deepslate_redstone_ore"] = 2, ["gold_block"] = 2, ["diamond_block"] = 2,
        ["emerald_block"] = 2, ["obsidian"] = 3, ["crying_obsidian"] = 3, ["ancient_debris"] = 3, ["netherite_block"] = 3,
    };

    /// <summary>Block name → (item, min count, max count); an empty item means "drops nothing".</summary>
    private static readonly Dictionary<string, (string Item, int Min, int Max)> Special = new()
    {
        ["stone"] = ("cobblestone", 1, 1), ["deepslate"] = ("cobbled_deepslate", 1, 1),
        ["grass_block"] = ("dirt", 1, 1), ["mycelium"] = ("dirt", 1, 1), ["podzol"] = ("dirt", 1, 1), ["dirt_path"] = ("dirt", 1, 1),
        ["farmland"] = ("dirt", 1, 1),
        ["coal_ore"] = ("coal", 1, 1), ["deepslate_coal_ore"] = ("coal", 1, 1),
        ["iron_ore"] = ("raw_iron", 1, 1), ["deepslate_iron_ore"] = ("raw_iron", 1, 1),
        ["copper_ore"] = ("raw_copper", 2, 5), ["deepslate_copper_ore"] = ("raw_copper", 2, 5),
        ["gold_ore"] = ("raw_gold", 1, 1), ["deepslate_gold_ore"] = ("raw_gold", 1, 1),
        ["diamond_ore"] = ("diamond", 1, 1), ["deepslate_diamond_ore"] = ("diamond", 1, 1),
        ["emerald_ore"] = ("emerald", 1, 1), ["deepslate_emerald_ore"] = ("emerald", 1, 1),
        ["lapis_ore"] = ("lapis_lazuli", 4, 9), ["deepslate_lapis_ore"] = ("lapis_lazuli", 4, 9),
        ["redstone_ore"] = ("redstone", 4, 5), ["deepslate_redstone_ore"] = ("redstone", 4, 5),
        ["lit_redstone_ore"] = ("redstone", 4, 5), ["lit_deepslate_redstone_ore"] = ("redstone", 4, 5),
        ["nether_quartz_ore"] = ("quartz", 1, 1), ["nether_gold_ore"] = ("gold_nugget", 2, 6),
        ["clay"] = ("clay_ball", 4, 4), ["glowstone"] = ("glowstone_dust", 2, 4), ["snow_layer"] = ("snowball", 1, 1),
        ["snow"] = ("snowball", 4, 4), ["bookshelf"] = ("book", 3, 3), ["melon_block"] = ("melon_slice", 3, 7),
        ["lit_furnace"] = ("furnace", 1, 1), ["redstone_wire"] = ("redstone", 1, 1), ["sea_lantern"] = ("prismarine_crystals", 2, 3),
        ["glass"] = ("", 0, 0), ["glass_pane"] = ("", 0, 0), ["ice"] = ("", 0, 0), ["packed_ice"] = ("", 0, 0),
        ["blue_ice"] = ("", 0, 0), ["short_grass"] = ("", 0, 0), ["tall_grass"] = ("", 0, 0), ["fern"] = ("", 0, 0),
        ["large_fern"] = ("", 0, 0), ["deadbush"] = ("stick", 0, 2), ["vine"] = ("", 0, 0), ["fire"] = ("", 0, 0),
        ["water"] = ("", 0, 0), ["lava"] = ("", 0, 0), ["flowing_water"] = ("", 0, 0), ["flowing_lava"] = ("", 0, 0),
        ["budding_amethyst"] = ("", 0, 0), ["spawner"] = ("", 0, 0), ["infested_stone"] = ("", 0, 0),
    };

    /// <summary>Leaves: sapling (5 %) and, for oak and dark oak, apple (0.5 %).</summary>
    private static readonly Dictionary<string, string> LeafSaplings = new()
    {
        ["oak_leaves"] = "oak_sapling", ["birch_leaves"] = "birch_sapling", ["spruce_leaves"] = "spruce_sapling",
        ["jungle_leaves"] = "jungle_sapling", ["acacia_leaves"] = "acacia_sapling", ["dark_oak_leaves"] = "dark_oak_sapling",
        ["cherry_leaves"] = "cherry_sapling", ["mangrove_leaves"] = "", ["azalea_leaves"] = "azalea",
        ["azalea_leaves_flowered"] = "flowering_azalea", ["pale_oak_leaves"] = "pale_oak_sapling",
    };

    /// <summary>Items dropped when <paramref name="block"/> is broken in survival with <paramref name="tool"/> held.</summary>
    public static List<ItemStack> Drops(BlockType block, ItemStack? tool, Random random)
    {
        var drops = new List<ItemStack>();
        if (!block.Name.StartsWith("minecraft:"))
        {
            if (ItemStack.Of(block.Name, 1) is { } customDrop)
                drops.Add(customDrop);
            return drops;
        }
        var name = block.Name["minecraft:".Length..];

        if (PickaxeTier.TryGetValue(name, out var tier) && PickaxeLevel(tool) < tier)
            return drops; // wrong or no tool: vanilla drops nothing

        if (LeafSaplings.TryGetValue(name, out var sapling))
        {
            if (sapling.Length > 0 && random.NextDouble() < 0.05)
                Add(drops, sapling, 1);
            if (name is "oak_leaves" or "dark_oak_leaves" && random.NextDouble() < 0.005)
                Add(drops, "apple", 1);
            return drops;
        }
        if (name is "short_grass" or "tall_grass" or "fern" or "large_fern" && random.NextDouble() < 0.125)
        {
            Add(drops, "wheat_seeds", 1);
            return drops;
        }
        if (name == "gravel" && random.NextDouble() < 0.1)
        {
            Add(drops, "flint", 1);
            return drops;
        }

        if (Special.TryGetValue(name, out var rule))
        {
            if (rule.Item.Length > 0)
                Add(drops, rule.Item, random.Next(rule.Min, rule.Max + 1));
            return drops;
        }
        if (name.EndsWith("_door") || name.EndsWith("_bed") || name.Contains("double_slab"))
        {
            // ponytail: two-block and double blocks drop their base item once.
            Add(drops, name.Replace("double_slab", "slab"), name.Contains("double_slab") ? 2 : 1);
            return drops;
        }
        Add(drops, name, 1);
        return drops;
    }

    private static void Add(List<ItemStack> drops, string item, int count)
    {
        if (count > 0 && ItemStack.Of(item, count) is { } stack)
            drops.Add(stack);
    }

    private static readonly Dictionary<string, float> Hardness = LoadHardness();

    /// <summary>
    /// Ticks needed to break <paramref name="block"/> with <paramref name="tool"/> (0 = instant).
    /// Formula from Dragonfly (MIT) block/break_info.go: tool speed / hardness, / 30 if harvestable else / 100.
    /// </summary>
    /// <remarks>ponytail: no enchantments, effects, underwater or airborne slowdowns.</remarks>
    public static int BreakTicks(BlockType block, ItemStack? tool)
    {
        var hardness = Hardness.GetValueOrDefault(block.Name, 1.5f);
        if (hardness <= 0)
            return 0;
        var name = block.Name.StartsWith("minecraft:") ? block.Name["minecraft:".Length..] : block.Name;
        var canHarvest = !PickaxeTier.TryGetValue(name, out var tier) || PickaxeLevel(tool) >= tier;
        var speed = ToolKind(tool) == EffectiveTool(name) && ToolKind(tool) != "" && canHarvest ? ToolSpeed(tool) : 1.0;
        if (speed > 1 && Enchanting.Level(tool, "efficiency") is var efficiency and > 0)
            speed += efficiency * efficiency + 1;
        var damage = speed / hardness / (canHarvest ? 30.0 : 100.0);
        return damage >= 1 ? 0 : (int)Math.Ceiling(1 / damage);
    }

    private static string EffectiveTool(string block) => block switch
    {
        _ when PickaxeTier.ContainsKey(block) || block.Contains("stone") || block.Contains("brick") || block.Contains("ore")
               || block.Contains("deepslate") || block.Contains("terracotta") || (block.Contains("concrete") && !block.Contains("powder"))
               || block.Contains("copper") || block is "furnace" or "anvil" or "cauldron" or "hopper" or "ice" or "packed_ice" => "pickaxe",
        _ when block.Contains("log") || block.Contains("planks") || block.Contains("wood") || block.Contains("fence")
               || block.Contains("door") || block is "crafting_table" or "chest" or "bookshelf" or "barrel" => "axe",
        "dirt" or "grass_block" or "sand" or "red_sand" or "gravel" or "clay" or "snow" or "snow_layer" or "mud"
            or "soul_sand" or "soul_soil" or "farmland" or "podzol" or "mycelium" or "coarse_dirt" or "dirt_path" => "shovel",
        _ when block.Contains("leaves") || block is "hay_block" or "sponge" or "moss_block" => "hoe",
        _ => "",
    };

    private static string ToolKind(ItemStack? tool)
    {
        var name = tool?.Type.Name ?? "";
        foreach (var kind in new[] { "pickaxe", "axe", "shovel", "hoe", "sword" })
            if (name.EndsWith("_" + kind))
                return kind;
        return "";
    }

    private static double ToolSpeed(ItemStack? tool) => tool?.Type.Name.Split(':')[1].Split('_')[0] switch
    {
        "wooden" => 2,
        "stone" => 4,
        "iron" => 6,
        "diamond" => 8,
        "netherite" => 9,
        "golden" => 12,
        _ => 1,
    };

    private static Dictionary<string, float> LoadHardness()
    {
        using var stream = typeof(DropRules).Assembly.GetManifestResourceStream("block_hardness.tsv")
                           ?? throw new InvalidOperationException("block_hardness.tsv resource is missing.");
        using var reader = new StreamReader(stream);
        var values = new Dictionary<string, float>();
        while (reader.ReadLine() is { } line)
        {
            var parts = line.Split('\t');
            if (parts.Length == 2 && float.TryParse(parts[1], System.Globalization.CultureInfo.InvariantCulture, out var value))
                values[parts[0]] = value;
        }
        return values;
    }

    private static int PickaxeLevel(ItemStack? tool) => tool?.Type.Name switch
    {
        "minecraft:wooden_pickaxe" or "minecraft:golden_pickaxe" => 0,
        "minecraft:stone_pickaxe" => 1,
        "minecraft:iron_pickaxe" => 2,
        "minecraft:diamond_pickaxe" or "minecraft:netherite_pickaxe" => 3,
        _ => -1,
    };
}
