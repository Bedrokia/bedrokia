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
/// Rails: their shape (rail_direction 0-9: straight north-south / east-west, four slopes, four curves) set from the
/// rails around when one is placed, and the two exits of each shape for minecarts.
/// </summary>
/// <remarks>ponytail: no redstone: a powered rail is on when a redstone block, a redstone torch or an on lever is next to it.</remarks>
public static class Rails
{
    // Exits of each shape: (dx, dy, dz); dy = 1 is the top of a slope.
    private static readonly (int X, int Y, int Z)[][] ExitsOf =
    [
        [(0, 0, -1), (0, 0, 1)], // 0 north-south
        [(1, 0, 0), (-1, 0, 0)], // 1 east-west
        [(1, 1, 0), (-1, 0, 0)], // 2 ascending east
        [(-1, 1, 0), (1, 0, 0)], // 3 ascending west
        [(0, 1, -1), (0, 0, 1)], // 4 ascending north
        [(0, 1, 1), (0, 0, -1)], // 5 ascending south
        [(0, 0, 1), (1, 0, 0)], // 6 south-east curve
        [(0, 0, 1), (-1, 0, 0)], // 7 south-west curve
        [(0, 0, -1), (-1, 0, 0)], // 8 north-west curve
        [(0, 0, -1), (1, 0, 0)], // 9 north-east curve
    ];

    public static bool IsRail(BlockType? block) => block?.Name is "minecraft:rail" or "minecraft:golden_rail"
        or "minecraft:detector_rail" or "minecraft:activator_rail";

    public static int Shape(BlockType rail) => rail.States.GetValueOrDefault("rail_direction") is int shape and >= 0 and <= 9 ? shape : 0;

    public static (int X, int Y, int Z)[] Exits(int shape) => ExitsOf[Math.Clamp(shape, 0, 9)];

    public static bool IsSlope(int shape) => shape is >= 2 and <= 5;

    public static bool IsCurve(int shape) => shape >= 6;

    /// <summary>A powered rail that is on: its own bit, or a power source next to it or under it.</summary>
    public static bool Powered(World world, int x, int y, int z, BlockType rail)
    {
        if (rail.States.GetValueOrDefault("rail_data_bit") is byte and not 0)
            return true;
        foreach (var (dx, dy, dz) in new[] { (1, 0, 0), (-1, 0, 0), (0, 0, 1), (0, 0, -1), (0, -1, 0), (0, 1, 0) })
            if (world.GetBlock(x + dx, y + dy, z + dz) is { } b && (b.Name is "minecraft:redstone_block" or "minecraft:redstone_torch"
                    || b.Name == "minecraft:lever" && b.States.GetValueOrDefault("open_bit") is byte and not 0))
                return true;
        return false;
    }

    /// <summary>
    /// Gives the rail at (x, y, z) the shape that joins the rails around it (slopes towards a rail one block higher,
    /// curves for plain rails), then bends the neighbours that still have a free end towards it.
    /// </summary>
    public static void Connect(World world, int x, int y, int z, Action<int, int, int, BlockType> set)
    {
        if (world.GetBlock(x, y, z) is not { } rail || !IsRail(rail))
            return;
        var neighbours = Neighbours(world, x, y, z);
        foreach (var n in neighbours.Where(n => !HasTwoConnections(world, n.X, n.Y, n.Z, (x, y, z))).ToList())
            Reshape(world, n.X, n.Y, n.Z, set);
        Reshape(world, x, y, z, set);
    }

    private static void Reshape(World world, int x, int y, int z, Action<int, int, int, BlockType> set)
    {
        if (world.GetBlock(x, y, z) is not { } rail || !IsRail(rail))
            return;
        var around = Neighbours(world, x, y, z);
        bool Has(int dx, int dz) => around.Any(n => n.X - x == dx && n.Z - z == dz);
        bool Up(int dx, int dz) => around.Any(n => n.X - x == dx && n.Z - z == dz && n.Y == y + 1);
        bool north = Has(0, -1), south = Has(0, 1), east = Has(1, 0), west = Has(-1, 0);
        var plain = rail.Name == "minecraft:rail";
        int? shape = null;
        if (plain && (north || south) && (east || west))
            shape = south && east ? 6 : south && west ? 7 : north && west ? 8 : 9;
        else if (north || south)
            shape = Up(0, -1) ? 4 : Up(0, 1) ? 5 : 0;
        else if (east || west)
            shape = Up(1, 0) ? 2 : Up(-1, 0) ? 3 : 1;
        if (shape is not { } s || s == Shape(rail))
            return;
        var states = new OrderedDictionary<string, object>(rail.States) { ["rail_direction"] = s };
        set(x, y, z, Blocks.Get(rail.Name["minecraft:".Length..], [.. states.Select(kv => (kv.Key, kv.Value))]));
    }

    // Rails on the four sides, at the same height or one block above or below.
    private static List<(int X, int Y, int Z)> Neighbours(World world, int x, int y, int z)
    {
        var found = new List<(int, int, int)>();
        foreach (var (dx, dz) in new[] { (0, -1), (0, 1), (1, 0), (-1, 0) })
            foreach (var dy in new[] { 0, 1, -1 })
                if (IsRail(world.GetBlock(x + dx, y + dy, z + dz)))
                {
                    found.Add((x + dx, y + dy, z + dz));
                    break;
                }
        return found;
    }

    // Both ends of this rail already lead to another rail (not counting the one at except): leave it as it is.
    private static bool HasTwoConnections(World world, int x, int y, int z, (int X, int Y, int Z) except)
    {
        if (world.GetBlock(x, y, z) is not { } rail)
            return true;
        var connected = 0;
        foreach (var (dx, dy, dz) in Exits(Shape(rail)))
        {
            if ((x + dx, z + dz) == (except.X, except.Z))
                continue;
            if (IsRail(world.GetBlock(x + dx, y + dy, z + dz)) || IsRail(world.GetBlock(x + dx, y + dy - 1, z + dz)))
                connected++;
        }
        return connected >= 2;
    }
}
