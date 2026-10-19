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

// Ported from Glowstone (MIT), net/glowstone/util/noise/{PerlinNoise,PerlinOctaveGenerator,SimplexNoise,SimplexOctaveGenerator}.java.
// The Bukkit base classes Glowstone extends are NOT used: the permutation table, improved Perlin noise (Ken Perlin's
// reference), simplex noise (Stefan Gustavson's public algorithm) and the octave sum are re-implemented here.
using System.Runtime.CompilerServices;

namespace Bedrokia.Server.Game.Generation;

/// <summary>Improved Perlin noise with a permutation table shuffled from a <see cref="JavaRandom"/>. Immutable.</summary>
internal class PerlinNoise
{
    protected readonly int[] Perm = new int[512];
    protected readonly double OffsetX, OffsetY, OffsetZ;

    public PerlinNoise(JavaRandom rand)
    {
        OffsetX = rand.NextDouble() * 256;
        OffsetY = rand.NextDouble() * 256;
        OffsetZ = rand.NextDouble() * 256;
        for (var i = 0; i < 256; i++)
            Perm[i] = i;
        for (var i = 0; i < 256; i++)
        {
            var pos = rand.NextInt(256 - i) + i;
            (Perm[i], Perm[pos]) = (Perm[pos], Perm[i]);
            Perm[i + 256] = Perm[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static int Floor(double x)
    {
        var floored = (int)x;
        return x < floored ? floored - 1 : floored;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static double Lerp(double t, double a, double b) => a + t * (b - a);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static double Grad(int hash, double x, double y, double z)
    {
        var h = hash & 15;
        var u = h < 8 ? x : y;
        var v = h < 4 ? y : h is 12 or 14 ? x : z;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    /// <summary>Single 3D sample (offsets applied).</summary>
    public virtual double Noise(double x, double y, double z)
    {
        x += OffsetX;
        y += OffsetY;
        z += OffsetZ;
        int fx = Floor(x), fy = Floor(y), fz = Floor(z);
        int ix = fx & 255, iy = fy & 255, iz = fz & 255;
        x -= fx;
        y -= fy;
        z -= fz;
        double u = Fade(x), v = Fade(y), w = Fade(z);
        int a = Perm[ix] + iy, aa = Perm[a] + iz, ab = Perm[a + 1] + iz;
        int b = Perm[ix + 1] + iy, ba = Perm[b] + iz, bb = Perm[b + 1] + iz;
        return Lerp(w,
            Lerp(v, Lerp(u, Grad(Perm[aa], x, y, z), Grad(Perm[ba], x - 1, y, z)),
                Lerp(u, Grad(Perm[ab], x, y - 1, z), Grad(Perm[bb], x - 1, y - 1, z))),
            Lerp(v, Lerp(u, Grad(Perm[aa + 1], x, y, z - 1), Grad(Perm[ba + 1], x - 1, y, z - 1)),
                Lerp(u, Grad(Perm[ab + 1], x, y - 1, z - 1), Grad(Perm[bb + 1], x - 1, y - 1, z - 1))));
    }

    public void GetNoise(double[] noise, double x, double y, double z, int sizeX, int sizeY, int sizeZ,
        double scaleX, double scaleY, double scaleZ, double amplitude)
    {
        if (sizeY == 1)
            Get2dNoise(noise, x, z, sizeX, sizeZ, scaleX, scaleZ, amplitude);
        else
            Get3dNoise(noise, x, y, z, sizeX, sizeY, sizeZ, scaleX, scaleY, scaleZ, amplitude);
    }

    protected virtual void Get2dNoise(double[] noise, double x, double z, int sizeX, int sizeZ, double scaleX,
        double scaleZ, double amplitude)
    {
        var index = 0;
        for (var i = 0; i < sizeX; i++)
        {
            var dx = x + OffsetX + i * scaleX;
            var floorX = Floor(dx);
            var ix = floorX & 255;
            dx -= floorX;
            var fx = Fade(dx);
            for (var j = 0; j < sizeZ; j++)
            {
                var dz = z + OffsetZ + j * scaleZ;
                var floorZ = Floor(dz);
                var iz = floorZ & 255;
                dz -= floorZ;
                var fz = Fade(dz);
                var a = Perm[ix];
                var aa = Perm[a] + iz;
                var b = Perm[ix + 1];
                var ba = Perm[b] + iz;
                var x1 = Lerp(fx, Grad(Perm[aa], dx, 0, dz), Grad(Perm[ba], dx - 1, 0, dz));
                var x2 = Lerp(fx, Grad(Perm[aa + 1], dx, 0, dz - 1), Grad(Perm[ba + 1], dx - 1, 0, dz - 1));
                noise[index++] += Lerp(fz, x1, x2) * amplitude;
            }
        }
    }

    protected virtual void Get3dNoise(double[] noise, double x, double y, double z, int sizeX, int sizeY, int sizeZ,
        double scaleX, double scaleY, double scaleZ, double amplitude)
    {
        var n = -1;
        double x1 = 0, x2 = 0, x3 = 0, x4 = 0;
        var index = 0;
        for (var i = 0; i < sizeX; i++)
        {
            var dx = x + OffsetX + i * scaleX;
            var floorX = Floor(dx);
            var ix = floorX & 255;
            dx -= floorX;
            var fx = Fade(dx);
            for (var j = 0; j < sizeZ; j++)
            {
                var dz = z + OffsetZ + j * scaleZ;
                var floorZ = Floor(dz);
                var iz = floorZ & 255;
                dz -= floorZ;
                var fz = Fade(dz);
                for (var k = 0; k < sizeY; k++)
                {
                    var dy = y + OffsetY + k * scaleY;
                    var floorY = Floor(dy);
                    var iy = floorY & 255;
                    dy -= floorY;
                    var fy = Fade(dy);
                    if (k == 0 || iy != n)
                    {
                        n = iy;
                        int a = Perm[ix] + iy, aa = Perm[a] + iz, ab = Perm[a + 1] + iz;
                        int b = Perm[ix + 1] + iy, ba = Perm[b] + iz, bb = Perm[b + 1] + iz;
                        x1 = Lerp(fx, Grad(Perm[aa], dx, dy, dz), Grad(Perm[ba], dx - 1, dy, dz));
                        x2 = Lerp(fx, Grad(Perm[ab], dx, dy - 1, dz), Grad(Perm[bb], dx - 1, dy - 1, dz));
                        x3 = Lerp(fx, Grad(Perm[aa + 1], dx, dy, dz - 1), Grad(Perm[ba + 1], dx - 1, dy, dz - 1));
                        x4 = Lerp(fx, Grad(Perm[ab + 1], dx, dy - 1, dz - 1), Grad(Perm[bb + 1], dx - 1, dy - 1, dz - 1));
                    }
                    var y1 = Lerp(fy, x1, x2);
                    var y2 = Lerp(fy, x3, x4);
                    noise[index++] += Lerp(fz, y1, y2) * amplitude;
                }
            }
        }
    }
}

/// <summary>Simplex noise (Gustavson) sharing the Perlin permutation table. Immutable.</summary>
internal sealed class SimplexNoise : PerlinNoise
{
    private const double Sqrt3 = 1.7320508075688772;
    private const double F2 = 0.5 * (Sqrt3 - 1), G2 = (3 - Sqrt3) / 6, G22 = G2 * 2.0 - 1;
    private const double F3 = 1.0 / 3.0, G3 = 1.0 / 6.0, G32 = G3 * 2.0, G33 = G3 * 3.0 - 1.0;

    private static readonly int[] Grad3 =
    [
        1, 1, 0, -1, 1, 0, 1, -1, 0, -1, -1, 0,
        1, 0, 1, -1, 0, 1, 1, 0, -1, -1, 0, -1,
        0, 1, 1, 0, -1, 1, 0, 1, -1, 0, -1, -1,
    ];

    private readonly int[] _permMod12 = new int[512];

    public SimplexNoise(JavaRandom rand) : base(rand)
    {
        for (var i = 0; i < 512; i++)
            _permMod12[i] = Perm[i] % 12;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SFloor(double x) => x > 0 ? (int)x : (int)x - 1;

    protected override void Get2dNoise(double[] noise, double x, double z, int sizeX, int sizeY, double scaleX,
        double scaleY, double amplitude)
    {
        var index = 0;
        for (var i = 0; i < sizeY; i++)
        {
            var zin = OffsetY + (z + i) * scaleY;
            for (var j = 0; j < sizeX; j++)
            {
                var xin = OffsetX + (x + j) * scaleX;
                noise[index++] += Simplex2D(xin, zin) * amplitude;
            }
        }
    }

    protected override void Get3dNoise(double[] noise, double x, double y, double z, int sizeX, int sizeY, int sizeZ,
        double scaleX, double scaleY, double scaleZ, double amplitude)
    {
        var index = 0;
        for (var i = 0; i < sizeZ; i++)
        {
            var zin = OffsetZ + (z + i) * scaleZ;
            for (var j = 0; j < sizeX; j++)
            {
                var xin = OffsetX + (x + j) * scaleX;
                for (var k = 0; k < sizeY; k++)
                {
                    var yin = OffsetY + (y + k) * scaleY;
                    noise[index++] += Simplex3D(xin, yin, zin) * amplitude;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Noise(double xin, double yin) => Simplex2D(xin + OffsetX, yin + OffsetY);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override double Noise(double xin, double yin, double zin) =>
        Simplex3D(xin + OffsetX, yin + OffsetY, zin + OffsetZ);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Dot(int g, double x, double y) => Grad3[g * 3] * x + Grad3[g * 3 + 1] * y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Dot(int g, double x, double y, double z) =>
        Grad3[g * 3] * x + Grad3[g * 3 + 1] * y + Grad3[g * 3 + 2] * z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double Simplex2D(double xin, double yin)
    {
        var s = (xin + yin) * F2;
        var i = SFloor(xin + s);
        var j = SFloor(yin + s);
        var t = (i + j) * G2;
        var x0 = xin - (i - t);
        var y0 = yin - (j - t);
        int i1, j1;
        if (x0 > y0)
        {
            i1 = 1;
            j1 = 0;
        }
        else
        {
            i1 = 0;
            j1 = 1;
        }
        var x1 = x0 - i1 + G2;
        var y1 = y0 - j1 + G2;
        var x2 = x0 + G22;
        var y2 = y0 + G22;
        int ii = i & 255, jj = j & 255;
        var gi0 = _permMod12[ii + Perm[jj]];
        var gi1 = _permMod12[ii + i1 + Perm[jj + j1]];
        var gi2 = _permMod12[ii + 1 + Perm[jj + 1]];
        double n0 = 0, n1 = 0, n2 = 0;
        var t0 = 0.5 - x0 * x0 - y0 * y0;
        if (t0 >= 0)
        {
            t0 *= t0;
            n0 = t0 * t0 * Dot(gi0, x0, y0);
        }
        var t1 = 0.5 - x1 * x1 - y1 * y1;
        if (t1 >= 0)
        {
            t1 *= t1;
            n1 = t1 * t1 * Dot(gi1, x1, y1);
        }
        var t2 = 0.5 - x2 * x2 - y2 * y2;
        if (t2 >= 0)
        {
            t2 *= t2;
            n2 = t2 * t2 * Dot(gi2, x2, y2);
        }
        return 70.0 * (n0 + n1 + n2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double Simplex3D(double xin, double yin, double zin)
    {
        var s = (xin + yin + zin) * F3;
        var i = SFloor(xin + s);
        var j = SFloor(yin + s);
        var k = SFloor(zin + s);
        var t = (i + j + k) * G3;
        var x0 = xin - (i - t);
        var y0 = yin - (j - t);
        var z0 = zin - (k - t);
        int i1, j1, k1, i2, j2, k2;
        if (x0 >= y0)
        {
            if (y0 >= z0) (i1, j1, k1, i2, j2, k2) = (1, 0, 0, 1, 1, 0);
            else if (x0 >= z0) (i1, j1, k1, i2, j2, k2) = (1, 0, 0, 1, 0, 1);
            else (i1, j1, k1, i2, j2, k2) = (0, 0, 1, 1, 0, 1);
        }
        else
        {
            if (y0 < z0) (i1, j1, k1, i2, j2, k2) = (0, 0, 1, 0, 1, 1);
            else if (x0 < z0) (i1, j1, k1, i2, j2, k2) = (0, 1, 0, 0, 1, 1);
            else (i1, j1, k1, i2, j2, k2) = (0, 1, 0, 1, 1, 0);
        }
        double x1 = x0 - i1 + G3, y1 = y0 - j1 + G3, z1 = z0 - k1 + G3;
        double x2 = x0 - i2 + G32, y2 = y0 - j2 + G32, z2 = z0 - k2 + G32;
        double x3 = x0 + G33, y3 = y0 + G33, z3 = z0 + G33;
        int ii = i & 255, jj = j & 255, kk = k & 255;
        var gi0 = _permMod12[ii + Perm[jj + Perm[kk]]];
        var gi1 = _permMod12[ii + i1 + Perm[jj + j1 + Perm[kk + k1]]];
        var gi2 = _permMod12[ii + i2 + Perm[jj + j2 + Perm[kk + k2]]];
        var gi3 = _permMod12[ii + 1 + Perm[jj + 1 + Perm[kk + 1]]];
        double n0 = 0, n1 = 0, n2 = 0, n3 = 0;
        var t0 = 0.5 - x0 * x0 - y0 * y0 - z0 * z0;
        if (t0 >= 0)
        {
            t0 *= t0;
            n0 = t0 * t0 * Dot(gi0, x0, y0, z0);
        }
        var t1 = 0.5 - x1 * x1 - y1 * y1 - z1 * z1;
        if (t1 >= 0)
        {
            t1 *= t1;
            n1 = t1 * t1 * Dot(gi1, x1, y1, z1);
        }
        var t2 = 0.5 - x2 * x2 - y2 * y2 - z2 * z2;
        if (t2 >= 0)
        {
            t2 *= t2;
            n2 = t2 * t2 * Dot(gi2, x2, y2, z2);
        }
        var t3 = 0.5 - x3 * x3 - y3 * y3 - z3 * z3;
        if (t3 >= 0)
        {
            t3 *= t3;
            n3 = t3 * t3 * Dot(gi3, x3, y3, z3);
        }
        return 32.0 * (n0 + n1 + n2 + n3);
    }
}

/// <summary>
/// Sum of noise octaves. Immutable after construction (scales are set once by the owner), so thread-safe:
/// <see cref="GetFractalBrownianMotion(double,double,double,double,double)"/> returns a fresh array.
/// </summary>
internal class PerlinOctaveGenerator
{
    protected readonly PerlinNoise[] Octaves;

    public PerlinOctaveGenerator(JavaRandom rand, int octaves, int sizeX, int sizeZ) : this(rand, octaves, sizeX, 1, sizeZ)
    {
    }

    public PerlinOctaveGenerator(JavaRandom rand, int octaves, int sizeX, int sizeY, int sizeZ)
        : this(CreateOctaves(rand, octaves, r => new PerlinNoise(r)), sizeX, sizeY, sizeZ)
    {
    }

    protected PerlinOctaveGenerator(PerlinNoise[] octaves, int sizeX, int sizeY, int sizeZ)
    {
        Octaves = octaves;
        SizeX = sizeX;
        SizeY = sizeY;
        SizeZ = sizeZ;
    }

    public int SizeX { get; }
    public int SizeY { get; }
    public int SizeZ { get; }
    public double XScale { get; set; } = 1;
    public double YScale { get; set; } = 1;
    public double ZScale { get; set; } = 1;

    public void SetScale(double scale) => XScale = YScale = ZScale = scale;

    protected static PerlinNoise[] CreateOctaves(JavaRandom rand, int octaves, Func<JavaRandom, PerlinNoise> create)
    {
        var result = new PerlinNoise[octaves];
        for (var i = 0; i < octaves; i++)
            result[i] = create(rand);
        return result;
    }

    /// <summary>Plain octave sum at one point (each octave's frequency and amplitude multiplied by the given factors).</summary>
    public double Noise(double x, double y, double z, double frequency, double amplitude, bool normalized = false)
    {
        double result = 0, amp = 1, freq = 1, max = 0;
        x *= XScale;
        y *= YScale;
        z *= ZScale;
        foreach (var octave in Octaves)
        {
            result += octave.Noise(x * freq, y * freq, z * freq) * amp;
            max += amp;
            freq *= frequency;
            amp *= amplitude;
        }
        return normalized ? result / max : result;
    }

    /// <summary>2D variant: (x, y) with the third coordinate 0.</summary>
    public double Noise(double x, double y, double frequency, double amplitude, bool normalized = false) =>
        Noise(x, y, 0, frequency, amplitude, normalized);

    public double[] GetFractalBrownianMotion(double x, double z, double lacunarity, double persistence) =>
        GetFractalBrownianMotion(x, 0, z, lacunarity, persistence);

    public virtual double[] GetFractalBrownianMotion(double x, double y, double z, double lacunarity, double persistence)
    {
        var noise = new double[SizeX * SizeY * SizeZ];
        double freq = 1, amp = 1;
        x *= XScale;
        y *= YScale;
        z *= ZScale;
        // Periodic over x and z to avoid precision issues far from the origin.
        foreach (var octave in Octaves)
        {
            var dx = x * freq;
            var dz = z * freq;
            var lx = LFloor(dx);
            var lz = LFloor(dz);
            dx -= lx;
            dz -= lz;
            lx %= 16777216;
            lz %= 16777216;
            dx += lx;
            dz += lz;
            var dy = y * freq;
            octave.GetNoise(noise, dx, dy, dz, SizeX, SizeY, SizeZ, XScale * freq, YScale * freq, ZScale * freq, amp);
            freq *= lacunarity;
            amp *= persistence;
        }
        return noise;
    }

    private static long LFloor(double x) => x >= 0 ? (long)x : (long)x - 1;
}

internal sealed class SimplexOctaveGenerator : PerlinOctaveGenerator
{
    public SimplexOctaveGenerator(JavaRandom rand, int octaves, int sizeX, int sizeZ) : this(rand, octaves, sizeX, 1, sizeZ)
    {
    }

    public SimplexOctaveGenerator(JavaRandom rand, int octaves, int sizeX, int sizeY, int sizeZ)
        : base(CreateOctaves(rand, octaves, r => new SimplexNoise(r)), sizeX, sizeY, sizeZ)
    {
    }

    public SimplexOctaveGenerator(JavaRandom rand, int octaves) : this(rand, octaves, 0, 0, 0)
    {
    }

    public override double[] GetFractalBrownianMotion(double x, double y, double z, double lacunarity, double persistence)
    {
        var noise = new double[SizeX * SizeY * SizeZ];
        double freq = 1, amp = 1;
        foreach (var octave in Octaves)
        {
            octave.GetNoise(noise, x, y, z, SizeX, SizeY, SizeZ, XScale * freq, YScale * freq, ZScale * freq, 0.55D / amp);
            freq *= lacunarity;
            amp *= persistence;
        }
        return noise;
    }
}
