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

// Ported from Glowstone (MIT), net/glowstone/generator/OverworldGenerator.java and generator/ground/*.java.
// Biome heights use the vanilla depth/scale of each biome (VanillaBiomes) instead of Glowstone's partial table;
// the mesa ground uses its colour bands (Glowstone computed them but never applied them).
namespace Bedrokia.Server.Game.Generation;

/// <summary>
/// Vanilla-1.12-style terrain: 3D density noise shaped by the biome heights, then biome surfaces. Terrain spans
/// y 0..255 like Java 1.12; below 0 is deepslate down to the bedrock floor. Thread-safe.
/// </summary>
internal sealed class OverworldTerrain
{
    /// <summary>Glowstone's sea level: water fills y &lt; 63, so the top water block is y 62.</summary>
    public const int GlowSeaLevel = 64;

    private const double CoordinateScale = 684.412, HeightScale = 684.412, HeightNoiseScaleX = 200, HeightNoiseScaleZ = 200;
    private const double DetailNoiseScaleX = 80, DetailNoiseScaleY = 160, DetailNoiseScaleZ = 80, SurfaceScale = 0.0625;
    private const double BaseSize = 8.5, StretchY = 12;

    private static readonly double[,] ElevationWeight = BuildElevationWeight();
    private static readonly Dictionary<int, VanillaBiome> BiomeData = VanillaBiomes.All.ToDictionary(b => b.Id);

    private readonly long _seed;
    private readonly PerlinOctaveGenerator _height, _roughness, _roughness2, _detail;
    private readonly SimplexOctaveGenerator _surface, _climateNoise;
    private readonly MesaBands _mesa;
    private readonly ThreadLocal<(MapLayer Blocks, MapLayer LowRes)> _layers;

    public OverworldTerrain(long seed)
    {
        _seed = seed;
        var random = new JavaRandom(seed);
        _height = new PerlinOctaveGenerator(random, 16, 5, 5) { XScale = HeightNoiseScaleX, ZScale = HeightNoiseScaleZ };
        _roughness = new PerlinOctaveGenerator(random, 16, 5, 33, 5) { XScale = CoordinateScale, YScale = HeightScale, ZScale = CoordinateScale };
        _roughness2 = new PerlinOctaveGenerator(random, 16, 5, 33, 5) { XScale = CoordinateScale, YScale = HeightScale, ZScale = CoordinateScale };
        _detail = new PerlinOctaveGenerator(random, 8, 5, 33, 5)
        {
            XScale = CoordinateScale / DetailNoiseScaleX, YScale = HeightScale / DetailNoiseScaleY, ZScale = CoordinateScale / DetailNoiseScaleZ,
        };
        _surface = new SimplexOctaveGenerator(random, 4, 16, 16);
        _surface.SetScale(SurfaceScale);
        // GlowBiomeClimate: altitude temperature variation noise.
        _climateNoise = new SimplexOctaveGenerator(new JavaRandom(1234), 1);
        _climateNoise.SetScale(1 / 8.0);
        _mesa = new MesaBands(seed);
        _layers = new ThreadLocal<(MapLayer, MapLayer)>(() => MapLayer.Initialize(seed));
    }

    /// <summary>Block-resolution legacy biome ids, row-major (x + z * sizeX).</summary>
    public int[] Biomes(int x, int z, int sizeX, int sizeZ) => _layers.Value.Blocks.Generate(x, z, sizeX, sizeZ);

    public static float Temperature(int biome) => BiomeData.TryGetValue(biome, out var b) ? b.Temperature : 0.5f;

    /// <summary>Glowstone GlowBiomeClimate.isCold: colder with altitude above y 64.</summary>
    public bool IsCold(int biome, int x, int y, int z)
    {
        double temperature = Temperature(biome);
        if (y > 64)
            temperature -= (_climateNoise.Noise(x, z, 0.5, 2.0) * 4.0 + (y - 64)) * 0.05 / 30.0;
        return temperature < 0.15;
    }

    /// <summary>Raw terrain, surfaces and biomes of one chunk (no trees or ores).</summary>
    public Chunk Generate(int chunkX, int chunkZ, int[] biomes)
    {
        var chunk = new Chunk(chunkX, chunkZ);
        var random = new JavaRandom(chunkX * 341873128712L + chunkZ * 132897987541L);
        var water = Blocks.Water;
        var stone = Blocks.Stone;

        // Density is sampled every 4 blocks horizontally and 8 vertically, then trilinearly interpolated.
        var density = Density(chunkX, chunkZ);
        for (var i = 0; i < 4; i++)
        for (var j = 0; j < 4; j++)
        for (var k = 0; k < 32; k++)
        {
            double d1 = density[i, j, k], d2 = density[i + 1, j, k], d3 = density[i, j + 1, k], d4 = density[i + 1, j + 1, k];
            var d5 = (density[i, j, k + 1] - d1) / 8;
            var d6 = (density[i + 1, j, k + 1] - d2) / 8;
            var d7 = (density[i, j + 1, k + 1] - d3) / 8;
            var d8 = (density[i + 1, j + 1, k + 1] - d4) / 8;
            for (var l = 0; l < 8; l++)
            {
                double d9 = d1, d10 = d3;
                for (var m = 0; m < 4; m++)
                {
                    var dens = d9;
                    for (var n = 0; n < 4; n++)
                    {
                        var y = l + (k << 3);
                        if (dens > 0)
                            chunk.SetBlock(m + (i << 2), y, n + (j << 2), stone);
                        else if (y < GlowSeaLevel - 1)
                            chunk.SetBlock(m + (i << 2), y, n + (j << 2), water);
                        dens += (d10 - d9) / 4;
                    }
                    d9 += (d2 - d1) / 4;
                    d10 += (d4 - d3) / 4;
                }
                d1 += d5;
                d3 += d7;
                d2 += d6;
                d4 += d8;
            }
        }

        int cx = chunkX << 4, cz = chunkZ << 4;
        var surfaceNoise = _surface.GetFractalBrownianMotion(cx, cz, 0.5, 0.5);
        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
        {
            var biome = biomes[x + z * 16];
            chunk.SetBiome(x, z, (byte)biome);
            Ground(chunk, random, cx + x, cz + z, biome, surfaceNoise[x | z << 4]);
            // Below the 1.12-style terrain: deepslate and a ragged bedrock floor, like a 1.18 world's depths.
            for (var y = Chunk.MinY; y < 0; y++)
                chunk.SetBlock(x, y, z, y <= Chunk.MinY + random.NextInt(5) ? Blocks.Bedrock : Blocks.Deepslate);
        }
        return chunk;
    }

    private double[,,] Density(int chunkX, int chunkZ)
    {
        int x = chunkX << 2, z = chunkZ << 2;
        var biomeGrid = _layers.Value.LowRes.Generate(x - 2, z - 2, 10, 10);
        var heightNoise = _height.GetFractalBrownianMotion(x, z, 0.5, 2.0);
        var roughness = _roughness.GetFractalBrownianMotion(x, 0, z, 0.5, 2.0);
        var roughness2 = _roughness2.GetFractalBrownianMotion(x, 0, z, 0.5, 2.0);
        var detail = _detail.GetFractalBrownianMotion(x, 0, z, 0.5, 2.0);
        var density = new double[5, 5, 33];

        int index = 0, indexHeight = 0;
        for (var i = 0; i < 5; i++)
        for (var j = 0; j < 5; j++)
        {
            double avgScale = 0, avgBase = 0, totalWeight = 0;
            var (centerHeight, _) = HeightOf(biomeGrid[i + 2 + (j + 2) * 10]);
            for (var m = 0; m < 5; m++)
            for (var n = 0; n < 5; n++)
            {
                var (heightBase, heightScale) = HeightOf(biomeGrid[i + m + (j + n) * 10]);
                var weight = ElevationWeight[m, n] / (heightBase + 2.0);
                if (heightBase > centerHeight)
                    weight *= 0.5;
                avgScale += heightScale * weight;
                avgBase += heightBase * weight;
                totalWeight += weight;
            }
            avgScale = avgScale / totalWeight * 0.9 + 0.1;
            avgBase = (avgBase / totalWeight * 4.0 - 1.0) / 8.0;

            var noiseH = heightNoise[indexHeight++] / 8000.0;
            if (noiseH < 0)
                noiseH = Math.Abs(noiseH) * 0.3;
            noiseH = noiseH * 3.0 - 2.0;
            noiseH = noiseH < 0 ? Math.Max(noiseH * 0.5, -1) / 1.4 * 0.5 : Math.Min(noiseH, 1) / 8.0;
            noiseH = (noiseH * 0.2 + avgBase) * BaseSize / 8.0 * 4.0 + BaseSize;

            for (var k = 0; k < 33; k++)
            {
                var nh = (k - noiseH) * StretchY * 128.0 / 256.0 / avgScale;
                if (nh < 0)
                    nh *= 4.0;
                var noiseR = roughness[index] / 512.0;
                var noiseR2 = roughness2[index] / 512.0;
                var noiseD = (detail[index] / 10.0 + 1.0) / 2.0;
                var dens = noiseD < 0 ? noiseR : noiseD > 1 ? noiseR2 : noiseR + (noiseR2 - noiseR) * noiseD;
                dens -= nh;
                index++;
                if (k > 29)
                {
                    var lowering = (k - 29) / 3.0;
                    dens = dens * (1.0 - lowering) + -10.0 * lowering;
                }
                density[i, j, k] = dens;
            }
        }
        return density;
    }

    // Vanilla biome depth/scale (rootHeight/heightVariation).
    private static (double Height, double Scale) HeightOf(int biome) =>
        BiomeData.TryGetValue(biome, out var b) ? (b.Depth, b.Scale) : (0.1, 0.2);

    private static double[,] BuildElevationWeight()
    {
        var weights = new double[5, 5];
        for (var x = 0; x < 5; x++)
        for (var z = 0; z < 5; z++)
            weights[x, z] = 10.0 / Math.Sqrt((x - 2) * (x - 2) + (z - 2) * (z - 2) + 0.2);
        return weights;
    }

    // --- Ground (Glowstone generator/ground) ---

    private static readonly BlockType Grass = Blocks.GrassBlock, Dirt = Blocks.Dirt, Sand = Blocks.Sand, Sandstone = Blocks.Sandstone,
        Gravel = Blocks.Gravel, Air = Blocks.Air, Stone = Blocks.Stone, Water = Blocks.Water;
    private static readonly BlockType Mycelium = Blocks.ByName("mycelium")!, SnowBlock = Blocks.ByName("snow")!,
        Podzol = Blocks.ByName("podzol")!, CoarseDirt = Blocks.ByName("coarse_dirt")!, Ice = Blocks.ByName("ice")!,
        RedSand = Blocks.ByName("red_sand")!, Terracotta = Blocks.ByName("hardened_clay")!,
        OrangeTerracotta = Blocks.ByName("orange_terracotta")!;

    private void Ground(Chunk chunk, JavaRandom random, int worldX, int worldZ, int biome, double surfaceNoise)
    {
        if (B.IsMesa(biome))
        {
            _mesa.Column(chunk, random, worldX, worldZ, biome == B.MesaBryce, surfaceNoise);
            return;
        }
        var (top, ground) = biome switch
        {
            B.Beach or B.ColdBeach or B.Desert or B.DesertHills or 130 => (Sand, Sand),
            B.MushroomIsland or B.MushroomShore => (Mycelium, Dirt),
            140 => (SnowBlock, Dirt), // ice spikes
            B.StoneBeach => (Stone, Stone),
            B.MegaTaiga or B.MegaTaigaHills or 160 or 161 =>
                (surfaceNoise > 1.75 ? CoarseDirt : surfaceNoise > -0.95 ? Podzol : Grass, Dirt),
            B.ExtremeHills or B.ExtremeHillsPlus or B.ExtremeHillsEdge or 131 or 162 =>
                surfaceNoise > 1.0 ? (Stone, Stone) : (Grass, Dirt),
            163 or 164 => surfaceNoise > 1.75 ? (Stone, Stone) : surfaceNoise > -0.5 ? (CoarseDirt, Dirt) : (Grass, Dirt),
            _ => (Grass, Dirt),
        };
        var topMat = top;
        var groundMat = ground;
        int x = worldX & 15, z = worldZ & 15;
        var surfaceHeight = Math.Max((int)(surfaceNoise / 3.0 + 3.0 + random.NextDouble() * 0.25), 1);
        var deep = -1;
        for (var y = 255; y >= 0; y--)
        {
            random.NextInt(5); // Glowstone's bedrock roll at the bottom of the 0..255 column (kept for the sequence)
            var block = chunk.GetBlock(x, y, z);
            if (block.IsAir)
                deep = -1;
            else if (block == Stone)
            {
                if (deep == -1)
                {
                    if (y >= GlowSeaLevel - 5 && y <= GlowSeaLevel)
                    {
                        topMat = top;
                        groundMat = ground;
                    }
                    deep = surfaceHeight;
                    if (y >= GlowSeaLevel - 2)
                        chunk.SetBlock(x, y, z, topMat);
                    else if (y < GlowSeaLevel - 8 - surfaceHeight)
                    {
                        topMat = Air;
                        groundMat = Stone;
                        chunk.SetBlock(x, y, z, Gravel);
                    }
                    else
                        chunk.SetBlock(x, y, z, groundMat);
                }
                else if (deep > 0)
                {
                    deep--;
                    chunk.SetBlock(x, y, z, groundMat);
                    if (deep == 0 && groundMat == Sand)
                    {
                        deep = random.NextInt(4) + Math.Max(0, y - GlowSeaLevel - 1);
                        groundMat = Sandstone;
                    }
                }
            }
            else if (block == Water && y == GlowSeaLevel - 2 && IsCold(biome, worldX, y, worldZ))
                chunk.SetBlock(x, y, z, Ice);
        }
    }

    /// <summary>Glowstone MesaGroundGenerator with its terracotta colour bands applied.</summary>
    private sealed class MesaBands
    {
        private readonly int[] _colours = new int[64];
        private readonly SimplexOctaveGenerator _colourNoise, _canyonHeight, _canyonScale;
        private static readonly string[] ColourNames =
        [
            "white", "orange", "magenta", "light_blue", "yellow", "lime", "pink", "gray", "light_gray", "cyan",
            "purple", "blue", "brown", "green", "red", "black",
        ];

        public MesaBands(long seed)
        {
            var random = new JavaRandom(seed);
            _colourNoise = new SimplexOctaveGenerator(random, 1);
            _colourNoise.SetScale(1 / 512.0);
            InitColours(random);
            _canyonHeight = new SimplexOctaveGenerator(random, 4);
            _canyonHeight.SetScale(1 / 4.0);
            _canyonScale = new SimplexOctaveGenerator(random, 1);
            _canyonScale.SetScale(1 / 512.0);
        }

        public void Column(Chunk chunk, JavaRandom random, int worldX, int worldZ, bool bryce, double surfaceNoise)
        {
            var surfaceHeight = Math.Max((int)(surfaceNoise / 3.0 + 3.0 + random.NextDouble() * 0.25), 1);
            var coloured = Math.Cos(surfaceNoise / 3.0 * Math.PI) <= 0;
            double canyon = 0;
            if (bryce)
            {
                var noiseX = (worldX & ~15) + (worldZ & 15);
                var noiseZ = (worldZ & ~15) + (worldX & 15);
                var height = Math.Min(Math.Abs(surfaceNoise), _canyonHeight.Noise(noiseX, noiseZ, 0.5, 2.0));
                if (height > 0)
                {
                    var scale = Math.Abs(_canyonScale.Noise(noiseX, noiseZ, 0.5, 2.0));
                    canyon = Math.Min(Math.Pow(height, 2) * 2.5, Math.Ceiling(50 * scale) + 14) + GlowSeaLevel;
                }
            }
            int x = worldX & 15, z = worldZ & 15;
            var deep = -1;
            var groundSet = false;
            var bandShift = (int)Math.Round(_colourNoise.Noise(worldX, worldZ, 0.5, 2.0) * 2.0);
            for (var y = 255; y >= 0; y--)
            {
                if (y < (int)canyon && chunk.GetBlock(x, y, z).IsAir)
                    chunk.SetBlock(x, y, z, Stone);
                random.NextInt(5);
                var block = chunk.GetBlock(x, y, z);
                if (block.IsAir)
                {
                    deep = -1;
                    continue;
                }
                if (block != Stone)
                    continue;
                if (deep == -1)
                {
                    groundSet = false;
                    deep = surfaceHeight + Math.Max(0, y - GlowSeaLevel - 1);
                    if (y >= GlowSeaLevel - 2)
                    {
                        if (y > GlowSeaLevel + 2 + surfaceHeight)
                            chunk.SetBlock(x, y, z, y < GlowSeaLevel || y > 128 ? OrangeTerracotta : coloured ? Band(y, bandShift) : Terracotta);
                        else
                        {
                            chunk.SetBlock(x, y, z, RedSand);
                            groundSet = true;
                        }
                    }
                    else
                        chunk.SetBlock(x, y, z, OrangeTerracotta);
                }
                else if (deep > 0)
                {
                    deep--;
                    chunk.SetBlock(x, y, z, groundSet ? OrangeTerracotta : Band(y, bandShift));
                }
            }
        }

        private BlockType Band(int y, int shift)
        {
            var colour = _colours[((y + shift) % _colours.Length + _colours.Length) % _colours.Length];
            return colour < 0 ? Terracotta : Blocks.ByName($"{ColourNames[colour]}_terracotta") ?? Terracotta;
        }

        private void InitColours(JavaRandom random)
        {
            Array.Fill(_colours, -1);
            var i = 0;
            while (i < _colours.Length)
            {
                i += random.NextInt(5) + 1;
                if (i < _colours.Length)
                    _colours[i++] = 1; // orange
            }
            RandomBands(random, 2, 1, 4); // yellow
            RandomBands(random, 2, 2, 12); // brown
            RandomBands(random, 2, 1, 14); // red
            var j = 0;
            for (i = 0; i < random.NextInt(3) + 3; i++)
            {
                j += random.NextInt(16) + 4;
                if (j >= _colours.Length)
                    break;
                if (random.NextInt(2) == 0 || j < _colours.Length - 1 && random.NextInt(2) == 0)
                    _colours[j - 1] = 8; // light gray
                else
                    _colours[j] = 0; // white
            }
        }

        private void RandomBands(JavaRandom random, int minCount, int minHeight, int colour)
        {
            for (var i = 0; i < random.NextInt(4) + minCount; i++)
            {
                var j = random.NextInt(_colours.Length);
                var k = 0;
                while (k < random.NextInt(3) + minHeight && j < _colours.Length)
                {
                    _colours[j++] = colour;
                    k++;
                }
            }
        }
    }
}
