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
using Bedrokia.Server.Events;
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
        var old = world.GetBlock(x, y, z);
        if (!world.SetBlock(x, y, z, block))
            return;
        if (old is not null)
            LeafSupportChanged(world, x, y, z, old, block);
        BroadcastIn(world, Packets.UpdateBlock(x, y, z, block.NetworkId));
        world.Fluids.Wake(x, y, z);
        UpdateShapes(x, y, z, world);
        CheckSupport(x, y + 1, z, world);
        if (block.Name == "minecraft:fire")
            ScheduleFire(world, x, y, z);
    }

    /// <summary>
    /// The block at (x, y, z) may have lost what holds it (the block under it changed): it breaks with its drops, and
    /// so on up the column (sugar cane, cactus, bamboo, kelp...).
    /// </summary>
    public void CheckSupport(int x, int y, int z, World? world = null)
    {
        world ??= World;
        for (var cy = y; cy <= Chunk.MaxY; cy++)
        {
            if (world.GetBlock(x, cy, z) is not { } block || !Support.Unsupported(block, world.GetBlock(x, cy - 1, z)))
                return;
            // The top half of a door or tall plant drops nothing: its lower half already did.
            if (block.States.GetValueOrDefault("upper_block_bit") is not (byte and not 0))
                foreach (var drop in DropRules.Drops(block, null, _blockRandom))
                    DropItem(drop, x + 0.5f, cy + 0.25f, z + 0.5f, world: world);
            world.SetBlock(x, cy, z, Blocks.Air);
            BroadcastIn(world, Packets.LevelEvent(2001, x, cy, z, unchecked((int)block.NetworkId))); // break particles
            BroadcastIn(world, Packets.UpdateBlock(x, cy, z, Blocks.Air.NetworkId));
            world.Fluids.Wake(x, cy, z);
        }
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
        TickLeafDecay();
    }

    // --- Random ticks: 3 random blocks per sub-chunk per tick, in chunks near players (vanilla randomTickSpeed 3) ---

    private readonly HashSet<(int, int)> _randomTickChunks = [];

    private void RandomTicks(World world)
    {
        var chunks = _randomTickChunks;
        chunks.Clear();
        foreach (var player in _players)
        {
            if (!player.IsInGame || player.World != world)
                continue;
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
            {
                if (chunk.SectionEmpty(section))
                    continue;
                for (var i = 0; i < 3; i++)
                {
                    var r = _blockRandom.Next(4096);
                    int x = r & 15, y = Chunk.MinY + section * 16 + (r >> 8), z = r >> 4 & 15;
                    var block = chunk.GetBlock(x, y, z);
                    if (!RandomlyTicked(block))
                        continue; // stone, dirt, air...: nearly every pick
                    if (!RandomTickBlock(world, cx * 16 + x, y, cz * 16 + z, block))
                        Grow(cx * 16 + x, y, cz * 16 + z, boneMeal: false, world);
                }
            }
        }
    }

    // --- Leaves decay (Bedrock: only after a block near them changed, not on every random tick) ---

    private readonly List<(World World, int X, int Y, int Z, long Due)> _leafChecks = [];
    private readonly HashSet<(World, int, int, int)> _leafQueued = [];

    /// <summary>A log or a leaf at (x, y, z) went away: the natural leaves around check, a little later, whether they still hold to a log.</summary>
    internal void LeafSupportChanged(World world, int x, int y, int z, BlockType old, BlockType now)
    {
        var wasWood = old.Name.Contains("leaves") || IsLog(old.Name);
        if (!wasWood || now.Name == old.Name)
            return;
        for (var dx = -4; dx <= 4; dx++)
        for (var dy = -4; dy <= 4; dy++)
        for (var dz = -4; dz <= 4; dz++)
        {
            int lx = x + dx, ly = y + dy, lz = z + dz;
            if (world.GetBlock(lx, ly, lz) is not { } leaf || !leaf.Name.Contains("leaves")
                || leaf.States.GetValueOrDefault("persistent_bit") is not (byte and 0) || !_leafQueued.Add((world, lx, ly, lz)))
                continue;
            _leafChecks.Add((world, lx, ly, lz, CurrentTick + 20 + _blockRandom.Next(200))); // they fall one by one
        }
    }

    // At most 64 checks a tick, the due ones.
    private void TickLeafDecay()
    {
        var done = 0;
        for (var i = _leafChecks.Count - 1; i >= 0 && done < 64; i--)
        {
            var (world, x, y, z, due) = _leafChecks[i];
            if (due > CurrentTick)
                continue;
            _leafChecks.RemoveAt(i);
            _leafQueued.Remove((world, x, y, z));
            done++;
            if (world.GetBlock(x, y, z) is { } leaf && leaf.Name.Contains("leaves") && !LogNearby(world, x, y, z))
                DecayLeaves(world, x, y, z, leaf);
        }
    }

    // Per block state: does a random tick do anything to it? Worked out once per state (0 unknown, 1 no, 2 yes).
    private static byte[] _randomlyTicked = [];

    private static bool RandomlyTicked(BlockType block)
    {
        if (block.Id >= _randomlyTicked.Length)
            Array.Resize(ref _randomlyTicked, Math.Max(Blocks.Count, block.Id + 1));
        ref var known = ref _randomlyTicked[block.Id];
        if (known == 0)
        {
            var name = block.Name;
            var ticked = name is "minecraft:farmland" or "minecraft:grass_block" or "minecraft:mycelium" or "minecraft:nether_wart"
                             or "minecraft:fire" or "minecraft:wheat" or "minecraft:carrots" or "minecraft:potatoes" or "minecraft:beetroot"
                             or "minecraft:pumpkin_stem" or "minecraft:melon_stem" or "minecraft:reeds" or "minecraft:cactus"
                         || name.EndsWith("_sapling");
            known = ticked ? (byte)2 : (byte)1;
        }
        return known == 2;
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
            var newBlock = BedrockConnection.With(block, "growth", Math.Min(7, growth + (boneMeal ? 2 + _blockRandom.Next(4) : 1)));
            if (Events.HasBlockGrow)
            {
                var growEv = new BlockGrowEvent(new BlockPos(x, y, z), block, newBlock);
                if (!Events.OnBlockGrow(growEv))
                    return false;
                newBlock = growEv.NewBlock;
            }
            SetBlockAndUpdate(x, y, z, newBlock, world);
            return true;
        }
        if (name.EndsWith("_sapling"))
        {
            if (!boneMeal && _blockRandom.Next(7) != 0 || boneMeal && _blockRandom.Next(100) >= 45)
                return boneMeal; // bone meal is used up even when the tree does not grow yet
            if (Events.HasBlockGrow)
            {
                var growEv = new BlockGrowEvent(new BlockPos(x, y, z), block, Blocks.Air);
                if (!Events.OnBlockGrow(growEv))
                    return false;
            }
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
            var newBlock = Blocks.ByName(name)!;
            if (Events.HasBlockGrow)
            {
                var growEv = new BlockGrowEvent(new BlockPos(x, y + 1, z), Blocks.Air, newBlock);
                if (!Events.OnBlockGrow(growEv))
                    return false;
                newBlock = growEv.NewBlock;
            }
            SetBlockAndUpdate(x, y + 1, z, newBlock, world);
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
        var fruitBlock = Blocks.ByName(fruit)!;
        if (Events.HasBlockSpread)
        {
            var spreadEv = new BlockSpreadEvent(new BlockPos(x, y, z), new BlockPos(x + dx, y, z + dz), fruitBlock);
            if (!Events.OnBlockSpread(spreadEv))
                return false;
            fruitBlock = spreadEv.NewBlock;
        }
        SetBlockAndUpdate(x + dx, y, z + dz, fruitBlock, world);
        return true;
    }

    /// <summary>Destroys a flammable block due to fire (cancellable with <see cref="ServerEvents.BlockBurn"/>).</summary>
    public bool BurnBlock(int x, int y, int z, World? world = null)
    {
        world ??= World;
        if (world.GetBlock(x, y, z) is not { IsAir: false } block)
            return false;
        if (Events.HasBlockBurn)
        {
            var burnEv = new BlockBurnEvent(new BlockPos(x, y, z), block);
            if (!Events.OnBlockBurn(burnEv))
                return false;
        }
        SetBlockAndUpdate(x, y, z, Blocks.Air, world);
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

    /// <summary>
    /// A lit TNT: falls, slides, and is thrown by the other explosions (TNT cannons), as vanilla (Java PrimedTnt:
    /// gravity 0.04, drag 0.98, ground friction 0.7, a small random kick when lit).
    /// Flowing water pushes it (0.014 a tick along the stream, as Java): cannons bring their TNT into place with it.
    /// </summary>
    private sealed class PrimedTnt(long id, World world, double x, double y, double z, long explodes)
    {
        public readonly long Id = id;
        public readonly World World = world;
        public readonly long Explodes = explodes;
        public double X = x, Y = y, Z = z, Vx, Vy, Vz;
        public bool OnGround;
    }

    private const double TntHalf = 0.49, TntHeight = 0.98;
    private readonly List<PrimedTnt> _tnt = [];

    /// <summary>A lit TNT block: a "minecraft:tnt" entity that explodes 4 seconds later.</summary>
    public void PrimeTnt(int x, int y, int z, int fuse = 80, World? world = null)
    {
        world ??= World;
        var id = NextEntityId();
        if (Events.HasTntPrime)
        {
            var ev = new TntPrimeEvent(id, new BlockPos(x, y, z), fuse);
            if (!Events.OnTntPrime(ev))
                return;
            fuse = ev.FuseTicks;
        }
        var angle = _blockRandom.NextDouble() * Math.PI * 2;
        var tnt = new PrimedTnt(id, world, x + 0.5, y, z + 0.5, CurrentTick + fuse) { Vx = -Math.Sin(angle) * 0.02, Vy = 0.2, Vz = -Math.Cos(angle) * 0.02 };
        _tnt.Add(tnt);
        BroadcastIn(world, EntityPackets.AddActor(id, "minecraft:tnt", tnt.X, tnt.Y, tnt.Z, 0, 1, 1, 0, 0.98f, 0.98f,
            EntityPackets.FlagGravity | EntityPackets.FlagHasCollision | (1L << 10))); // 10: ignited
        BroadcastIn(world, EntityPackets.Motion(id, tnt.Vx, tnt.Vy, tnt.Vz));
    }

    /// <summary>Lit TNT entities (for plugins and tests).</summary>
    public int PrimedTntCount => _tnt.Count;

    private void TickTnt()
    {
        if (_tnt.Count == 0)
            return;
        // A copy: explosions light more TNT and remove the ones they reach.
        foreach (var tnt in _tnt.ToArray())
        {
            if (!_tnt.Contains(tnt))
                continue;
            MoveTnt(tnt);
            if (tnt.Explodes > CurrentTick)
                continue;
            _tnt.Remove(tnt);
            BroadcastIn(tnt.World, Packets.RemoveActor(tnt.Id));
            Explode(tnt.X, tnt.Y + 0.0625, tnt.Z, 4, tnt.World); // Java PrimedTnt explodes 1/16 above its base
        }
    }

    private void MoveTnt(PrimedTnt t)
    {
        var world = t.World;
        if (world.LoadedChunk((int)Math.Floor(t.X) >> 4, (int)Math.Floor(t.Z) >> 4) is null)
            return; // frozen in unloaded chunks
        var (oldX, oldY, oldZ) = (t.X, t.Y, t.Z);
        var (flowX, flowY, flowZ) = Fluids.Flow(world, (int)Math.Floor(t.X), (int)Math.Floor(t.Y + TntHeight / 2), (int)Math.Floor(t.Z));
        t.Vx += flowX * 0.014;
        t.Vy += flowY * 0.014;
        t.Vz += flowZ * 0.014;
        t.Vy -= 0.04;
        // Steps of at most half a block: a cannon throws TNT several blocks a tick, it must not cross walls.
        var steps = (int)Math.Clamp(Math.Ceiling(Math.Max(Math.Abs(t.Vx), Math.Max(Math.Abs(t.Vy), Math.Abs(t.Vz))) / 0.5), 1, 40);
        t.OnGround = false;
        for (var i = 0; i < steps; i++)
        {
            var ny = t.Y + t.Vy / steps;
            if (TntCollides(world, t.X, ny, t.Z))
            {
                if (t.Vy < 0)
                {
                    t.OnGround = true;
                    var floor = Math.Floor(ny) + 1;
                    if (!TntCollides(world, t.X, floor, t.Z))
                        t.Y = floor;
                }
                t.Vy = 0;
            }
            else
                t.Y = ny;
            var nx = t.X + t.Vx / steps;
            if (TntCollides(world, nx, t.Y, t.Z))
                t.Vx = 0;
            else
                t.X = nx;
            var nz = t.Z + t.Vz / steps;
            if (TntCollides(world, t.X, t.Y, nz))
                t.Vz = 0;
            else
                t.Z = nz;
        }
        t.Vx *= 0.98;
        t.Vy *= 0.98;
        t.Vz *= 0.98;
        if (t.OnGround || TntCollides(world, t.X, t.Y - 0.01, t.Z))
        {
            t.OnGround = true;
            t.Vx *= 0.7;
            t.Vz *= 0.7;
            t.Vy *= -0.5;
        }
        if (Math.Abs(t.X - oldX) + Math.Abs(t.Y - oldY) + Math.Abs(t.Z - oldZ) > 0.001)
            BroadcastInChunk(world, (int)Math.Floor(t.X) >> 4, (int)Math.Floor(t.Z) >> 4,
                EntityPackets.MoveAbsolute(t.Id, t.X, t.Y, t.Z, 0, 0, t.OnGround));
    }

    private static bool TntCollides(World world, double x, double y, double z)
    {
        for (var bx = (int)Math.Floor(x - TntHalf); bx <= (int)Math.Floor(x + TntHalf); bx++)
        for (var bz = (int)Math.Floor(z - TntHalf); bz <= (int)Math.Floor(z + TntHalf); bz++)
        for (var by = (int)Math.Floor(y); by <= (int)Math.Floor(y + TntHeight); by++)
            if (BlockPhysics.IsSolid(world.GetBlock(bx, by, bz)))
                return true;
        return false;
    }

    // 1352 rays from the centre, as vanilla (Dragonfly block/explosion.go, MIT): the faces of a 16x16x16 cube.
    private static readonly (double X, double Y, double Z)[] ExplosionRays = BuildRays();

    private static (double, double, double)[] BuildRays()
    {
        var rays = new List<(double, double, double)>(1352);
        for (var x = 0; x < 16; x++)
        for (var y = 0; y < 16; y++)
        for (var z = 0; z < 16; z++)
        {
            if (x != 0 && x != 15 && y != 0 && y != 15 && z != 0 && z != 15)
                continue;
            double dx = x / 15.0 * 2 - 1, dy = y / 15.0 * 2 - 1, dz = z / 15.0 * 2 - 1;
            var length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            rays.Add((dx / length * 0.3, dy / length * 0.3, dz / length * 0.3));
        }
        return [.. rays];
    }

    /// <summary>
    /// Vanilla blast resistance (Java values): what an explosion ray loses crossing the block. Water and lava
    /// (100) stop it, so TNT exploding in water breaks nothing; obsidian and the like (1200) are never reached.
    /// </summary>
    public static float BlastResistance(BlockType block)
    {
        if (block.IsAir)
            return 0;
        if (Fluids.IsLiquid(block))
            return 100;
        if (block.Unbreakable)
            return float.PositiveInfinity;
        var name = block.Name.StartsWith("minecraft:") ? block.Name["minecraft:".Length..] : block.Name;
        if (name.Contains("obsidian") || name is "anvil" or "chipped_anvil" or "damaged_anvil" or "enchanting_table" or "netherite_block"
                or "ancient_debris" or "respawn_anchor" or "reinforced_deepslate")
            return 1200;
        if (name == "ender_chest")
            return 600;
        if (name == "end_stone" || name.StartsWith("end_stone_brick"))
            return 9;
        var hardness = DropRules.HardnessOf(block);
        // Stone, bricks, ores, metal blocks: 6 (their hardness is lower).
        return DropRules.EffectiveTool(block) == "pickaxe" && hardness >= 1.5f ? Math.Max(6, hardness) : hardness;
    }

    // Obsidian hits per block (obsidian-durability): explosions needed before it breaks.
    // ponytail: the count stays if the obsidian is mined and placed again; it resets when the server restarts.
    private readonly Dictionary<(World, int, int, int), int> _obsidianHits = [];

    /// <summary>
    /// A vanilla explosion of strength <paramref name="radius"/> (TNT 4): 1352 rays lose force through the blocks by
    /// their blast resistance (water and obsidian stop them) and break what they get through, some dropping; TNT
    /// reached is lit, players and mobs are hurt and thrown by how much of them the blast sees (TNT is thrown too:
    /// cannons). With obsidian-durability, obsidian close by breaks after that many explosions (factions raids).
    /// </summary>
    public void Explode(double x, double y, double z, int radius, World? world = null)
    {
        world ??= World;
        var size = (double)radius;
        int cx = (int)Math.Floor(x), cy = (int)Math.Floor(y), cz = (int)Math.Floor(z);
        var affected = new List<BlockPos>();
        var resistance = new Dictionary<(int, int, int), float>();
        var seen = new HashSet<(int, int, int)>();
        foreach (var ray in ExplosionRays)
        {
            double px = x, py = y, pz = z;
            for (var force = size * (0.7 + _blockRandom.NextDouble() * 0.6); force > 0; force -= 0.225)
            {
                var pos = ((int)Math.Floor(px), (int)Math.Floor(py), (int)Math.Floor(pz));
                if (!resistance.TryGetValue(pos, out var r))
                    resistance[pos] = r = world.GetBlock(pos.Item1, pos.Item2, pos.Item3) is { } block ? BlastResistance(block) : float.PositiveInfinity;
                if (float.IsPositiveInfinity(r))
                    break;
                force -= (r + 0.3) * 0.3;
                if (force > 0 && r > 0 && r < 100 && seen.Add(pos))
                    affected.Add(new BlockPos(pos.Item1, pos.Item2, pos.Item3));
                px += ray.X;
                py += ray.Y;
                pz += ray.Z;
            }
        }
        // Factions: obsidian within 3 blocks takes a hit, unless the explosion is in water or lava.
        var durability = Config.ObsidianDurability;
        var inLiquid = world.GetBlock(cx, cy, cz) is { } centre && Fluids.IsLiquid(centre);
        if (durability > 0 && !inLiquid)
            for (var bx = cx - 3; bx <= cx + 3; bx++)
            for (var by = cy - 3; by <= cy + 3; by++)
            for (var bz = cz - 3; bz <= cz + 3; bz++)
            {
                if ((bx + 0.5 - x) * (bx + 0.5 - x) + (by + 0.5 - y) * (by + 0.5 - y) + (bz + 0.5 - z) * (bz + 0.5 - z) > 3.5 * 3.5
                    || world.GetBlock(bx, by, bz)?.Name is not ("minecraft:obsidian" or "minecraft:crying_obsidian"))
                    continue;
                var key = (world, bx, by, bz);
                var hits = _obsidianHits.GetValueOrDefault(key) + 1;
                if (hits < durability)
                {
                    _obsidianHits[key] = hits;
                    continue;
                }
                _obsidianHits.Remove(key);
                affected.Add(new BlockPos(bx, by, bz));
            }

        bool breaksBlocks = true;
        bool dropItems = true;
        if (Events.HasExplosion)
        {
            var ev = new ExplosionEvent(new System.Numerics.Vector3((float)x, (float)y, (float)z), radius, affected);
            if (!Events.OnExplosion(ev))
                return;
            size = Math.Max(0, ev.Radius);
            breaksBlocks = ev.BreaksBlocks;
        }

        if (breaksBlocks && affected.Count > 0 && Events.HasExplosionBlocks)
        {
            var blocksEv = new ExplosionBlocksEvent(new System.Numerics.Vector3((float)x, (float)y, (float)z), affected);
            if (!Events.OnExplosionBlocks(blocksEv))
                breaksBlocks = false;
            else
                dropItems = blocksEv.DropItems;
        }

        if (breaksBlocks)
        {
            foreach (var pos in affected)
            {
                var (bx, by, bz) = (pos.X, pos.Y, pos.Z);
                if (world.GetBlock(bx, by, bz) is not { IsAir: false } block)
                    continue;
                if (dropItems && world.GetBlockEntity(bx, by, bz)?.Items is { } contents)
                    for (var slot = 0; slot < contents.Size; slot++)
                        if (contents[slot] is { } stack)
                            DropItem(stack, bx + 0.5f, by + 0.5f, bz + 0.5f, world: world);
                SetBlockAndUpdate(bx, by, bz, Blocks.Air, world);
                if (block.Name == "minecraft:tnt")
                    PrimeTnt(bx, by, bz, 10 + _blockRandom.Next(20), world);
                else if (dropItems && (block.Name is "minecraft:obsidian" or "minecraft:crying_obsidian" || _blockRandom.NextDouble() < 1 / Math.Max(1, size)))
                    foreach (var drop in DropRules.Drops(block, null, Random.Shared))
                        DropItem(drop, bx + 0.5f, by + 0.5f, bz + 0.5f, world: world);
            }
            foreach (var pos in affected)
                ApplyGravity(pos.X, pos.Y + 1, pos.Z, world);
        }
        BroadcastIn(world, Packets.LevelEvent(2025, cx, cy, cz, 0));
        BroadcastIn(world, BedrockConnection.LevelSound("explode", (float)x, (float)y, (float)z, -1));

        // Entities: impact = (1 - distance / (2 x size)) x exposure; damage and push as vanilla.
        var reach = size * 2;
        if (reach <= 0)
            return;
        foreach (var player in _players.Where(p => p.IsInGame && p.World == world && !p.Dead).ToList())
        {
            var feet = player.Position.Y - BedrockConnection.EyeHeight;
            var impact = Impact(world, x, y, z, reach, player.Position.X, feet, player.Position.Z, 0.3, 1.8);
            if (impact <= 0)
                continue;
            player.Damage((float)Math.Floor((impact * impact + impact) / 2 * 7 * size + 1), x, z, source: DamageSource.Explosion());
            if (player.GameMode is Core.GameMode.Survival or Core.GameMode.Adventure && !player.Dead)
            {
                var (dx, dy, dz) = Direction(x, y, z, player.Position.X, player.Position.Y, player.Position.Z);
                player.SetMotion(dx * impact, dy * impact, dz * impact);
            }
        }
        foreach (var mob in Mobs.All.ToList())
        {
            if (mob.World != world || mob.Dead)
                continue;
            var impact = Impact(world, x, y, z, reach, mob.X, mob.Y, mob.Z, mob.Width / 2, mob.Height);
            if (impact <= 0)
                continue;
            Mobs.Damage(mob, (float)Math.Floor((impact * impact + impact) / 2 * 7 * size + 1), (x, z), source: DamageSource.Explosion());
            var (dx, dy, dz) = Direction(x, y, z, mob.X, mob.Y + mob.Height * 0.85, mob.Z);
            mob.Vx += dx * impact;
            mob.Vy += dy * impact;
            mob.Vz += dz * impact;
        }
        foreach (var tnt in _tnt)
        {
            if (tnt.World != world)
                continue;
            var impact = Impact(world, x, y, z, reach, tnt.X, tnt.Y, tnt.Z, TntHalf, TntHeight);
            if (impact <= 0)
                continue;
            var (dx, dy, dz) = Direction(x, y, z, tnt.X, tnt.Y, tnt.Z); // TNT: from its base, as Java
            tnt.Vx += dx * impact;
            tnt.Vy += dy * impact;
            tnt.Vz += dz * impact;
        }
    }

    private static (double X, double Y, double Z) Direction(double fromX, double fromY, double fromZ, double x, double y, double z)
    {
        double dx = x - fromX, dy = y - fromY, dz = z - fromZ;
        var length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        return length < 1e-4 ? (0, 0, 0) : (dx / length, dy / length, dz / length);
    }

    // (1 - distance / reach) x the share of 9 points of the box (corners and centre) the blast sees.
    private static double Impact(World world, double x, double y, double z, double reach, double ex, double ey, double ez, double half, double height)
    {
        var distance = Math.Sqrt((ex - x) * (ex - x) + (ey - y) * (ey - y) + (ez - z) * (ez - z));
        if (distance > reach)
            return 0;
        var seen = 0;
        const double inset = 0.05;
        for (var i = 0; i < 9; i++)
        {
            var (px, py, pz) = i == 8 ? (ex, ey + height / 2, ez)
                : (ex + ((i & 1) == 0 ? -half + inset : half - inset), ey + ((i & 2) == 0 ? inset : height - inset), ez + ((i & 4) == 0 ? -half + inset : half - inset));
            if (!RayBlocked(world, x, y, z, px, py, pz))
                seen++;
        }
        return (1 - distance / reach) * seen / 9;
    }

    private static bool RayBlocked(World world, double x, double y, double z, double tx, double ty, double tz)
    {
        double dx = tx - x, dy = ty - y, dz = tz - z;
        var length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        var steps = (int)Math.Ceiling(length / 0.3);
        for (var i = 1; i < steps; i++)
        {
            var t = (double)i / steps;
            if (BlockPhysics.IsSolid(world.GetBlock((int)Math.Floor(x + dx * t), (int)Math.Floor(y + dy * t), (int)Math.Floor(z + dz * t))))
                return true;
        }
        return false;
    }
}
