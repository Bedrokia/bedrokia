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

using Bedrokia.Protocol;
using Bedrokia.Server.Entities;
using Bedrokia.Server.Game;
using Bedrokia.Server.Game.Generation;
using Bedrokia.Server.Network;

namespace Bedrokia.Server;

/// <summary>
/// Living blocks: scheduled block updates, flowing water and lava, random ticks (crops, saplings, sugar cane,
/// cactus, stems), bone meal, primed TNT and explosions. Rules from vanilla as implemented by Dragonfly (MIT)
/// block/liquid.go, block/crop.go, block/sapling.go, block/tnt.go and world/explosion.go.
/// Every operation takes the world it happens in (the overworld when none is given).
/// </summary>
public sealed partial class BedrokiaServer
{
    private readonly SortedDictionary<long, List<Action>> _scheduled = [];
    private readonly Random _blockRandom = new();

    /// <summary>Runs <paramref name="action"/> on the game thread in <paramref name="ticks"/> ticks.</summary>
    public void Schedule(int ticks, Action action)
    {
        var due = CurrentTick + Math.Max(1, ticks);
        if (!_scheduled.TryGetValue(due, out var list))
            _scheduled[due] = list = [];
        list.Add(action);
    }

    /// <summary>Sets a block, tells the players of that world, and wakes the liquids and walls around it.</summary>
    public void SetBlockAndUpdate(int x, int y, int z, BlockType block, World? world = null)
    {
        world ??= World;
        if (!world.SetBlock(x, y, z, block))
            return;
        BroadcastIn(world, Packets.UpdateBlock(x, y, z, block.NetworkId));
        world.Fluids.Wake(x, y, z);
        UpdateShapes(x, y, z, world);
    }

    /// <summary>A block changed at (x, y, z): walls there and around reshape their connections.</summary>
    public void UpdateShapes(int x, int y, int z, World? world = null)
    {
        world ??= World;
        foreach (var (dx, dy, dz) in new[] { (0, 0, 0), (1, 0, 0), (-1, 0, 0), (0, 0, 1), (0, 0, -1), (0, -1, 0) })
        {
            int wx = x + dx, wy = y + dy, wz = z + dz;
            if (world.GetBlock(wx, wy, wz) is not { } wall || !Walls.IsWall(wall))
                continue;
            var shaped = Walls.Connect(wall, world.GetBlock, wx, wy, wz);
            if (shaped != wall && world.SetBlock(wx, wy, wz, shaped))
                BroadcastIn(world, Packets.UpdateBlock(wx, wy, wz, shaped.NetworkId));
        }
    }

    private void TickBlocks()
    {
        while (_scheduled.Count > 0 && _scheduled.First().Key <= CurrentTick)
        {
            var (due, actions) = _scheduled.First();
            _scheduled.Remove(due);
            foreach (var action in actions)
                action();
        }
        foreach (var world in Worlds)
        {
            world.Fluids.Tick();
            RandomTicks(world);
        }
        TickTnt();
    }

    // --- Random ticks: 3 random blocks per sub-chunk per tick, in chunks near players (vanilla randomTickSpeed 3) ---

    private void RandomTicks(World world)
    {
        var chunks = new HashSet<(int, int)>();
        foreach (var player in _players.Where(p => p.IsInGame && p.World == world))
        {
            var (cx, cz) = player.ChunkPosition;
            for (var dx = -6; dx <= 6; dx++)
            for (var dz = -6; dz <= 6; dz++)
                chunks.Add((cx + dx, cz + dz));
        }
        foreach (var (cx, cz) in chunks)
        {
            if (world.LoadedChunk(cx, cz) is not { } chunk)
                continue;
            for (var section = 0; section < Chunk.Sections; section++)
            for (var i = 0; i < 3; i++)
            {
                var r = _blockRandom.Next(4096);
                int x = r & 15, y = Chunk.MinY + section * 16 + (r >> 8), z = r >> 4 & 15;
                var block = chunk.GetBlock(x, y, z);
                if (!block.IsAir)
                    Grow(cx * 16 + x, y, cz * 16 + z, boneMeal: false, world);
            }
        }
    }

    /// <summary>One growth step (random tick) or a bone meal use; returns true if something grew.</summary>
    public bool Grow(int x, int y, int z, bool boneMeal, World? world = null)
    {
        world ??= World;
        if (world.GetBlock(x, y, z) is not { } block || !block.Name.StartsWith("minecraft:"))
            return false;
        var name = block.Name["minecraft:".Length..];
        if (name is "wheat" or "carrots" or "potatoes" or "beetroot" or "pumpkin_stem" or "melon_stem"
            && block.States.TryGetValue("growth", out var g))
        {
            var growth = Convert.ToInt32(g);
            if (growth >= 7)
            {
                if (name.EndsWith("_stem") && !boneMeal && _blockRandom.Next(3) == 0)
                    return GrowFruit(x, y, z, name == "pumpkin_stem" ? "pumpkin" : "melon_block", world);
                return false;
            }
            if (!boneMeal && _blockRandom.Next(3) != 0)
                return false; // ponytail: vanilla's chance also depends on farmland moisture and neighbours
            SetBlockAndUpdate(x, y, z, BedrockConnection.With(block, "growth", Math.Min(7, growth + (boneMeal ? 2 + _blockRandom.Next(4) : 1))), world);
            return true;
        }
        if (name.EndsWith("_sapling"))
        {
            if (!boneMeal && _blockRandom.Next(7) != 0 || boneMeal && _blockRandom.Next(100) >= 45)
                return boneMeal; // bone meal is used up even when the tree does not grow yet
            GrowTree(x, y, z, name, world);
            return true;
        }
        if (name is "reeds" or "cactus" && !boneMeal)
        {
            var height = 1;
            while (height < 3 && world.GetBlock(x, y - height, z)?.Name == block.Name)
                height++;
            if (height >= 3 || world.GetBlock(x, y + 1, z) is not { IsAir: true } || _blockRandom.Next(4) != 0)
                return false;
            SetBlockAndUpdate(x, y + 1, z, Blocks.ByName(name)!, world);
            return true;
        }
        if (name == "grass_block" && boneMeal)
        {
            for (var i = 0; i < 12; i++)
            {
                int gx = x + _blockRandom.Next(7) - 3, gz = z + _blockRandom.Next(7) - 3;
                if (world.GetBlock(gx, y, gz)?.Name == "minecraft:grass_block" && world.GetBlock(gx, y + 1, gz) is { IsAir: true })
                    SetBlockAndUpdate(gx, y + 1, gz, _blockRandom.Next(8) == 0 ? Blocks.Dandelion : Blocks.ShortGrass, world);
            }
            return true;
        }
        return false;
    }

    private bool GrowFruit(int x, int y, int z, string fruit, World world)
    {
        var (dx, dz) = _blockRandom.Next(4) switch { 0 => (1, 0), 1 => (-1, 0), 2 => (0, 1), _ => (0, -1) };
        if (world.GetBlock(x + dx, y, z + dz) is not { IsAir: true } || world.GetBlock(x + dx, y - 1, z + dz)?.Name is not
                ("minecraft:dirt" or "minecraft:grass_block" or "minecraft:farmland"))
            return false;
        SetBlockAndUpdate(x + dx, y, z + dz, Blocks.ByName(fruit)!, world);
        return true;
    }

    // Grows a tree with the world generator's shapes, then sends every block that changed.
    private void GrowTree(int x, int y, int z, string sapling, World world)
    {
        var biome = sapling switch
        {
            "spruce_sapling" => 5, "birch_sapling" => 27, "jungle_sapling" => 21, "acacia_sapling" => 35, "dark_oak_sapling" => 29, _ => 4,
        };
        world.SetBlock(x, y, z, Blocks.Air);
        const int radius = 5, height = 32;
        var before = new Dictionary<(int, int, int), BlockType>();
        for (var bx = x - radius; bx <= x + radius; bx++)
        for (var bz = z - radius; bz <= z + radius; bz++)
        for (var by = y - 1; by <= y + height; by++)
            if (world.GetBlock(bx, by, bz) is { } b)
                before[(bx, by, bz)] = b;
        for (var cx = (x - radius) >> 4; cx <= (x + radius) >> 4; cx++)
        for (var cz = (z - radius) >> 4; cz <= (z + radius) >> 4; cz++)
            if (world.LoadedChunk(cx, cz) is { } chunk)
            {
                Trees.Place(new ChunkWriter(chunk, cx * 16, cz * 16), x, y, z, biome, 0.1, 0.6);
                chunk.Dirty = true;
            }
        foreach (var ((bx, by, bz), old) in before)
            if (world.GetBlock(bx, by, bz) is { } now && now != old)
                BroadcastIn(world, Packets.UpdateBlock(bx, by, bz, now.NetworkId));
        if (world.GetBlock(x, y, z) is { IsAir: true })
            BroadcastIn(world, Packets.UpdateBlock(x, y, z, Blocks.Air.NetworkId));
    }

    // --- TNT and explosions ---

    private readonly List<(long EntityId, World World, int X, int Y, int Z, long Explodes)> _tnt = [];

    /// <summary>A lit TNT block: a "minecraft:tnt" entity that explodes 4 seconds later.</summary>
    public void PrimeTnt(int x, int y, int z, int fuse = 80, World? world = null)
    {
        world ??= World;
        var id = NextEntityId();
        _tnt.Add((id, world, x, y, z, CurrentTick + fuse));
        BroadcastIn(world, EntityPackets.AddActor(id, "minecraft:tnt", x + 0.5, y, z + 0.5, 0, 1, 1, 0, 0.98f, 0.98f,
            EntityPackets.FlagGravity | EntityPackets.FlagHasCollision | (1L << 10))); // 10: ignited
    }

    private void TickTnt()
    {
        foreach (var tnt in _tnt.Where(t => t.Explodes <= CurrentTick).ToList())
        {
            _tnt.Remove(tnt);
            BroadcastIn(tnt.World, Packets.RemoveActor(tnt.EntityId));
            Explode(tnt.X + 0.5, tnt.Y + 0.5, tnt.Z + 0.5, 4, tnt.World);
        }
    }

    /// <summary>
    /// Destroys blocks in a sphere of <paramref name="radius"/> (not bedrock and other unbreakable blocks), drops
    /// some of them, lights nearby TNT, and hurts players and mobs around. Particles for the players of that world.
    /// </summary>
    public void Explode(double x, double y, double z, int radius, World? world = null)
    {
        world ??= World;
        int cx = (int)Math.Floor(x), cy = (int)Math.Floor(y), cz = (int)Math.Floor(z);
        for (var bx = cx - radius; bx <= cx + radius; bx++)
        for (var by = cy - radius; by <= cy + radius; by++)
        for (var bz = cz - radius; bz <= cz + radius; bz++)
        {
            var d2 = (bx - cx) * (bx - cx) + (by - cy) * (by - cy) + (bz - cz) * (bz - cz);
            if (d2 > radius * radius + _blockRandom.Next(3))
                continue;
            if (world.GetBlock(bx, by, bz) is not { IsAir: false } block || block.Unbreakable || Fluids.IsLiquid(block)
                || block.Name is "minecraft:obsidian" or "minecraft:crying_obsidian")
                continue;
            if (world.GetBlockEntity(bx, by, bz)?.Items is { } contents)
                for (var slot = 0; slot < contents.Size; slot++)
                    if (contents[slot] is { } stack)
                        DropItem(stack, bx + 0.5f, by + 0.5f, bz + 0.5f, world: world);
            SetBlockAndUpdate(bx, by, bz, Blocks.Air, world);
            if (block.Name == "minecraft:tnt")
                PrimeTnt(bx, by, bz, 10 + _blockRandom.Next(20), world);
            else if (_blockRandom.Next(4) == 0)
                foreach (var drop in DropRules.Drops(block, null, Random.Shared))
                    DropItem(drop, bx + 0.5f, by + 0.5f, bz + 0.5f, world: world);
        }
        BroadcastIn(world, Packets.LevelEvent(2025, cx, cy, cz, 0));
        for (var bx = cx - radius; bx <= cx + radius; bx++)
        for (var bz = cz - radius; bz <= cz + radius; bz++)
        for (var by = cy - radius; by <= cy + radius + 1; by++)
            ApplyGravity(bx, by, bz, world);
        var reach = radius * 2;
        foreach (var player in _players.Where(p => p.IsInGame && p.World == world))
        {
            var dx = player.Position.X - x;
            var dy = player.Position.Y - BedrockConnection.EyeHeight - y;
            var dz = player.Position.Z - z;
            var distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (distance < reach)
                player.Damage((float)Math.Round(7 * radius * (1 - distance / reach)), x, z, source: DamageSource.Explosion());
        }
        if (world != World)
            return; // ponytail: mobs only live in the overworld for now
        foreach (var mob in Mobs.All.ToList())
        {
            var distance = Math.Sqrt((mob.X - x) * (mob.X - x) + (mob.Y - y) * (mob.Y - y) + (mob.Z - z) * (mob.Z - z));
            if (distance < reach)
                Mobs.Damage(mob, (float)Math.Round(7 * radius * (1 - distance / reach)), (x, z), source: DamageSource.Explosion());
        }
    }
}
