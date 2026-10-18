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

using System.Diagnostics;
using Bedrokia.Server.Game;

namespace Bedrokia.Tests;

public class BiomeGenerationTests
{
    private static readonly WorldGenerator Generator = new(20261018);

    [Fact]
    public void Biome_layout_is_varied_and_only_uses_known_biomes()
    {
        var known = VanillaBiomes.All.Select(b => b.Id).ToHashSet();
        var seen = new HashSet<int>();
        for (var x = -4096; x < 4096; x += 256)
        for (var z = -4096; z < 4096; z += 256)
            foreach (var id in Generator.GetBiomes(x, z, 4, 4))
            {
                Assert.Contains(id, known);
                seen.Add(id);
            }
        Assert.True(seen.Count >= 10, $"only {seen.Count} biomes: {string.Join(",", seen)}");
    }

    [Fact]
    public void Spawn_is_on_dry_land()
    {
        var (x, y, z) = Generator.FindSpawn();
        Assert.True(y > WorldGenerator.SeaLevel + 1);
        var chunk = Generator.Generate(x >> 4, z >> 4);
        Assert.NotSame(Blocks.Water, chunk.GetBlock(x & 15, y - 1, z & 15));
    }

    [Fact]
    public void Deserts_are_sand_and_forests_have_trees()
    {
        var desert = Find(2);
        var forest = Find(4);
        var desertChunk = Generator.Generate(desert.X >> 4, desert.Z >> 4);
        var top = desertChunk.GetBlock(desert.X & 15, desertChunk.Height(desert.X & 15, desert.Z & 15), desert.Z & 15);
        Assert.True(top == Blocks.Sand || top.Name is "minecraft:cactus" or "minecraft:deadbush" or "minecraft:reeds", top.Name);

        var logs = 0;
        for (var dx = -1; dx <= 1; dx++)
        for (var dz = -1; dz <= 1; dz++)
        {
            var chunk = Generator.Generate((forest.X >> 4) + dx, (forest.Z >> 4) + dz);
            for (var x = 0; x < 16; x++)
            for (var z = 0; z < 16; z++)
            for (var y = 60; y < 140; y++)
                if (chunk.GetBlock(x, y, z).Name.EndsWith("_log"))
                    logs++;
        }
        Assert.True(logs > 10, $"only {logs} logs around a forest");
    }

    [Fact]
    public void Trees_crossing_a_chunk_border_match_on_both_sides()
    {
        // Generating the same chunk with a fresh generator (empty caches) must give the same blocks.
        var forest = Find(4);
        var (cx, cz) = (forest.X >> 4, forest.Z >> 4);
        var a = Generator.Generate(cx, cz).Serialize();
        var b = new WorldGenerator(20261018).Generate(cx, cz).Serialize();
        Assert.Equal(a, b);
    }

    [Fact]
    public void Chunks_generate_quickly()
    {
        var generator = new WorldGenerator(7);
        generator.Generate(100, 100); // warm up
        var watch = Stopwatch.StartNew();
        for (var i = 0; i < 20; i++)
            generator.Generate(100 + i, 200);
        Assert.True(watch.ElapsedMilliseconds / 20 < 250, $"{watch.ElapsedMilliseconds / 20} ms per chunk");
    }

    private static (int X, int Z) Find(int biome)
    {
        for (var r = 0; r < 20000; r += 64)
        for (var a = 0; a < 16; a++)
        {
            var x = (int)(r * Math.Cos(a * Math.PI / 8));
            var z = (int)(r * Math.Sin(a * Math.PI / 8));
            if (Generator.GetBiomes(x, z, 1, 1)[0] == biome && Generator.GetBiomes(x + 32, z + 32, 1, 1)[0] == biome)
                return (x, z);
        }
        throw new InvalidOperationException($"biome {biome} not found");
    }
}
