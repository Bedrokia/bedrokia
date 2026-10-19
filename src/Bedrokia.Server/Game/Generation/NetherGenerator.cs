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

using static Bedrokia.Server.Game.WorldGenerator;

namespace Bedrokia.Server.Game.Generation;

/// <summary>
/// The Nether (y 0-127): 3D density terrain of netherrack with a lava sea up to y 31, bedrock floor and ceiling,
/// soul sand and gravel shores, then glowstone clusters, fire, mushrooms, hidden lava, quartz, nether gold and magma.
/// Terrain and decorators ported from Glowstone (MIT) generator/NetherGenerator.java and generator/populators.
/// </summary>
public sealed class NetherGenerator : IWorldGenerator
{
    public const int Height = 128, LavaLevel = 32;
    private const byte HellBiome = 8;

    // Glowstone WorldConfig defaults for the nether.
    private const double CoordinateScale = 684.412, HeightScale = 2053.236, HeightNoiseScaleX = 100, HeightNoiseScaleZ = 100,
        DetailNoiseScaleX = 80, DetailNoiseScaleY = 60, DetailNoiseScaleZ = 80, SurfaceScale = 0.0625;

    private readonly PerlinOctaveGenerator _height, _roughness, _roughness2, _detail, _surface, _soulSand, _gravel;

    private static readonly BlockType Netherrack = Req("netherrack"), SoulSand = Req("soul_sand"), Gravel = Req("gravel"),
        Glowstone = Req("glowstone"), Fire = Req("fire"), BrownMushroom = Req("brown_mushroom"), RedMushroom = Req("red_mushroom"),
        QuartzOre = Req("quartz_ore"), NetherGoldOre = Req("nether_gold_ore"), Magma = Req("magma"), AncientDebris = Req("ancient_debris"),
        Lava = Blocks.Get("lava", ("liquid_depth", 0));

    public NetherGenerator(long seed)
    {
        Seed = seed;
        var random = new JavaRandom(seed);
        _height = new PerlinOctaveGenerator(random, 16, 5, 5) { XScale = HeightNoiseScaleX, ZScale = HeightNoiseScaleZ };
        _roughness = new PerlinOctaveGenerator(random, 16, 5, 17, 5) { XScale = CoordinateScale, YScale = HeightScale, ZScale = CoordinateScale };
        _roughness2 = new PerlinOctaveGenerator(random, 16, 5, 17, 5) { XScale = CoordinateScale, YScale = HeightScale, ZScale = CoordinateScale };
        _detail = new PerlinOctaveGenerator(random, 8, 5, 17, 5)
        {
            XScale = CoordinateScale / DetailNoiseScaleX, YScale = HeightScale / DetailNoiseScaleY, ZScale = CoordinateScale / DetailNoiseScaleZ,
        };
        _surface = new PerlinOctaveGenerator(random, 4, 16, 16, 1);
        _surface.SetScale(SurfaceScale);
        _soulSand = new PerlinOctaveGenerator(random, 4, 16, 16, 1) { XScale = SurfaceScale / 2, YScale = SurfaceScale / 2 };
        _gravel = new PerlinOctaveGenerator(random, 4, 16, 1, 16) { XScale = SurfaceScale / 2, ZScale = SurfaceScale / 2 };
    }

    public long Seed { get; }

    public Chunk Generate(int chunkX, int chunkZ)
    {
        var chunk = new Chunk(chunkX, chunkZ);
        var random = new JavaRandom(chunkX * 341873128712L + chunkZ * 132897987541L);
        RawTerrain(chunk, chunkX, chunkZ);
        int cx = chunkX << 4, cz = chunkZ << 4;
        var surfaceNoise = _surface.GetFractalBrownianMotion(cx, cz, 0, 0.5, 2.0);
        var soulSandNoise = _soulSand.GetFractalBrownianMotion(cx, cz, 0, 0.5, 2.0);
        var gravelNoise = _gravel.GetFractalBrownianMotion(cx, 0, cz, 0.5, 2.0);
        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
        {
            chunk.SetBiome(x, z, HellBiome);
            Column(chunk, random, x, z, surfaceNoise[x | z << 4], soulSandNoise[x | z << 4], gravelNoise[x | z << 4]);
        }
        Populate(chunk, random);
        return chunk;
    }

    /// <summary>Glowstone getFixedSpawnLocation: an air pocket near the origin, lowered onto the ground.</summary>
    public (int X, int Y, int Z) FindSpawn()
    {
        var random = new JavaRandom(Seed);
        for (var attempt = 0; attempt < 1000; attempt++)
        {
            int x = random.NextInt(128) - 64, y = Height * 3 / 4, z = random.NextInt(128) - 64;
            var chunk = Generate(x >> 4, z >> 4);
            if (!chunk.GetBlock(x & 15, y, z & 15).IsAir)
                continue;
            while (y > 1 && chunk.GetBlock(x & 15, y - 1, z & 15).IsAir)
                y--;
            if (chunk.GetBlock(x & 15, y - 1, z & 15) != Lava)
                return (x, y, z);
        }
        return (0, 64, 0);
    }

    // --- Terrain: density on a 5x5x17 grid, interpolated to blocks ---

    private void RawTerrain(Chunk chunk, int chunkX, int chunkZ)
    {
        var density = Density(chunkX << 2, chunkZ << 2);
        for (var i = 0; i < 4; i++)
        for (var j = 0; j < 4; j++)
        for (var k = 0; k < 16; k++)
        {
            double d1 = density[i, j, k], d2 = density[i + 1, j, k], d3 = density[i, j + 1, k], d4 = density[i + 1, j + 1, k];
            double d5 = (density[i, j, k + 1] - d1) / 8, d6 = (density[i + 1, j, k + 1] - d2) / 8;
            double d7 = (density[i, j + 1, k + 1] - d3) / 8, d8 = (density[i + 1, j + 1, k + 1] - d4) / 8;
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
                            chunk.SetBlock(m + (i << 2), y, n + (j << 2), Netherrack);
                        else if (y < LavaLevel)
                            chunk.SetBlock(m + (i << 2), y, n + (j << 2), Lava);
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
    }

    private double[,,] Density(int x, int z)
    {
        var heightNoise = _height.GetFractalBrownianMotion(x, z, 0.5, 2.0);
        var roughnessNoise = _roughness.GetFractalBrownianMotion(x, 0, z, 0.5, 2.0);
        var roughnessNoise2 = _roughness2.GetFractalBrownianMotion(x, 0, z, 0.5, 2.0);
        var detailNoise = _detail.GetFractalBrownianMotion(x, 0, z, 0.5, 2.0);
        var nv = new double[17];
        for (var i = 0; i < 17; i++)
        {
            nv[i] = Math.Cos(i * Math.PI * 6.0 / 17.0) * 2.0;
            double nh = i > 17 / 2 ? 17 - 1 - i : i;
            if (nh < 4.0)
            {
                nh = 4.0 - nh;
                nv[i] -= nh * nh * nh * 10.0;
            }
        }
        var density = new double[5, 5, 17];
        int index = 0, indexHeight = 0;
        for (var i = 0; i < 5; i++)
        for (var j = 0; j < 5; j++)
        {
            _ = heightNoise[indexHeight++]; // computed by Glowstone but not used for the nether
            for (var k = 0; k < 17; k++)
            {
                var noiseR = roughnessNoise[index] / 512.0;
                var noiseR2 = roughnessNoise2[index] / 512.0;
                var noiseD = (detailNoise[index] / 10.0 + 1.0) / 2.0;
                var dens = noiseD < 0 ? noiseR : noiseD > 1 ? noiseR2 : noiseR + (noiseR2 - noiseR) * noiseD;
                dens -= nv[k];
                index++;
                if (k > 13)
                {
                    var lowering = (k - 13) / 3.0;
                    dens = dens * (1.0 - lowering) + lowering * -10.0;
                }
                density[i, j, k] = dens;
            }
        }
        return density;
    }

    private static void Column(Chunk chunk, JavaRandom random, int x, int z, double surfaceNoise, double soulSandNoise, double gravelNoise)
    {
        BlockType top = Netherrack, ground = Netherrack;
        var soulSand = soulSandNoise + random.NextDouble() * 0.2 > 0;
        var gravel = gravelNoise + random.NextDouble() * 0.2 > 0;
        var surfaceHeight = (int)(surfaceNoise / 3.0 + 3.0 + random.NextDouble() * 0.25);
        var deep = -1;
        for (var y = Height - 1; y >= 0; y--)
        {
            if (y <= random.NextInt(5) || y >= Height - 1 - random.NextInt(5))
            {
                chunk.SetBlock(x, y, z, Blocks.Bedrock);
                continue;
            }
            var block = chunk.GetBlock(x, y, z);
            if (block.IsAir)
                deep = -1;
            else if (block == Netherrack)
            {
                if (deep == -1)
                {
                    if (surfaceHeight <= 0)
                    {
                        top = Blocks.Air;
                        ground = Netherrack;
                    }
                    else if (y is >= 60 and <= 65)
                    {
                        top = gravel ? Gravel : Netherrack;
                        ground = Netherrack;
                        if (soulSand)
                            top = ground = SoulSand;
                    }
                    deep = surfaceHeight;
                    chunk.SetBlock(x, y, z, y >= 63 ? top : ground);
                }
                else if (deep > 0)
                {
                    deep--;
                    chunk.SetBlock(x, y, z, ground);
                }
            }
        }
    }

    // --- Populators (Glowstone NetherPopulator): ores, hidden lava, fire, glowstone, mushrooms ---

    private static void Populate(Chunk chunk, JavaRandom random)
    {
        // In-ground: quartz (vanilla 16 veins of 14), nether gold (10 of 10), magma band (4 of 33, y 27-36), ancient debris.
        for (var i = 0; i < 16; i++)
            Vein(chunk, random, QuartzOre, 14, random.NextInt(16), 10 + random.NextInt(108), random.NextInt(16), Netherrack);
        for (var i = 0; i < 10; i++)
            Vein(chunk, random, NetherGoldOre, 10, random.NextInt(16), 10 + random.NextInt(108), random.NextInt(16), Netherrack);
        for (var i = 0; i < 4; i++)
            Vein(chunk, random, Magma, 33, random.NextInt(16), 27 + random.NextInt(10), random.NextInt(16), Netherrack);
        if (random.NextInt(2) == 0)
            Vein(chunk, random, AncientDebris, 3, random.NextInt(16), 8 + random.NextInt(16), random.NextInt(16), Netherrack);

        HiddenLava(chunk, random, 8, flowing: true);
        PlaceFire(chunk, random);
        GlowstoneCluster(chunk, random, variableHeight: true);
        GlowstoneCluster(chunk, random, variableHeight: false);
        PlaceFire(chunk, random);
        Mushroom(chunk, random, BrownMushroom);
        Mushroom(chunk, random, RedMushroom);
        HiddenLava(chunk, random, 16, flowing: false);
    }

    // LavaDecorator: lava in netherrack walls that have exactly one air side (flowing) or none (hidden).
    private static void HiddenLava(Chunk chunk, JavaRandom random, int amount, bool flowing)
    {
        for (var i = 0; i < amount; i++)
        {
            int x = 1 + random.NextInt(14), z = 1 + random.NextInt(14);
            var y = flowing ? random.NextInt(random.NextInt(120) + 8) : 10 + random.NextInt(108);
            if (y is < 1 or > Height - 2 || chunk.GetBlock(x, y, z) != Netherrack || chunk.GetBlock(x, y + 1, z) != Netherrack)
                continue;
            var netherrack = 0;
            var air = 0;
            foreach (var (dx, dz) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                var side = chunk.GetBlock(x + dx, y, z + dz);
                if (side == Netherrack)
                    netherrack++;
                else if (side.IsAir)
                    air++;
            }
            if (chunk.GetBlock(x, y - 1, z) == Netherrack)
                netherrack++;
            if (netherrack == 4 && air == (flowing ? 1 : 0) || !flowing && netherrack == 5)
                chunk.SetBlock(x, y, z, Lava);
        }
    }

    // FireDecorator: fire on netherrack.
    private static void PlaceFire(Chunk chunk, JavaRandom random)
    {
        var amount = 1 + random.NextInt(random.NextInt(10) + 1);
        for (var i = 0; i < amount; i++)
        {
            int x = random.NextInt(16), z = random.NextInt(16), y = 4 + random.NextInt(120);
            for (var j = 0; j < 64; j++)
            {
                int fx = x + random.NextInt(8) - random.NextInt(8), fz = z + random.NextInt(8) - random.NextInt(8);
                var fy = y + random.NextInt(4) - random.NextInt(4);
                if (fx is < 0 or > 15 || fz is < 0 or > 15 || fy is < 1 or > Height - 2)
                    continue;
                if (chunk.GetBlock(fx, fy, fz).IsAir && chunk.GetBlock(fx, fy - 1, fz) == Netherrack)
                    chunk.SetBlock(fx, fy, fz, Fire);
            }
        }
    }

    // GlowstoneDecorator: a cluster hanging from a netherrack ceiling, grown from blocks touching exactly one glowstone.
    private static void GlowstoneCluster(Chunk chunk, JavaRandom random, bool variableHeight)
    {
        var amount = variableHeight ? 1 + random.NextInt(random.NextInt(10) + 1) : random.NextInt(10);
        for (var i = 0; i < amount; i++)
        {
            int x = 2 + random.NextInt(12), z = 2 + random.NextInt(12), y = 4 + random.NextInt(120);
            if (y > Height - 2 || !chunk.GetBlock(x, y, z).IsAir || chunk.GetBlock(x, y + 1, z) != Netherrack)
                continue;
            chunk.SetBlock(x, y, z, Glowstone);
            for (var j = 0; j < 1500; j++)
            {
                int gx = x + random.NextInt(8) - random.NextInt(8), gz = z + random.NextInt(8) - random.NextInt(8);
                var gy = y - random.NextInt(12);
                if (gx is < 1 or > 14 || gz is < 1 or > 14 || gy is < 1 or > Height - 2 || !chunk.GetBlock(gx, gy, gz).IsAir)
                    continue;
                var touching = 0;
                foreach (var (dx, dy, dz) in new[] { (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1) })
                    if (chunk.GetBlock(gx + dx, gy + dy, gz + dz) == Glowstone)
                        touching++;
                if (touching == 1)
                    chunk.SetBlock(gx, gy, gz, Glowstone);
            }
        }
    }

    // MushroomDecorator: one in four chunks, a few mushrooms on netherrack or soul sand.
    private static void Mushroom(Chunk chunk, JavaRandom random, BlockType mushroom)
    {
        if (random.NextInt(4) != 0)
            return;
        int x = random.NextInt(16), z = random.NextInt(16), y = random.NextInt(Height);
        for (var i = 0; i < 64; i++)
        {
            int mx = x + random.NextInt(8) - random.NextInt(8), mz = z + random.NextInt(8) - random.NextInt(8);
            var my = y + random.NextInt(4) - random.NextInt(4);
            if (mx is < 0 or > 15 || mz is < 0 or > 15 || my is < 1 or > Height - 2)
                continue;
            var below = chunk.GetBlock(mx, my - 1, mz);
            if (chunk.GetBlock(mx, my, mz).IsAir && (below == Netherrack || below == SoulSand))
                chunk.SetBlock(mx, my, mz, mushroom);
        }
    }
}
