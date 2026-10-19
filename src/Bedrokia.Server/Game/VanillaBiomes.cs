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

/// <summary>A vanilla biome: id used in chunks plus the data the client needs (BiomeDefinitionList).</summary>
public sealed record VanillaBiome(int Id, string Name, float Temperature, float Rainfall, float Depth, float Scale, int WaterColour, string[] Tags);

public static class VanillaBiomes
{
    /// <summary>All 89 vanilla biomes, sorted by id.</summary>
    public static readonly VanillaBiome[] All =
    [
        new(0, "ocean", 0.5f, 0.5f, -1.0f, 0.1f, unchecked((int)0xA51787D4), ["monster", "ocean", "overworld", "fast_fishing", "high_seas"]),
        new(1, "plains", 0.8f, 0.4f, 0.125f, 0.05f, unchecked((int)0xA544AFF5), ["animal", "monster", "overworld", "plains", "bee_habitat"]),
        new(2, "desert", 2.0f, 0.0f, 0.125f, 0.05f, unchecked((int)0xA532A598), ["desert", "monster", "overworld", "spawns_gold_rabbits", "spawns_warm_variant_farm_animals", "spawns_warm_variant_frogs"]),
        new(3, "extreme_hills", 0.2f, 0.3f, 1.0f, 0.5f, unchecked((int)0xA5007BF7), ["animal", "extreme_hills", "monster", "overworld", "spawns_cold_variant_farm_animals"]),
        new(4, "forest", 0.7f, 0.8f, 0.1f, 0.2f, unchecked((int)0xA51E97F2), ["animal", "forest", "monster", "overworld", "bee_habitat", "has_structure_abandoned_camp", "oak_forest"]),
        new(5, "taiga", 0.25f, 0.8f, 0.1f, 0.2f, unchecked((int)0xA5287082), ["animal", "forest", "monster", "overworld", "taiga", "has_structure_trail_ruins", "spawns_cold_variant_farm_animals", "has_structure_abandoned_camp"]),
        new(6, "swampland", 0.8f, 0.9f, -0.2f, 0.1f, unchecked((int)0xA5617B64), ["animal", "monster", "overworld", "swamp", "spawns_slimes_on_surface", "slime", "swamp_water_huge_mushroom", "has_structure_abandoned_camp"]),
        new(7, "river", 0.5f, 0.5f, -0.5f, 0.0f, unchecked((int)0xA50084FF), ["overworld", "spawns_more_frequent_drowned", "spawns_reduced_water_ambient_mobs", "spawns_river_mobs", "river"]),
        new(8, "hell", 2.0f, 0.0f, 0.1f, 0.2f, unchecked((int)0xA5905957), ["nether", "nether_wastes", "spawn_endermen", "spawn_few_piglins", "spawn_ghast", "spawn_magma_cubes", "spawns_nether_mobs", "spawn_zombified_piglin", "spawns_warm_variant_farm_animals"]),
        new(9, "the_end", 0.5f, 0.5f, 0.1f, 0.2f, unchecked((int)0xA562529E), ["the_end", "spawns_cold_variant_farm_animals", "spawns_cold_variant_frogs"]),
        new(10, "legacy_frozen_ocean", 0.0f, 0.5f, -1.0f, 0.1f, unchecked((int)0xA660B7FF), ["legacy", "frozen", "ocean", "overworld", "spawns_cold_variant_farm_animals", "spawns_polar_bears_on_alternate_blocks", "fast_fishing", "high_seas"]),
        new(11, "frozen_river", 0.0f, 0.5f, -0.5f, 0.0f, unchecked((int)0xA5185390), ["frozen", "overworld", "river", "spawns_cold_variant_farm_animals", "spawns_cold_variant_frogs", "spawns_river_mobs", "spawns_snow_foxes", "spawns_white_rabbits"]),
        new(12, "ice_plains", 0.0f, 0.5f, 0.125f, 0.05f, unchecked((int)0xA514559B), ["frozen", "ice", "ice_plains", "overworld", "spawns_cold_variant_farm_animals", "spawns_cold_variant_frogs", "spawns_snow_foxes", "spawns_white_rabbits"]),
        new(13, "ice_mountains", 0.0f, 0.5f, 0.45f, 0.3f, unchecked((int)0xA51156A7), ["frozen", "ice", "mountain", "overworld", "spawns_cold_variant_farm_animals", "spawns_cold_variant_frogs", "spawns_snow_foxes", "spawns_white_rabbits"]),
        new(14, "mushroom_island", 0.9f, 1.0f, 0.2f, 0.3f, unchecked((int)0xA58A8997), ["mooshroom_island", "overworld", "spawns_without_patrols"]),
        new(15, "mushroom_island_shore", 0.9f, 1.0f, 0.0f, 0.025f, unchecked((int)0xA5818193), ["mooshroom_island", "overworld", "shore", "spawns_without_patrols"]),
        new(16, "beach", 0.8f, 0.4f, 0.0f, 0.025f, unchecked((int)0xA5157CAB), ["beach", "monster", "overworld", "warm"]),
        new(17, "desert_hills", 2.0f, 0.0f, 0.45f, 0.3f, unchecked((int)0xA51A7AA1), ["desert", "hills", "monster", "overworld", "spawns_gold_rabbits", "spawns_warm_variant_farm_animals"]),
        new(18, "forest_hills", 0.7f, 0.8f, 0.45f, 0.3f, unchecked((int)0xA5056BD1), ["animal", "hills", "monster", "overworld", "forest", "bee_habitat"]),
        new(19, "taiga_hills", 0.25f, 0.8f, 0.45f, 0.3f, unchecked((int)0xA5236583), ["animal", "hills", "monster", "overworld", "forest", "taiga", "spawns_cold_variant_farm_animals"]),
        new(20, "extreme_hills_edge", 0.2f, 0.3f, 0.8f, 0.4f, unchecked((int)0xA5045CD5), ["animal", "edge", "extreme_hills", "monster", "mountain", "overworld", "spawns_cold_variant_farm_animals"]),
        new(21, "jungle", 0.95f, 0.9f, 0.1f, 0.2f, unchecked((int)0xA514A2C5), ["animal", "has_structure_trail_ruins", "jungle", "monster", "overworld", "rare", "spawns_jungle_mobs", "spawns_warm_variant_farm_animals"]),
        new(22, "jungle_hills", 0.95f, 0.9f, 0.45f, 0.3f, unchecked((int)0xA51B9ED8), ["animal", "hills", "jungle", "monster", "overworld", "spawns_jungle_mobs", "spawns_warm_variant_farm_animals"]),
        new(23, "jungle_edge", 0.95f, 0.8f, 0.1f, 0.2f, unchecked((int)0xA50D8AE3), ["animal", "edge", "jungle", "monster", "overworld", "spawns_jungle_mobs", "spawns_warm_variant_farm_animals", "has_structure_abandoned_camp"]),
        new(24, "deep_ocean", 0.5f, 0.5f, -1.8f, 0.1f, unchecked((int)0xA51787D4), ["deep", "monster", "ocean", "overworld", "spawns_warm_variant_farm_animals", "fast_fishing", "high_seas"]),
        new(25, "stone_beach", 0.2f, 0.3f, 0.1f, 0.8f, unchecked((int)0xA50D67BB), ["beach", "monster", "overworld", "stone"]),
        new(26, "cold_beach", 0.05f, 0.3f, 0.0f, 0.025f, unchecked((int)0xA51463A5), ["beach", "cold", "monster", "overworld", "spawns_cold_variant_farm_animals", "spawns_cold_variant_frogs", "spawns_snow_foxes", "spawns_white_rabbits"]),
        new(27, "birch_forest", 0.6f, 0.6f, 0.1f, 0.2f, unchecked((int)0xA50677CE), ["animal", "birch", "forest", "monster", "overworld", "bee_habitat", "has_structure_abandoned_camp"]),
        new(28, "birch_forest_hills", 0.6f, 0.6f, 0.45f, 0.3f, unchecked((int)0xA50A74C4), ["animal", "birch", "forest", "hills", "monster", "overworld", "bee_habitat"]),
        new(29, "roofed_forest", 0.7f, 0.8f, 0.1f, 0.2f, unchecked((int)0xA53B6CD1), ["animal", "forest", "monster", "no_legacy_worldgen", "overworld", "roofed"]),
        new(30, "cold_taiga", -0.5f, 0.4f, 0.2f, 0.2f, unchecked((int)0xA5205E83), ["animal", "cold", "forest", "monster", "overworld", "taiga", "has_structure_trail_ruins", "spawns_cold_variant_farm_animals", "has_structure_abandoned_camp"]),
        new(31, "cold_taiga_hills", -0.5f, 0.4f, 0.45f, 0.3f, unchecked((int)0xA5245B78), ["animal", "cold", "forest", "hills", "monster", "overworld", "taiga", "spawns_cold_variant_farm_animals", "spawns_cold_variant_frogs", "spawns_white_rabbits"]),
        new(32, "mega_taiga", 0.3f, 0.8f, 0.2f, 0.2f, unchecked((int)0xA52D6D77), ["animal", "forest", "mega", "monster", "overworld", "rare", "taiga", "has_structure_trail_ruins", "spawns_cold_variant_farm_animals", "has_structure_abandoned_camp"]),
        new(33, "mega_taiga_hills", 0.3f, 0.8f, 0.45f, 0.3f, unchecked((int)0xA5286378), ["animal", "forest", "hills", "mega", "monster", "overworld", "taiga", "spawns_cold_variant_farm_animals"]),
        new(34, "extreme_hills_plus_trees", 0.2f, 0.3f, 1.0f, 0.5f, unchecked((int)0xA50E63AB), ["animal", "extreme_hills", "forest", "monster", "mountain", "overworld", "spawns_cold_variant_farm_animals", "has_structure_abandoned_camp"]),
        new(35, "savanna", 1.2f, 0.0f, 0.125f, 0.05f, unchecked((int)0xA52C8B9C), ["animal", "monster", "overworld", "savanna", "spawns_savanna_mobs", "spawns_warm_variant_farm_animals", "has_structure_abandoned_camp"]),
        new(36, "savanna_plateau", 1.0f, 0.0f, 1.5f, 0.025f, unchecked((int)0xA52590A8), ["animal", "monster", "overworld", "plateau", "savanna", "spawns_savanna_mobs", "spawns_warm_variant_farm_animals"]),
        new(37, "mesa", 2.0f, 0.0f, 0.1f, 0.2f, unchecked((int)0xA54E7F81), ["animal", "mesa", "monster", "overworld", "spawns_mesa_mobs", "spawns_warm_variant_farm_animals", "surface_mineshaft"]),
        new(38, "mesa_plateau_stone", 2.0f, 0.0f, 1.5f, 0.025f, unchecked((int)0xA555809E), ["animal", "mesa", "monster", "overworld", "plateau", "rare", "stone", "spawns_mesa_mobs", "spawns_warm_variant_farm_animals", "surface_mineshaft", "has_structure_abandoned_camp"]),
        new(39, "mesa_plateau", 2.0f, 0.0f, 1.5f, 0.025f, unchecked((int)0xA555809E), ["animal", "mesa", "monster", "overworld", "plateau", "rare", "spawns_mesa_mobs", "spawns_warm_variant_farm_animals", "surface_mineshaft"]),
        new(40, "warm_ocean", 0.5f, 0.5f, -1.0f, 0.1f, unchecked((int)0xA502B0E5), ["monster", "ocean", "overworld", "warm", "spawns_warm_variant_farm_animals", "spawns_warm_variant_frogs", "fast_fishing", "high_seas", "temperate_ocean"]),
        new(41, "deep_warm_ocean", 0.5f, 0.5f, -1.8f, 0.1f, unchecked((int)0xA502B0E5), ["deep", "monster", "ocean", "overworld", "warm", "spawns_warm_variant_farm_animals", "fast_fishing", "high_seas", "temperate_ocean"]),
        new(42, "lukewarm_ocean", 0.5f, 0.5f, -1.0f, 0.1f, unchecked((int)0xA50D96DB), ["lukewarm", "monster", "ocean", "overworld", "spawns_warm_variant_farm_animals", "fast_fishing", "high_seas", "temperate_ocean"]),
        new(43, "deep_lukewarm_ocean", 0.5f, 0.5f, -1.8f, 0.1f, unchecked((int)0xA50D96DB), ["deep", "lukewarm", "monster", "ocean", "overworld", "spawns_warm_variant_farm_animals", "fast_fishing", "high_seas", "temperate_ocean"]),
        new(44, "cold_ocean", 0.5f, 0.5f, -1.0f, 0.1f, unchecked((int)0xA52080C9), ["cold", "monster", "ocean", "overworld", "spawns_cold_variant_farm_animals", "fast_fishing", "high_seas"]),
        new(45, "deep_cold_ocean", 0.5f, 0.5f, -1.8f, 0.1f, unchecked((int)0xA52080C9), ["cold", "deep", "monster", "ocean", "overworld", "spawns_cold_variant_farm_animals", "fast_fishing", "high_seas"]),
        new(46, "frozen_ocean", 0.0f, 0.5f, -1.0f, 0.1f, unchecked((int)0xA52570B5), ["frozen", "monster", "ocean", "overworld", "spawns_polar_bears_on_alternate_blocks", "spawns_cold_variant_farm_animals", "spawns_cold_variant_frogs", "spawns_snow_foxes", "spawns_white_rabbits", "fast_fishing", "high_seas"]),
        new(47, "deep_frozen_ocean", 0.5f, 0.5f, -1.8f, 0.1f, unchecked((int)0xA52570B5), ["deep", "frozen", "monster", "ocean", "overworld", "spawns_cold_variant_farm_animals", "spawns_cold_variant_frogs", "spawns_polar_bears_on_alternate_blocks", "fast_fishing", "high_seas"]),
        new(48, "bamboo_jungle", 0.95f, 0.9f, 0.1f, 0.2f, unchecked((int)0xA514A2C5), ["animal", "bamboo", "jungle", "monster", "overworld", "spawns_jungle_mobs", "spawns_warm_variant_farm_animals", "has_structure_abandoned_camp"]),
        new(49, "bamboo_jungle_hills", 0.95f, 0.9f, 0.45f, 0.3f, unchecked((int)0xA51B9ED8), ["animal", "bamboo", "hills", "jungle", "monster", "overworld", "spawns_jungle_mobs", "spawns_warm_variant_farm_animals"]),
        new(129, "sunflower_plains", 0.8f, 0.4f, 0.125f, 0.05f, unchecked((int)0xA660B7FF), ["animal", "monster", "mutated", "overworld", "plains", "bee_habitat"]),
        new(130, "desert_mutated", 2.0f, 0.0f, 0.225f, 0.25f, unchecked((int)0xA532A598), ["desert", "monster", "mutated", "overworld_generation", "spawns_gold_rabbits", "spawns_warm_variant_farm_animals"]),
        new(131, "extreme_hills_mutated", 0.2f, 0.3f, 1.0f, 0.5f, unchecked((int)0xA50E63AB), ["animal", "extreme_hills", "monster", "mutated", "overworld", "spawns_cold_variant_farm_animals"]),
        new(132, "flower_forest", 0.7f, 0.8f, 0.1f, 0.4f, unchecked((int)0xA520A3CC), ["animal", "flower_forest", "monster", "mutated", "overworld", "bee_habitat", "has_structure_abandoned_camp"]),
        new(133, "taiga_mutated", 0.25f, 0.8f, 0.2f, 0.4f, unchecked((int)0xA51E6B82), ["animal", "forest", "monster", "mutated", "taiga", "overworld_generation", "spawns_cold_variant_farm_animals"]),
        new(134, "swampland_mutated", 0.8f, 0.5f, -0.1f, 0.3f, unchecked((int)0xA5617B64), ["animal", "monster", "mutated", "swamp", "overworld_generation", "spawns_slimes_on_surface", "slime", "swamp_water_huge_mushroom"]),
        new(140, "ice_plains_spikes", 0.0f, 1.0f, 0.425f, 0.45f, unchecked((int)0xA660B7FF), ["frozen", "ice_plains", "monster", "mutated", "overworld", "spawns_cold_variant_farm_animals", "spawns_cold_variant_frogs", "spawns_white_rabbits"]),
        new(149, "jungle_mutated", 0.95f, 0.9f, 0.2f, 0.4f, unchecked((int)0xA51B9ED8), ["animal", "jungle", "monster", "mutated", "overworld_generation", "spawns_warm_variant_farm_animals"]),
        new(151, "jungle_edge_mutated", 0.95f, 0.8f, 0.2f, 0.4f, unchecked((int)0xA50D8AE3), ["animal", "edge", "jungle", "monster", "mutated", "overworld_generation", "spawns_jungle_mobs", "spawns_warm_variant_farm_animals"]),
        new(155, "birch_forest_mutated", 0.6f, 0.6f, 0.2f, 0.4f, unchecked((int)0xA50677CE), ["animal", "birch", "forest", "monster", "mutated", "bee_habitat", "overworld_generation", "has_structure_trail_ruins", "has_structure_abandoned_camp"]),
        new(156, "birch_forest_hills_mutated", 0.7f, 0.8f, 0.55f, 0.5f, unchecked((int)0xA50A74C4), ["animal", "birch", "forest", "hills", "monster", "mutated", "overworld_generation"]),
        new(157, "roofed_forest_mutated", 0.7f, 0.8f, 0.2f, 0.4f, unchecked((int)0xA53B6CD1), ["animal", "forest", "monster", "mutated", "roofed", "overworld_generation"]),
        new(158, "cold_taiga_mutated", -0.5f, 0.4f, 0.3f, 0.4f, unchecked((int)0xA5205E83), ["animal", "cold", "forest", "monster", "mutated", "taiga", "overworld_generation", "spawns_cold_variant_farm_animals"]),
        new(160, "redwood_taiga_mutated", 0.25f, 0.8f, 0.2f, 0.2f, unchecked((int)0xA52D6D77), ["animal", "forest", "mega", "monster", "mutated", "overworld", "taiga", "has_structure_trail_ruins", "spawns_cold_variant_farm_animals", "has_structure_abandoned_camp"]),
        new(161, "redwood_taiga_hills_mutated", 0.3f, 0.8f, 0.55f, 0.5f, unchecked((int)0xA5286378), ["animal", "forest", "hills", "mega", "monster", "mutated", "taiga", "overworld_generation", "spawns_cold_variant_farm_animals"]),
        new(162, "extreme_hills_plus_trees_mutated", 0.2f, 0.3f, 1.0f, 0.5f, unchecked((int)0xA50E63AB), ["animal", "extreme_hills", "forest", "monster", "mutated", "overworld", "spawns_cold_variant_farm_animals"]),
        new(163, "savanna_mutated", 2.0f, 0.0f, 0.3625f, 1.225f, unchecked((int)0xA660B7FF), ["animal", "monster", "mutated", "overworld", "savanna", "spawns_savanna_mobs", "spawns_warm_variant_farm_animals"]),
        new(164, "savanna_plateau_mutated", 1.0f, 0.5f, 1.05f, 1.2125f, unchecked((int)0xA660B7FF), ["animal", "monster", "mutated", "overworld", "plateau", "savanna", "spawns_savanna_mobs", "spawns_warm_variant_farm_animals"]),
        new(165, "mesa_bryce", 2.0f, 0.0f, 0.1f, 0.2f, unchecked((int)0xA514A2C5), ["animal", "mesa", "monster", "mutated", "overworld", "spawns_mesa_mobs", "spawns_warm_variant_farm_animals", "surface_mineshaft"]),
        new(166, "mesa_plateau_stone_mutated", 2.0f, 0.0f, 0.45f, 0.3f, unchecked((int)0xA555809E), ["animal", "mesa", "monster", "mutated", "overworld", "plateau", "spawns_mesa_mobs", "spawns_warm_variant_farm_animals", "surface_mineshaft"]),
        new(167, "mesa_plateau_mutated", 2.0f, 0.0f, 0.45f, 0.3f, unchecked((int)0xA555809E), ["animal", "mesa", "monster", "mutated", "overworld", "plateau", "stone", "spawns_mesa_mobs", "spawns_warm_variant_farm_animals", "surface_mineshaft"]),
        new(178, "soulsand_valley", 2.0f, 0.0f, 0.1f, 0.2f, unchecked((int)0xA5905957), ["nether", "soulsand_valley", "spawn_ghast", "spawn_endermen", "spawns_warm_variant_farm_animals"]),
        new(179, "crimson_forest", 2.0f, 0.0f, 0.1f, 0.2f, unchecked((int)0xA5905957), ["nether", "netherwart_forest", "crimson_forest", "spawn_few_zombified_piglins", "spawn_piglin", "spawns_warm_variant_farm_animals"]),
        new(180, "warped_forest", 2.0f, 0.0f, 0.1f, 0.2f, unchecked((int)0xA5905957), ["nether", "netherwart_forest", "warped_forest", "spawn_endermen", "spawns_warm_variant_farm_animals"]),
        new(181, "basalt_deltas", 2.0f, 0.0f, 0.1f, 0.2f, unchecked((int)0xA53F76E4), ["nether", "basalt_deltas", "spawn_many_magma_cubes", "spawn_ghast", "spawns_warm_variant_farm_animals"]),
        new(182, "jagged_peaks", -0.7f, 0.9f, 0.1f, 0.2f, unchecked((int)0xA660B7FF), ["mountains", "monster", "overworld", "frozen", "jagged_peaks", "spawns_cold_variant_farm_animals", "spawns_cold_variant_frogs", "spawns_snow_foxes", "spawns_white_rabbits"]),
        new(183, "frozen_peaks", -0.7f, 0.9f, 0.1f, 0.2f, unchecked((int)0xA660B7FF), ["mountains", "monster", "overworld", "frozen", "frozen_peaks", "spawns_cold_variant_farm_animals", "spawns_cold_variant_frogs", "spawns_snow_foxes", "spawns_white_rabbits"]),
        new(184, "snowy_slopes", -0.3f, 0.9f, 0.1f, 0.2f, unchecked((int)0xA660B7FF), ["mountains", "monster", "overworld", "frozen", "spawns_cold_variant_frogs", "spawns_snow_foxes", "spawns_white_rabbits", "snowy_slopes", "spawns_cold_variant_farm_animals"]),
        new(185, "grove", -0.2f, 0.8f, 0.1f, 0.2f, unchecked((int)0xA660B7FF), ["mountains", "cold", "monster", "overworld", "grove", "spawns_cold_variant_farm_animals", "spawns_cold_variant_frogs", "spawns_snow_foxes", "spawns_white_rabbits"]),
        new(186, "meadow", 0.3f, 0.8f, 0.1f, 0.2f, unchecked((int)0xA660B7FF), ["mountains", "monster", "overworld", "meadow", "bee_habitat", "has_structure_abandoned_camp"]),
        new(187, "lush_caves", 0.9f, 0.0f, 0.1f, 0.2f, unchecked((int)0xA660B7FF), ["caves", "lush_caves", "overworld", "monster", "spawns_tropical_fish_at_any_height"]),
        new(188, "dripstone_caves", 0.2f, 0.0f, 0.1f, 0.2f, unchecked((int)0xA660B7FF), ["caves", "overworld", "dripstone_caves", "monster"]),
        new(189, "stony_peaks", 1.0f, 0.3f, 0.1f, 0.2f, unchecked((int)0xA660B7FF), ["mountains", "monster", "overworld", "spawns_cold_variant_farm_animals"]),
        new(190, "deep_dark", 0.8f, 0.4f, 0.1f, 0.2f, unchecked((int)0xA660B7FF), ["caves", "deep_dark", "overworld", "spawns_cold_variant_farm_animals", "spawns_cold_variant_frogs"]),
        new(191, "mangrove_swamp", 0.8f, 0.9f, -0.2f, 0.1f, unchecked((int)0xA660B7FF), ["mangrove_swamp", "overworld", "monster", "spawns_slimes_on_surface", "spawns_warm_variant_farm_animals", "spawns_warm_variant_frogs", "slime"]),
        new(192, "cherry_grove", 0.3f, 0.8f, 0.1f, 0.2f, unchecked((int)0xA660B7FF), ["mountains", "monster", "overworld", "cherry_grove", "bee_habitat", "has_structure_abandoned_camp"]),
        new(193, "pale_garden", 0.7f, 0.8f, 0.1f, 0.2f, unchecked((int)0xA660B7FF), ["monster", "overworld", "pale_garden", "has_structure_abandoned_camp"]),
        new(194, "sulfur_caves", 0.8f, 0.4f, 0.1f, 0.2f, unchecked((int)0xA660B7FF), ["caves", "sulfur_caves", "overworld", "monster"]),
        new(195, "dappled_forest", 0.6f, 0.6f, 0.1f, 0.2f, unchecked((int)0xA660B7FF), ["animal", "dappled_forest", "monster", "forest", "overworld", "spawns_cold_variant_farm_animals", "has_structure_abandoned_camp"]),
    ];

    public static readonly System.Collections.Frozen.FrozenDictionary<int, VanillaBiome> ById = All.ToFrozenDictionary(b => b.Id);
    public static readonly System.Collections.Frozen.FrozenDictionary<string, VanillaBiome> ByName = All.ToFrozenDictionary(b => b.Name, StringComparer.OrdinalIgnoreCase);
}
