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

using Bedrokia.Protocol;
using Bedrokia.Server.Game;

namespace Bedrokia.Tests;

public sealed class WorldTests : IDisposable
{
    private readonly string _dir = TestWorlds.NewDirectory();

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Generation_is_deterministic_and_layered()
    {
        var a = new WorldGenerator(1234).Generate(3, -7);
        var b = new WorldGenerator(1234).Generate(3, -7);
        Assert.Equal(a.Serialize(), b.Serialize());

        for (var x = 0; x < 16; x += 5)
        for (var z = 0; z < 16; z += 5)
        {
            Assert.Same(Blocks.Bedrock, a.GetBlock(x, Chunk.MinY, z));
            var top = a.Height(x, z);
            Assert.InRange(top, 0, 200);
            Assert.False(a.GetBlock(x, 10, z).IsAir); // solid ground (or sea) deep under the surface, whatever grows on top
        }
    }

    [Fact]
    public void Different_seeds_give_different_terrain()
    {
        var heights1 = Enumerable.Range(0, 64).Select(i => new WorldGenerator(1).TerrainHeight(i * 37, i * 11));
        var heights2 = Enumerable.Range(0, 64).Select(i => new WorldGenerator(2).TerrainHeight(i * 37, i * 11));
        Assert.NotEqual(heights1, heights2);
    }

    [Fact]
    public void Network_payload_layout()
    {
        var chunk = new Chunk(0, 0);
        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
        {
            chunk.SetBlock(x, -64, z, Blocks.Bedrock);
            chunk.SetBlock(x, -63, z, (x + z) % 2 == 0 ? Blocks.Stone : Blocks.Dirt);
        }
        chunk.SetBlock(1, 0, 1, Blocks.Glass); // section 4

        var (count, payload) = chunk.NetworkPayload();
        Assert.Equal(5u, count); // sections up to the highest non-air one
        Assert.Equal(9, payload[0]); // sub-chunk version
        Assert.Equal(1, payload[1]); // one layer
        Assert.Equal(unchecked((byte)-4), payload[2]); // y index of the lowest sub-chunk
        Assert.Equal((2 << 1) | 1, payload[3]); // air, bedrock, stone, dirt: 2 bits per block
        Assert.Equal(0, payload[^1]); // no border blocks
        Assert.Same(payload, chunk.NetworkPayload().Payload); // cached

        chunk.SetBlock(1, 0, 1, Blocks.Air);
        Assert.Equal(1u, chunk.NetworkPayload().SubChunkCount); // cache invalidated
    }

    [Fact]
    public void Single_value_section_uses_zero_bits()
    {
        var chunk = new Chunk(0, 0);
        for (var x = 0; x < 16; x++)
        for (var y = -64; y < -48; y++)
        for (var z = 0; z < 16; z++)
            chunk.SetBlock(x, y, z, Blocks.Stone);

        var payload = chunk.NetworkPayload().Payload;
        Assert.Equal(1, payload[3]); // 0 bits, runtime palette
        var reader = new BedrockReader(payload.AsSpan(4));
        Assert.Equal(unchecked((int)Blocks.Stone.NetworkId), reader.VarInt32());
    }

    [Fact]
    public async Task Modified_chunks_survive_a_restart()
    {
        var world = World.OpenOrCreate(_dir, 99);
        var spawn = world.Spawn;
        var chunk = await world.GetChunkAsync(spawn.X >> 4, spawn.Z >> 4);
        var below = (spawn.X, spawn.Y - 1, spawn.Z);
        Assert.False(world.GetBlock(below.X, below.Item2, below.Z)!.IsAir);
        Assert.True(world.SetBlock(below.X, below.Item2, below.Z, Blocks.Glass));
        world.Time = 4321;
        Assert.Equal(1, world.Save());
        Assert.False(chunk.Dirty);

        var reopened = World.OpenOrCreate(_dir);
        Assert.Equal(99, reopened.Generator.Seed);
        Assert.Equal(4321, reopened.Time);
        Assert.Equal(spawn, reopened.Spawn);
        await reopened.GetChunkAsync(spawn.X >> 4, spawn.Z >> 4);
        Assert.Same(Blocks.Glass, reopened.GetBlock(below.X, below.Item2, below.Z));
    }

    [Fact]
    public async Task Corrupt_chunk_file_is_set_aside_and_regenerated()
    {
        var world = World.OpenOrCreate(_dir, 5);
        File.WriteAllBytes(Path.Combine(_dir, "chunks", "0.0.nxc"), [1, 2, 3, 4, 5]);

        var chunk = await world.GetChunkAsync(0, 0);

        Assert.Same(Blocks.Bedrock, chunk.GetBlock(0, Chunk.MinY, 0));
        Assert.True(File.Exists(Path.Combine(_dir, "chunks", "0.0.nxc.corrupt")));
    }
}
