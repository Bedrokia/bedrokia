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
/// Blocks that stand on the block under them and break when it goes: plants, crops, sugar cane and cactus columns,
/// bamboo, kelp, snow layers, carpets, rails, torches on the ground, the top half of doors and tall plants.
/// </summary>
public static class Support
{
    private static readonly FrozenSet<string> OnGround = new[]
    {
        "short_grass", "tall_grass", "fern", "large_fern", "deadbush", "dandelion", "poppy", "blue_orchid", "allium", "azure_bluet",
        "red_tulip", "orange_tulip", "white_tulip", "pink_tulip", "oxeye_daisy", "cornflower", "lily_of_the_valley", "wither_rose",
        "torchflower", "pitcher_plant", "sunflower", "lilac", "rose_bush", "peony", "wheat", "carrots", "potatoes", "beetroot",
        "pumpkin_stem", "melon_stem", "nether_wart", "sweet_berry_bush", "brown_mushroom", "red_mushroom", "crimson_fungus",
        "warped_fungus", "crimson_roots", "warped_roots", "nether_sprouts", "snow_layer", "rail", "golden_rail", "detector_rail",
        "activator_rail", "redstone_wire", "torch", "soul_torch", "redstone_torch", "copper_torch", "lever", "stone_pressure_plate",
        "wooden_pressure_plate", "light_weighted_pressure_plate", "heavy_weighted_pressure_plate", "repeater", "comparator",
        "flower_pot", "sea_pickle", "pink_petals", "wildflowers", "leaf_litter", "short_dry_grass", "tall_dry_grass", "bush",
        "firefly_bush", "cactus_flower", "frog_spawn", "waterlily",
    }.ToFrozenSet();

    // Columns: each block stands on another of its kind or on the ground it grows on.
    private static readonly FrozenDictionary<string, string[]> Columns = new Dictionary<string, string[]>
    {
        ["reeds"] = ["reeds", "grass_block", "dirt", "coarse_dirt", "podzol", "mycelium", "sand", "red_sand", "mud", "moss_block", "rooted_dirt", "suspicious_sand"],
        ["cactus"] = ["cactus", "sand", "red_sand", "suspicious_sand"],
        ["bamboo"] = ["bamboo"], // or any solid block
        ["kelp"] = ["kelp"], // or any solid block
        ["twisting_vines"] = ["twisting_vines"],
    }.ToFrozenDictionary();

    private static string Short(BlockType block) => block.Name.StartsWith("minecraft:") ? block.Name["minecraft:".Length..] : block.Name;

    /// <summary>True when <paramref name="block"/> (at the position above <paramref name="below"/>) has lost what holds it.</summary>
    public static bool Unsupported(BlockType block, BlockType? below)
    {
        if (block.IsAir || below is null)
            return false; // unloaded chunk under it: leave it
        var name = Short(block);
        var under = Short(below);
        // The top half of a door or a tall plant goes with its lower half.
        if (block.States.GetValueOrDefault("upper_block_bit") is byte and not 0)
            return below.Name != block.Name;
        if (Columns.TryGetValue(name, out var holders))
            return !holders.Contains(under) && (name is "reeds" or "cactus" || !Entities.BlockPhysics.IsSolid(below));
        // A torch on a wall, a hanging lantern... only the ones standing on the ground are checked.
        if (block.States.GetValueOrDefault("torch_facing_direction") is string facing && facing is not ("top" or "unknown"))
            return false;
        if (block.Name.EndsWith("_carpet") || block.Name == "minecraft:moss_carpet")
            return below.IsAir;
        if (!OnGround.Contains(name) && !block.Name.EndsWith("_sapling") && !block.Name.EndsWith("_door"))
            return false;
        if (name == "waterlily")
            return below.Name is not ("minecraft:water" or "minecraft:flowing_water" or "minecraft:ice");
        return !Entities.BlockPhysics.IsSolid(below) && !(name is "snow_layer" && under is "ice" or "packed_ice");
    }
}
