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
/// Flowing water and lava, like vanilla (Dragonfly block/liquid.go, MIT): a liquid falls first, otherwise spreads
/// sideways one level further each block (water 7 blocks, lava 3, lava every 30 ticks and water every 5), dries up
/// when nothing feeds it, two water sources make a third, and water meeting lava makes obsidian or cobblestone.
/// Block states: "liquid_depth" 0 = source, 1-7 = flowing level, 8+ = falling.
/// </summary>
public sealed class Fluids(BedrokiaServer server, World world)
{
    private const int MaxUpdatesPerTick = 2000;

    private readonly SortedDictionary<long, HashSet<(int X, int Y, int Z)>> _due = [];
    private readonly HashSet<(int X, int Y, int Z)> _queued = [];

    public static bool IsLiquid(BlockType? block) => block is not null && block.Name is "minecraft:water" or "minecraft:flowing_water"
        or "minecraft:lava" or "minecraft:flowing_lava";

    private static bool IsWater(BlockType block) => block.Name.EndsWith("water");

    public static int Depth(BlockType block) => block.States.TryGetValue("liquid_depth", out var d) ? Convert.ToInt32(d) : 0;

    public static bool IsSource(BlockType? block) => IsLiquid(block) && Depth(block!) == 0;

    public static BlockType Source(string kind) => Liquid(kind, 0, still: true);

    private static BlockType Liquid(string kind, int depth, bool still = false)
    {
        var name = still ? $"minecraft:{kind}" : $"minecraft:flowing_{kind}";
        return Blocks.ByKey(new BlockType(0, name, new OrderedDictionary<string, object> { ["liquid_depth"] = depth }).Key)
               ?? Blocks.ByName(name) ?? Blocks.Air;
    }

    /// <summary>Something changed at (x, y, z): the liquids there and around must look again.</summary>
    public void Wake(int x, int y, int z)
    {
        foreach (var (dx, dy, dz) in Around)
            if (IsLiquid(world.GetBlock(x + dx, y + dy, z + dz)))
                Schedule(x + dx, y + dy, z + dz);
    }

    private static readonly (int, int, int)[] Around = [(0, 0, 0), (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1)];
    private static readonly (int X, int Z)[] Sides = [(1, 0), (-1, 0), (0, 1), (0, -1)];

    private void Schedule(int x, int y, int z)
    {
        if (!_queued.Add((x, y, z)) || world.GetBlock(x, y, z) is not { } block)
            return;
        // Lava is faster and flows further in the Nether (vanilla: every 10 ticks, 7 blocks).
        var due = server.CurrentTick + (IsWater(block) || !IsLiquid(block) ? 5 : world.Dimension == 1 ? 10 : 30);
        if (!_due.TryGetValue(due, out var set))
            _due[due] = set = [];
        set.Add((x, y, z));
    }

    public void Tick()
    {
        var budget = MaxUpdatesPerTick;
        while (budget > 0 && _due.Count > 0 && _due.First().Key <= server.CurrentTick)
        {
            var (tick, positions) = _due.First();
            _due.Remove(tick);
            foreach (var at in positions)
            {
                _queued.Remove(at);
                Update(at.X, at.Y, at.Z);
                budget--;
            }
        }
    }

    private void Update(int x, int y, int z)
    {
        if (world.GetBlock(x, y, z) is not { } block || !IsLiquid(block))
            return;
        var kind = IsWater(block) ? "water" : "lava";
        var step = kind == "water" || world.Dimension == 1 ? 1 : 2;
        var depth = Depth(block);

        if (depth != 0)
        {
            // A flowing block keeps the level its best neighbour gives it, or dries up.
            var expected = Incoming(x, y, z, kind, step);
            if (expected is null)
            {
                Set(x, y, z, Blocks.Air);
                return;
            }
            if (expected != depth)
            {
                Set(x, y, z, expected == 0 ? Source(kind) : Liquid(kind, expected.Value));
                return; // the new level spreads on its next update
            }
        }

        var level = depth >= 8 ? 0 : depth;
        if (CanFlowInto(x, y - 1, z, kind, 8, out var below))
        {
            Flow(x, y - 1, z, kind, 8, below);
            return; // falling liquid does not spread sideways
        }
        if (level + step > 7)
            return;
        foreach (var (dx, dz) in Sides)
            if (CanFlowInto(x + dx, y, z + dz, kind, level + step, out var target))
                Flow(x + dx, y, z + dz, kind, level + step, target);
    }

    // Lowest level a neighbour gives (x, y, z): from above (falling = 8), from the sides (their level + step);
    // water also becomes a source between two sources over solid ground or water.
    private int? Incoming(int x, int y, int z, string kind, int step)
    {
        if (world.GetBlock(x, y + 1, z) is { } above && IsLiquid(above) && IsWater(above) == (kind == "water"))
            return 8;
        int? best = null;
        var sources = 0;
        foreach (var (dx, dz) in Sides)
        {
            if (world.GetBlock(x + dx, y, z + dz) is not { } side || !IsLiquid(side) || IsWater(side) != (kind == "water"))
                continue;
            var sideDepth = Depth(side);
            if (sideDepth == 0)
                sources++;
            var sideLevel = sideDepth >= 8 ? 0 : sideDepth;
            if (sideLevel + step <= 7)
                best = Math.Min(best ?? 99, sideLevel + step);
        }
        if (kind == "water" && sources >= 2 && (Entities.BlockPhysics.IsSolid(world.GetBlock(x, y - 1, z)) || IsSource(world.GetBlock(x, y - 1, z))))
            return 0;
        return best;
    }

    private bool CanFlowInto(int x, int y, int z, string kind, int depth, out BlockType? target)
    {
        target = world.GetBlock(x, y, z);
        if (target is null || y < Chunk.MinY)
            return false;
        if (IsLiquid(target))
        {
            if (IsWater(target) != (kind == "water"))
                return true; // water meets lava
            var current = Depth(target);
            return current != 0 && (current >= 8 ? 0 : current) > (depth >= 8 ? 0 : depth) && depth < 8;
        }
        return target.IsAir || !Entities.BlockPhysics.IsSolid(target) && !target.Name.EndsWith("_door") && target.Name != "minecraft:ladder";
    }

    private void Flow(int x, int y, int z, string kind, int depth, BlockType? target)
    {
        if (target is not null && IsLiquid(target) && IsWater(target) != (kind == "water"))
        {
            // Water touching lava: a lava source turns to obsidian, flowing lava (or lava onto water) to cobblestone.
            var lavaSource = !IsWater(target) && Depth(target) == 0;
            Set(x, y, z, Blocks.ByName(lavaSource ? "obsidian" : kind == "lava" && y < 0 ? "stone" : "cobblestone")!);
            server.BroadcastIn(world, Protocol.Packets.LevelEvent(1004, x, y, z, 0)); // fizz
            return;
        }
        if (target is not null && !target.IsAir && !IsLiquid(target))
            foreach (var drop in DropRules.Drops(target, null, Random.Shared)) // washed-away plants and torches
                server.DropItem(drop, x + 0.5f, y + 0.5f, z + 0.5f, world: world);
        Set(x, y, z, Liquid(kind, depth));
    }

    private void Set(int x, int y, int z, BlockType block)
    {
        if (!world.SetBlock(x, y, z, block))
            return;
        server.BroadcastIn(world, Protocol.Packets.UpdateBlock(x, y, z, block.NetworkId));
        foreach (var (dx, dy, dz) in Around)
            if (dx != 0 || dy != 0 || dz != 0 || IsLiquid(block))
                if (IsLiquid(world.GetBlock(x + dx, y + dy, z + dz)))
                    Schedule(x + dx, y + dy, z + dz);
    }
}
