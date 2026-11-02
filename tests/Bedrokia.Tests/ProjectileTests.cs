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


using System.Numerics;
using Bedrokia.Server;
using Bedrokia.Server.Core;
using Bedrokia.Server.Entities;
using Bedrokia.Server.Game;

namespace Bedrokia.Tests;

public sealed class ProjectileTests
{
    [Fact]
    public async Task A_thrown_snowball_falls_and_stops_on_the_ground()
    {
        using var logger = new Logger(LogLevel.Error, null);
        var server = new BedrokiaServer(new ServerConfig { WorldsDirectory = TestWorlds.NewDirectory(), Seed = 42, Port = 0 }, logger);
        var world = World.OpenOrCreate(TestWorlds.NewDirectory(), 42);
        var chunk = await world.GetChunkAsync(0, 0);
        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
            chunk.SetBlock(x, 64, z, Blocks.Stone);
        for (var y = 65; y < 90; y++)
            chunk.SetBlock(8, y, 8, Blocks.Air);

        var ball = server.Projectiles.Spawn(ProjectileManager.Snowball, world, new Vector3(8.5f, 80, 8.5f), new Vector3(0, 0.5f, 0));
        for (var tick = 0; tick < 200 && server.Projectiles.All.Count > 0; tick++)
            server.Projectiles.Tick();

        Assert.Empty(server.Projectiles.All);
        Assert.InRange(ball.Position.Y, 65, 66.5f); // it went up first, then landed on the stone at y 64
    }

    [Fact]
    public async Task A_plugin_throwable_is_known_to_the_client_flies_with_its_gravity_and_calls_its_hit()
    {
        using var logger = new Logger(LogLevel.Error, null);
        var server = new BedrokiaServer(new ServerConfig { WorldsDirectory = TestWorlds.NewDirectory(), Seed = 42, Port = 0 }, logger);
        var world = World.OpenOrCreate(TestWorlds.NewDirectory(), 42);
        var chunk = await world.GetChunkAsync(0, 0);
        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
        {
            chunk.SetBlock(x, 64, z, Blocks.Stone);
            for (var y = 65; y < 90; y++)
                chunk.SetBlock(x, y, z, Blocks.Air);
        }
        ProjectileHit? hit = null;
        var grenade = new ThrowableType { Entity = "test:grenade", Gravity = 0.1f, OnHit = h => hit = h };
        server.RegisterThrowable("minecraft:stick", grenade);

        Assert.True(ProjectileManager.IsThrowable(ItemStack.Of("stick")));
        Assert.True(ActorIdentifiers.Packet.AsSpan().IndexOf("test:grenade"u8) >= 0); // the client learns the entity
        var thrown = server.Projectiles.Spawn("minecraft:stick", world, new Vector3(8.5f, 80, 8.5f), new Vector3(0.1f, 0, 0), type: grenade);
        Assert.Equal("test:grenade", thrown.Identifier);
        for (var tick = 0; tick < 200 && server.Projectiles.All.Count > 0; tick++)
            server.Projectiles.Tick();

        Assert.NotNull(hit);
        Assert.Same(thrown, hit.Projectile);
        Assert.InRange(hit.At.Y, 64, 65.1f);
        Assert.InRange(hit.At.X, 8.5f, 10.5f); // heavy: it fell close (a snowball would have gone further)
    }
}
