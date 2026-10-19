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

using System.Buffers;
using System.Diagnostics;
using System.IO.Compression;
using Bedrokia.Protocol;
using Bedrokia.Server;
using Bedrokia.Server.Game;
using Xunit;
using Xunit.Abstractions;

namespace Bedrokia.Tests;

/// <summary>
/// Empirical speed and allocation benchmarks validating the optimizations implemented for the 'opti' branch.
/// </summary>
public class OptimizationBenchmarksTests(ITestOutputHelper output)
{
    [Fact]
    public void Benchmark_BatchCodec_SmallPacket_CompressionNone_vs_Deflate()
    {
        var codecNone = new BatchCodec(withHeader: true);
        codecNone.EnableCompression(256); // packets < 256B bypass compression (0xFF)

        // Simulate a typical MovePlayer / SetEntityMotion packet payload (48 bytes)
        var smallPayload = new byte[48];
        smallPayload[0] = 0x13; // MovePlayer packet id
        for (var i = 1; i < smallPayload.Length; i++)
            smallPayload[i] = (byte)i;

        const int iterations = 20_000;

        // Warm up
        for (var i = 0; i < 500; i++)
        {
            _ = codecNone.Encode(smallPayload);
            _ = EncodeWithForcedDeflate(smallPayload);
        }

        // Measure Deflate
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var memBeforeDeflate = GC.GetTotalAllocatedBytes(precise: true);
        var swDeflate = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
            _ = EncodeWithForcedDeflate(smallPayload);
        swDeflate.Stop();
        var memAfterDeflate = GC.GetTotalAllocatedBytes(precise: true);
        var deflateAlloc = memAfterDeflate - memBeforeDeflate;

        // Measure CompressionNone (optimized path)
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var memBeforeNone = GC.GetTotalAllocatedBytes(precise: true);
        var swNone = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
            _ = codecNone.Encode(smallPayload);
        swNone.Stop();
        var memAfterNone = GC.GetTotalAllocatedBytes(precise: true);
        var noneAlloc = memAfterNone - memBeforeNone;

        var speedup = (double)swDeflate.ElapsedTicks / Math.Max(1, swNone.ElapsedTicks);
        var allocReduction = 100.0 * (deflateAlloc - noneAlloc) / Math.Max(1, deflateAlloc);

        output.WriteLine($"=== BatchCodec Sub-256B CompressionNone Benchmark ({iterations} iterations) ===");
        output.WriteLine($"Forced Deflate: {swDeflate.ElapsedMilliseconds} ms, {deflateAlloc / 1024} KB allocated");
        output.WriteLine($"CompressionNone: {swNone.ElapsedMilliseconds} ms, {noneAlloc / 1024} KB allocated");
        output.WriteLine($"Speedup: x{speedup:F2} faster");
        output.WriteLine($"Memory Allocation Reduction: {allocReduction:F1} %");

        _ = speedup; // timing thresholds fail at random on shared CI machines: only reported, not asserted
        Assert.True(noneAlloc < deflateAlloc, "Expected lower allocation with CompressionNone bypass");
    }

    [Fact]
    public void Benchmark_Broadcast_PreEncoded_vs_Redundant_Encoding()
    {
        var codec = new BatchCodec(withHeader: true);
        codec.EnableCompression(256);
        var packet = new byte[128]; // text message / block event
        packet[0] = 0x09;

        const int targetsCount = 50;
        const int broadcasts = 2_000;

        // Baseline: encode N times (50 times per broadcast)
        var swBaseline = Stopwatch.StartNew();
        for (var b = 0; b < broadcasts; b++)
        {
            for (var t = 0; t < targetsCount; t++)
            {
                var encoded = codec.Encode(packet);
                _ = encoded.Length;
            }
        }
        swBaseline.Stop();

        // Optimized: encode once, fan out pre-encoded buffer
        var swOptimized = Stopwatch.StartNew();
        for (var b = 0; b < broadcasts; b++)
        {
            var preEncoded = codec.Encode(packet);
            for (var t = 0; t < targetsCount; t++)
            {
                // Dispatches directly to socket/channel
                _ = preEncoded.Length;
            }
        }
        swOptimized.Stop();

        var speedup = (double)swBaseline.ElapsedTicks / Math.Max(1, swOptimized.ElapsedTicks);

        output.WriteLine($"=== Broadcast Pre-Encoded (50 players, {broadcasts} broadcasts) ===");
        output.WriteLine($"Baseline (Encode x50): {swBaseline.ElapsedMilliseconds} ms");
        output.WriteLine($"Optimized (Encode Once): {swOptimized.ElapsedMilliseconds} ms");
        output.WriteLine($"Speedup: x{speedup:F2} faster");

        _ = speedup; // timing thresholds fail at random on shared CI machines: only reported, not asserted
    }

    [Fact]
    public void Benchmark_DroppedItems_SpatialGrid_vs_LinearScan()
    {
        // 1,000 dropped items scattered across 50x50 chunks
        const int itemCount = 1_000;
        var rng = new Random(42);
        var items = new (float X, float Y, float Z)[itemCount];
        for (var i = 0; i < itemCount; i++)
        {
            items[i] = (rng.Next(-400, 400) + (float)rng.NextDouble(),
                        rng.Next(60, 80) + (float)rng.NextDouble(),
                        rng.Next(-400, 400) + (float)rng.NextDouble());
        }

        // 4 active players in different chunks
        var players = new (float X, float Y, float Z, int ChunkX, int ChunkZ)[]
        {
            (10.5f, 64f, 10.5f, 0, 0),
            (250.2f, 65f, -120.4f, 15, -8),
            (-80.0f, 70f, 310.0f, -5, 19),
            (0.0f, 64f, 0.0f, 0, 0)
        };

        const int iterations = 10_000;

        // Baseline: O(N * M) linear scan
        var swBaseline = Stopwatch.StartNew();
        var baselineFound = 0;
        for (var iter = 0; iter < iterations; iter++)
        {
            foreach (var p in players)
            {
                foreach (var item in items)
                {
                    if (Math.Abs(p.X - item.X) < 1.5f && Math.Abs(p.Z - item.Z) < 1.5f && Math.Abs(p.Y - 1.62f - item.Y) < 2f)
                        baselineFound++;
                }
            }
        }
        swBaseline.Stop();

        // Optimized: Persistent 2D Spatial Grid (3x3 chunk inspection as in DroppedItems._byChunk)
        var grid = new Dictionary<(int X, int Z), List<(float X, float Y, float Z)>>();
        foreach (var item in items)
        {
            var key = ((int)MathF.Floor(item.X) >> 4, (int)MathF.Floor(item.Z) >> 4);
            if (!grid.TryGetValue(key, out var list))
                grid[key] = list = [];
            list.Add(item);
        }

        var swOptimized = Stopwatch.StartNew();
        var optFound = 0;
        for (var iter = 0; iter < iterations; iter++)
        {
            foreach (var p in players)
            {
                for (var dx = -1; dx <= 1; dx++)
                for (var dz = -1; dz <= 1; dz++)
                {
                    if (grid.TryGetValue((p.ChunkX + dx, p.ChunkZ + dz), out var candidates))
                    {
                        foreach (var item in candidates)
                        {
                            if (Math.Abs(p.X - item.X) < 1.5f && Math.Abs(p.Z - item.Z) < 1.5f && Math.Abs(p.Y - 1.62f - item.Y) < 2f)
                                optFound++;
                        }
                    }
                }
            }
        }
        swOptimized.Stop();

        var speedup = (double)swBaseline.ElapsedTicks / Math.Max(1, swOptimized.ElapsedTicks);

        output.WriteLine($"=== Dropped Items 2D Spatial Index Benchmark (1,000 items, {iterations} ticks) ===");
        output.WriteLine($"Linear Scan O(N*M): {swBaseline.ElapsedMilliseconds} ms");
        output.WriteLine($"2D Spatial Grid O(Players * 3x3): {swOptimized.ElapsedMilliseconds} ms");
        output.WriteLine($"Speedup: x{speedup:F2} faster");

        Assert.Equal(baselineFound, optFound);
        _ = speedup; // timing thresholds fail at random on shared CI machines: only reported, not asserted
    }

    [Fact]
    public void Benchmark_NetherNet_Span_Slicing_vs_Linq_SkipTakeToArray()
    {
        // Big message: 64 KB (typical chunk or creative content packet)
        var bigMessage = new byte[64_000];
        Random.Shared.NextBytes(bigMessage);

        const int iterations = 10_000;

        // Baseline: LINQ byte[].Skip().Take().ToArray()
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var memBeforeBaseline = GC.GetTotalAllocatedBytes(precise: true);
        var swBaseline = Stopwatch.StartNew();
        for (var it = 0; it < iterations; it++)
        {
            var total = (bigMessage.Length + 9_999) / 10_000;
            for (var i = 0; i < total; i++)
            {
                var slice = bigMessage.Skip(i * 10_000).Take(10_000).ToArray();
                var seg = new byte[slice.Length + 1];
                seg[0] = (byte)(total - 1 - i);
                Buffer.BlockCopy(slice, 0, seg, 1, slice.Length);
                _ = seg.Length;
            }
        }
        swBaseline.Stop();
        var memAfterBaseline = GC.GetTotalAllocatedBytes(precise: true);
        var baselineAlloc = memAfterBaseline - memBeforeBaseline;

        // Optimized: Span<byte> with rented buffer
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var memBeforeOpt = GC.GetTotalAllocatedBytes(precise: true);
        var swOptimized = Stopwatch.StartNew();
        var rented = ArrayPool<byte>.Shared.Rent(10_001);
        try
        {
            for (var it = 0; it < iterations; it++)
            {
                var span = bigMessage.AsSpan();
                var remaining = span.Length;
                var totalSegments = (remaining + 9_999) / 10_000;
                var offset = 0;
                for (var seg = totalSegments - 1; seg >= 0; seg--)
                {
                    var chunkLen = Math.Min(10_000, remaining);
                    rented[0] = (byte)seg;
                    span.Slice(offset, chunkLen).CopyTo(rented.AsSpan(1));
                    var packetSlice = rented.AsSpan(0, chunkLen + 1);
                    _ = packetSlice.Length;
                    offset += chunkLen;
                    remaining -= chunkLen;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
        swOptimized.Stop();
        var memAfterOpt = GC.GetTotalAllocatedBytes(precise: true);
        var optAlloc = memAfterOpt - memBeforeOpt;

        var speedup = (double)swBaseline.ElapsedTicks / Math.Max(1, swOptimized.ElapsedTicks);
        var allocReduction = 100.0 * (baselineAlloc - optAlloc) / Math.Max(1, baselineAlloc);

        output.WriteLine($"=== NetherNet SCTP 10 KB Slicing Benchmark ({iterations} packets) ===");
        output.WriteLine($"LINQ Skip/Take/ToArray: {swBaseline.ElapsedMilliseconds} ms, {baselineAlloc / 1024} KB allocated");
        output.WriteLine($"Span<byte> Slice Copy: {swOptimized.ElapsedMilliseconds} ms, {optAlloc / 1024} KB allocated");
        output.WriteLine($"Speedup: x{speedup:F2} faster");
        output.WriteLine($"Memory Allocation Reduction: {allocReduction:F1} %");

        _ = speedup; // timing thresholds fail at random on shared CI machines: only reported, not asserted
        Assert.True(optAlloc < baselineAlloc / 10, "Expected at least 90% GC allocation reduction");
    }

    [Fact]
    public void Benchmark_Chunk_EmptySubChunk_0Bit_DirectEmission()
    {
        // 16 subchunks, top 12 are empty air
        var chunk = new Chunk(0, 0);
        // Fill only bottom 4 sections
        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
        {
            chunk.SetBlock(x, -60, z, Blocks.Stone);
            chunk.SetBlock(x, 0, z, Blocks.Dirt);
        }

        const int iterations = 1_000;

        // Warmup
        _ = chunk.NetworkPayload();

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            var res = chunk.NetworkPayload();
            _ = res.Payload.Length;
        }
        sw.Stop();

        output.WriteLine($"=== Chunk NetworkPayload Serialization (1,000 chunks with empty subchunks) ===");
        output.WriteLine($"Total Time: {sw.ElapsedMilliseconds} ms ({sw.ElapsedMilliseconds / (double)iterations:F3} ms/chunk)");

        // timing thresholds fail at random on shared CI machines
    }

    [Fact]
    public void Benchmark_DTLS_Certificate_Caching()
    {
        const int iterations = 100;

        // Baseline: dynamic certificate generation on each connection
        var swGen = Stopwatch.StartNew();
        for (var i = 0; i < 5; i++)
        {
            var (cert, key) = SIPSorcery.Net.SharpSRTP.DTLS.DtlsCertificateUtils.GenerateCertificate("bedrokia", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddYears(1), useRSA: false);
            var x509 = new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificate(cert.GetCertificateAt(0).GetEncoded());
            _ = new SIPSorcery.Net.RTCCertificate2 { Certificate = x509, PrivateKey = key };
        }
        swGen.Stop();
        var msPerGen = swGen.ElapsedMilliseconds / 5.0;

        // Optimized: precomputed shared key and certificate
        var (cachedCert, cachedKey) = SIPSorcery.Net.SharpSRTP.DTLS.DtlsCertificateUtils.GenerateCertificate("bedrokia", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddYears(1), useRSA: false);
        var cachedX509 = new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificate(cachedCert.GetCertificateAt(0).GetEncoded());

        var swCached = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            _ = new SIPSorcery.Net.RTCCertificate2 { Certificate = cachedX509, PrivateKey = cachedKey };
        }
        swCached.Stop();
        var msPerCached = swCached.ElapsedMilliseconds / (double)iterations;

        var speedup = msPerGen / Math.Max(0.0001, msPerCached);

        output.WriteLine($"=== DTLS Certificate Caching Benchmark ===");
        output.WriteLine($"Dynamic Generation: {msPerGen:F2} ms/connection");
        output.WriteLine($"Cached Retrieval: {msPerCached:F4} ms/connection");
        output.WriteLine($"Speedup: x{speedup:F1} faster");

        _ = speedup; // timing thresholds fail at random on shared CI machines: only reported, not asserted
    }

    [Fact]
    public void Benchmark_PooledBedrockWriter_ZeroAlloc()
    {
        const int iterations = 50_000;

        // Baseline: MemoryStream + BinaryWriter
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var allocBaselineBefore = GC.GetAllocatedBytesForCurrentThread();
        var swBaseline = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            using var ms = new MemoryStream(64);
            using var bw = new BinaryWriter(ms);
            bw.Write((byte)0x13);
            bw.Write((float)123.45f);
            bw.Write((float)67.89f);
            bw.Write((float)98.76f);
            var result = ms.ToArray();
            _ = result.Length;
        }
        swBaseline.Stop();
        var allocBaseline = GC.GetAllocatedBytesForCurrentThread() - allocBaselineBefore;

        // Optimized: PooledBedrockWriter
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var allocOptimizedBefore = GC.GetAllocatedBytesForCurrentThread();
        var swOptimized = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            using var writer = new PooledBedrockWriter(64);
            writer.Byte(0x13);
            writer.Float32(123.45f);
            writer.Float32(67.89f);
            writer.Float32(98.76f);
            var span = writer.WrittenSpan;
            _ = span.Length;
        }
        swOptimized.Stop();
        var allocOptimized = GC.GetAllocatedBytesForCurrentThread() - allocOptimizedBefore;

        var speedup = (double)swBaseline.ElapsedTicks / Math.Max(1, swOptimized.ElapsedTicks);

        output.WriteLine($"=== PooledBedrockWriter vs MemoryStream ({iterations} iterations) ===");
        output.WriteLine($"Baseline (MemoryStream): {swBaseline.ElapsedMilliseconds} ms, {allocBaseline / 1024} KB allocated");
        output.WriteLine($"Optimized (PooledBedrockWriter): {swOptimized.ElapsedMilliseconds} ms, {allocOptimized / 1024} KB allocated");
        output.WriteLine($"Speedup: x{speedup:F2} faster");
        output.WriteLine($"Allocation Reduction: {100.0 * (allocBaseline - allocOptimized) / Math.Max(1, allocBaseline):F1} %");

        Assert.True(allocOptimized < allocBaseline, "Expected near-zero allocation with PooledBedrockWriter");
    }

    [Fact]
    public void Benchmark_Chunk_NetworkPayload_Caching()
    {
        var chunk = new Chunk(10, 20);
        for (var x = 0; x < 16; x++)
        for (var z = 0; z < 16; z++)
        {
            chunk.SetBlock(x, 64, z, Blocks.Stone);
            chunk.SetBlock(x, 65, z, Blocks.GrassBlock);
        }

        // Prime cache
        _ = chunk.NetworkPayload();

        const int iterations = 10_000;

        // Measure cached network payload retrieval (O(1))
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var allocCachedBefore = GC.GetAllocatedBytesForCurrentThread();
        var swCached = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            var res = chunk.NetworkPayload();
            _ = res.Payload.Length;
        }
        swCached.Stop();
        var allocCached = GC.GetAllocatedBytesForCurrentThread() - allocCachedBefore;

        output.WriteLine($"=== Chunk NetworkPayload Caching ({iterations} calls) ===");
        output.WriteLine($"Cached Retrieval: {swCached.ElapsedMilliseconds} ms, {allocCached / 1024} KB allocated ({swCached.ElapsedMilliseconds / (double)iterations:F4} ms/call)");

        Assert.True(allocCached < 1024 * 1024, "the cached payload must not be rebuilt");
    }

    [Fact]
    public void Benchmark_FrozenCollections_Lookups()
    {
        var standardDict = VanillaBiomes.All.ToDictionary(b => b.Id);
        var frozenDict = VanillaBiomes.ById;

        const int iterations = 200_000;

        // Baseline: Standard Dictionary
        var swStandard = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            _ = standardDict.TryGetValue(i % 100, out _);
        }
        swStandard.Stop();

        // Optimized: FrozenDictionary
        var swFrozen = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            _ = frozenDict.TryGetValue(i % 100, out _);
        }
        swFrozen.Stop();

        var speedup = (double)swStandard.ElapsedTicks / Math.Max(1, swFrozen.ElapsedTicks);

        output.WriteLine($"=== FrozenDictionary vs Standard Dictionary ({iterations} lookups) ===");
        output.WriteLine($"Standard Dictionary: {swStandard.ElapsedMilliseconds} ms");
        output.WriteLine($"FrozenDictionary: {swFrozen.ElapsedMilliseconds} ms");
        output.WriteLine($"Speedup: x{speedup:F2} faster");

        _ = speedup; // timing thresholds fail at random on shared CI machines: only reported, not asserted
    }

    private static byte[] EncodeWithForcedDeflate(byte[] packet)
    {
        var batch = new BedrockWriter();
        batch.ByteArray(packet);
        var payload = batch.ToArray();

        var output = new BedrockWriter(payload.Length + 16);
        output.Byte(0xFE); // Header
        output.Byte(0x00); // CompressionFlate
        using var ms = new MemoryStream();
        using (var ds = new DeflateStream(ms, CompressionLevel.Fastest, leaveOpen: true))
            ds.Write(payload, 0, payload.Length);
        output.Bytes(ms.ToArray());
        return output.ToArray();
    }
}
