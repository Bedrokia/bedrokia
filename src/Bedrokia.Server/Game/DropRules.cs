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

using System.Collections.Frozen;

namespace Bedrokia.Server.Game;

/// <summary>
/// What a block drops when broken in survival, following vanilla rules: by default the item of the same name;
/// the exceptions below cover the common blocks (ores, stone, plants...). Fortune and Silk Touch are not handled.
/// </summary>
/// <remarks>ponytail: curated subset of vanilla loot tables; extend the tables when a block drops the wrong thing.</remarks>
public static class DropRules
{
    /// <summary>Tool tier: 0 = any pickaxe (wood), 1 = stone, 2 = iron, 3 = diamond.</summary>
    private static readonly FrozenDictionary<string, int> PickaxeTier = new Dictionary<string, int>
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
    }.ToFrozenDictionary();

    /// <summary>Block name → (item, min count, max count); an empty item means "drops nothing".</summary>
    private static readonly FrozenDictionary<string, (string Item, int Min, int Max)> Special = new Dictionary<string, (string Item, int Min, int Max)>
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
    }.ToFrozenDictionary();

    /// <summary>Leaves: sapling (5 %) and, for oak and dark oak, apple (0.5 %).</summary>
    private static readonly FrozenDictionary<string, string> LeafSaplings = new Dictionary<string, string>
    {
        ["oak_leaves"] = "oak_sapling", ["birch_leaves"] = "birch_sapling", ["spruce_leaves"] = "spruce_sapling",
        ["jungle_leaves"] = "jungle_sapling", ["acacia_leaves"] = "acacia_sapling", ["dark_oak_leaves"] = "dark_oak_sapling",
        ["cherry_leaves"] = "cherry_sapling", ["mangrove_leaves"] = "", ["azalea_leaves"] = "azalea",
        ["azalea_leaves_flowered"] = "flowering_azalea", ["pale_oak_leaves"] = "pale_oak_sapling",
    }.ToFrozenDictionary();

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

        // Shears collect leaves and plants themselves.
        if (tool?.Type.Name == "minecraft:shears" && (name.EndsWith("_leaves") || ShearedPlants.Contains(name)))
        {
            Add(drops, name is "tall_grass" ? "short_grass" : name is "large_fern" ? "fern" : name, name is "tall_grass" or "large_fern" ? 2 : 1);
            return drops;
        }

        if (PickaxeTier.TryGetValue(name, out var tier) && PickaxeLevel(tool) < tier)
            return drops; // wrong or no tool: vanilla drops nothing

        var silkTouch = Enchanting.Level(tool, "silk_touch");
        var fortune = Enchanting.Level(tool, "fortune");

        // Silk Touch takes precedence over Fortune and normal drops
        if (silkTouch > 0)
        {
            if (name is not ("spawner" or "budding_amethyst" or "infested_stone" or "fire" or "water" or "flowing_water" or "lava" or "flowing_lava"))
            {
                Add(drops, name, 1);
                return drops;
            }
        }

        if (LeafSaplings.TryGetValue(name, out var sapling))
        {
            var saplingChance = fortune switch { 0 => 0.05, 1 => 0.0625, 2 => 0.0833, _ => 0.10 };
            if (sapling.Length > 0 && random.NextDouble() < saplingChance)
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
        if (name == "gravel")
        {
            var flintChance = fortune switch { 0 => 0.10, 1 => 0.14, 2 => 0.25, _ => 1.0 };
            if (random.NextDouble() < flintChance)
            {
                Add(drops, "flint", 1);
                return drops;
            }
        }

        if (Crop(block, name, random) is { } harvest)
        {
            foreach (var (item, count) in harvest)
                if (count > 0)
                    Add(drops, item, count);
            return drops;
        }
        if (Alias(name) is { } alias)
        {
            if (alias.Length > 0)
                Add(drops, alias, 1);
            return drops;
        }
        if (Special.TryGetValue(name, out var rule))
        {
            if (rule.Item.Length > 0)
            {
                var count = random.Next(rule.Min, rule.Max + 1);
                if (fortune > 0 && name.Contains("ore"))
                {
                    var k = random.Next(fortune + 2);
                    var multiplier = k <= 1 ? 1 : k;
                    if (multiplier > 1)
                        count = rule.Min == 1 && rule.Max == 1 ? multiplier : count * multiplier;
                }
                Add(drops, rule.Item, count);
            }
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

    private static readonly FrozenDictionary<string, float> Hardness = LoadHardness().ToFrozenDictionary();

    private static readonly FrozenSet<string> ShearedPlants = new[]
    {
        "short_grass", "tall_grass", "fern", "large_fern", "deadbush", "vine", "seagrass", "glow_lichen", "web",
        "hanging_roots", "nether_sprouts", "twisting_vines", "weeping_vines", "cave_vines",
    }.ToFrozenSet();

    private static int State(BlockType block, string key) => block.States.TryGetValue(key, out var v) ? Convert.ToInt32(v) : 0;

    // Crops give their harvest when grown, their seed (or one crop) before.
    private static (string Item, int Count)[]? Crop(BlockType block, string name, Random random) => name switch
    {
        "wheat" => State(block, "growth") >= 7 ? [("wheat", 1), ("wheat_seeds", random.Next(0, 4))] : [("wheat_seeds", 1)],
        "carrots" => [("carrot", State(block, "growth") >= 7 ? random.Next(1, 5) : 1)],
        "potatoes" => State(block, "growth") >= 7
            ? [("potato", random.Next(1, 5)), ("poisonous_potato", random.NextDouble() < 0.02 ? 1 : 0)]
            : [("potato", 1)],
        "beetroot" => State(block, "growth") >= 7 ? [("beetroot", 1), ("beetroot_seeds", random.Next(1, 4))] : [("beetroot_seeds", 1)],
        "nether_wart" => [("nether_wart", State(block, "age") >= 3 ? random.Next(2, 5) : 1)],
        "cocoa" => [("cocoa_beans", State(block, "age") >= 2 ? random.Next(2, 4) : 1)],
        "pumpkin_stem" or "attached_pumpkin_stem" => [("pumpkin_seeds", random.Next(0, 2))],
        "melon_stem" or "attached_melon_stem" => [("melon_seeds", random.Next(0, 2))],
        "sweet_berry_bush" => [("sweet_berries", State(block, "growth") >= 2 ? random.Next(1, 4) : 0)],
        "torchflower_crop" => [("torchflower_seeds", 1)],
        "pitcher_crop" => [("pitcher_pod", 1)],
        "cave_vines_body_with_berries" or "cave_vines_head_with_berries" => [("glow_berries", 1)],
        _ => null,
    };

    // Blocks whose item has another name; "" drops nothing.
    private static string? Alias(string name) => name switch
    {
        "reeds" => "sugar_cane",
        "standing_sign" or "wall_sign" => "oak_sign",
        _ when name.EndsWith("_standing_sign") => name.Replace("_standing_sign", "_sign"),
        _ when name.EndsWith("_wall_sign") => name.Replace("_wall_sign", "_sign"),
        "standing_banner" or "wall_banner" => "banner",
        "unpowered_repeater" or "powered_repeater" => "repeater",
        "unpowered_comparator" or "powered_comparator" => "comparator",
        "tripwire" => "string",
        "lit_redstone_lamp" => "redstone_lamp",
        "unlit_redstone_torch" => "redstone_torch",
        "daylight_detector_inverted" => "daylight_detector",
        "lit_blast_furnace" => "blast_furnace",
        "lit_smoker" => "smoker",
        "bamboo_sapling" => "bamboo",
        "cake" or "frosted_ice" or "powder_snow" or "piston_arm_collision" or "sticky_piston_arm_collision" or "moving_block"
            or "cave_vines" or "cave_vines_body_with_berries" or "cave_vines_head_with_berries" or "end_portal" or "portal" => "",
        _ when name.EndsWith("_stained_glass") || name.EndsWith("_stained_glass_pane") => "",
        _ => null,
    };

    /// <summary>How long a block takes to break (0: at once), Bedrock values.</summary>
    public static float HardnessOf(BlockType block) => Hardness.GetValueOrDefault(block.Name, 1.5f);

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

    /// <summary>The tool that mines a vanilla block fastest: pickaxe, axe, shovel, hoe, or "" for none.</summary>
    public static string EffectiveTool(BlockType block) =>
        block.Name.StartsWith("minecraft:") ? EffectiveTool(block.Name["minecraft:".Length..]) : "";

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

    // Custom tools (plugins): kind (pickaxe, axe, shovel, hoe, sword), harvest level (0 wood .. 3 diamond, 4 netherite) and speed.
    private static readonly Dictionary<string, (string Kind, int Level, double Speed)> CustomTools = [];

    /// <summary>Makes a custom item mine like a vanilla tool: which blocks, which ores it can harvest, how fast.</summary>
    public static void RegisterTool(string itemName, string kind, int level, double speed) => CustomTools[itemName] = (kind, level, speed);

    /// <summary>
    /// What a held item is as a tool: kind (pickaxe, axe, shovel, hoe, sword, or "" for none) and harvest level
    /// (0 wood/gold, 1 stone, 2 iron, 3 diamond, 4 netherite, -1 for none). Custom tools from <see cref="RegisterTool"/>.
    /// </summary>
    public static (string Kind, int Level) ToolInfo(ItemStack? tool)
    {
        if (tool is not null && CustomTools.TryGetValue(tool.Type.Name, out var custom))
            return (custom.Kind, custom.Level);
        var kind = ToolKind(tool);
        if (kind == "")
            return ("", -1);
        return (kind, tool!.Type.Name.Split(':')[1].Split('_')[0] switch
        {
            "wooden" or "golden" => 0,
            "stone" => 1,
            "iron" => 2,
            "diamond" => 3,
            "netherite" => 4,
            _ => -1,
        });
    }

    private static string ToolKind(ItemStack? tool)
    {
        var name = tool?.Type.Name ?? "";
        if (CustomTools.TryGetValue(name, out var custom))
            return custom.Kind;
        foreach (var kind in new[] { "pickaxe", "axe", "shovel", "hoe", "sword" })
            if (name.EndsWith("_" + kind))
                return kind;
        return "";
    }

    private static double ToolSpeed(ItemStack? tool) => tool is not null && CustomTools.TryGetValue(tool.Type.Name, out var custom) ? custom.Speed
        : tool?.Type.Name.Split(':')[1].Split('_')[0] switch
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

    private static int PickaxeLevel(ItemStack? tool) => tool is not null && CustomTools.TryGetValue(tool.Type.Name, out var custom)
        ? custom.Kind == "pickaxe" ? custom.Level : -1
        : tool?.Type.Name switch
    {
        "minecraft:wooden_pickaxe" or "minecraft:golden_pickaxe" => 0,
        "minecraft:stone_pickaxe" => 1,
        "minecraft:iron_pickaxe" => 2,
        "minecraft:diamond_pickaxe" or "minecraft:netherite_pickaxe" => 3,
        _ => -1,
    };
}
