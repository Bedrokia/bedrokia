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
using Bedrokia.Protocol;
using Bedrokia.Server;
using Bedrokia.Server.Core;
using Bedrokia.Server.Entities;
using Bedrokia.Server.Game;

namespace Bedrokia.Tests;

public sealed class NpcTests
{
    // A tiny RGBA PNG: rows filtered with "sub" (1) and "paeth" (4) so the unfiltering is checked too.
    private static byte[] MakePng(int width, int height, byte[] rgba)
    {
        var raw = new MemoryStream();
        for (var y = 0; y < height; y++)
        {
            var filter = y % 2 == 0 ? 1 : 4;
            raw.WriteByte((byte)filter);
            for (var i = 0; i < width * 4; i++)
            {
                int Px(int yy, int ii) => yy < 0 || ii < 0 ? 0 : rgba[yy * width * 4 + ii];
                int a = Px(y, i - 4), b = Px(y - 1, i), c = Px(y - 1, i - 4);
                var p = a + b - c;
                int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
                var predictor = filter == 1 ? a : pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
                raw.WriteByte((byte)(rgba[y * width * 4 + i] - predictor));
            }
        }
        var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(raw.ToArray());
        var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        void Chunk(string type, byte[] data)
        {
            Span<byte> length = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
            png.Write(length);
            png.Write(System.Text.Encoding.ASCII.GetBytes(type));
            png.Write(data);
            png.Write(new byte[4]); // CRC: not checked by the reader
        }
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8;
        header[9] = 6;
        Chunk("IHDR", header);
        Chunk("IDAT", compressed.ToArray());
        Chunk("IEND", []);
        return png.ToArray();
    }

    [Fact]
    public void Png_decodes_filtered_rgba()
    {
        var pixels = new byte[3 * 3 * 4];
        new Random(5).NextBytes(pixels);
        var (width, height, rgba) = Png.Decode(MakePng(3, 3, pixels));
        Assert.Equal((3, 3), (width, height));
        Assert.Equal(pixels, rgba);
    }

    [Fact]
    public void Npc_skin_uses_the_geometry_identifier_and_large_images_are_scaled()
    {
        var png = MakePng(256, 256, new byte[256 * 256 * 4]);
        const string geometry = """{"format_version":"1.12.0","minecraft:geometry":[{"description":{"identifier":"geometry.box"}}]}""";
        var skin = NpcManager.SkinFromPng("box", png, geometry);
        var reader = new BedrockReader(skin);
        Assert.Equal("box", reader.String());
        reader.String(); // play fab id
        Assert.Contains("geometry.box", System.Text.Encoding.UTF8.GetString(reader.ByteArray()));
        Assert.Equal(128, reader.Int32());
        Assert.Equal(128, reader.Int32());
        Assert.Equal(128 * 128 * 4, reader.ByteArray().Length);
    }

    [Fact]
    public void Spawned_npcs_can_be_found_and_removed()
    {
        using var logger = new Logger(LogLevel.Error, null);
        var server = new BedrokiaServer(new ServerConfig { WorldsDirectory = TestWorlds.NewDirectory(), Seed = 42, Port = 0 }, logger);
        var world = World.OpenOrCreate(TestWorlds.NewDirectory(), 1);
        var npc = server.Npcs.Spawn(world, 0.5, 64, 0.5, 90, "Box", PlayerPackets.CustomSkin("s", 64, 64, new byte[64 * 64 * 4]));
        npc.Tag = "antium";
        Assert.Same(npc, server.Npcs.Find(npc.EntityId));
        server.Npcs.Remove(npc);
        Assert.Null(server.Npcs.Find(npc.EntityId));
    }
}
