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

namespace Bedrokia.Tests;

public sealed class WorldSettingsTests : IDisposable
{
    private readonly string _dir = TestWorlds.NewDirectory();

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void A_flat_world_uses_the_configured_layers_and_spawns_on_top()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllLines(Path.Combine(_dir, WorldSettings.FileName), ["generator=flat", "flat-layers=bedrock,3*stone,grass_block", "flat-biome=desert"]);

        var world = World.OpenOrCreate(_dir, 1);
        var chunk = world.Generator.Generate(5, -3);

        Assert.Same(Blocks.Bedrock, chunk.GetBlock(0, -64, 0));
        Assert.Same(Blocks.Stone, chunk.GetBlock(0, -61, 0));
        Assert.Same(Blocks.GrassBlock, chunk.GetBlock(0, -60, 0));
        Assert.True(chunk.GetBlock(0, -59, 0).IsAir);
        Assert.Equal(2, chunk.GetBiome(3, 3));
        Assert.Equal(-59, world.Spawn.Y);
    }

    [Fact]
    public void Defaults_are_written_and_bad_values_are_refused()
    {
        WorldSettings.LoadOrCreate(_dir);
        Assert.Contains("generator=normal", File.ReadAllText(Path.Combine(_dir, WorldSettings.FileName)));

        File.WriteAllLines(Path.Combine(_dir, WorldSettings.FileName), ["generator=flat", "flat-layers=2*not_a_block"]);
        Assert.Throws<InvalidDataException>(() => WorldSettings.LoadOrCreate(_dir));
    }
}
