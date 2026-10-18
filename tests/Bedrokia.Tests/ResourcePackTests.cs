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
using Bedrokia.Server.Core;
using Bedrokia.Server.Game;

namespace Bedrokia.Tests;

public sealed class ResourcePackTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("bedrokia-packs").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void A_pack_folder_is_loaded_and_sent_in_chunks_that_rebuild_it()
    {
        var pack = Path.Combine(_dir, "MyPack");
        Directory.CreateDirectory(Path.Combine(pack, "textures"));
        File.WriteAllText(Path.Combine(pack, "manifest.json"), """
            {
              // comments are allowed in manifests
              "format_version": 2,
              "header": { "name": "My pack", "uuid": "11111111-2222-3333-4444-555555555555", "version": [1, 2, 3] },
              "modules": [ { "type": "resources", "uuid": "66666666-2222-3333-4444-555555555555", "version": [1, 2, 3] } ],
            }
            """);
        File.WriteAllBytes(Path.Combine(pack, "textures", "big.bin"), Enumerable.Range(0, 300_000).Select(i => (byte)(i * 7)).ToArray());
        File.WriteAllText(Path.Combine(_dir, "notes.txt"), "not a pack");

        using var logger = new Logger(LogLevel.Error, null);
        var packs = ResourcePacks.Load(_dir, logger);

        var loaded = Assert.Single(packs.All);
        Assert.Equal("My pack", loaded.Name);
        Assert.Equal("11111111-2222-3333-4444-555555555555_1.2.3", loaded.Id);
        Assert.Same(loaded, packs.ById(loaded.Id));

        var rebuilt = new List<byte>();
        for (var i = 0; ResourcePacks.ChunkPacket(loaded, i) is { } chunk; i++)
        {
            var reader = new BedrockReader(chunk);
            reader.VarUInt32();
            Assert.Equal(loaded.Id, reader.String());
            reader.Bytes(12); // index, offset
            rebuilt.AddRange(reader.ByteArray().ToArray());
        }
        Assert.Equal(loaded.Data, rebuilt);
        Assert.Equal(PacketId.ResourcePacksInfo, new BedrockReader(packs.InfoPacket(false)).VarUInt32());
    }
}
