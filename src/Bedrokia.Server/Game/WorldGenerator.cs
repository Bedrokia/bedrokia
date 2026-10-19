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

using System.Collections.Concurrent;
using Bedrokia.Server.Game.Generation;

namespace Bedrokia.Server.Game;

/// <summary>
/// Vanilla biome ids used in chunks (see <see cref="VanillaBiomes"/>). They are only meaningful to the client
/// together with the full BiomeDefinitionList sent at login.
/// </summary>
public static class Biome
{
    public const byte Ocean = 0;
    public const byte Plains = 1;
    public const byte Desert = 2;
    public const byte Forest = 4;
    public const byte Taiga = 5;
    public const byte Beach = 16;
    public const byte DeepOcean = 24;
    public const byte BirchForest = 27;

    public static byte[] DefinitionListPacket { get; } = Bedrokia.Protocol.Packets.BiomeDefinitionList(
        VanillaBiomes.All.Select(b => new Bedrokia.Protocol.Packets.BiomeDefinition(b.Name, b.Temperature, b.Rainfall, b.Depth, b.Scale, b.WaterColour, b.Tags)).ToList());
}

/// <summary>
/// Overworld generator: vanilla-1.12-style biome layout and terrain (ported from Glowstone, MIT, see
/// <see cref="OverworldTerrain"/>), then decorations per biome: trees that may cross chunk borders, plants, ores,
/// snow and ice. Pure function of the seed; chunks can be generated concurrently and identically again.
/// </summary>
/// <remarks>ponytail: no caves, lakes or structures yet.</remarks>
public sealed class WorldGenerator : IWorldGenerator
{
    /// <summary>Y of the top water block of oceans.</summary>
    public const int SeaLevel = OverworldTerrain.GlowSeaLevel - 2;

    private const int TreeMargin = 4; // trees reach this far from their trunk
    private const int MaxCachedColumns = 4096;

    private readonly OverworldTerrain _terrain;
    private readonly ConcurrentDictionary<(int X, int Z), Lazy<Columns>> _columns = new();
    private readonly long _seed;

    public WorldGenerator(long seed)
    {
        _seed = seed;
        _terrain = new OverworldTerrain(seed);
    }

    public long Seed => _seed;

    /// <summary>Block-resolution legacy biome ids, row-major (x + z * sizeX).</summary>
    public int[] GetBiomes(int x, int z, int sizeX, int sizeZ) => _terrain.Biomes(x, z, sizeX, sizeZ);

    /// <summary>Y of the highest non-air block at world column (x, z) before decorations.</summary>
    public int TerrainHeight(int x, int z) => ColumnsOf(x >> 4, z >> 4).Top[(x & 15) + ((z & 15) << 4)];

    public Chunk Generate(int chunkX, int chunkZ)
    {
        var chunk = _terrain.Generate(chunkX, chunkZ, GetBiomes(chunkX << 4, chunkZ << 4, 16, 16));
        var baseX = chunkX << 4;
        var baseZ = chunkZ << 4;

        // Trees: every trunk whose crown can reach this chunk, in a fixed global order so that a tree crossing a
        // border is built the same way from both sides.
        for (var wx = baseX - TreeMargin; wx < baseX + 16 + TreeMargin; wx++)
        for (var wz = baseZ - TreeMargin; wz < baseZ + 16 + TreeMargin; wz++)
        {
            var columns = ColumnsOf(wx >> 4, wz >> 4);
            var i = (wx & 15) + ((wz & 15) << 4);
            var biome = columns.Biome[i];
            var ground = columns.TopBlock[i];
            if (ground != Blocks.GrassBlock && ground != Blocks.Dirt && ground != Podzol && ground != CoarseDirt && ground != SnowBlock)
                continue;
            if (Random01(wx, wz, 7) >= TreeDensity(biome))
                continue;
            Trees.Place(new ChunkWriter(chunk, baseX, baseZ), wx, columns.Top[i] + 1, wz, biome, Random01(wx, wz, 8), Random01(wx, wz, 9));
        }

        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
            Vegetation(chunk, x, z, baseX + x, baseZ + z);
        Ores(chunk, chunkX, chunkZ);
        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
            Snow(chunk, x, z, baseX + x, baseZ + z);
        return chunk;
    }

    /// <summary>A dry spawn column near the origin, not in an ocean, river or beach.</summary>
    public (int X, int Y, int Z) FindSpawn()
    {
        for (var radius = 0; radius < 4000; radius += 32)
        for (var angle = 0; angle < 8; angle++)
        {
            var x = (int)(radius * Math.Cos(angle * Math.PI / 4));
            var z = (int)(radius * Math.Sin(angle * Math.PI / 4));
            var biome = GetBiomes(x, z, 1, 1)[0];
            if (B.IsOcean(biome) || biome is B.River or B.FrozenRiver or B.Beach or B.ColdBeach or B.StoneBeach)
                continue;
            var height = TerrainHeight(x, z);
            if (height > SeaLevel)
                return (x, height + 1, z);
        }
        return (0, TerrainHeight(0, 0) + 1, 0);
    }

    // --- Column summaries of terrain-only chunks (heights, surface, biome), cached for neighbours ---

    private sealed record Columns(int[] Top, BlockType[] TopBlock, int[] Biome);

    private Columns ColumnsOf(int chunkX, int chunkZ)
    {
        if (_columns.Count > MaxCachedColumns)
            _columns.Clear();
        return _columns.GetOrAdd((chunkX, chunkZ), key => new Lazy<Columns>(() =>
        {
            var biomes = GetBiomes(key.X << 4, key.Z << 4, 16, 16);
            var chunk = _terrain.Generate(key.X, key.Z, biomes);
            var top = new int[256];
            var topBlock = new BlockType[256];
            for (var x = 0; x < 16; x++)
            for (var z = 0; z < 16; z++)
            {
                var y = chunk.Height(x, z);
                top[x + (z << 4)] = y;
                topBlock[x + (z << 4)] = chunk.GetBlock(x, y, z);
            }
            return new Columns(top, topBlock, biomes);
        })).Value;
    }

    // Trees per column, from the vanilla trees-per-chunk of each biome (divided by 256 columns).
    private static double TreeDensity(int biome) => biome switch
    {
        B.Forest or B.ForestHills or B.BirchForest or B.BirchForestHills or 155 or 156 or B.Taiga or B.TaigaHills
            or B.ColdTaiga or B.ColdTaigaHills or 133 or 158 or B.MegaTaiga or B.MegaTaigaHills or 160 or 161 => 10 / 256.0,
        132 => 6 / 256.0, // flower forest
        B.RoofedForest or 157 => 22 / 256.0,
        B.Jungle or B.JungleHills or 149 => 40 / 256.0,
        B.JungleEdge or 151 => 3 / 256.0,
        B.ExtremeHillsPlus or 162 => 3 / 256.0,
        B.Swamp or 134 => 2 / 256.0,
        B.Savanna or B.SavannaPlateau or 163 or 164 => 1.2 / 256.0,
        B.MesaPlateauStone or 166 => 4 / 256.0,
        B.ExtremeHills or B.ExtremeHillsEdge or 131 or B.IcePlains or B.Plains or B.SunflowerPlains => 0.12 / 256.0,
        _ => 0,
    };

    // --- Plants (single column, inside this chunk) ---

    private static readonly BlockType Podzol = Req("podzol"), CoarseDirt = Req("coarse_dirt"), SnowBlock = Req("snow"),
        SnowLayer = Blocks.SnowLayer, Ice = Req("ice"), Fern = Req("fern"), DeadBush = Req("deadbush"), Reeds = Req("reeds"),
        LilyPad = Req("waterlily"), Pumpkin = Req("pumpkin"), Melon = Req("melon_block"), BrownMushroom = Req("brown_mushroom"),
        RedMushroom = Req("red_mushroom"), Clay = Blocks.Clay;

    private static readonly BlockType[] Flowers = new[] { "dandelion", "poppy" }.Select(Req).ToArray();
    private static readonly BlockType[] ForestFlowers =
        new[] { "dandelion", "poppy", "allium", "azure_bluet", "red_tulip", "orange_tulip", "white_tulip", "pink_tulip", "oxeye_daisy",
            "cornflower", "lily_of_the_valley" }.Select(Req).ToArray();
    private static readonly BlockType SwampFlower = Req("blue_orchid");
    private static readonly (BlockType Lower, BlockType Upper) TallGrass = DoublePlant("tall_grass"), LargeFern = DoublePlant("large_fern"),
        Sunflower = DoublePlant("sunflower"), Lilac = DoublePlant("lilac"), RoseBush = DoublePlant("rose_bush"), Peony = DoublePlant("peony");

    internal static BlockType Req(string name) =>
        Blocks.ByName(name) ?? throw new KeyNotFoundException($"Block {name} is missing from the block data.");

    private static (BlockType, BlockType) DoublePlant(string name) =>
        (Blocks.Get(name, ("upper_block_bit", (byte)0)), Blocks.Get(name, ("upper_block_bit", (byte)1)));

    private void Vegetation(Chunk chunk, int x, int z, int wx, int wz)
    {
        var y = chunk.Height(x, z);
        var top = chunk.GetBlock(x, y, z);
        var biome = chunk.GetBiome(x, z);
        var roll = Random01(wx, wz, 3);
        var pick = Random01(wx, wz, 4);

        if (top == Blocks.Water)
        {
            if (biome is B.Swamp or 134 && roll < 0.04 && chunk.GetBlock(x, y - 1, z) != Blocks.Water)
                chunk.SetBlock(x, y + 1, z, LilyPad);
            else if (y < SeaLevel - 2 && roll < 0.01 && chunk.GetBlock(x, y - 1, z) is { } floor && (floor == Blocks.Dirt || floor == Blocks.Sand || floor == Blocks.Gravel))
                chunk.SetBlock(x, y - 1, z, Clay);
            return;
        }
        if (!chunk.GetBlock(x, y + 1, z).IsAir || y >= Chunk.MaxY - 3)
            return;

        if (top == Blocks.Sand || top == Blocks.ByName("red_sand"))
        {
            var desert = biome is B.Desert or B.DesertHills or 130 || B.IsMesa(biome);
            if (NextToWater(chunk, x, y, z) && roll < 0.15)
                Stack(chunk, x, y + 1, z, Reeds, 1 + (int)(pick * 3));
            else if (desert && roll < 0.006 && !SolidAround(chunk, x, y + 1, z))
                Stack(chunk, x, y + 1, z, Blocks.Cactus, 1 + (int)(pick * 3));
            else if (desert && roll < 0.012)
                chunk.SetBlock(x, y + 1, z, DeadBush);
            return;
        }
        if (top == Blocks.ByName("hardened_clay") || top.Name.EndsWith("_terracotta"))
        {
            if (roll < 0.01)
                chunk.SetBlock(x, y + 1, z, DeadBush);
            return;
        }
        if (top == Req("mycelium"))
        {
            if (roll < 0.01)
                chunk.SetBlock(x, y + 1, z, pick < 0.5 ? BrownMushroom : RedMushroom);
            return;
        }
        if (top != Blocks.GrassBlock && top != Podzol && top != CoarseDirt)
            return;

        if (NextToWater(chunk, x, y, z) && roll < 0.1)
        {
            Stack(chunk, x, y + 1, z, Reeds, 1 + (int)(pick * 3));
            return;
        }
        var (grass, flowers) = biome switch
        {
            B.Plains => (0.3, 0.02),
            B.SunflowerPlains => (0.3, 0.06),
            132 => (0.08, 0.25), // flower forest
            B.Jungle or B.JungleHills or B.JungleEdge or 149 or 151 => (0.45, 0.01),
            B.Savanna or B.SavannaPlateau or 163 or 164 => (0.4, 0.004),
            B.Swamp or 134 => (0.12, 0.02),
            B.Taiga or B.TaigaHills or B.MegaTaiga or B.MegaTaigaHills or B.ColdTaiga or B.ColdTaigaHills or 133 or 160 or 161 => (0.15, 0.0),
            B.RoofedForest or 157 => (0.1, 0.01),
            B.ExtremeHills or B.ExtremeHillsPlus or B.ExtremeHillsEdge or 131 or 162 => (0.05, 0.004),
            B.IcePlains or B.IceMountains or 140 => (0.0, 0.0),
            _ => (0.1, 0.01),
        };
        if (roll < flowers)
        {
            if (biome == B.SunflowerPlains && pick < 0.6)
                Double(chunk, x, y + 1, z, Sunflower);
            else if (biome == 132 && pick < 0.15)
                Double(chunk, x, y + 1, z, pick < 0.05 ? Lilac : pick < 0.1 ? RoseBush : Peony);
            else if (biome is B.Swamp or 134)
                chunk.SetBlock(x, y + 1, z, SwampFlower);
            else
            {
                var pool = biome == 132 ? ForestFlowers : Flowers;
                chunk.SetBlock(x, y + 1, z, pool[(int)(pick * pool.Length) % pool.Length]);
            }
        }
        else if (roll < flowers + grass)
        {
            var taigaLike = biome is B.Taiga or B.TaigaHills or B.MegaTaiga or B.MegaTaigaHills or B.ColdTaiga or B.ColdTaigaHills
                or B.Jungle or B.JungleHills or B.JungleEdge or 133 or 149 or 151 or 160 or 161;
            if (pick < 0.08)
                Double(chunk, x, y + 1, z, taigaLike && pick < 0.05 ? LargeFern : TallGrass);
            else
                chunk.SetBlock(x, y + 1, z, taigaLike && pick < 0.45 ? Fern : Blocks.ShortGrass);
        }
        else if (roll < flowers + grass + 0.0015 && top == Blocks.GrassBlock)
            chunk.SetBlock(x, y + 1, z, biome is B.Jungle or B.JungleHills or 149 ? Melon : Pumpkin);
    }

    private static void Stack(Chunk chunk, int x, int y, int z, BlockType block, int count)
    {
        for (var i = 0; i < count && chunk.GetBlock(x, y + i, z).IsAir; i++)
            chunk.SetBlock(x, y + i, z, block);
    }

    private static void Double(Chunk chunk, int x, int y, int z, (BlockType Lower, BlockType Upper) plant)
    {
        if (!chunk.GetBlock(x, y + 1, z).IsAir)
            return;
        chunk.SetBlock(x, y, z, plant.Lower);
        chunk.SetBlock(x, y + 1, z, plant.Upper);
    }

    private static bool NextToWater(Chunk chunk, int x, int y, int z) =>
        (x > 0 && chunk.GetBlock(x - 1, y, z) == Blocks.Water) || (x < 15 && chunk.GetBlock(x + 1, y, z) == Blocks.Water)
        || (z > 0 && chunk.GetBlock(x, y, z - 1) == Blocks.Water) || (z < 15 && chunk.GetBlock(x, y, z + 1) == Blocks.Water);

    private static bool SolidAround(Chunk chunk, int x, int y, int z) =>
        (x > 0 && !chunk.GetBlock(x - 1, y, z).IsAir) || (x < 15 && !chunk.GetBlock(x + 1, y, z).IsAir)
        || (z > 0 && !chunk.GetBlock(x, y, z - 1).IsAir) || (z < 15 && !chunk.GetBlock(x, y, z + 1).IsAir);

    private void Snow(Chunk chunk, int x, int z, int wx, int wz)
    {
        var y = chunk.Height(x, z);
        var top = chunk.GetBlock(x, y, z);
        if (!_terrain.IsCold(chunk.GetBiome(x, z), wx, y + 1, wz))
            return;
        if (top == Blocks.Water)
            chunk.SetBlock(x, y, z, Ice);
        else if (chunk.GetBlock(x, y + 1, z).IsAir
                 && (top.Name.Contains("leaves") || top == Blocks.GrassBlock || top == Blocks.Dirt || top == Podzol || top == SnowBlock
                     || top == Blocks.Stone || top == Blocks.Gravel || top == Blocks.Sand || top.Name.EndsWith("_log")))
            chunk.SetBlock(x, y + 1, z, SnowLayer);
    }

    // --- Ores: vanilla vein shapes (WorldGenMinable), counts and heights of 1.12 plus 1.18 deepslate ores ---

    private static readonly (string Ore, int Veins, int Size, int MinY, int MaxY)[] OreTable =
    [
        ("dirt", 10, 33, 0, 256), ("gravel", 8, 33, 0, 256), ("granite", 10, 33, 0, 80), ("diorite", 10, 33, 0, 80),
        ("andesite", 10, 33, 0, 80), ("coal_ore", 20, 17, 0, 128), ("iron_ore", 20, 9, 0, 64), ("copper_ore", 16, 10, 0, 96),
        ("gold_ore", 2, 9, 0, 32), ("redstone_ore", 8, 8, 0, 16), ("diamond_ore", 1, 8, 0, 16), ("lapis_ore", 1, 7, 0, 32),
        ("deepslate_iron_ore", 10, 9, -64, 0), ("deepslate_gold_ore", 4, 9, -64, 0), ("deepslate_redstone_ore", 8, 8, -64, -32),
        ("deepslate_diamond_ore", 4, 8, -64, -48), ("deepslate_lapis_ore", 2, 7, -64, -32), ("deepslate_coal_ore", 6, 17, -16, 0),
        ("tuff", 6, 33, -64, 0),
    ];

    private static readonly (BlockType Ore, int Veins, int Size, int MinY, int MaxY)[] Ores_ =
        OreTable.Select(o => (Req(o.Ore), o.Veins, o.Size, o.MinY, o.MaxY)).ToArray();

    private void Ores(Chunk chunk, int chunkX, int chunkZ)
    {
        var random = new JavaRandom(_seed ^ (chunkX * 341873128712L + chunkZ * 132897987541L) ^ 0x5DEECE66DL);
        foreach (var (ore, veins, size, minY, maxY) in Ores_)
            for (var v = 0; v < veins; v++)
                Vein(chunk, random, ore, size, random.NextInt(16), minY + random.NextInt(maxY - minY), random.NextInt(16));
        // Emeralds: single blocks in mountains.
        var biome = chunk.GetBiome(8, 8);
        if (biome is B.ExtremeHills or B.ExtremeHillsPlus or B.ExtremeHillsEdge or 131 or 162)
            for (var i = 3 + random.NextInt(6); i > 0; i--)
            {
                int x = random.NextInt(16), y = 4 + random.NextInt(28), z = random.NextInt(16);
                if (chunk.GetBlock(x, y, z) == Blocks.Stone)
                    chunk.SetBlock(x, y, z, Req("emerald_ore"));
            }
    }

    /// <summary>One ore vein; it replaces stone and deepslate, or only <paramref name="host"/> when given (netherrack...).</summary>
    internal static void Vein(Chunk chunk, JavaRandom random, BlockType ore, int size, int originX, int originY, int originZ, BlockType? host = null)
    {
        var deep = originY < 0;
        var angle = random.NextFloat() * Math.PI;
        double x1 = originX + Math.Sin(angle) * size / 8, x2 = originX - Math.Sin(angle) * size / 8;
        double z1 = originZ + Math.Cos(angle) * size / 8, z2 = originZ - Math.Cos(angle) * size / 8;
        double y1 = originY + random.NextInt(3) - 2, y2 = originY + random.NextInt(3) - 2;
        for (var i = 0; i < size; i++)
        {
            double cx = x1 + (x2 - x1) * i / size, cy = y1 + (y2 - y1) * i / size, cz = z1 + (z2 - z1) * i / size;
            var radius = random.NextDouble() * size / 16;
            var hRadius = (Math.Sin(i * Math.PI / size) + 1) * radius + 1;
            var vRadius = (Math.Sin(i * Math.PI / size) + 1) * radius + 1;
            for (var x = (int)Math.Floor(cx - hRadius / 2); x <= (int)Math.Floor(cx + hRadius / 2); x++)
            for (var y = (int)Math.Floor(cy - vRadius / 2); y <= (int)Math.Floor(cy + vRadius / 2); y++)
            for (var z = (int)Math.Floor(cz - hRadius / 2); z <= (int)Math.Floor(cz + hRadius / 2); z++)
            {
                if (x is < 0 or > 15 || z is < 0 or > 15)
                    continue;
                double dx = (x + 0.5 - cx) / (hRadius / 2), dy = (y + 0.5 - cy) / (vRadius / 2), dz = (z + 0.5 - cz) / (hRadius / 2);
                if (dx * dx + dy * dy + dz * dz >= 1)
                    continue;
                var current = chunk.GetBlock(x, y, z);
                if (host is not null ? current == host : current == Blocks.Stone || (deep && current == Blocks.Deepslate))
                    chunk.SetBlock(x, y, z, ore);
            }
        }
    }

    /// <summary>Deterministic pseudo-random value in [0, 1) for a position and salt.</summary>
    private double Random01(int x, int z, int salt)
    {
        var h = (ulong)_seed ^ ((ulong)(uint)x * 0x9E3779B97F4A7C15UL) ^ ((ulong)(uint)z * 0xC2B2AE3D27D4EB4FUL) ^ ((ulong)(uint)salt * 0x165667B19E3779F9UL);
        h ^= h >> 33;
        h *= 0xFF51AFD7ED558CCDUL;
        h ^= h >> 33;
        h *= 0xC4CEB9FE1A85EC53UL;
        h ^= h >> 33;
        return (h >> 11) * (1.0 / (1UL << 53));
    }
}

/// <summary>Writes world coordinates into one chunk, ignoring what falls outside (trees crossing borders).</summary>
internal readonly struct ChunkWriter(Chunk chunk, int baseX, int baseZ)
{
    public BlockType? Get(int x, int y, int z) =>
        x - baseX is < 0 or > 15 || z - baseZ is < 0 or > 15 ? null : chunk.GetBlock(x - baseX, y, z - baseZ);

    public void Set(int x, int y, int z, BlockType block)
    {
        if (x - baseX is >= 0 and <= 15 && z - baseZ is >= 0 and <= 15)
            chunk.SetBlock(x - baseX, y, z - baseZ, block);
    }
}
