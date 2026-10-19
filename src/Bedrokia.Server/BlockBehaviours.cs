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

using Bedrokia.Server.Entities;
using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server;

/// <summary>
/// Blocks that change by themselves: leaf decay, farmland drying, grass and mycelium spreading, nether wart growing,
/// and fire (burning out, burning blocks, spreading). Rules and values from Dragonfly (MIT) block/leaves.go,
/// farmland.go, grass.go, nether_wart.go, fire.go and the flammability of each block.
/// </summary>
public sealed partial class BedrokiaServer
{
    private static readonly (int X, int Y, int Z)[] Faces = [(1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1)];

    // Fires waiting for their next tick, so a fire is never scheduled twice.
    private readonly HashSet<(World, int, int, int)> _burning = [];

    /// <summary>Random tick of the blocks handled here; false if <paramref name="block"/> is not one of them.</summary>
    private bool RandomTickBlock(World world, int x, int y, int z, BlockType block)
    {
        var name = block.Name;
        if (name.Contains("leaves") && block.States.TryGetValue("persistent_bit", out var persistent))
        {
            if (Convert.ToInt32(persistent) == 0 && !LogNearby(world, x, y, z))
                DecayLeaves(world, x, y, z, block);
            return true;
        }
        switch (name)
        {
            case "minecraft:farmland":
                TickFarmland(world, x, y, z, block);
                return true;
            case "minecraft:grass_block" or "minecraft:mycelium":
                SpreadGrass(world, x, y, z, block);
                return true;
            case "minecraft:nether_wart" when block.States.TryGetValue("age", out var age) && Convert.ToInt32(age) < 3:
                if (_blockRandom.Next(10) == 0)
                    SetBlockAndUpdate(x, y, z, BedrockConnection.With(block, "age", Convert.ToInt32(age) + 1), world);
                return true;
            case "minecraft:fire":
                ScheduleFire(world, x, y, z); // fires from a saved world start burning again
                return true;
        }
        return false;
    }

    // --- Leaves: without a log within 6 blocks (through other leaves, as in Java; the generated trees need it), they decay ---

    private static bool LogNearby(World world, int x, int y, int z)
    {
        var queue = new Queue<(int X, int Y, int Z, int Distance)>();
        var seen = new HashSet<(int, int, int)> { (x, y, z) };
        queue.Enqueue((x, y, z, 0));
        while (queue.Count > 0)
        {
            var (cx, cy, cz, distance) = queue.Dequeue();
            foreach (var (dx, dy, dz) in Faces)
            {
                int nx = cx + dx, ny = cy + dy, nz = cz + dz;
                if (!seen.Add((nx, ny, nz)) || world.GetBlock(nx, ny, nz) is not { } next)
                    continue;
                if (IsLog(next.Name))
                    return true;
                if (distance + 1 < 6 && next.Name.Contains("leaves"))
                    queue.Enqueue((nx, ny, nz, distance + 1));
            }
        }
        return false;
    }

    private static bool IsLog(string name) =>
        name.EndsWith("_log") || name.EndsWith("_wood") || name is "minecraft:crimson_stem" or "minecraft:warped_stem"
            or "minecraft:mangrove_roots" or "minecraft:muddy_mangrove_roots";

    private void DecayLeaves(World world, int x, int y, int z, BlockType block)
    {
        SetBlockAndUpdate(x, y, z, Blocks.Air, world);
        foreach (var drop in DropRules.Drops(block, null, Random.Shared))
            DropItem(drop, x + 0.5f, y + 0.25f, z + 0.5f, world: world);
    }

    // --- Farmland: wet near water (4 blocks around, same level or one above), otherwise dries, then turns to dirt ---

    private void TickFarmland(World world, int x, int y, int z, BlockType block)
    {
        var moisture = block.States.TryGetValue("moisturized_amount", out var m) ? Convert.ToInt32(m) : 0;
        if (WaterNear(world, x, y, z))
        {
            if (moisture < 7)
                SetBlockAndUpdate(x, y, z, BedrockConnection.With(block, "moisturized_amount", 7), world);
        }
        else if (moisture > 0)
            SetBlockAndUpdate(x, y, z, BedrockConnection.With(block, "moisturized_amount", moisture - 1), world);
        else if (!IsCrop(world.GetBlock(x, y + 1, z)))
            SetBlockAndUpdate(x, y, z, Blocks.Dirt, world);
    }

    private static bool WaterNear(World world, int x, int y, int z)
    {
        for (var dx = -4; dx <= 4; dx++)
        for (var dz = -4; dz <= 4; dz++)
        for (var dy = 0; dy <= 1; dy++)
            if (world.GetBlock(x + dx, y + dy, z + dz)?.Name is "minecraft:water" or "minecraft:flowing_water")
                return true;
        return false;
    }

    private static bool IsCrop(BlockType? block) => block?.Name is "minecraft:wheat" or "minecraft:carrots" or "minecraft:potatoes"
        or "minecraft:beetroot" or "minecraft:pumpkin_stem" or "minecraft:melon_stem" or "minecraft:torchflower_crop"
        or "minecraft:pitcher_crop";

    // --- Grass and mycelium: die under a solid block, otherwise spread onto dirt nearby ---
    // ponytail: no light engine, so the light conditions of vanilla are not checked.

    private void SpreadGrass(World world, int x, int y, int z, BlockType block)
    {
        if (Covered(world.GetBlock(x, y + 1, z)))
        {
            SetBlockAndUpdate(x, y, z, Blocks.Dirt, world);
            return;
        }
        for (var i = 0; i < 4; i++)
        {
            int sx = x + _blockRandom.Next(3) - 1, sy = y + _blockRandom.Next(5) - 3, sz = z + _blockRandom.Next(3) - 1;
            if (world.GetBlock(sx, sy, sz) == Blocks.Dirt && !Covered(world.GetBlock(sx, sy + 1, sz)))
                SetBlockAndUpdate(sx, sy, sz, block, world);
        }
    }

    private static bool Covered(BlockType? above) => BlockPhysics.IsSolid(above) && !above!.Name.Contains("leaves")
        && !above.Name.Contains("glass") && !above.Name.EndsWith("_slab") && !above.Name.EndsWith("_stairs") && !above.Name.EndsWith("_fence");

    // --- Fire ---

    private static readonly string[] Woods =
        ["oak", "spruce", "birch", "jungle", "acacia", "dark_oak", "mangrove", "cherry", "pale_oak", "bamboo"];

    /// <summary>How readily fire spreads to a block (encouragement) and burns it (flammability); (0, 0) if it does not burn.</summary>
    internal static (int Encouragement, int Flammability) Flammability(BlockType? block)
    {
        if (block?.Name is not { } full || !full.StartsWith("minecraft:"))
            return (0, 0);
        var name = full["minecraft:".Length..];
        if (name.Contains("leaves") || name.EndsWith("_wool") || name == "dried_kelp_block")
            return (30, 60);
        if (name.EndsWith("_carpet") || name is "hay_block")
            return (60, 20);
        if (name is "short_grass" or "tall_grass" or "fern" or "large_fern" or "deadbush" or "dandelion" or "poppy"
            or "blue_orchid" or "allium" or "azure_bluet" or "oxeye_daisy" or "cornflower" or "lily_of_the_valley"
            or "sunflower" or "lilac" or "rose_bush" or "peony" || name.EndsWith("_tulip"))
            return (60, 100);
        if (name is "vine" or "tnt")
            return (15, 100);
        if (name is "bookshelf" or "chiseled_bookshelf")
            return (30, 20);
        if (name is "coal_block")
            return (5, 5);
        if (name == "scaffolding")
            return (60, 60);
        var wooden = Woods.Any(w => name.StartsWith(w + "_") || name.StartsWith("stripped_" + w + "_"));
        if (wooden && (name.EndsWith("_log") || name.EndsWith("_wood")))
            return (5, 5);
        if (wooden && (name.EndsWith("_planks") || name.EndsWith("_slab") || name.EndsWith("_stairs") || name.EndsWith("_fence")
                       || name.EndsWith("_fence_gate")))
            return (5, 20);
        return (0, 0);
    }

    private static bool Burns(BlockType? block) => Flammability(block).Flammability > 0;

    internal void ScheduleFire(World world, int x, int y, int z)
    {
        if (_burning.Add((world, x, y, z)))
            Schedule(30 + _blockRandom.Next(10), () => FireTick(world, x, y, z));
    }

    private void FireTick(World world, int x, int y, int z)
    {
        _burning.Remove((world, x, y, z));
        if (world.GetBlock(x, y, z) is not { Name: "minecraft:fire" } fire)
            return;
        var below = world.GetBlock(x, y - 1, z);
        var infinite = below?.Name is "minecraft:netherrack" or "minecraft:magma" || below?.Name == "minecraft:bedrock" && world.Dimension == 1;
        var age = fire.States.TryGetValue("age", out var a) ? Convert.ToInt32(a) : 0;

        if (!infinite)
        {
            var fuelAround = Faces.Any(f => Burns(world.GetBlock(x + f.X, y + f.Y, z + f.Z)));
            if (!fuelAround)
            {
                if (!BlockPhysics.IsSolid(below) || age > 3)
                {
                    SetBlockAndUpdate(x, y, z, Blocks.Air, world);
                    return;
                }
            }
            else if (!Burns(below) && age == 15 && _blockRandom.Next(4) == 0)
            {
                SetBlockAndUpdate(x, y, z, Blocks.Air, world);
                return;
            }
        }
        var newAge = Math.Min(15, age + _blockRandom.Next(3) / 2);
        if (newAge != age)
            world.SetBlock(x, y, z, fire = BedrockConnection.With(fire, "age", newAge)); // invisible to the client

        // Burn the blocks touching the fire.
        foreach (var (dx, dy, dz) in Faces)
            BurnBlock(world, x + dx, y + dy, z + dz, dy == 0 ? 300 : 250, newAge);

        // Spread into the air around (3x6x3) next to something that burns.
        var difficulty = (int)Config.Difficulty;
        for (var dx = -1; dx <= 1; dx++)
        for (var dz = -1; dz <= 1; dz++)
        for (var dy = -1; dy <= 4; dy++)
        {
            if (dx == 0 && dy == 0 && dz == 0 || world.GetBlock(x + dx, y + dy, z + dz) is not { IsAir: true })
                continue;
            var encouragement = Faces.Max(f => Flammability(world.GetBlock(x + dx + f.X, y + dy + f.Y, z + dz + f.Z)).Encouragement);
            if (encouragement <= 0)
                continue;
            var rate = dy > 1 ? 100 + (dy - 1) * 100 : 100;
            var chance = (encouragement + 40 + difficulty * 7) / (newAge + 30);
            if (chance > 0 && _blockRandom.Next(rate) <= chance)
                SetFire(world, x + dx, y + dy, z + dz, Math.Min(15, newAge + _blockRandom.Next(5) / 4));
        }
        ScheduleFire(world, x, y, z);
    }

    private void BurnBlock(World world, int x, int y, int z, int chanceBound, int age)
    {
        var block = world.GetBlock(x, y, z);
        var flammability = Flammability(block).Flammability;
        if (flammability <= 0 || _blockRandom.Next(chanceBound) >= flammability)
            return;
        if (block!.Name == "minecraft:tnt")
        {
            SetBlockAndUpdate(x, y, z, Blocks.Air, world);
            PrimeTnt(x, y, z, world: world);
            return;
        }
        if (_blockRandom.Next(age + 10) < 5)
            SetFire(world, x, y, z, Math.Min(15, age + _blockRandom.Next(5) / 4));
        else
            SetBlockAndUpdate(x, y, z, Blocks.Air, world);
    }

    private void SetFire(World world, int x, int y, int z, int age)
    {
        if (Blocks.ByName("fire") is not { } fire)
            return;
        SetBlockAndUpdate(x, y, z, BedrockConnection.With(fire, "age", age), world);
    }
}
