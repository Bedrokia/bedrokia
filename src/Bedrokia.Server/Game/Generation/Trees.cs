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

namespace Bedrokia.Server.Game.Generation;

/// <summary>
/// Vanilla-shaped trees per biome: oak, birch, spruce, mega spruce, jungle (tall, 2x2 and bushes), acacia, dark oak
/// and swamp oak. Shapes depend only on the trunk position, so a tree crossing chunks is identical on both sides.
/// </summary>
/// <remarks>ponytail: shapes follow the vanilla proportions; no vines, cocoa or branches on big trees yet.</remarks>
internal static class Trees
{
    private static readonly (BlockType Log, BlockType Leaves) Oak = Kind("oak"), Birch = Kind("birch"), Spruce = Kind("spruce"),
        Jungle = Kind("jungle"), Acacia = Kind("acacia"), DarkOak = Kind("dark_oak");

    private static (BlockType, BlockType) Kind(string wood) =>
        (Blocks.Get(wood + "_log", ("pillar_axis", "y")), WorldGenerator.Req(wood + "_leaves"));

    public static void Place(ChunkWriter w, int x, int y, int z, int biome, double roll, double size)
    {
        if (y > Chunk.MaxY - 32)
            return;
        var random = new JavaRandom(x * 3129871L ^ z * 116129781L ^ y);
        switch (biome)
        {
            case B.BirchForest or B.BirchForestHills:
                Blob(w, random, x, y, z, Birch, 5 + random.NextInt(3));
                break;
            case 155 or 156: // tall birch forest
                Blob(w, random, x, y, z, Birch, 7 + random.NextInt(5));
                break;
            case B.RoofedForest or 157:
                if (roll < 0.75)
                    DarkOakTree(w, random, x, y, z);
                else
                    Blob(w, random, x, y, z, roll < 0.9 ? Oak : Birch, 4 + random.NextInt(3));
                break;
            case B.MegaTaiga or B.MegaTaigaHills or 160 or 161:
                if (size < 0.35)
                    MegaSpruce(w, random, x, y, z);
                else
                    SpruceTree(w, random, x, y, z);
                break;
            case B.Taiga or B.TaigaHills or B.ColdTaiga or B.ColdTaigaHills or 133 or 158 or B.IcePlains or B.IceMountains
                or B.ExtremeHills or B.ExtremeHillsPlus or B.ExtremeHillsEdge or 131 or 162:
                if (biome is B.ExtremeHills or B.ExtremeHillsPlus or B.ExtremeHillsEdge or 131 or 162 && size < 0.3)
                    Blob(w, random, x, y, z, Oak, 4 + random.NextInt(3));
                else
                    SpruceTree(w, random, x, y, z);
                break;
            case B.Jungle or B.JungleHills or 149 or B.JungleEdge or 151:
                if (size < 0.1 && biome is not (B.JungleEdge or 151))
                    MegaJungle(w, random, x, y, z);
                else if (size < 0.5)
                    Bush(w, random, x, y, z);
                else
                    Blob(w, random, x, y, z, Jungle, 6 + random.NextInt(7));
                break;
            case B.Savanna or B.SavannaPlateau or 163 or 164:
                if (size < 0.8)
                    AcaciaTree(w, random, x, y, z);
                else
                    Blob(w, random, x, y, z, Oak, 4 + random.NextInt(3));
                break;
            case B.Swamp or 134:
                SwampOak(w, random, x, y, z);
                break;
            default:
                Blob(w, random, x, y, z, size < 0.2 && biome is not (B.Plains or B.SunflowerPlains) ? Birch : Oak, 4 + random.NextInt(3));
                break;
        }
    }

    // --- Shapes ---

    /// <summary>Vanilla small tree (WorldGenTrees): trunk plus a 4-layer leaf blob with random corners.</summary>
    private static void Blob(ChunkWriter w, JavaRandom random, int x, int y, int z, (BlockType Log, BlockType Leaves) kind, int height)
    {
        for (var ly = y + height - 3; ly <= y + height; ly++)
        {
            var dy = ly - (y + height);
            var radius = 1 - dy / 2;
            for (var lx = x - radius; lx <= x + radius; lx++)
            for (var lz = z - radius; lz <= z + radius; lz++)
            {
                var corner = Math.Abs(lx - x) == radius && Math.Abs(lz - z) == radius;
                if (corner && (random.NextInt(2) == 0 || dy == 0))
                    continue;
                Leaf(w, lx, ly, lz, kind.Leaves);
            }
        }
        Trunk(w, x, y, z, kind.Log, height);
    }

    private static void SwampOak(ChunkWriter w, JavaRandom random, int x, int y, int z)
    {
        var height = 5 + random.NextInt(4);
        for (var ly = y + height - 3; ly <= y + height; ly++)
        {
            var radius = ly >= y + height - 1 ? 2 : 3;
            for (var lx = x - radius; lx <= x + radius; lx++)
            for (var lz = z - radius; lz <= z + radius; lz++)
                if (Math.Abs(lx - x) + Math.Abs(lz - z) <= radius + 1 && random.NextInt(5) > 0)
                    Leaf(w, lx, ly, lz, Oak.Leaves);
        }
        Trunk(w, x, y, z, Oak.Log, height);
    }

    /// <summary>Spruce (WorldGenTaiga2): narrow cone with alternating ring radii.</summary>
    private static void SpruceTree(ChunkWriter w, JavaRandom random, int x, int y, int z)
    {
        var height = 6 + random.NextInt(4);
        var bare = 1 + random.NextInt(2);
        var maxRadius = 2 + random.NextInt(2);
        var radius = random.NextInt(2);
        var step = 1;
        for (var ly = y + height; ly >= y + bare; ly--)
        {
            for (var lx = x - radius; lx <= x + radius; lx++)
            for (var lz = z - radius; lz <= z + radius; lz++)
                if (radius == 0 || Math.Abs(lx - x) != radius || Math.Abs(lz - z) != radius)
                    Leaf(w, lx, ly, lz, Spruce.Leaves);
            if (radius >= step)
            {
                radius = 1;
                step = Math.Min(step + 1, maxRadius);
            }
            else
                radius++;
        }
        Leaf(w, x, y + height + 1, z, Spruce.Leaves);
        Trunk(w, x, y, z, Spruce.Log, height);
    }

    /// <summary>Mega spruce: 2x2 trunk with a long tapering crown.</summary>
    private static void MegaSpruce(ChunkWriter w, JavaRandom random, int x, int y, int z)
    {
        var height = 15 + random.NextInt(12);
        var crown = height - 4 - random.NextInt(4);
        for (var ly = y + height - crown; ly <= y + height + 1; ly++)
        {
            var fromTop = y + height + 1 - ly;
            var radius = Math.Min(4, fromTop / 3 + (fromTop % 3 == 0 ? 0 : 1));
            for (var lx = x - radius; lx <= x + 1 + radius; lx++)
            for (var lz = z - radius; lz <= z + 1 + radius; lz++)
            {
                double dx = lx - (x + 0.5), dz = lz - (z + 0.5);
                if (dx * dx + dz * dz <= (radius + 0.5) * (radius + 0.5))
                    Leaf(w, lx, ly, lz, Spruce.Leaves);
            }
        }
        for (var dx = 0; dx <= 1; dx++)
        for (var dz = 0; dz <= 1; dz++)
            Trunk(w, x + dx, y, z + dz, Spruce.Log, height);
    }

    /// <summary>Mega jungle tree: 2x2 trunk, big round crown and a few side leaf clusters.</summary>
    private static void MegaJungle(ChunkWriter w, JavaRandom random, int x, int y, int z)
    {
        var height = 18 + random.NextInt(12);
        Sphere(w, x, y + height, z, 4, 2, Jungle.Leaves, random);
        for (var i = 0; i < 3; i++)
        {
            var ly = y + height / 2 + random.NextInt(height / 3);
            var dir = random.NextInt(4);
            int ox = dir switch { 0 => 3, 1 => -2, _ => 0 }, oz = dir switch { 2 => 3, 3 => -2, _ => 0 };
            Sphere(w, x + ox, ly, z + oz, 2, 1, Jungle.Leaves, random);
            w.Set(x + ox / 2, ly - 1, z + oz / 2, Jungle.Log);
        }
        for (var dx = 0; dx <= 1; dx++)
        for (var dz = 0; dz <= 1; dz++)
            Trunk(w, x + dx, y, z + dz, Jungle.Log, height);
    }

    /// <summary>Jungle bush: one log with a small leaf mound (WorldGenShrub).</summary>
    private static void Bush(ChunkWriter w, JavaRandom random, int x, int y, int z)
    {
        for (var ly = y; ly <= y + 2; ly++)
        {
            var radius = 2 - (ly - y);
            for (var lx = x - radius; lx <= x + radius; lx++)
            for (var lz = z - radius; lz <= z + radius; lz++)
                if (Math.Abs(lx - x) != radius || Math.Abs(lz - z) != radius || random.NextInt(2) == 0)
                    Leaf(w, lx, ly, lz, Jungle.Leaves);
        }
        w.Set(x, y, z, Jungle.Log);
        w.Set(x, y - 1, z, Blocks.Dirt);
    }

    /// <summary>Acacia (WorldGenSavannaTree): leaning trunk and flat canopies.</summary>
    private static void AcaciaTree(ChunkWriter w, JavaRandom random, int x, int y, int z)
    {
        var height = 5 + random.NextInt(3);
        var bendAt = height - random.NextInt(4) - 1;
        var bend = 3 - random.NextInt(3);
        var dir = random.NextInt(4);
        int dx = dir switch { 0 => 1, 1 => -1, _ => 0 }, dz = dir switch { 2 => 1, 3 => -1, _ => 0 };
        int tx = x, tz = z, top = y;
        for (var i = 0; i < height; i++)
        {
            if (i >= bendAt && bend > 0)
            {
                tx += dx;
                tz += dz;
                bend--;
            }
            Log(w, tx, y + i, tz, Acacia.Log);
            top = y + i;
        }
        Canopy(w, tx, top + 1, tz, 3, 2);
        w.Set(x, y - 1, z, Blocks.Dirt);
    }

    private static void Canopy(ChunkWriter w, int x, int y, int z, int bottom, int upper)
    {
        for (var lx = -bottom; lx <= bottom; lx++)
        for (var lz = -bottom; lz <= bottom; lz++)
            if (Math.Abs(lx) + Math.Abs(lz) <= bottom + 1 && !(Math.Abs(lx) == bottom && Math.Abs(lz) == bottom))
                Leaf(w, x + lx, y - 1, z + lz, Acacia.Leaves);
        for (var lx = -upper; lx <= upper; lx++)
        for (var lz = -upper; lz <= upper; lz++)
            if (Math.Abs(lx) + Math.Abs(lz) <= upper + 1)
                Leaf(w, x + lx, y, z + lz, Acacia.Leaves);
    }

    /// <summary>Dark oak (WorldGenCanopyTree): 2x2 trunk and a wide flat crown.</summary>
    private static void DarkOakTree(ChunkWriter w, JavaRandom random, int x, int y, int z)
    {
        var height = 6 + random.NextInt(3);
        for (var ly = y + height - 2; ly <= y + height + 1; ly++)
        {
            var radius = ly <= y + height ? 3 : 2;
            for (var lx = x - radius; lx <= x + 1 + radius; lx++)
            for (var lz = z - radius; lz <= z + 1 + radius; lz++)
            {
                var edgeX = lx == x - radius || lx == x + 1 + radius;
                var edgeZ = lz == z - radius || lz == z + 1 + radius;
                if (edgeX && edgeZ || (edgeX || edgeZ) && random.NextInt(3) == 0)
                    continue;
                Leaf(w, lx, ly, lz, DarkOak.Leaves);
            }
        }
        for (var dx = 0; dx <= 1; dx++)
        for (var dz = 0; dz <= 1; dz++)
            Trunk(w, x + dx, y, z + dz, DarkOak.Log, height);
    }

    private static void Sphere(ChunkWriter w, int x, int y, int z, int radius, int height, BlockType leaves, JavaRandom random)
    {
        for (var ly = y - height; ly <= y + 1; ly++)
        {
            var r = ly > y ? radius - 2 : radius - (y - ly) / 2;
            for (var lx = x - r; lx <= x + 1 + r; lx++)
            for (var lz = z - r; lz <= z + 1 + r; lz++)
            {
                double dx = lx - (x + 0.5), dz = lz - (z + 0.5);
                if (dx * dx + dz * dz <= r * r + random.NextInt(2))
                    Leaf(w, lx, ly, lz, leaves);
            }
        }
    }

    // --- Block placement ---

    private static void Trunk(ChunkWriter w, int x, int y, int z, BlockType log, int height)
    {
        for (var ly = y; ly < y + height; ly++)
            Log(w, x, ly, z, log);
        if (w.Get(x, y - 1, z) is { } below && below.Name is "minecraft:grass_block" or "minecraft:podzol" or "minecraft:coarse_dirt" or "minecraft:snow")
            w.Set(x, y - 1, z, Blocks.Dirt);
    }

    private static void Log(ChunkWriter w, int x, int y, int z, BlockType log)
    {
        if (w.Get(x, y, z) is { } current && (current.IsAir || Replaceable(current) || current.Name.EndsWith("_leaves")))
            w.Set(x, y, z, log);
    }

    private static void Leaf(ChunkWriter w, int x, int y, int z, BlockType leaves)
    {
        if (w.Get(x, y, z) is { } current && (current.IsAir || Replaceable(current)))
            w.Set(x, y, z, leaves);
    }

    private static bool Replaceable(BlockType block) =>
        block.Name is "minecraft:short_grass" or "minecraft:fern" or "minecraft:snow_layer" or "minecraft:tall_grass"
            or "minecraft:large_fern" or "minecraft:deadbush" or "minecraft:dandelion" or "minecraft:poppy";
}
