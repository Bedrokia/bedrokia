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

using Bedrokia.Server.Core;

namespace Bedrokia.Server.Game;

/// <summary>What a world generator must provide.</summary>
public interface IWorldGenerator
{
    long Seed { get; }

    Chunk Generate(int chunkX, int chunkZ);

    /// <summary>Where new players appear in a new world.</summary>
    (int X, int Y, int Z) FindSpawn();
}

/// <summary>
/// Per-world settings, in worlds/&lt;name&gt;/world.properties (created with the defaults; edit it while the server is
/// stopped): the generator type and its options, and the day/night cycle.
/// </summary>
public sealed class WorldSettings
{
    /// <summary>normal (vanilla terrain and biomes), flat (layers below) or void (empty, one block under the spawn).</summary>
    public string Generator { get; set; } = "normal";

    /// <summary>Flat world layers from the bottom up, like "bedrock,2*dirt,grass_block".</summary>
    public string FlatLayers { get; set; } = "bedrock,2*dirt,grass_block";

    /// <summary>Biome of flat and void worlds (legacy biome name, e.g. plains, desert, ice_plains).</summary>
    public string FlatBiome { get; set; } = "plains";

    public bool DaylightCycle { get; set; } = true;

    public const string FileName = "world.properties";

    public static WorldSettings LoadOrCreate(string directory)
    {
        var path = Path.Combine(directory, FileName);
        var settings = new WorldSettings();
        if (!File.Exists(path))
        {
            settings.Save(directory);
            return settings;
        }
        var lineNumber = 0;
        foreach (var raw in File.ReadAllLines(path))
        {
            lineNumber++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            var separator = line.IndexOf('=');
            if (separator <= 0)
                throw new InvalidDataException($"{path}:{lineNumber}: expected key=value.");
            var value = line[(separator + 1)..].Trim();
            switch (line[..separator].Trim().ToLowerInvariant())
            {
                case "generator":
                    settings.Generator = value.ToLowerInvariant() is "normal" or "flat" or "void" or "nether"
                        ? value.ToLowerInvariant()
                        : throw new InvalidDataException($"{path}:{lineNumber}: generator must be normal, flat, void or nether.");
                    break;
                case "flat-layers":
                    settings.FlatLayers = value;
                    break;
                case "flat-biome":
                    settings.FlatBiome = value;
                    break;
                case "daylight-cycle":
                    settings.DaylightCycle = value.ToLowerInvariant() is "on" or "true" or "yes" or "1";
                    break;
                default:
                    throw new InvalidDataException($"{path}:{lineNumber}: unknown setting '{line[..separator].Trim()}'.");
            }
        }
        _ = settings.CreateGenerator(0); // validates the layers and biome now rather than at first chunk
        return settings;
    }

    public void Save(string directory)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllLines(Path.Combine(directory, FileName),
        [
            $"# {Product.Name} world settings - {Product.CreatedBy}",
            "# Changing the generator only affects chunks that were never generated.",
            "# normal (vanilla terrain and biomes), flat, void or nether (the Nether dimension)",
            $"generator={Generator}",
            "# flat worlds: layers from the bottom up, count*block (e.g. bedrock,3*stone,2*dirt,grass_block)",
            $"flat-layers={FlatLayers}",
            "# flat and void worlds: biome (plains, desert, ice_plains, mushroom_island...)",
            $"flat-biome={FlatBiome}",
            "# on/off: time passes (off = time stays where it is)",
            $"daylight-cycle={(DaylightCycle ? "on" : "off")}",
        ]);
    }

    /// <exception cref="InvalidDataException">Unknown block or biome in the flat settings.</exception>
    public IWorldGenerator CreateGenerator(long seed) => Generator switch
    {
        "flat" => new FlatGenerator(seed, ParseLayers(FlatLayers), BiomeId(FlatBiome)),
        "void" => new FlatGenerator(seed, [], BiomeId(FlatBiome)),
        "nether" => new Generation.NetherGenerator(seed),
        _ => new WorldGenerator(seed),
    };

    private static List<BlockType> ParseLayers(string layers)
    {
        var result = new List<BlockType>();
        foreach (var part in layers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var star = part.IndexOf('*');
            var count = star > 0 && int.TryParse(part[..star], out var n) && n > 0 ? n : 1;
            var name = star > 0 ? part[(star + 1)..] : part;
            var block = Blocks.ByName(name) ?? throw new InvalidDataException($"flat-layers: unknown block '{name}'.");
            result.AddRange(Enumerable.Repeat(block, count));
        }
        if (result.Count > Chunk.MaxY - Chunk.MinY)
            throw new InvalidDataException("flat-layers: too many layers.");
        return result;
    }

    private static byte BiomeId(string name) =>
        (byte)(VanillaBiomes.All.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Id
               ?? throw new InvalidDataException($"flat-biome: unknown biome '{name}'."));
}

/// <summary>Flat world (layers from y -64 up) or, with no layers, an empty void world with one block under the spawn.</summary>
public sealed class FlatGenerator(long seed, IReadOnlyList<BlockType> layers, byte biome) : IWorldGenerator
{
    public long Seed => seed;

    private int SurfaceY => Chunk.MinY + layers.Count; // first air block

    public Chunk Generate(int chunkX, int chunkZ)
    {
        var chunk = new Chunk(chunkX, chunkZ);
        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
        {
            chunk.SetBiome(x, z, biome);
            for (var i = 0; i < layers.Count; i++)
                chunk.SetBlock(x, Chunk.MinY + i, z, layers[i]);
        }
        if (layers.Count == 0 && chunkX == 0 && chunkZ == 0)
            chunk.SetBlock(0, 63, 0, Blocks.Stone); // void: something to stand on
        return chunk;
    }

    public (int X, int Y, int Z) FindSpawn() => layers.Count == 0 ? (0, 64, 0) : (0, SurfaceY, 0);
}
