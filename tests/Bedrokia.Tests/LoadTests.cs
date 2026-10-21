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

using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using Bedrokia.Protocol;
using Bedrokia.Server;
using Bedrokia.Server.Core;
using Bedrokia.Server.Network;
using Xunit.Abstractions;

namespace Bedrokia.Tests;

/// <summary>
/// Load test: N players spread over a map, each moving every tick and seeing the players of the chunks around it.
/// Prints the tick time per part. Runs only with BEDROKIA_LOAD=players (e.g. 1000): too long for every test run.
/// </summary>
public class LoadTests(ITestOutputHelper output)
{
    private sealed class CountingTransport : IGameTransport
    {
        public long Bytes;
        public IPEndPoint RemoteEndPoint { get; } = new(IPAddress.Loopback, 19132);
        public ECDsa? PeerPublicKey => null;
        public void Send(byte[] batch) => Interlocked.Add(ref Bytes, batch.Length);
        public void Disconnect(string reason) { }
    }

    [Fact]
    public async Task Many_players_moving()
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable("BEDROKIA_LOAD"), out var count) || count <= 0)
            return;
        var server = new BedrokiaServer(new ServerConfig { WorldsDirectory = TestWorlds.NewDirectory(), Port = 0, OnlineMode = false, Mobs = false },
            new Logger(LogLevel.Error, null));
        server.InitWorldForTesting();
        var world = server.World;
        const int span = 20, view = 4; // players over 20 x 20 chunks, each seeing 9 x 9 chunks
        var chunks = new List<Bedrokia.Server.Game.Chunk>();
        for (var cx = -view; cx < span + view; cx++)
        for (var cz = -view; cz < span + view; cz++)
            chunks.Add(await world.GetChunkAsync(cx, cz));
        var random = new Random(1);
        var players = new List<(BedrockConnection Player, CountingTransport Transport)>();
        var connections = (List<BedrockConnection>)typeof(BedrokiaServer).GetField("_connections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(server)!;
        for (var i = 0; i < count; i++)
        {
            var transport = new CountingTransport();
            var player = new BedrockConnection(server, transport, 1000 + i)
            {
                Position = (random.Next(span * 16), 100 + BedrockConnection.EyeHeight, random.Next(span * 16)),
                IsInGame = true,
            };
            server.AddPlayer(player);
            connections.Add(player); // flushed every tick, as the network connections
            var seen = (HashSet<long>)typeof(BedrockConnection).GetField("_sentChunks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(player)!;
            var (pcx, pcz) = player.ChunkPosition;
            for (var cx = pcx - view; cx <= pcx + view; cx++)
            for (var cz = pcz - view; cz <= pcz + view; cz++)
            {
                world.LoadedChunk(cx, cz)?.Viewers.Add(player);
                seen.Add(Bedrokia.Server.Game.World.ChunkKey(cx, cz));
            }
            players.Add((player, transport));
        }
        server.Timings.Start();
        var ticks = 40;
        var watch = Stopwatch.StartNew();
        for (var t = 0; t < ticks; t++)
        {
            foreach (var (player, _) in players)
            {
                var (x, y, z) = player.Position;
                var w = new BedrockWriter().Float32(0).Float32(random.NextSingle() * 360).Float32(x + random.NextSingle() * 0.4f - 0.2f).Float32(y)
                    .Float32(z + random.NextSingle() * 0.4f - 0.2f);
                var bytes = w.ToArray();
                server.RunOnMainThread(() =>
                {
                    var reader = new BedrockReader(bytes);
                    player.OnPlayerAuthInput(ref reader);
                });
            }
            server.Tick();
        }
        watch.Stop();
        var sent = players.Sum(p => p.Transport.Bytes);
        output.WriteLine($"{count} players, {ticks} ticks: {watch.Elapsed.TotalMilliseconds / ticks:F1} ms a tick (budget 50 ms), " +
                         $"{sent / ticks / 1024.0:F0} KiB sent a tick ({sent / ticks / count} bytes a player)");
        output.WriteLine(server.Timings.GenerateReport().Replace("§", "&"));
    }
}
