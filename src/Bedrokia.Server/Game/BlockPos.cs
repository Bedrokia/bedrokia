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

namespace Bedrokia.Server.Game;

/// <summary>
/// Integer block coordinates in the world.
/// </summary>
public readonly record struct BlockPos(int X, int Y, int Z)
{
    public static implicit operator BlockPos((int X, int Y, int Z) tuple) => new(tuple.X, tuple.Y, tuple.Z);
    public static implicit operator (int X, int Y, int Z)(BlockPos pos) => (pos.X, pos.Y, pos.Z);

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static long Pack(int x, int y, int z) =>
        (((long)(x & 0x3FFFFFF)) << 38) | (((long)(z & 0x3FFFFFF)) << 12) | ((long)(y & 0xFFF));

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static (int X, int Y, int Z) Unpack(long packed)
    {
        var x = (int)(packed >> 38);
        var z = (int)((packed << 26) >> 38);
        var y = (int)((packed << 52) >> 52);
        return (x, y, z);
    }

    public long Packed => Pack(X, Y, Z);

    public override string ToString() => $"({X}, {Y}, {Z})";
}
