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

// Ported from Glowstone (MIT), net/glowstone/generator/biomegrid/*.java. The biome tables (climate groups, hills,
// edges, shores) are restored to the vanilla 1.12 layout that Glowstone had simplified.
namespace Bedrokia.Server.Game.Generation;

/// <summary>Legacy biome ids (Java 1.12 / Bedrock legacy ids, all present in <see cref="VanillaBiomes"/>).</summary>
internal static class B
{
    public const int Ocean = 0, Plains = 1, Desert = 2, ExtremeHills = 3, Forest = 4, Taiga = 5, Swamp = 6, River = 7,
        FrozenOcean = 10, FrozenRiver = 11, IcePlains = 12, IceMountains = 13, MushroomIsland = 14, MushroomShore = 15,
        Beach = 16, DesertHills = 17, ForestHills = 18, TaigaHills = 19, ExtremeHillsEdge = 20, Jungle = 21,
        JungleHills = 22, JungleEdge = 23, DeepOcean = 24, StoneBeach = 25, ColdBeach = 26, BirchForest = 27,
        BirchForestHills = 28, RoofedForest = 29, ColdTaiga = 30, ColdTaigaHills = 31, MegaTaiga = 32,
        MegaTaigaHills = 33, ExtremeHillsPlus = 34, Savanna = 35, SavannaPlateau = 36, Mesa = 37,
        MesaPlateauStone = 38, MesaPlateau = 39, SunflowerPlains = 129, MesaBryce = 165;

    private static readonly HashSet<int> Valid = VanillaBiomes.All.Select(b => b.Id).ToHashSet();

    public static bool Exists(int id) => Valid.Contains(id);

    public static bool IsOcean(int id) => id is Ocean or DeepOcean or FrozenOcean;

    public static bool IsMesa(int id) => id is Mesa or MesaPlateau or MesaPlateauStone or MesaBryce or 166 or 167;

    public static bool IsJungle(int id) => id is Jungle or JungleHills or JungleEdge or 149 or 151;

    public static bool IsSnowy(int id) => id is IcePlains or IceMountains or ColdTaiga or ColdTaigaHills or 140 or 158;
}

/// <summary>One step of the biome pipeline. Not thread-safe (per-coordinate random): build one chain per thread.</summary>
internal abstract class MapLayer(long seed)
{
    private readonly JavaRandom _random = new(0);

    protected void SetCoordsSeed(int x, int z)
    {
        _random.SetSeed(seed);
        _random.SetSeed(unchecked(x * _random.NextLong() + z * _random.NextLong()) ^ seed);
    }

    protected int NextInt(int max) => _random.NextInt(max);

    public abstract int[] Generate(int x, int z, int sizeX, int sizeZ);

    /// <summary>The block-resolution layer and the 1:4 layer used for terrain heights.</summary>
    public static (MapLayer Blocks, MapLayer LowRes) Initialize(long seed)
    {
        const int zoom = 2;
        MapLayer layer = new NoiseLayer(seed);
        layer = new WhittakerLayer(seed + 1, layer, WhittakerLayer.Kind.WarmWet);
        layer = new WhittakerLayer(seed + 1, layer, WhittakerLayer.Kind.ColdDry);
        layer = new WhittakerLayer(seed + 2, layer, WhittakerLayer.Kind.LargerBiomes);
        for (var i = 0; i < 2; i++)
            layer = new ZoomLayer(seed + 100 + i, layer, blurry: true);
        for (var i = 0; i < 2; i++)
            layer = new ErosionLayer(seed + 3 + i, layer);
        layer = new DeepOceanLayer(seed + 4, layer);

        MapLayer mountains = new VariationLayer(seed + 200, layer, null);
        for (var i = 0; i < 2; i++)
            mountains = new ZoomLayer(seed + 200 + i, mountains);

        layer = new BiomeLayer(seed + 5, layer);
        for (var i = 0; i < 2; i++)
            layer = new ZoomLayer(seed + 200 + i, layer);
        layer = new EdgeLayer(seed + 200, layer);
        layer = new VariationLayer(seed + 200, layer, mountains);
        layer = new RarePlainsLayer(seed + 201, layer);
        layer = new ZoomLayer(seed + 300, layer);
        layer = new ErosionLayer(seed + 6, layer);
        layer = new ZoomLayer(seed + 400, layer);
        layer = new ThinEdgeLayer(seed + 400, layer);
        layer = new ShoreLayer(seed + 7, layer);
        for (var i = 0; i < zoom; i++)
            layer = new ZoomLayer(seed + 500 + i, layer);

        var river = mountains;
        river = new ZoomLayer(seed + 300, river);
        river = new ZoomLayer(seed + 400, river);
        for (var i = 0; i < zoom; i++)
            river = new ZoomLayer(seed + 500 + i, river);
        river = new RiverLayer(seed + 10, river, null);
        layer = new RiverLayer(seed + 1000, river, layer);

        var lowRes = layer;
        for (var i = 0; i < 2; i++)
            layer = new ZoomLayer(seed + 2000 + i, layer);
        layer = new SmoothLayer(seed + 1001, layer);
        return (layer, lowRes);
    }

    // Reads the (size + 2)² neighbourhood of the area: helpers for the 3x3 layers.
    protected static int Up(int[] v, int j, int i, int w) => v[j + 1 + i * w];
    protected static int Down(int[] v, int j, int i, int w) => v[j + 1 + (i + 2) * w];
    protected static int Left(int[] v, int j, int i, int w) => v[j + (i + 1) * w];
    protected static int Right(int[] v, int j, int i, int w) => v[j + 2 + (i + 1) * w];
    protected static int Center(int[] v, int j, int i, int w) => v[j + 1 + (i + 1) * w];
}

/// <summary>Initial land/ocean spread: 0 ocean, 2 warm, 3 cold.</summary>
internal sealed class NoiseLayer(long seed) : MapLayer(seed)
{
    private readonly SimplexOctaveGenerator _noise = new(new JavaRandom(seed), 2);

    public override int[] Generate(int x, int z, int sizeX, int sizeZ)
    {
        var values = new int[sizeX * sizeZ];
        for (var i = 0; i < sizeZ; i++)
        for (var j = 0; j < sizeX; j++)
        {
            var noise = _noise.Noise(x + j, z + i, 0.175, 0.8, true) * 4.0;
            int value;
            if (noise >= 0.05)
                value = noise <= 0.2 ? 3 : 2;
            else
            {
                SetCoordsSeed(x + j, z + i);
                value = NextInt(2) == 0 ? 3 : 0;
            }
            values[j + i * sizeX] = value;
        }
        return values;
    }
}

/// <summary>Climate smoothing (warm next to cold becomes temperate) and rare "special" climates (+1000).</summary>
internal sealed class WhittakerLayer(long seed, MapLayer below, WhittakerLayer.Kind kind) : MapLayer(seed)
{
    public enum Kind { WarmWet, ColdDry, LargerBiomes }

    public override int[] Generate(int x, int z, int sizeX, int sizeZ)
    {
        if (kind == Kind.LargerBiomes)
        {
            var values = below.Generate(x, z, sizeX, sizeZ);
            for (var i = 0; i < sizeZ; i++)
            for (var j = 0; j < sizeX; j++)
            {
                if (values[j + i * sizeX] == 0)
                    continue;
                SetCoordsSeed(x + j, z + i);
                if (NextInt(13) == 0)
                    values[j + i * sizeX] += 1000;
            }
            return values;
        }

        var (value, cross, final) = kind == Kind.WarmWet ? (2, new[] { 3, 1 }, 4) : (3, new[] { 2, 4 }, 1);
        int w = sizeX + 2;
        var grid = below.Generate(x - 1, z - 1, w, sizeZ + 2);
        var result = new int[sizeX * sizeZ];
        for (var i = 0; i < sizeZ; i++)
        for (var j = 0; j < sizeX; j++)
        {
            var center = Center(grid, j, i, w);
            if (center == value && cross.Any(t => Up(grid, j, i, w) == t || Down(grid, j, i, w) == t
                                                  || Left(grid, j, i, w) == t || Right(grid, j, i, w) == t))
                center = final;
            result[j + i * sizeX] = center;
        }
        return result;
    }
}

internal sealed class ZoomLayer(long seed, MapLayer below, bool blurry = false) : MapLayer(seed)
{
    public override int[] Generate(int x, int z, int sizeX, int sizeZ)
    {
        int gridX = x >> 1, gridZ = z >> 1;
        int gridSizeX = (sizeX >> 1) + 2, gridSizeZ = (sizeZ >> 1) + 2;
        var values = below.Generate(gridX, gridZ, gridSizeX, gridSizeZ);
        int zoomSizeX = (gridSizeX - 1) << 1, zoomSizeZ = (gridSizeZ - 1) << 1;
        var tmp = new int[zoomSizeX * zoomSizeZ];
        for (var i = 0; i < gridSizeZ - 1; i++)
        {
            var n = i * 2 * zoomSizeX;
            var upperLeft = values[i * gridSizeX];
            var lowerLeft = values[(i + 1) * gridSizeX];
            for (var j = 0; j < gridSizeX - 1; j++)
            {
                SetCoordsSeed((gridX + j) << 1, (gridZ + i) << 1);
                tmp[n] = upperLeft;
                tmp[n + zoomSizeX] = NextInt(2) > 0 ? upperLeft : lowerLeft;
                var upperRight = values[j + 1 + i * gridSizeX];
                var lowerRight = values[j + 1 + (i + 1) * gridSizeX];
                tmp[n + 1] = NextInt(2) > 0 ? upperLeft : upperRight;
                tmp[n + 1 + zoomSizeX] = Nearest(upperLeft, upperRight, lowerLeft, lowerRight);
                upperLeft = upperRight;
                lowerLeft = lowerRight;
                n += 2;
            }
        }
        var result = new int[sizeX * sizeZ];
        for (var i = 0; i < sizeZ; i++)
        for (var j = 0; j < sizeX; j++)
            result[j + i * sizeX] = tmp[j + (i + (z & 1)) * zoomSizeX + (x & 1)];
        return result;
    }

    private int Nearest(int ul, int ur, int ll, int lr)
    {
        if (!blurry)
        {
            if (ur == ll && ll == lr) return ur;
            if (ul == ur && ul == ll) return ul;
            if (ul == ur && ul == lr) return ul;
            if (ul == ll && ul == lr) return ul;
            if (ul == ur && ll != lr) return ul;
            if (ul == ll && ur != lr) return ul;
            if (ul == lr && ur != ll) return ul;
            if (ur == ll && ul != lr) return ur;
            if (ur == lr && ul != ll) return ur;
            if (ll == lr && ul != ur) return ll;
        }
        return NextInt(4) switch { 0 => ul, 1 => ur, 2 => ll, _ => lr };
    }
}

internal sealed class ErosionLayer(long seed, MapLayer below) : MapLayer(seed)
{
    public override int[] Generate(int x, int z, int sizeX, int sizeZ)
    {
        var w = sizeX + 2;
        var v = below.Generate(x - 1, z - 1, w, sizeZ + 2);
        var result = new int[sizeX * sizeZ];
        for (var i = 0; i < sizeZ; i++)
        for (var j = 0; j < sizeX; j++)
        {
            var ul = v[j + i * w];
            var ll = v[j + (i + 2) * w];
            var ur = v[j + 2 + i * w];
            var lr = v[j + 2 + (i + 2) * w];
            var center = Center(v, j, i, w);
            SetCoordsSeed(x + j, z + i);
            if (center != 0 && (ul == 0 || ur == 0 || ll == 0 || lr == 0))
                result[j + i * sizeX] = NextInt(5) == 0 ? 0 : center;
            else if (center == 0 && (ul != 0 || ur != 0 || ll != 0 || lr != 0))
                result[j + i * sizeX] = NextInt(3) == 0 ? ul : 0;
            else
                result[j + i * sizeX] = center;
        }
        return result;
    }
}

/// <summary>Ocean surrounded by ocean becomes deep ocean (or, rarely, a mushroom island).</summary>
internal sealed class DeepOceanLayer(long seed, MapLayer below) : MapLayer(seed)
{
    public override int[] Generate(int x, int z, int sizeX, int sizeZ)
    {
        var w = sizeX + 2;
        var v = below.Generate(x - 1, z - 1, w, sizeZ + 2);
        var result = new int[sizeX * sizeZ];
        for (var i = 0; i < sizeZ; i++)
        for (var j = 0; j < sizeX; j++)
        {
            var center = Center(v, j, i, w);
            if (center == 0 && Up(v, j, i, w) == 0 && Down(v, j, i, w) == 0 && Left(v, j, i, w) == 0 && Right(v, j, i, w) == 0)
            {
                SetCoordsSeed(x + j, z + i);
                center = NextInt(100) == 0 ? B.MushroomIsland : B.DeepOcean;
            }
            result[j + i * sizeX] = center;
        }
        return result;
    }
}

/// <summary>Climate groups to biomes, with the vanilla 1.12 biome tables.</summary>
internal sealed class BiomeLayer(long seed, MapLayer below) : MapLayer(seed)
{
    private static readonly int[] Warm = [B.Desert, B.Desert, B.Desert, B.Savanna, B.Savanna, B.Plains];
    private static readonly int[] Temperate = [B.Forest, B.RoofedForest, B.ExtremeHills, B.Plains, B.BirchForest, B.Swamp];
    private static readonly int[] Cool = [B.Forest, B.ExtremeHills, B.Taiga, B.Plains];
    private static readonly int[] Icy = [B.IcePlains, B.IcePlains, B.IcePlains, B.ColdTaiga];

    public override int[] Generate(int x, int z, int sizeX, int sizeZ)
    {
        var values = below.Generate(x, z, sizeX, sizeZ);
        for (var i = 0; i < sizeZ; i++)
        for (var j = 0; j < sizeX; j++)
        {
            var value = values[j + i * sizeX];
            if (value == 0 || value == B.DeepOcean || value == B.MushroomIsland)
                continue;
            SetCoordsSeed(x + j, z + i);
            values[j + i * sizeX] = value switch
            {
                2 => Warm[NextInt(Warm.Length)],
                1002 => NextInt(3) == 0 ? B.MesaPlateau : B.MesaPlateauStone,
                4 => Temperate[NextInt(Temperate.Length)],
                1004 => B.Jungle,
                1 => Cool[NextInt(Cool.Length)],
                1001 => B.MegaTaiga,
                _ => Icy[NextInt(Icy.Length)], // 3, 1003
            };
        }
        return values;
    }
}

/// <summary>Biome borders that vanilla smooths (mega taiga, desert/ice, swamp, mesa, mountains).</summary>
internal sealed class EdgeLayer(long seed, MapLayer below) : MapLayer(seed)
{
    public override int[] Generate(int x, int z, int sizeX, int sizeZ)
    {
        var w = sizeX + 2;
        var v = below.Generate(x - 1, z - 1, w, sizeZ + 2);
        var result = new int[sizeX * sizeZ];
        for (var i = 0; i < sizeZ; i++)
        for (var j = 0; j < sizeX; j++)
        {
            var center = Center(v, j, i, w);
            int[] around = [Up(v, j, i, w), Down(v, j, i, w), Left(v, j, i, w), Right(v, j, i, w)];
            result[j + i * sizeX] = center switch
            {
                B.ExtremeHills when around.Any(n => n is not (B.ExtremeHills or B.ExtremeHillsPlus)) => B.ExtremeHillsEdge,
                B.MesaPlateau or B.MesaPlateauStone when around.Any(n => !B.IsMesa(n) && !B.IsOcean(n)) => B.Mesa,
                B.MegaTaiga when around.Any(n => n is not (B.MegaTaiga or B.MegaTaigaHills)) => B.Taiga,
                B.Desert when around.Any(n => n is B.IcePlains or B.ColdTaiga) => B.ExtremeHillsPlus,
                B.Swamp when around.Any(n => n is B.Desert or B.ColdTaiga or B.IcePlains) => B.Plains,
                B.Swamp when around.Any(n => n is B.Jungle) => B.JungleEdge,
                _ => center,
            };
        }
        return result;
    }
}

/// <summary>Hills and mutated variants, driven by the "mountains" noise layer (vanilla GenLayerHills).</summary>
internal sealed class VariationLayer(long seed, MapLayer below, MapLayer? variation) : MapLayer(seed)
{
    private static readonly Dictionary<int, int[]> Hills = new()
    {
        [B.Desert] = [B.DesertHills],
        [B.Forest] = [B.ForestHills],
        [B.BirchForest] = [B.BirchForestHills],
        [B.RoofedForest] = [B.Plains],
        [B.Taiga] = [B.TaigaHills],
        [B.MegaTaiga] = [B.MegaTaigaHills],
        [B.ColdTaiga] = [B.ColdTaigaHills],
        [B.Plains] = [B.ForestHills, B.Forest, B.Forest],
        [B.IcePlains] = [B.IceMountains],
        [B.Jungle] = [B.JungleHills],
        [B.Ocean] = [B.DeepOcean],
        [B.ExtremeHills] = [B.ExtremeHillsPlus],
        [B.Savanna] = [B.SavannaPlateau],
        [B.MesaPlateau] = [B.Mesa],
        [B.MesaPlateauStone] = [B.Mesa],
    };

    private static readonly int[] Islands = [B.Plains, B.Forest];

    public override int[] Generate(int x, int z, int sizeX, int sizeZ) =>
        variation is null ? Random(x, z, sizeX, sizeZ) : Merge(x, z, sizeX, sizeZ);

    private int[] Random(int x, int z, int sizeX, int sizeZ)
    {
        var values = below.Generate(x, z, sizeX, sizeZ);
        for (var i = 0; i < sizeZ; i++)
        for (var j = 0; j < sizeX; j++)
        {
            if (values[j + i * sizeX] <= 0)
                continue;
            SetCoordsSeed(x + j, z + i);
            values[j + i * sizeX] = NextInt(30) + 2;
        }
        return values;
    }

    private int[] Merge(int x, int z, int sizeX, int sizeZ)
    {
        var w = sizeX + 2;
        var v = below.Generate(x - 1, z - 1, w, sizeZ + 2);
        var vary = variation!.Generate(x - 1, z - 1, w, sizeZ + 2);
        var result = new int[sizeX * sizeZ];
        for (var i = 0; i < sizeZ; i++)
        for (var j = 0; j < sizeX; j++)
        {
            SetCoordsSeed(x + j, z + i);
            var center = Center(v, j, i, w);
            var varyValue = Center(vary, j, i, w);
            if (center != 0 && varyValue == 3 && center < 128)
            {
                result[j + i * sizeX] = B.Exists(center + 128) ? center + 128 : center;
            }
            else if (varyValue == 2 || NextInt(3) == 0)
            {
                var value = center;
                if (Hills.TryGetValue(center, out var hills))
                    value = hills[NextInt(hills.Length)];
                else if (center == B.DeepOcean && NextInt(3) == 0)
                    value = Islands[NextInt(Islands.Length)];
                if (varyValue == 2 && value != center)
                    value = B.Exists(value + 128) ? value + 128 : center;
                if (value != center)
                {
                    var same = (Up(v, j, i, w) == center ? 1 : 0) + (Down(v, j, i, w) == center ? 1 : 0)
                               + (Left(v, j, i, w) == center ? 1 : 0) + (Right(v, j, i, w) == center ? 1 : 0);
                    result[j + i * sizeX] = same < 3 ? center : value;
                }
                else
                    result[j + i * sizeX] = value;
            }
            else
                result[j + i * sizeX] = center;
        }
        return result;
    }
}

internal sealed class RarePlainsLayer(long seed, MapLayer below) : MapLayer(seed)
{
    public override int[] Generate(int x, int z, int sizeX, int sizeZ)
    {
        var w = sizeX + 2;
        var v = below.Generate(x - 1, z - 1, w, sizeZ + 2);
        var result = new int[sizeX * sizeZ];
        for (var i = 0; i < sizeZ; i++)
        for (var j = 0; j < sizeX; j++)
        {
            SetCoordsSeed(x + j, z + i);
            var center = Center(v, j, i, w);
            result[j + i * sizeX] = NextInt(57) == 0 && center == B.Plains ? B.SunflowerPlains : center;
        }
        return result;
    }
}

/// <summary>Jungle edges and mesa borders (vanilla GenLayerBiomeEdge thin pass).</summary>
internal sealed class ThinEdgeLayer(long seed, MapLayer below) : MapLayer(seed)
{
    public override int[] Generate(int x, int z, int sizeX, int sizeZ)
    {
        var w = sizeX + 2;
        var v = below.Generate(x - 1, z - 1, w, sizeZ + 2);
        var result = new int[sizeX * sizeZ];
        for (var i = 0; i < sizeZ; i++)
        for (var j = 0; j < sizeX; j++)
        {
            var center = Center(v, j, i, w);
            int[] around = [Up(v, j, i, w), Down(v, j, i, w), Left(v, j, i, w), Right(v, j, i, w)];
            result[j + i * sizeX] = center switch
            {
                B.Jungle or B.JungleHills when around.Any(n => !B.IsJungle(n) && !B.IsOcean(n)
                                                             && n is not (B.Forest or B.Taiga)) => B.JungleEdge,
                _ when B.IsMesa(center) && around.Any(n => !B.IsMesa(n) && !B.IsOcean(n)) => B.Desert,
                _ => center,
            };
        }
        return result;
    }
}

/// <summary>Beaches between land and ocean (stone beaches below mountains, cold beaches in snowy biomes).</summary>
internal sealed class ShoreLayer(long seed, MapLayer below) : MapLayer(seed)
{
    public override int[] Generate(int x, int z, int sizeX, int sizeZ)
    {
        var w = sizeX + 2;
        var v = below.Generate(x - 1, z - 1, w, sizeZ + 2);
        var result = new int[sizeX * sizeZ];
        for (var i = 0; i < sizeZ; i++)
        for (var j = 0; j < sizeX; j++)
        {
            var center = Center(v, j, i, w);
            var nearOcean = B.IsOcean(Up(v, j, i, w)) || B.IsOcean(Down(v, j, i, w))
                            || B.IsOcean(Left(v, j, i, w)) || B.IsOcean(Right(v, j, i, w));
            result[j + i * sizeX] = B.IsOcean(center) || !nearOcean ? center : center switch
            {
                B.MushroomIsland => B.MushroomShore,
                B.ExtremeHills or B.ExtremeHillsPlus or B.ExtremeHillsEdge => B.StoneBeach,
                B.Swamp or B.River => center,
                _ when B.IsMesa(center) => center,
                _ when B.IsSnowy(center) => B.ColdBeach,
                _ => B.Beach,
            };
        }
        return result;
    }
}

internal sealed class RiverLayer(long seed, MapLayer below, MapLayer? merge) : MapLayer(seed)
{
    public override int[] Generate(int x, int z, int sizeX, int sizeZ) =>
        merge is null ? Rivers(x, z, sizeX, sizeZ) : Merge(x, z, sizeX, sizeZ);

    private int[] Rivers(int x, int z, int sizeX, int sizeZ)
    {
        var w = sizeX + 2;
        var v = below.Generate(x - 1, z - 1, w, sizeZ + 2);
        var result = new int[sizeX * sizeZ];
        for (var i = 0; i < sizeZ; i++)
        for (var j = 0; j < sizeX; j++)
        {
            var center = Center(v, j, i, w) & 1;
            result[j + i * sizeX] = center != (Up(v, j, i, w) & 1) || center != (Down(v, j, i, w) & 1)
                                    || center != (Left(v, j, i, w) & 1) || center != (Right(v, j, i, w) & 1) ? 1 : 0;
        }
        return result;
    }

    private int[] Merge(int x, int z, int sizeX, int sizeZ)
    {
        var rivers = below.Generate(x, z, sizeX, sizeZ);
        var biomes = merge!.Generate(x, z, sizeX, sizeZ);
        for (var i = 0; i < biomes.Length; i++)
        {
            if (B.IsOcean(biomes[i]) || rivers[i] != 1)
                continue;
            biomes[i] = biomes[i] switch
            {
                _ when B.IsSnowy(biomes[i]) => B.FrozenRiver,
                B.MushroomIsland or B.MushroomShore => B.MushroomShore,
                _ => B.River,
            };
        }
        return biomes;
    }
}

internal sealed class SmoothLayer(long seed, MapLayer below) : MapLayer(seed)
{
    public override int[] Generate(int x, int z, int sizeX, int sizeZ)
    {
        var w = sizeX + 2;
        var v = below.Generate(x - 1, z - 1, w, sizeZ + 2);
        var result = new int[sizeX * sizeZ];
        for (var i = 0; i < sizeZ; i++)
        for (var j = 0; j < sizeX; j++)
        {
            int up = Up(v, j, i, w), down = Down(v, j, i, w), left = Left(v, j, i, w), right = Right(v, j, i, w);
            var center = Center(v, j, i, w);
            if (up == down && left == right)
            {
                SetCoordsSeed(x + j, z + i);
                center = NextInt(2) == 0 ? up : left;
            }
            else if (up == down)
                center = up;
            else if (left == right)
                center = left;
            result[j + i * sizeX] = center;
        }
        return result;
    }
}
