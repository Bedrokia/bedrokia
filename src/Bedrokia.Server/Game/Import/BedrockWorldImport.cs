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

using System.Buffers.Binary;
using System.IO.Compression;
using System.Text.Json;
using Bedrokia.Protocol;
using Bedrokia.Server.Core;

namespace Bedrokia.Server.Game.Import;

/// <summary>
/// Imports vanilla Bedrock worlds (maps, worlds from a client or BDS): a "name.mcworld" file or a world folder with
/// db/ and level.dat, put in worlds/. The overworld's blocks and biomes are converted into the server's chunk files
/// once, at startup; the original files are kept. Chunk storage layout from the public Bedrock format docs
/// (sub-chunk versions 8 and 9, little-endian NBT block palettes, 3D biome palettes).
/// </summary>
/// <remarks>ponytail: the Nether and End, block entities (chest contents, signs) and entities are not imported.</remarks>
public static class BedrockWorldImport
{
    private const byte TagData3D = 43, TagData2D = 45, TagSubChunk = 47;

    /// <summary>Unpacks .mcworld files and converts every Bedrock world folder of <paramref name="worldsDirectory"/>.</summary>
    public static void ImportAll(string worldsDirectory, Logger logger)
    {
        if (!Directory.Exists(worldsDirectory))
            return;
        foreach (var archive in Directory.GetFiles(worldsDirectory, "*.mcworld"))
        {
            var target = Path.Combine(worldsDirectory, Path.GetFileNameWithoutExtension(archive));
            if (Directory.Exists(target))
                continue;
            logger.Info($"Unpacking {Path.GetFileName(archive)}...");
            ZipFile.ExtractToDirectory(archive, target);
            File.Move(archive, archive + ".imported");
        }
        foreach (var folder in Directory.GetDirectories(worldsDirectory))
        {
            if (!Directory.Exists(Path.Combine(folder, "db")) || File.Exists(Path.Combine(folder, "level.json")))
                continue;
            try
            {
                var chunks = Convert(folder, logger);
                logger.Info($"Imported Bedrock world '{Path.GetFileName(folder)}': {chunks} chunks. Set level-name={Path.GetFileName(folder)} to play it.");
            }
            catch (Exception e) when (e is IOException or InvalidDataException or IndexOutOfRangeException or ArgumentException)
            {
                logger.Error($"Could not import the Bedrock world in {folder}: {e.Message}");
            }
        }
    }

    /// <summary>Converts the world in <paramref name="folder"/>; returns the number of chunks written.</summary>
    public static int Convert(string folder, Logger? logger = null)
    {
        var entries = LevelDb.ReadAll(Path.Combine(folder, "db"));
        var chunkFolder = Path.Combine(folder, "chunks");
        Directory.CreateDirectory(chunkFolder);

        // Overworld chunk keys: x (4), z (4), tag (1) [, sub-chunk y (1)]; other dimensions add 4 bytes.
        var columns = new Dictionary<(int X, int Z), List<(byte Tag, sbyte Sub, byte[] Value)>>();
        foreach (var (key, value) in entries)
        {
            if (key.Length is not (9 or 10))
                continue;
            var tag = key[8];
            if (tag is not (TagSubChunk or TagData3D or TagData2D))
                continue;
            var at = (BinaryPrimitives.ReadInt32LittleEndian(key), BinaryPrimitives.ReadInt32LittleEndian(key.AsSpan(4)));
            if (!columns.TryGetValue(at, out var list))
                columns[at] = list = [];
            list.Add((tag, key.Length == 10 ? (sbyte)key[9] : (sbyte)0, value));
        }

        var unknown = new HashSet<string>();
        foreach (var ((x, z), parts) in columns)
        {
            var chunk = new Chunk(x, z);
            foreach (var (tag, sub, value) in parts.Where(p => p.Tag == TagSubChunk))
                SubChunk(chunk, sub, value, unknown);
            if (parts.FirstOrDefault(p => p.Tag == TagData3D).Value is { } data3D)
                Biomes3D(chunk, data3D);
            else if (parts.FirstOrDefault(p => p.Tag == TagData2D).Value is { Length: >= 768 } data2D)
                for (var bx = 0; bx < 16; bx++)
                for (var bz = 0; bz < 16; bz++)
                    chunk.SetBiome(bx, bz, Biome(data2D[512 + (bz << 4 | bx)]));
            File.WriteAllBytes(Path.Combine(chunkFolder, $"{x}.{z}.nxc"), chunk.Serialize());
        }
        if (unknown.Count > 0)
            logger?.Warn($"{unknown.Count} block states unknown to this version were replaced by their default or air: {string.Join(", ", unknown.Take(10))}");

        WriteLevel(folder, columns.Keys.ToList());
        // Beyond the map's edges: nothing (maps are islands). Switch to generator=normal in world.properties for terrain.
        new WorldSettings { Generator = "void" }.Save(folder);
        return columns.Count;
    }

    private static void SubChunk(Chunk chunk, sbyte keyY, byte[] data, HashSet<string> unknown)
    {
        var reader = new BedrockReader(data);
        var version = reader.Byte();
        if (version is not (8 or 9))
            return; // ponytail: pre-1.2.13 sub-chunk formats are not converted
        var layers = reader.Byte();
        var sectionY = version == 9 ? (sbyte)reader.Byte() : keyY;
        var baseY = sectionY * 16;
        for (var layer = 0; layer < layers; layer++)
        {
            var (indices, palette) = Storage(ref reader, unknown);
            for (var i = 0; i < 4096; i++)
            {
                var block = palette[indices?[i] ?? 0];
                int bx = i >> 8 & 15, bz = i >> 4 & 15, by = baseY + (i & 15);
                if (layer == 0)
                    chunk.SetBlock(bx, by, bz, block);
                else if (block == Blocks.Water && chunk.GetBlock(bx, by, bz).IsAir)
                    chunk.SetBlock(bx, by, bz, block); // water-logging layer: keep the water where the block is gone
            }
        }
    }

    // Persistent paletted storage: header (bits << 1 | 0), packed indices, palette count, palette NBT.
    private static (int[]? Indices, BlockType[] Palette) Storage(ref BedrockReader reader, HashSet<string> unknown)
    {
        var bits = reader.Byte() >> 1;
        var indices = bits == 0 ? null : Unpack(ref reader, bits);
        var count = BinaryPrimitives.ReadInt32LittleEndian(reader.Bytes(4));
        var palette = new BlockType[count];
        for (var i = 0; i < count; i++)
        {
            var nbt = Nbt.ReadRoot(ref reader, network: false);
            var name = nbt.TryGetValue("name", out var n) ? (string)n : "minecraft:air";
            var states = nbt.TryGetValue("states", out var s) && s is OrderedDictionary<string, object> st ? st : [];
            var key = new BlockType(0, name, states).Key;
            palette[i] = Blocks.ByKey(key) ?? Blocks.ByName(name) ?? Blocks.Air;
            if (Blocks.ByKey(key) is null)
                unknown.Add(key);
        }
        return (indices, palette);
    }

    private static int[] Unpack(ref BedrockReader reader, int bits)
    {
        var perWord = 32 / bits;
        var words = (4096 + perWord - 1) / perWord;
        var raw = reader.Bytes(words * 4);
        var indices = new int[4096];
        var mask = (1 << bits) - 1;
        for (var i = 0; i < 4096; i++)
        {
            var word = BinaryPrimitives.ReadUInt32LittleEndian(raw[(i / perWord * 4)..]);
            indices[i] = (int)(word >> (i % perWord * bits)) & mask;
        }
        return indices;
    }

    // Data3D: heightmap (512 bytes), then one biome storage per sub-chunk from y -64 (0xFF = same as previous).
    // The column's biome is the one at y 64, where the surface usually is.
    private static void Biomes3D(Chunk chunk, byte[] data)
    {
        var reader = new BedrockReader(data);
        reader.Bytes(512);
        int[]? indices = null;
        int[] palette = [1];
        for (var section = 0; section <= 8 && reader.Remaining > 0; section++)
        {
            var header = reader.Byte();
            if (header == 0xFF)
                continue;
            var bits = header >> 1;
            indices = bits == 0 ? null : Unpack(ref reader, bits);
            var count = bits == 0 ? 1 : BinaryPrimitives.ReadInt32LittleEndian(reader.Bytes(4));
            palette = new int[count];
            for (var i = 0; i < count; i++)
                palette[i] = BinaryPrimitives.ReadInt32LittleEndian(reader.Bytes(4));
        }
        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
            chunk.SetBiome(x, z, Biome(palette[indices?[x << 8 | z << 4] ?? 0]));
    }

    private static byte Biome(int id) => VanillaBiomes.All.Any(b => b.Id == id) ? (byte)id : (byte)1;

    // level.dat: 8-byte header (storage version, length) + little-endian NBT.
    private static void WriteLevel(string folder, List<(int X, int Z)> imported)
    {
        long seed = 0, time = 6000;
        int spawnX = 0, spawnY = 32767, spawnZ = 0;
        var levelDat = Path.Combine(folder, "level.dat");
        if (File.Exists(levelDat))
        {
            var bytes = File.ReadAllBytes(levelDat);
            var reader = new BedrockReader(bytes.AsSpan(8));
            var nbt = Nbt.ReadRoot(ref reader, network: false);
            seed = nbt.TryGetValue("RandomSeed", out var s) ? System.Convert.ToInt64(s) : 0;
            time = nbt.TryGetValue("Time", out var t) ? System.Convert.ToInt64(t) : 6000;
            spawnX = nbt.TryGetValue("SpawnX", out var sx) ? System.Convert.ToInt32(sx) : 0;
            spawnY = nbt.TryGetValue("SpawnY", out var sy) ? System.Convert.ToInt32(sy) : 32767;
            spawnZ = nbt.TryGetValue("SpawnZ", out var sz) ? System.Convert.ToInt32(sz) : 0;
        }
        // The spawn must be on imported ground: use the nearest imported chunk (its centre) when the spawn's chunk was never saved.
        if (imported.Count > 0 && !imported.Contains((spawnX >> 4, spawnZ >> 4)))
        {
            var (cx, cz) = imported.MinBy(c => Math.Pow(c.X - (spawnX >> 4), 2) + Math.Pow(c.Z - (spawnZ >> 4), 2));
            (spawnX, spawnZ, spawnY) = (cx * 16 + 8, cz * 16 + 8, 32767);
        }
        if (spawnY is > Chunk.MaxY or < Chunk.MinY)
        {
            // "Find the ground": the highest block of the spawn column.
            var file = Path.Combine(folder, "chunks", $"{spawnX >> 4}.{spawnZ >> 4}.nxc");
            spawnY = File.Exists(file) ? Chunk.Deserialize(spawnX >> 4, spawnZ >> 4, File.ReadAllBytes(file)).Height(spawnX & 15, spawnZ & 15) + 1 : 100;
        }
        File.WriteAllText(Path.Combine(folder, "level.json"), JsonSerializer.Serialize(new
        {
            Seed = seed, Time = time, SpawnX = spawnX, SpawnY = spawnY, SpawnZ = spawnZ, CreatedBy = Product.CreatedBy + " (imported Bedrock world)",
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
