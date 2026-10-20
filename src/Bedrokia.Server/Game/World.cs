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
using System.Text.Json;
using Bedrokia.Server.Core;

namespace Bedrokia.Server.Game;

/// <summary>
/// The overworld: generated from a seed, only modified chunks are saved (one compressed file each).
/// Chunks are generated or loaded on the thread pool; block reads and writes happen on the game thread.
/// </summary>
/// <remarks>ponytail: one file per modified chunk; switch to region files if worlds grow past ~100k edited chunks.</remarks>
public sealed class World
{
    private const int MaxCachedChunks = 4096;

    private readonly string _directory;

    /// <summary>Folder of this world (level.json, chunks/, players/).</summary>
    public string Folder => _directory;
    private readonly ConcurrentDictionary<long, Lazy<Task<Chunk>>> _chunks = new();

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static long ChunkKey(int x, int z) => ((long)x << 32) | (uint)z;

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static (int X, int Z) UnpackChunkKey(long key) => ((int)(key >> 32), (int)key);

    /// <summary>Number of cached chunks currently loaded in memory.</summary>
    public int LoadedChunkCount => _chunks.Count;

    private World(string directory, long seed, long time)
    {
        _directory = directory;
        Settings = WorldSettings.LoadOrCreate(directory);
        Generator = Settings.CreateGenerator(seed);
        Time = time;
        Directory.CreateDirectory(ChunkDirectory);
    }

    public IWorldGenerator Generator { get; }

    /// <summary>Bedrock dimension id: 0 overworld, 1 nether.</summary>
    public int Dimension => Generator is Generation.NetherGenerator ? 1 : 0;

    // Per-world simulation, set up by the server when it opens the world.
    public DroppedItems DroppedItems { get; internal set; } = null!;
    public Fluids Fluids { get; internal set; } = null!;
    public Entities.FallingBlocks FallingBlocks { get; internal set; } = null!;

    /// <summary>world.properties of this world (generator, day/night cycle).</summary>
    public WorldSettings Settings { get; }

    /// <summary>World time in ticks (0 = sunrise, 6000 = noon, 24000 = one day).</summary>
    public long Time { get; set; }

    public (int X, int Y, int Z) Spawn { get; set; }

    private string ChunkDirectory => Path.Combine(_directory, "chunks");
    private string LevelFile => Path.Combine(_directory, "level.json");

    private sealed record LevelData(long Seed, long Time, int SpawnX, int SpawnY, int SpawnZ, string? CreatedBy = null);

    /// <summary>Opens the world in <paramref name="directory"/>, creating it with a random seed if needed.</summary>
    /// <exception cref="InvalidDataException">level.json is corrupt.</exception>
    public static World OpenOrCreate(string directory, long? seed = null)
    {
        var levelFile = Path.Combine(directory, "level.json");
        if (File.Exists(levelFile))
        {
            LevelData data;
            try
            {
                data = JsonSerializer.Deserialize<LevelData>(File.ReadAllText(levelFile))
                       ?? throw new InvalidDataException($"{levelFile} is empty.");
            }
            catch (JsonException e)
            {
                throw new InvalidDataException($"{levelFile}: {e.Message}", e);
            }
            return new World(directory, data.Seed, data.Time) { Spawn = (data.SpawnX, data.SpawnY, data.SpawnZ) };
        }

        var world = new World(directory, seed ?? Random.Shared.NextInt64(), 6000); // start at noon
        var (x, _, z) = world.Generator.FindSpawn();
        var chunk = world.Generator.Generate(x >> 4, z >> 4);
        world.Spawn = (x, chunk.Height(x & 15, z & 15) + 1, z);
        world.SaveLevel();
        return world;
    }

    /// <summary>Returns the chunk, loading it from disk or generating it in the background if needed.</summary>
    public Task<Chunk> GetChunkAsync(int x, int z) =>
        _chunks.GetOrAdd(ChunkKey(x, z), _ => new Lazy<Task<Chunk>>(() => Task.Run(() => LoadOrGenerateAsync(x, z)))).Value;

    // A player who joins asks for hundreds of chunks at once: generating them all in parallel took every core.
    // Half the cores generate; the rest keep the game running (disk loads are not limited).
    private static readonly SemaphoreSlim GenerationSlots = new(Math.Max(1, Environment.ProcessorCount / 2));

    /// <summary>The chunk if it is already in memory and ready, otherwise null.</summary>
    public Chunk? LoadedChunk(int x, int z) =>
        _chunks.TryGetValue(ChunkKey(x, z), out var lazy) && lazy.IsValueCreated && lazy.Value.IsCompletedSuccessfully ? lazy.Value.Result : null;

    /// <summary>Block at a world position, or null when its chunk is not loaded.</summary>
    public BlockType? GetBlock(int x, int y, int z) => LoadedChunk(x >> 4, z >> 4)?.GetBlock(x & 15, y, z & 15);

    /// <summary>Sets a block in a loaded chunk; returns false if the chunk is not loaded.</summary>
    public bool SetBlock(int x, int y, int z, BlockType block)
    {
        var chunk = LoadedChunk(x >> 4, z >> 4);
        if (chunk is null || y is < Chunk.MinY or > Chunk.MaxY)
            return false;
        chunk.SetBlock(x & 15, y, z & 15, block);
        chunk.Dirty = true;
        // A block entity only lives with its block (furnace and lit furnace share one).
        if (chunk.GetBlockEntity(x & 15, y, z & 15) is { } entity && BlockEntity.For(block)?.Id != entity.Id)
            chunk.SetBlockEntity(x & 15, y, z & 15, null);
        return true;
    }

    public BlockEntity? GetBlockEntity(int x, int y, int z) => LoadedChunk(x >> 4, z >> 4)?.GetBlockEntity(x & 15, y, z & 15);

    public void SetBlockEntity(int x, int y, int z, BlockEntity? entity) => LoadedChunk(x >> 4, z >> 4)?.SetBlockEntity(x & 15, y, z & 15, entity);

    private async Task<Chunk> LoadOrGenerateAsync(int x, int z)
    {
        var file = ChunkFile(x, z);
        if (File.Exists(file))
        {
            try
            {
                return Chunk.Deserialize(x, z, File.ReadAllBytes(file));
            }
            catch (InvalidDataException)
            {
                // Keep the damaged file for inspection and regenerate rather than losing the whole server.
                File.Move(file, file + ".corrupt", overwrite: true);
            }
        }
        await GenerationSlots.WaitAsync();
        try
        {
            return Generator.Generate(x, z);
        }
        finally
        {
            GenerationSlots.Release();
        }
    }

    private string ChunkFile(int x, int z) => Path.Combine(ChunkDirectory, $"{x}.{z}.nxc");

    /// <summary>Writes every modified chunk and level.json. Game thread: snapshots, then writes files.</summary>
    /// <returns>Number of chunks written.</returns>
    public int Save()
    {
        var saved = 0;
        foreach (var (key, lazy) in _chunks)
        {
            if (!lazy.IsValueCreated || !lazy.Value.IsCompletedSuccessfully)
                continue;
            var chunk = lazy.Value.Result;
            if (!chunk.Dirty)
                continue;
            var (cx, cz) = UnpackChunkKey(key);
            WriteAtomically(ChunkFile(cx, cz), chunk.Serialize());
            chunk.Dirty = false;
            saved++;
        }
        SaveLevel();
        return saved;
    }

    /// <summary>Drops clean chunks nobody is near, to bound memory.</summary>
    public void Unload(Func<int, int, bool> keep)
    {
        if (_chunks.Count <= MaxCachedChunks)
            return;
        foreach (var (key, lazy) in _chunks)
        {
            var (cx, cz) = UnpackChunkKey(key);
            if (keep(cx, cz) || !lazy.IsValueCreated || !lazy.Value.IsCompletedSuccessfully || lazy.Value.Result.Dirty)
                continue;
            _chunks.TryRemove(key, out _);
        }
    }

    private void SaveLevel() => WriteAtomically(LevelFile,
        JsonSerializer.SerializeToUtf8Bytes(new LevelData(Generator.Seed, Time, Spawn.X, Spawn.Y, Spawn.Z, Product.CreatedBy), new JsonSerializerOptions { WriteIndented = true }));

    // Write to a temp file then rename, so a crash never leaves a half-written save.
    private static void WriteAtomically(string path, byte[] data)
    {
        var temp = path + ".tmp";
        File.WriteAllBytes(temp, data);
        File.Move(temp, path, overwrite: true);
    }
}
