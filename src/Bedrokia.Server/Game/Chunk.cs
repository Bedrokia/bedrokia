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

using System.IO.Compression;
using Bedrokia.Protocol;

namespace Bedrokia.Server.Game;

/// <summary>
/// A 16×384×16 column of the overworld (y -64..319): 24 sections of 4096 block ids in XZY order, plus one
/// biome per column. Mutated only on the game thread.
/// </summary>
public sealed class Chunk
{
    public const int MinY = -64;
    public const int MaxY = 319;
    public const int Sections = 24;
    private static readonly int[] ValidBits = [1, 2, 3, 4, 5, 6, 8, 16];
    private static readonly byte[] SaveMagic = "NXC1"u8.ToArray();

    private readonly ushort[]?[] _sections = new ushort[Sections][];
    private readonly byte[] _biomes = new byte[256];
    private byte[]? _networkPayload;
    private uint _networkSubChunks;

    public Chunk(int x, int z)
    {
        X = x;
        Z = z;
    }

    public int X { get; }
    public int Z { get; }

    /// <summary>Changed since the last save.</summary>
    public bool Dirty { get; set; }

    /// <summary>Block entities by (local x, world y, local z).</summary>
    public Dictionary<(int X, int Y, int Z), BlockEntity> BlockEntities { get; } = [];

    public BlockEntity? GetBlockEntity(int x, int y, int z) => BlockEntities.GetValueOrDefault((x, y, z));

    public void SetBlockEntity(int x, int y, int z, BlockEntity? entity)
    {
        if (entity is null)
            BlockEntities.Remove((x, y, z));
        else
            BlockEntities[(x, y, z)] = entity;
        _networkPayload = null;
        Dirty = true;
    }

    private static int Index(int x, int y, int z) => (x << 8) | (z << 4) | (y & 15);

    /// <param name="x">0..15</param>
    /// <param name="y">-64..319</param>
    /// <param name="z">0..15</param>
    public BlockType GetBlock(int x, int y, int z)
    {
        if (y is < MinY or > MaxY)
            return Blocks.Air;
        var section = _sections[(y - MinY) >> 4];
        return section is null ? Blocks.Air : Blocks.ById(section[Index(x, y, z)]);
    }

    public void SetBlock(int x, int y, int z, BlockType block)
    {
        if (y is < MinY or > MaxY)
            return;
        ref var section = ref _sections[(y - MinY) >> 4];
        if (section is null)
        {
            if (block.IsAir)
                return;
            section = new ushort[4096];
        }
        section[Index(x, y, z)] = block.Id;
        _networkPayload = null;
    }

    public byte GetBiome(int x, int z) => _biomes[(x << 4) | z];

    public void SetBiome(int x, int z, byte biome)
    {
        _biomes[(x << 4) | z] = biome;
        _networkPayload = null;
    }

    /// <summary>Highest y with a non-air block in column (x, z), or <see cref="MinY"/> - 1.</summary>
    public int Height(int x, int z)
    {
        for (var y = MaxY; y >= MinY; y--)
            if (!GetBlock(x, y, z).IsAir)
                return y;
        return MinY - 1;
    }

    /// <summary>LevelChunk payload (cached until the chunk changes) and the number of sub-chunks it carries.</summary>
    /// <param name="nether">The Nether only sends y 0..127: sections 4..11, with sub-chunk indices from 0.</param>
    public (uint SubChunkCount, byte[] Payload) NetworkPayload(bool nether = false)
    {
        if (_networkPayload is not null)
            return (_networkSubChunks, _networkPayload);

        int first = nether ? -MinY >> 4 : 0, sections = nether ? 8 : Sections;
        var count = sections;
        while (count > 1 && IsEmpty(_sections[first + count - 1]))
            count--;

        var writer = new BedrockWriter(16 * 1024);
        var air = new ushort[4096];
        for (var s = 0; s < count; s++)
        {
            writer.Byte(9).Byte(1).Byte(unchecked((byte)((nether ? 0 : MinY >> 4) + s))); // version 9, one layer, signed y index
            WriteStorage(writer, _sections[first + s] ?? air, id => unchecked((int)Blocks.ById(id).NetworkId));
        }

        var biomes = new ushort[4096];
        for (var i = 0; i < 4096; i++)
            biomes[i] = _biomes[i >> 4]; // index (x<<8)|(z<<4)|y → column (x<<4)|z
        WriteStorage(writer, biomes, id => id);
        for (var s = 1; s < sections; s++)
            writer.Byte(0xFF); // same biomes as the section below
        writer.Byte(0); // border blocks
        foreach (var ((bx, by, bz), entity) in BlockEntities)
            writer.Bytes(Nbt.Write(entity.NetworkNbt(X * 16 + bx, by, Z * 16 + bz), network: true));

        _networkSubChunks = (uint)count;
        _networkPayload = writer.ToArray();
        return (_networkSubChunks, _networkPayload);
    }

    private static bool IsEmpty(ushort[]? section) => section is null || Array.TrueForAll(section, id => id == 0);

    /// <summary>Paletted storage, network form: header (bits &lt;&lt; 1 | 1), packed words, varint palette.</summary>
    private static void WriteStorage(BedrockWriter writer, ushort[] values, Func<ushort, int> toNetwork)
    {
        var palette = new List<ushort>();
        var lookup = new Dictionary<ushort, int>();
        var indices = new int[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            if (!lookup.TryGetValue(values[i], out var index))
            {
                index = palette.Count;
                lookup[values[i]] = index;
                palette.Add(values[i]);
            }
            indices[i] = index;
        }

        if (palette.Count == 1)
        {
            writer.Byte(1); // 0 bits: one value, no palette length
            writer.VarInt32(toNetwork(palette[0]));
            return;
        }

        var bits = ValidBits.First(b => (1 << b) >= palette.Count);
        var perWord = 32 / bits;
        var words = (values.Length + perWord - 1) / perWord;
        writer.Byte((byte)((bits << 1) | 1));
        for (var w = 0; w < words; w++)
        {
            uint word = 0;
            for (var j = 0; j < perWord; j++)
            {
                var i = w * perWord + j;
                if (i < values.Length)
                    word |= (uint)indices[i] << (j * bits);
            }
            writer.UInt32(word);
        }
        writer.VarInt32(palette.Count);
        foreach (var value in palette)
            writer.VarInt32(toNetwork(value));
    }

    /// <summary>Save format: "NXC1", deflate( biomes[256], sectionMask u32, per section: palette (keys) + u16 indices ).</summary>
    public byte[] Serialize()
    {
        using var raw = new MemoryStream();
        using (var w = new BinaryWriter(raw))
        {
            w.Write(_biomes);
            uint mask = 0;
            for (var s = 0; s < Sections; s++)
                if (!IsEmpty(_sections[s]))
                    mask |= 1u << s;
            w.Write(mask);
            for (var s = 0; s < Sections; s++)
            {
                if ((mask & (1u << s)) == 0)
                    continue;
                var section = _sections[s]!;
                var palette = section.Distinct().ToList();
                w.Write((ushort)palette.Count);
                foreach (var id in palette)
                    w.Write(Blocks.ById(id).Key);
                var lookup = palette.Select((id, i) => (id, i)).ToDictionary(p => p.id, p => (ushort)p.i);
                foreach (var id in section)
                    w.Write(lookup[id]);
            }
            // Block entities (absent from saves of older versions).
            w.Write(BlockEntities.Count);
            foreach (var ((bx, by, bz), entity) in BlockEntities)
            {
                w.Write((byte)bx);
                w.Write((short)by);
                w.Write((byte)bz);
                entity.Write(w);
            }
        }
        using var output = new MemoryStream();
        output.Write(SaveMagic);
        using (var deflate = new DeflateStream(output, CompressionLevel.Fastest, leaveOpen: true))
            deflate.Write(raw.ToArray());
        return output.ToArray();
    }

    /// <exception cref="InvalidDataException">Corrupt or unknown format.</exception>
    public static Chunk Deserialize(int x, int z, byte[] data)
    {
        if (data.Length < 4 || !data.AsSpan(0, 4).SequenceEqual(SaveMagic))
            throw new InvalidDataException($"Not a {Core.Product.Name} chunk file.");
        var chunk = new Chunk(x, z);
        try
        {
            using var deflate = new DeflateStream(new MemoryStream(data, 4, data.Length - 4), CompressionMode.Decompress);
            using var r = new BinaryReader(deflate);
            r.ReadExactly(chunk._biomes);
            var mask = r.ReadUInt32();
            for (var s = 0; s < Sections; s++)
            {
                if ((mask & (1u << s)) == 0)
                    continue;
                var palette = new ushort[r.ReadUInt16()];
                for (var i = 0; i < palette.Length; i++)
                {
                    var key = r.ReadString();
                    palette[i] = (Blocks.ByKey(key) ?? Blocks.Air).Id; // ponytail: unknown blocks become air
                }
                var section = new ushort[4096];
                for (var i = 0; i < 4096; i++)
                {
                    var index = r.ReadUInt16();
                    if (index >= palette.Length)
                        throw new InvalidDataException("Palette index out of range.");
                    section[i] = palette[index];
                }
                chunk._sections[s] = section;
            }
            int entities;
            try
            {
                entities = r.ReadInt32();
            }
            catch (EndOfStreamException)
            {
                entities = 0; // saved by an older version
            }
            for (var i = 0; i < entities; i++)
            {
                var (bx, by, bz) = (r.ReadByte(), r.ReadInt16(), r.ReadByte());
                chunk.BlockEntities[(bx, by, bz)] = BlockEntity.Read(r);
            }
        }
        catch (EndOfStreamException e)
        {
            throw new InvalidDataException("Truncated chunk file.", e);
        }
        return chunk;
    }
}
