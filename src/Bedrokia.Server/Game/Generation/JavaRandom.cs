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

namespace Bedrokia.Server.Game.Generation;

/// <summary>
/// The 48-bit linear congruential generator specified by java.util.Random, so generator code ported from
/// Java (Glowstone, MIT) gives the same numbers for the same seed.
/// </summary>
public sealed class JavaRandom
{
    private const long Multiplier = 0x5DEECE66DL, Addend = 0xBL, Mask = (1L << 48) - 1;
    private long _seed;
    private double? _nextGaussian;

    public JavaRandom(long seed) => SetSeed(seed);

    public void SetSeed(long seed)
    {
        _seed = (seed ^ Multiplier) & Mask;
        _nextGaussian = null;
    }

    private int Next(int bits)
    {
        _seed = unchecked(_seed * Multiplier + Addend) & Mask;
        return (int)((ulong)_seed >> (48 - bits));
    }

    public int NextInt() => Next(32);

    public int NextInt(int bound)
    {
        if (bound <= 0)
            throw new ArgumentOutOfRangeException(nameof(bound));
        if ((bound & -bound) == bound)
            return (int)((bound * (long)Next(31)) >> 31);
        int bits, value;
        do
        {
            bits = Next(31);
            value = bits % bound;
        } while (bits - value + (bound - 1) < 0);
        return value;
    }

    public long NextLong() => unchecked(((long)Next(32) << 32) + Next(32));

    public bool NextBoolean() => Next(1) != 0;

    public float NextFloat() => Next(24) / (float)(1 << 24);

    public double NextDouble() => (((long)Next(26) << 27) + Next(27)) * (1.0 / (1L << 53));

    public double NextGaussian()
    {
        if (_nextGaussian is { } cached)
        {
            _nextGaussian = null;
            return cached;
        }
        double v1, v2, s;
        do
        {
            v1 = 2 * NextDouble() - 1;
            v2 = 2 * NextDouble() - 1;
            s = v1 * v1 + v2 * v2;
        } while (s >= 1 || s == 0);
        var multiplier = Math.Sqrt(-2 * Math.Log(s) / s);
        _nextGaussian = v2 * multiplier;
        return v1 * multiplier;
    }
}

/// <summary>
/// Block access for world generation. Coordinates are world coordinates; reads outside the generated area return
/// null and writes there are ignored.
/// </summary>
public interface IBlockAccess
{
    BlockType? GetBlock(int x, int y, int z);
    void SetBlock(int x, int y, int z, BlockType block);

    /// <summary>Legacy biome id (see <see cref="VanillaBiomes"/>) of the column.</summary>
    int GetBiome(int x, int z);

    /// <summary>Y of the first air block above the highest non-air block of the column.</summary>
    int GetHighestBlockY(int x, int z);
}
