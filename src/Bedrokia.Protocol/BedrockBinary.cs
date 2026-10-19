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
using System.Text;

namespace Bedrokia.Protocol;

/// <summary>
/// Growable writer for Bedrock game packets. Fixed-width integers are little-endian unless named BE;
/// strings and byte arrays are prefixed with a varuint32 length.
/// </summary>
public sealed class BedrockWriter(int capacity = 256)
{
    private byte[] _buffer = new byte[capacity];

    public int Length { get; private set; }

    private Span<byte> Grow(int count)
    {
        if (Length + count > _buffer.Length)
            Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, Length + count));
        var span = _buffer.AsSpan(Length, count);
        Length += count;
        return span;
    }

    public BedrockWriter Byte(byte value)
    {
        Grow(1)[0] = value;
        return this;
    }

    public BedrockWriter Bool(bool value) => Byte(value ? (byte)1 : (byte)0);
    public BedrockWriter Bytes(ReadOnlySpan<byte> value)
    {
        value.CopyTo(Grow(value.Length));
        return this;
    }

    public BedrockWriter Int16(short value) { BinaryPrimitives.WriteInt16LittleEndian(Grow(2), value); return this; }
    public BedrockWriter UInt16(ushort value) { BinaryPrimitives.WriteUInt16LittleEndian(Grow(2), value); return this; }
    public BedrockWriter Int32(int value) { BinaryPrimitives.WriteInt32LittleEndian(Grow(4), value); return this; }
    public BedrockWriter UInt32(uint value) { BinaryPrimitives.WriteUInt32LittleEndian(Grow(4), value); return this; }
    public BedrockWriter Int32BE(int value) { BinaryPrimitives.WriteInt32BigEndian(Grow(4), value); return this; }
    public BedrockWriter Int64(long value) { BinaryPrimitives.WriteInt64LittleEndian(Grow(8), value); return this; }
    public BedrockWriter UInt64(ulong value) { BinaryPrimitives.WriteUInt64LittleEndian(Grow(8), value); return this; }
    public BedrockWriter Float32(float value) { BinaryPrimitives.WriteSingleLittleEndian(Grow(4), value); return this; }

    public BedrockWriter VarUInt64(ulong value)
    {
        while (value >= 0x80)
        {
            Byte((byte)(value | 0x80));
            value >>= 7;
        }
        return Byte((byte)value);
    }

    public BedrockWriter VarUInt32(uint value) => VarUInt64(value);
    public static int VarUInt32Length(uint value) =>
        value < 0x80 ? 1 : value < 0x4000 ? 2 : value < 0x200000 ? 3 : value < 0x10000000 ? 4 : 5;
    public BedrockWriter VarInt32(int value) => VarUInt64((uint)((value << 1) ^ (value >> 31)));

    public BedrockWriter VarInt64(long value) => VarUInt64((ulong)((value << 1) ^ (value >> 63)));

    public BedrockWriter String(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        VarUInt32((uint)bytes.Length);
        return Bytes(bytes);
    }

    public BedrockWriter ByteArray(ReadOnlySpan<byte> value)
    {
        VarUInt32((uint)value.Length);
        return Bytes(value);
    }

    public BedrockWriter Vec3(float x, float y, float z) => Float32(x).Float32(y).Float32(z);

    /// <summary>Block position: three zigzag varint32.</summary>
    public BedrockWriter BlockPos(int x, int y, int z) => VarInt32(x).VarInt32(y).VarInt32(z);

    /// <summary>The nil UUID (16 zero bytes; byte order does not matter for it).</summary>
    public BedrockWriter EmptyUuid() => Bytes(stackalloc byte[16]);

    public byte[] ToArray() => _buffer.AsSpan(0, Length).ToArray();
}

/// <summary>Bounds-checked reader for Bedrock game packets; overruns throw <see cref="InvalidDataException"/>.</summary>
public ref struct BedrockReader(ReadOnlySpan<byte> data)
{
    private const int MaxStringLength = 1 << 20;

    private readonly ReadOnlySpan<byte> _data = data;
    private int _position;

    public readonly int Remaining => _data.Length - _position;

    public ReadOnlySpan<byte> Bytes(int count)
    {
        if (count < 0 || count > Remaining)
            throw new InvalidDataException("Unexpected end of packet.");
        var span = _data.Slice(_position, count);
        _position += count;
        return span;
    }

    public byte Byte() => Bytes(1)[0];
    public bool Bool() => Byte() != 0;
    public int Int32() => BinaryPrimitives.ReadInt32LittleEndian(Bytes(4));
    public int Int32BE() => BinaryPrimitives.ReadInt32BigEndian(Bytes(4));
    public float Float32() => BinaryPrimitives.ReadSingleLittleEndian(Bytes(4));

    public ulong VarUInt64()
    {
        ulong value = 0;
        for (var shift = 0; shift < 70; shift += 7)
        {
            var b = Byte();
            value |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
                return value;
        }
        throw new InvalidDataException("Varint is too long.");
    }

    public uint VarUInt32()
    {
        var value = VarUInt64();
        if (value > uint.MaxValue)
            throw new InvalidDataException("Varint32 overflow.");
        return (uint)value;
    }

    public int VarInt32()
    {
        var raw = VarUInt32();
        return (int)(raw >> 1) ^ -(int)(raw & 1);
    }

    public long VarInt64()
    {
        var raw = VarUInt64();
        return (long)(raw >> 1) ^ -(long)(raw & 1);
    }

    public ReadOnlySpan<byte> ByteArray()
    {
        var length = VarUInt32();
        if (length > MaxStringLength * 16)
            throw new InvalidDataException("Byte array is too long.");
        return Bytes((int)length);
    }

    public string String()
    {
        var length = VarUInt32();
        if (length > MaxStringLength)
            throw new InvalidDataException("String is too long.");
        return Encoding.UTF8.GetString(Bytes((int)length));
    }
}
