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

using Bedrokia.Server;
using Bedrokia.Server.Core;
using Bedrokia.Server.Entities;
using Bedrokia.Server.Game;

namespace Bedrokia.Tests;

public sealed class MobTests : IAsyncDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private readonly Logger _logger = new(LogLevel.Error, null);
    private readonly BedrokiaServer _server;
    private readonly Task _run;

    public MobTests()
    {
        _server = new BedrokiaServer(new ServerConfig { WorldsDirectory = TestWorlds.NewDirectory(), Seed = 42, Port = 0, OnlineMode = false }, _logger);
        _run = _server.RunAsync();
    }

    private async Task<T> OnMain<T>(Func<T> action)
    {
        var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _server.RunOnMainThread(() =>
        {
            try { done.SetResult(action()); }
            catch (Exception e) { done.SetException(e); }
        });
        return await done.Task.WaitAsync(Timeout);
    }

    [Fact]
    public async Task A_mob_falls_onto_the_ground_and_dies_from_damage()
    {
        await _server.Started.WaitAsync(Timeout);
        var spawn = _server.World.Spawn;
        await _server.World.GetChunkAsync(spawn.X >> 4, spawn.Z >> 4).WaitAsync(Timeout); // mobs only live in loaded chunks
        var mob = await OnMain(() => _server.Mobs.Spawn(MobKind.Cow, spawn.X + 0.5, spawn.Y + 6, spawn.Z + 0.5));

        // Let it fall for two seconds (no player around: it stays loaded because the spawn chunk is).
        await Task.Delay(2000);
        var (y, onGround) = await OnMain(() => (mob.Y, mob.OnGround));
        Assert.True(onGround, $"cow still falling at y {y}");
        Assert.InRange(y, spawn.Y - 2, spawn.Y + 1);

        var killed = await OnMain(() =>
        {
            for (var i = 0; i < 20 && !mob.Dead; i++)
            {
                mob.HurtTicks = 0;
                _server.Mobs.Damage(mob, 4, (mob.X - 1, mob.Z));
            }
            return mob.Dead;
        });
        Assert.True(killed);
    }

    [Fact]
    public async Task Sand_falls_onto_the_ground()
    {
        await _server.Started.WaitAsync(Timeout);
        var (x, y, z) = _server.World.Spawn;
        await _server.World.GetChunkAsync(x >> 4, z >> 4).WaitAsync(Timeout);
        var landed = await OnMain(() =>
        {
            for (var dy = 0; dy < 6; dy++)
                _server.World.SetBlock(x, y + dy, z, Blocks.Air);
            _server.World.SetBlock(x, y + 5, z, Blocks.Sand);
            _server.ApplyGravity(x, y + 5, z);
            return _server.World.GetBlock(x, y + 5, z);
        });
        Assert.Same(Blocks.Air, landed); // it is now a falling entity
        await Task.Delay(2000);
        Assert.Same(Blocks.Sand, await OnMain(() => _server.World.GetBlock(x, y, z)));
    }

    [Fact]
    public async Task Water_flows_explosions_break_blocks_and_bone_meal_grows_crops()
    {
        await _server.Started.WaitAsync(Timeout);
        var (x, y, z) = _server.World.Spawn;
        (x, y, z) = ((x & ~15) + 8, y + 20, (z & ~15) + 8); // in the air above spawn, inside its loaded chunk
        await _server.World.GetChunkAsync(x >> 4, z >> 4).WaitAsync(Timeout);
        await OnMain(() =>
        {
            for (var dx = -3; dx <= 3; dx++)
            for (var dz = -3; dz <= 3; dz++)
                _server.World.SetBlock(x + dx, y - 1, z + dz, Blocks.Stone);
            _server.SetBlockAndUpdate(x, y, z, Fluids.Source("water"));
            return 0;
        });
        await Task.Delay(1500);
        var flowing = await OnMain(() => _server.World.GetBlock(x + 1, y, z))!;
        Assert.Equal("minecraft:flowing_water", flowing!.Name);
        Assert.Equal(1, Fluids.Depth(flowing));

        var (crater, wheat) = await OnMain(() =>
        {
            _server.World.SetBlock(x + 2, y - 1, z + 2, Blocks.ByName("farmland")!);
            _server.World.SetBlock(x + 2, y, z + 2, Blocks.ByName("wheat")!);
            _server.Grow(x + 2, y, z + 2, boneMeal: true);
            var grown = _server.World.GetBlock(x + 2, y, z + 2)!;
            _server.Explode(x + 0.5, y - 0.5, z - 2.5, 2);
            return (_server.World.GetBlock(x, y - 1, z - 3), grown);
        });
        Assert.True(crater!.IsAir);
        Assert.True(Convert.ToInt32(wheat.States["growth"]) >= 2);
    }

    [Fact]
    public void Blocks_that_mobs_walk_through()
    {
        Assert.True(BlockPhysics.IsSolid(Blocks.Stone));
        Assert.False(BlockPhysics.IsSolid(Blocks.Water));
        Assert.False(BlockPhysics.IsSolid(Blocks.ShortGrass));
        Assert.False(BlockPhysics.IsSolid(null));
    }

    public async ValueTask DisposeAsync()
    {
        _server.Stop();
        await _run.WaitAsync(Timeout);
        _logger.Dispose();
    }
}
