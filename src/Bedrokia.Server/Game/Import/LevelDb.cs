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

namespace Bedrokia.Server.Game.Import;

/// <summary>
/// Read-only LevelDB reader for Bedrock worlds (the db/ folder): table files (.ldb/.sst) and write-ahead logs
/// (.log), written from the public LevelDB file format documentation. Mojang's fork compresses blocks with zlib
/// (type 2) or raw deflate (type 4). Returns the live value of every key (latest sequence wins, deletions removed).
/// </summary>
public static class LevelDb
{
    public static Dictionary<byte[], byte[]> ReadAll(string directory)
    {
        var latest = new Dictionary<byte[], (ulong Seq, byte[]? Value)>(ByteArrayComparer.Instance);
        void Put(byte[] key, ulong seq, byte[]? value)
        {
            if (!latest.TryGetValue(key, out var current) || current.Seq <= seq)
                latest[key] = (seq, value);
        }

        foreach (var file in Directory.EnumerateFiles(directory))
        {
            var extension = Path.GetExtension(file).ToLowerInvariant();
            if (extension is ".ldb" or ".sst")
                ReadTable(File.ReadAllBytes(file), Put);
            else if (extension == ".log")
                ReadLog(File.ReadAllBytes(file), Put);
        }
        return latest.Where(e => e.Value.Value is not null)
            .ToDictionary(e => e.Key, e => e.Value.Value!, ByteArrayComparer.Instance);
    }

    // --- Table files ---

    private static void ReadTable(byte[] file, Action<byte[], ulong, byte[]?> put)
    {
        if (file.Length < 48)
            return;
        var footer = file.AsSpan(file.Length - 48);
        var position = 0;
        ReadVarint(footer, ref position); // metaindex offset
        ReadVarint(footer, ref position); // metaindex size
        var indexOffset = (long)ReadVarint(footer, ref position);
        var indexSize = (long)ReadVarint(footer, ref position);
        foreach (var (_, handle) in Entries(Block(file, indexOffset, indexSize)))
        {
            var p = 0;
            var offset = (long)ReadVarint(handle, ref p);
            var size = (long)ReadVarint(handle, ref p);
            foreach (var (key, value) in Entries(Block(file, offset, size)))
            {
                if (key.Length < 8)
                    continue;
                var trailer = BinaryPrimitives.ReadUInt64LittleEndian(key.AsSpan(key.Length - 8));
                put(key[..^8], trailer >> 8, (trailer & 0xFF) == 1 ? value : null);
            }
        }
    }

    private static byte[] Block(byte[] file, long offset, long size)
    {
        var raw = file.AsSpan((int)offset, (int)size);
        var compression = file[offset + size];
        return compression switch
        {
            0 => raw.ToArray(),
            2 => Inflate(new ZLibStream(new MemoryStream(raw.ToArray()), CompressionMode.Decompress)),
            4 => Inflate(new DeflateStream(new MemoryStream(raw.ToArray()), CompressionMode.Decompress)),
            _ => throw new InvalidDataException($"Unsupported LevelDB block compression {compression}."),
        };
    }

    private static byte[] Inflate(Stream stream)
    {
        using (stream)
        using (var output = new MemoryStream())
        {
            stream.CopyTo(output);
            return output.ToArray();
        }
    }

    // Prefix-compressed entries followed by the restart array and its length.
    private static IEnumerable<(byte[] Key, byte[] Value)> Entries(byte[] block)
    {
        if (block.Length < 4)
            yield break;
        var restarts = BinaryPrimitives.ReadInt32LittleEndian(block.AsSpan(block.Length - 4));
        var end = block.Length - 4 - restarts * 4;
        var position = 0;
        var key = Array.Empty<byte>();
        while (position < end)
        {
            var shared = (int)ReadVarint(block, ref position);
            var unshared = (int)ReadVarint(block, ref position);
            var valueLength = (int)ReadVarint(block, ref position);
            var next = new byte[shared + unshared];
            key.AsSpan(0, shared).CopyTo(next);
            block.AsSpan(position, unshared).CopyTo(next.AsSpan(shared));
            position += unshared;
            key = next;
            yield return (key, block.AsSpan(position, valueLength).ToArray());
            position += valueLength;
        }
    }

    // --- Write-ahead log ---

    private static void ReadLog(byte[] file, Action<byte[], ulong, byte[]?> put)
    {
        const int blockSize = 32768;
        var record = new MemoryStream();
        var position = 0;
        while (position + 7 <= file.Length)
        {
            var left = blockSize - position % blockSize;
            if (left < 7)
            {
                position += left;
                continue;
            }
            var length = BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(position + 4));
            var type = file[position + 6];
            position += 7;
            if (type == 0 || position + length > file.Length)
            {
                position += left - 7; // zero padding: skip to the next block
                continue;
            }
            record.Write(file, position, length);
            position += length;
            if (type is 1 or 4) // full or last fragment
            {
                ReadBatch(record.ToArray(), put);
                record.SetLength(0);
            }
        }
    }

    // WriteBatch: sequence (8), count (4), then (type, key, [value]) records.
    private static void ReadBatch(byte[] batch, Action<byte[], ulong, byte[]?> put)
    {
        if (batch.Length < 12)
            return;
        var sequence = BinaryPrimitives.ReadUInt64LittleEndian(batch);
        var count = BinaryPrimitives.ReadInt32LittleEndian(batch.AsSpan(8));
        var position = 12;
        for (var i = 0; i < count && position < batch.Length; i++, sequence++)
        {
            var type = batch[position++];
            var keyLength = (int)ReadVarint(batch, ref position);
            var key = batch.AsSpan(position, keyLength).ToArray();
            position += keyLength;
            if (type == 1)
            {
                var valueLength = (int)ReadVarint(batch, ref position);
                put(key, sequence, batch.AsSpan(position, valueLength).ToArray());
                position += valueLength;
            }
            else
                put(key, sequence, null);
        }
    }

    private static ulong ReadVarint(ReadOnlySpan<byte> data, ref int position)
    {
        ulong value = 0;
        for (var shift = 0; shift < 64; shift += 7)
        {
            var b = data[position++];
            value |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
                break;
        }
        return value;
    }

    private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public static readonly ByteArrayComparer Instance = new();

        public bool Equals(byte[]? a, byte[]? b) => a.AsSpan().SequenceEqual(b);

        public int GetHashCode(byte[] key)
        {
            var hash = new HashCode();
            hash.AddBytes(key);
            return hash.ToHashCode();
        }
    }
}
