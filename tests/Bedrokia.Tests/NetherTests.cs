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

using Bedrokia.Server.Game;
using Bedrokia.Server.Game.Generation;

namespace Bedrokia.Tests;

public sealed class NetherTests
{
    [Fact]
    public void Nether_is_deterministic_and_closed_by_bedrock()
    {
        var a = new NetherGenerator(42).Generate(2, -3);
        Assert.Equal(a.Serialize(), new NetherGenerator(42).Generate(2, -3).Serialize());

        var names = new HashSet<string>();
        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
        {
            Assert.Equal("minecraft:bedrock", a.GetBlock(x, 0, z).Name);
            Assert.Equal("minecraft:bedrock", a.GetBlock(x, 127, z).Name);
            Assert.True(a.GetBlock(x, 128, z).IsAir); // nothing above the roof
            for (var y = 1; y < 127; y++)
                names.Add(a.GetBlock(x, y, z).Name);
        }
        Assert.Contains("minecraft:netherrack", names);
        Assert.DoesNotContain("minecraft:grass_block", names);
        Assert.DoesNotContain("minecraft:water", names);
    }

    [Fact]
    public void Nether_chunks_carry_eight_sections_from_y0()
    {
        var chunk = new NetherGenerator(7).Generate(0, 0);
        var (count, payload) = chunk.NetworkPayload(nether: true);
        Assert.InRange(count, 1u, 8u);
        Assert.Equal(new byte[] { 9, 1, 0 }, payload[..3]); // version 9, one layer, sub-chunk index 0 = y 0
    }
}
