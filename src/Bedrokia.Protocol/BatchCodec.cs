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
using System.Security.Cryptography;

namespace Bedrokia.Protocol;

/// <summary>
/// Encodes and decodes Bedrock packet batches: 0xFE, [encrypted]( [compression id] compressed( (varuint length, packet)* ) [checksum] ).
/// One instance per connection; not thread-safe (callers serialize access).
/// </summary>
/// <param name="withHeader">False for NetherNet, which carries batches without the 0xFE header.</param>
public sealed class BatchCodec(bool withHeader = true)
{
    public const byte Header = 0xFE;
    public const byte CompressionFlate = 0x00;
    public const byte CompressionNone = 0xFF;
    public const int MaxDecompressedSize = 16 * 1024 * 1024;
    public const int MaxPacketsPerBatch = 812;

    private int _compressionThreshold = -1;
    private Cipher? _send, _receive;

    public bool CompressionEnabled => _compressionThreshold >= 0;
    public bool EncryptionEnabled => _send is not null;

    /// <summary>Batches from now on carry a compression id; those of <paramref name="threshold"/> bytes or more are deflated.</summary>
    public void EnableCompression(int threshold) => _compressionThreshold = threshold;

    /// <summary>Enables AES-256-CTR with checksums, both directions, from the next batch on.</summary>
    public void EnableEncryption(byte[] key)
    {
        if (!withHeader)
            throw new InvalidOperationException("NetherNet connections are encrypted by DTLS, not by the game layer.");
        _send = new Cipher(key);
        _receive = new Cipher(key);
    }

    public byte[] Encode(IEnumerable<byte[]> packets)
    {
        var batch = new BedrockWriter();
        foreach (var packet in packets)
            batch.ByteArray(packet);
        var payload = batch.ToArray();

        var output = new BedrockWriter(payload.Length + 16);
        if (withHeader)
            output.Byte(Header);
        if (CompressionEnabled)
        {
            if (payload.Length >= _compressionThreshold)
            {
                output.Byte(CompressionFlate);
                payload = Deflate(payload);
            }
            else
            {
                output.Byte(CompressionNone);
            }
        }
        output.Bytes(payload);
        var data = output.ToArray();
        return _send is null ? data : _send.Encrypt(data);
    }

    /// <summary>Splits one batch into packets (each starting with its varuint header).</summary>
    /// <exception cref="InvalidDataException">Bad header, checksum, compression or framing.</exception>
    public List<byte[]> Decode(ReadOnlySpan<byte> data)
    {
        if (withHeader && (data.IsEmpty || data[0] != Header))
            throw new InvalidDataException("Not a game packet batch.");
        var body = (withHeader ? data[1..] : data).ToArray();
        if (_receive is not null)
            body = _receive.Decrypt(body);

        if (CompressionEnabled)
        {
            if (body.Length == 0)
                throw new InvalidDataException("Missing compression id.");
            body = body[0] switch
            {
                CompressionNone => body[1..],
                CompressionFlate => Inflate(body.AsSpan(1)),
                _ => throw new InvalidDataException($"Unsupported compression {body[0]}."),
            };
        }

        var packets = new List<byte[]>();
        var reader = new BedrockReader(body);
        while (reader.Remaining > 0)
        {
            if (packets.Count >= MaxPacketsPerBatch)
                throw new InvalidDataException("Too many packets in one batch.");
            packets.Add(reader.ByteArray().ToArray());
        }
        return packets;
    }

    private static byte[] Deflate(byte[] data)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Fastest))
            deflate.Write(data);
        return output.ToArray();
    }

    private static byte[] Inflate(ReadOnlySpan<byte> data)
    {
        using var input = new MemoryStream(data.ToArray());
        using var inflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        var buffer = new byte[16 * 1024];
        int read;
        try
        {
            while ((read = inflate.Read(buffer)) > 0)
            {
                if (output.Length + read > MaxDecompressedSize)
                    throw new InvalidDataException("Decompressed batch is too large.");
                output.Write(buffer, 0, read);
            }
        }
        catch (IOException e)
        {
            throw new InvalidDataException("Corrupt compressed batch.", e);
        }
        return output.ToArray();
    }

    /// <summary>
    /// AES-256-CTR keystream (IV = key[0..12] ‖ 00000002, 128-bit big-endian counter) plus the Bedrock checksum:
    /// SHA-256(counter LE64 ‖ plaintext ‖ key)[0..8], appended before encryption.
    /// </summary>
    private sealed class Cipher
    {
        private readonly byte[] _key;
        private readonly Aes _aes = Aes.Create();
        private readonly byte[] _counterBlock = new byte[16];
        private readonly byte[] _keystream = new byte[16];
        private int _keystreamUsed = 16;
        private ulong _packetCounter;

        public Cipher(byte[] key)
        {
            _key = key;
            _aes.Key = key;
            key.AsSpan(0, 12).CopyTo(_counterBlock);
            _counterBlock[15] = 2;
        }

        // data = 0xFE ‖ plaintext; returns 0xFE ‖ encrypt(plaintext ‖ checksum).
        public byte[] Encrypt(byte[] data)
        {
            var result = new byte[data.Length + 8];
            data.CopyTo(result, 0);
            Checksum(data.AsSpan(1)).CopyTo(result.AsSpan(data.Length));
            Xor(result.AsSpan(1));
            return result;
        }

        public byte[] Decrypt(byte[] body)
        {
            Xor(body);
            if (body.Length < 8)
                throw new InvalidDataException("Encrypted batch is too short.");
            var plain = body[..^8];
            if (!CryptographicOperations.FixedTimeEquals(Checksum(plain), body.AsSpan(body.Length - 8)))
                throw new InvalidDataException("Invalid batch checksum.");
            return plain;
        }

        private byte[] Checksum(ReadOnlySpan<byte> plain)
        {
            using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            Span<byte> counter = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(counter, _packetCounter++);
            sha.AppendData(counter);
            sha.AppendData(plain);
            sha.AppendData(_key);
            return sha.GetHashAndReset()[..8];
        }

        private void Xor(Span<byte> data)
        {
            for (var i = 0; i < data.Length; i++)
            {
                if (_keystreamUsed == 16)
                {
                    _aes.EncryptEcb(_counterBlock, _keystream, PaddingMode.None);
                    for (var j = 15; j >= 0 && ++_counterBlock[j] == 0; j--)
                    {
                    }
                    _keystreamUsed = 0;
                }
                data[i] ^= _keystream[_keystreamUsed++];
            }
        }
    }
}
