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

namespace Bedrokia.Server.Game;

/// <summary>
/// Minimal PNG reader for skins: 8-bit RGB or RGBA, not interlaced (what image editors export for skins).
/// ponytail: no palette, grey or 16-bit images; add them if a plugin needs one.
/// </summary>
public static class Png
{
    private static ReadOnlySpan<byte> Signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Decodes a PNG file to RGBA bytes, row by row from the top.</summary>
    /// <exception cref="InvalidDataException">Not a PNG, or a kind this reader does not handle.</exception>
    public static (int Width, int Height, byte[] Rgba) Decode(byte[] png)
    {
        if (png.Length < 8 || !png.AsSpan(0, 8).SequenceEqual(Signature))
            throw new InvalidDataException("Not a PNG file.");
        int width = 0, height = 0, channels = 0;
        using var idat = new MemoryStream();
        for (var pos = 8; pos + 8 <= png.Length;)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(pos));
            var type = System.Text.Encoding.ASCII.GetString(png, pos + 4, 4);
            if (length < 0 || pos + 12 + length > png.Length)
                throw new InvalidDataException("Truncated PNG chunk.");
            var data = png.AsSpan(pos + 8, length);
            if (type == "IHDR")
            {
                width = BinaryPrimitives.ReadInt32BigEndian(data);
                height = BinaryPrimitives.ReadInt32BigEndian(data[4..]);
                channels = data[9] switch { 2 => 3, 6 => 4, _ => 0 };
                if (data[8] != 8 || channels == 0 || data[12] != 0)
                    throw new InvalidDataException("Only 8-bit RGB or RGBA PNGs without interlacing are supported.");
                if (width <= 0 || height <= 0 || (long)width * height > 4096 * 4096)
                    throw new InvalidDataException("Bad PNG size.");
            }
            else if (type == "IDAT")
                idat.Write(data);
            else if (type == "IEND")
                break;
            pos += 12 + length;
        }
        if (channels == 0)
            throw new InvalidDataException("PNG without header.");

        idat.Position = 0;
        using var zlib = new ZLibStream(idat, CompressionMode.Decompress);
        var stride = width * channels;
        var raw = new byte[(stride + 1) * height];
        zlib.ReadExactly(raw);

        var rgba = new byte[width * height * 4];
        var previous = new byte[stride];
        var line = new byte[stride];
        for (var y = 0; y < height; y++)
        {
            var filter = raw[y * (stride + 1)];
            raw.AsSpan(y * (stride + 1) + 1, stride).CopyTo(line);
            for (var i = 0; i < stride; i++)
            {
                int a = i >= channels ? line[i - channels] : 0, b = previous[i], c = i >= channels ? previous[i - channels] : 0;
                line[i] += filter switch
                {
                    0 => 0,
                    1 => (byte)a,
                    2 => (byte)b,
                    3 => (byte)((a + b) / 2),
                    4 => Paeth(a, b, c),
                    _ => throw new InvalidDataException("Bad PNG filter."),
                };
            }
            for (var x = 0; x < width; x++)
            {
                var o = (y * width + x) * 4;
                rgba[o] = line[x * channels];
                rgba[o + 1] = line[x * channels + 1];
                rgba[o + 2] = line[x * channels + 2];
                rgba[o + 3] = channels == 4 ? line[x * channels + 3] : (byte)255;
            }
            (previous, line) = (line, previous);
        }
        return (width, height, rgba);
    }

    private static byte Paeth(int a, int b, int c)
    {
        var p = a + b - c;
        int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return (byte)(pa <= pb && pa <= pc ? a : pb <= pc ? b : c);
    }

    /// <summary>Nearest-neighbour resize of an RGBA image.</summary>
    public static byte[] Resize(byte[] rgba, int width, int height, int newWidth, int newHeight)
    {
        var result = new byte[newWidth * newHeight * 4];
        for (var y = 0; y < newHeight; y++)
        for (var x = 0; x < newWidth; x++)
            rgba.AsSpan(((y * height / newHeight) * width + x * width / newWidth) * 4, 4).CopyTo(result.AsSpan((y * newWidth + x) * 4));
        return result;
    }
}
