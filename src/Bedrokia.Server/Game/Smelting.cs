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
using Bedrokia.Server.Game.Custom.Items;

namespace Bedrokia.Server.Game;

/// <summary>
/// Furnace, blast furnace and smoker rules: what smelts into what, fuel burn times and cook times
/// (vanilla values; Dragonfly, MIT, keeps them per item in item/*.go SmeltInfo and FuelInfo).
/// </summary>
public static class Smelting
{
    public const int FurnaceCookTicks = 200, FastCookTicks = 100;

    private static readonly FrozenDictionary<string, string> Results = new Dictionary<string, string>
    {
        ["raw_iron"] = "iron_ingot", ["raw_gold"] = "gold_ingot", ["raw_copper"] = "copper_ingot",
        ["iron_ore"] = "iron_ingot", ["deepslate_iron_ore"] = "iron_ingot", ["gold_ore"] = "gold_ingot",
        ["deepslate_gold_ore"] = "gold_ingot", ["nether_gold_ore"] = "gold_ingot", ["copper_ore"] = "copper_ingot",
        ["deepslate_copper_ore"] = "copper_ingot", ["coal_ore"] = "coal", ["deepslate_coal_ore"] = "coal",
        ["diamond_ore"] = "diamond", ["deepslate_diamond_ore"] = "diamond", ["emerald_ore"] = "emerald",
        ["deepslate_emerald_ore"] = "emerald", ["lapis_ore"] = "lapis_lazuli", ["deepslate_lapis_ore"] = "lapis_lazuli",
        ["redstone_ore"] = "redstone", ["deepslate_redstone_ore"] = "redstone", ["nether_quartz_ore"] = "quartz",
        ["ancient_debris"] = "netherite_scrap",
        ["iron_sword"] = "iron_nugget", ["iron_pickaxe"] = "iron_nugget", ["iron_axe"] = "iron_nugget",
        ["golden_sword"] = "gold_nugget", ["golden_pickaxe"] = "gold_nugget", ["golden_axe"] = "gold_nugget",
        ["sand"] = "glass", ["red_sand"] = "glass", ["cobblestone"] = "stone", ["stone"] = "smooth_stone",
        ["cobbled_deepslate"] = "deepslate", ["clay_ball"] = "brick", ["clay"] = "hardened_clay", ["netherrack"] = "netherbrick",
        ["cactus"] = "green_dye", ["wet_sponge"] = "sponge", ["sea_pickle"] = "lime_dye", ["chorus_fruit"] = "popped_chorus_fruit",
        ["quartz_block"] = "smooth_quartz", ["sandstone"] = "smooth_sandstone", ["red_sandstone"] = "smooth_red_sandstone",
        ["stone_bricks"] = "cracked_stone_bricks", ["basalt"] = "smooth_basalt", ["kelp"] = "dried_kelp",
        ["beef"] = "cooked_beef", ["porkchop"] = "cooked_porkchop", ["chicken"] = "cooked_chicken", ["mutton"] = "cooked_mutton",
        ["rabbit"] = "cooked_rabbit", ["cod"] = "cooked_cod", ["salmon"] = "cooked_salmon", ["potato"] = "baked_potato",
    }.ToFrozenDictionary();

    private static readonly FrozenSet<string> Food = new[]
    {
        "beef", "porkchop", "chicken", "mutton", "rabbit", "cod", "salmon", "potato", "kelp"
    }.ToFrozenSet();

    /// <summary>What <paramref name="item"/> becomes in this kind of furnace (Furnace, BlastFurnace, Smoker), or null.</summary>
    public static string? Result(string furnace, ItemStack item)
    {
        if (!item.Type.Name.StartsWith("minecraft:"))
            return null;
        var name = item.Type.Name["minecraft:".Length..];
        if (name.EndsWith("_log") || name.EndsWith("_wood") || name.EndsWith("_stem") || name.EndsWith("_hyphae"))
            return furnace == "Furnace" ? "charcoal" : null;
        if (!Results.TryGetValue(name, out var result))
            return null;
        var isFood = Food.Contains(name);
        var isOre = name.Contains("ore") || name.StartsWith("raw_") || name == "ancient_debris" || name.EndsWith("_sword")
                    || name.EndsWith("_pickaxe") || name.EndsWith("_axe");
        return furnace switch
        {
            "BlastFurnace" => isOre ? result : null,
            "Smoker" => isFood ? result : null,
            _ => result,
        };
    }

    /// <summary>Burn time of a fuel in ticks, 0 if it does not burn.</summary>
    public static int BurnTicks(ItemStack item)
    {
        if (CustomItemRegistry.Get(item.Type.Name)?.GetComponent<FuelItemComponent>() is { } fuel)
            return (int)(fuel.DurationSeconds * 20);
        if (!item.Type.Name.StartsWith("minecraft:"))
            return 0;
        var name = item.Type.Name["minecraft:".Length..];
        return name switch
        {
            "coal" or "charcoal" => 1600,
            "coal_block" => 16000,
            "lava_bucket" => 20000,
            "blaze_rod" => 2400,
            "dried_kelp_block" => 4000,
            "stick" or "bowl" => 100,
            "bamboo" => 50,
            "crafting_table" or "chest" or "trapped_chest" or "bookshelf" or "barrel" or "jukebox" or "note_block" or "lectern" => 300,
            _ when name.EndsWith("_log") || name.EndsWith("_wood") || name.EndsWith("_planks") || name.EndsWith("_fence")
                   || name.EndsWith("_fence_gate") || name.EndsWith("_stairs") && name.Contains("oak") || name.EndsWith("_door") && !name.StartsWith("iron") => 300,
            _ when name.EndsWith("_slab") && !name.Contains("stone") => 150,
            _ when name.EndsWith("_sapling") || name.EndsWith("_wool") => 100,
            _ when name.EndsWith("_carpet") => 67,
            _ when name.StartsWith("wooden_") => 200,
            _ => 0,
        };
    }
}
