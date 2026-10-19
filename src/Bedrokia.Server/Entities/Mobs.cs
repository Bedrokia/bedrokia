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

using Bedrokia.Server.Core;
using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Entities;

/// <summary>A kind of mob with its vanilla (Bedrock) size, health, speed and loot.</summary>
public sealed record MobKind(string Id, float Width, float Height, float MaxHealth, double Speed, bool Hostile, float Damage,
    (string Item, int Min, int Max)[] Loot, bool BurnsInDaylight = false)
{
    // Animaux passifs
    public static readonly MobKind Cow = new("minecraft:cow", 0.9f, 1.3f, 10, 0.2, false, 0, [("beef", 1, 3), ("leather", 0, 2)]);
    public static readonly MobKind Pig = new("minecraft:pig", 0.9f, 0.9f, 10, 0.25, false, 0, [("porkchop", 1, 3)]);
    public static readonly MobKind Sheep = new("minecraft:sheep", 0.9f, 1.3f, 8, 0.23, false, 0, [("mutton", 1, 2), ("white_wool", 1, 1)]);
    public static readonly MobKind Chicken = new("minecraft:chicken", 0.6f, 0.8f, 4, 0.25, false, 0, [("chicken", 1, 1), ("feather", 0, 2)]);
    public static readonly MobKind Rabbit = new("minecraft:rabbit", 0.4f, 0.5f, 3, 0.3, false, 0, [("rabbit", 0, 1), ("rabbit_hide", 0, 1), ("rabbit_foot", 0, 1)]);
    public static readonly MobKind Horse = new("minecraft:horse", 1.4f, 1.6f, 15, 0.225, false, 0, [("leather", 0, 2)]);
    public static readonly MobKind Donkey = new("minecraft:donkey", 1.4f, 1.6f, 15, 0.175, false, 0, [("leather", 0, 2)]);
    public static readonly MobKind Mule = new("minecraft:mule", 1.4f, 1.6f, 15, 0.175, false, 0, [("leather", 0, 2)]);
    public static readonly MobKind Llama = new("minecraft:llama", 0.9f, 1.87f, 15, 0.2, false, 0, [("leather", 0, 2)]);
    public static readonly MobKind Wolf = new("minecraft:wolf", 0.6f, 0.85f, 8, 0.3, false, 2, []);
    public static readonly MobKind Cat = new("minecraft:cat", 0.6f, 0.7f, 10, 0.3, false, 0, [("string", 0, 2)]);
    public static readonly MobKind Ocelot = new("minecraft:ocelot", 0.6f, 0.7f, 10, 0.3, false, 0, []);
    public static readonly MobKind Parrot = new("minecraft:parrot", 0.5f, 0.9f, 6, 0.2, false, 0, [("feather", 1, 2)]);
    public static readonly MobKind Bat = new("minecraft:bat", 0.5f, 0.9f, 6, 0.25, false, 0, []);
    public static readonly MobKind Mooshroom = new("minecraft:mooshroom", 0.9f, 1.3f, 10, 0.2, false, 0, [("beef", 1, 3), ("leather", 0, 2)]);
    public static readonly MobKind SquidGlow = new("minecraft:glow_squid", 0.8f, 0.8f, 10, 0.2, false, 0, [("glow_ink_sac", 1, 3)]);
    public static readonly MobKind Squid = new("minecraft:squid", 0.8f, 0.8f, 10, 0.2, false, 0, [("ink_sac", 1, 3)]);
    public static readonly MobKind Cod = new("minecraft:cod", 0.5f, 0.3f, 3, 0.2, false, 0, [("cod", 1, 1)]);
    public static readonly MobKind Salmon = new("minecraft:salmon", 0.7f, 0.4f, 3, 0.2, false, 0, [("salmon", 1, 1)]);
    public static readonly MobKind TropicalFish = new("minecraft:tropicalfish", 0.5f, 0.4f, 3, 0.2, false, 0, [("tropical_fish", 1, 1)]);
    public static readonly MobKind Pufferfish = new("minecraft:pufferfish", 0.7f, 0.7f, 3, 0.2, false, 0, [("pufferfish", 1, 1)]);
    public static readonly MobKind Turtle = new("minecraft:turtle", 1.2f, 0.4f, 30, 0.1, false, 0, [("seagrass", 0, 2)]);
    public static readonly MobKind Panda = new("minecraft:panda", 1.3f, 1.25f, 20, 0.15, false, 6, [("bamboo", 0, 2)]);
    public static readonly MobKind PolarBear = new("minecraft:polar_bear", 1.3f, 1.4f, 30, 0.25, false, 6, [("cod", 0, 2), ("salmon", 0, 2)]);
    public static readonly MobKind Fox = new("minecraft:fox", 0.6f, 0.7f, 10, 0.3, false, 2, []);
    public static readonly MobKind Bee = new("minecraft:bee", 0.7f, 0.6f, 10, 0.3, false, 2, [("honeycomb", 1, 3)]);
    public static readonly MobKind Strider = new("minecraft:strider", 0.9f, 1.7f, 20, 0.16, false, 0, [("string", 2, 5)]);
    public static readonly MobKind Axolotl = new("minecraft:axolotl", 0.75f, 0.42f, 14, 0.2, false, 2, []);
    public static readonly MobKind Goat = new("minecraft:goat", 0.9f, 1.3f, 10, 0.25, false, 2, [("mutton", 1, 2)]);
    public static readonly MobKind Frog = new("minecraft:frog", 0.5f, 0.5f, 10, 0.2, false, 0, []);
    public static readonly MobKind Tadpole = new("minecraft:tadpole", 0.4f, 0.3f, 6, 0.2, false, 0, []);
    public static readonly MobKind Allay = new("minecraft:allay", 0.35f, 0.6f, 20, 0.25, false, 0, []);
    public static readonly MobKind Camel = new("minecraft:camel", 1.7f, 2.375f, 32, 0.09, false, 0, []);
    public static readonly MobKind Sniffer = new("minecraft:sniffer", 1.9f, 1.75f, 14, 0.1, false, 0, []);
    
    // Villageois et commerce
    public static readonly MobKind Villager = new("minecraft:villager", 0.6f, 1.95f, 20, 0.2, false, 0, []);
    public static readonly MobKind ZombieVillager = new("minecraft:zombie_villager", 0.6f, 1.95f, 20, 0.23, true, 3, [("rotten_flesh", 0, 2)], BurnsInDaylight: true);
    public static readonly MobKind WanderingTrader = new("minecraft:wandering_trader", 0.6f, 1.95f, 20, 0.2, false, 0, []);
    
    // Golems
    public static readonly MobKind IronGolem = new("minecraft:iron_golem", 1.4f, 2.7f, 100, 0.25, false, 7, [("iron_ingot", 3, 5), ("poppy", 0, 2)]);
    public static readonly MobKind SnowGolem = new("minecraft:snow_golem", 0.7f, 1.9f, 4, 0.2, false, 0, [("snowball", 0, 15)]);
    
    // Monstres hostiles - Overworld
    public static readonly MobKind Zombie = new("minecraft:zombie", 0.6f, 1.9f, 20, 0.23, true, 3, [("rotten_flesh", 0, 2)], BurnsInDaylight: true);
    public static readonly MobKind Husk = new("minecraft:husk", 0.6f, 1.9f, 20, 0.23, true, 3, [("rotten_flesh", 0, 2)], BurnsInDaylight: false);
    public static readonly MobKind Drowned = new("minecraft:drowned", 0.6f, 1.95f, 20, 0.23, true, 3, [("rotten_flesh", 0, 2), ("copper_ingot", 0, 1)]);
    public static readonly MobKind Skeleton = new("minecraft:skeleton", 0.6f, 1.99f, 20, 0.25, true, 3, [("bone", 0, 2), ("arrow", 0, 2)], BurnsInDaylight: true);
    public static readonly MobKind Stray = new("minecraft:stray", 0.6f, 1.99f, 20, 0.25, true, 3, [("bone", 0, 2), ("arrow", 0, 2)], BurnsInDaylight: true);
    public static readonly MobKind Creeper = new("minecraft:creeper", 0.6f, 1.7f, 20, 0.2, true, 0, [("gunpowder", 0, 2)]);
    public static readonly MobKind Spider = new("minecraft:spider", 1.4f, 0.9f, 16, 0.3, true, 2, [("string", 0, 2), ("spider_eye", 0, 1)]);
    public static readonly MobKind CaveSpider = new("minecraft:cave_spider", 0.7f, 0.5f, 12, 0.3, true, 2, [("string", 0, 2), ("spider_eye", 0, 1)]);
    public static readonly MobKind Enderman = new("minecraft:enderman", 0.6f, 2.9f, 40, 0.3, true, 7, [("ender_pearl", 0, 1)]);
    public static readonly MobKind Witch = new("minecraft:witch", 0.6f, 1.95f, 26, 0.25, true, 0, [("glowstone_dust", 0, 2), ("sugar", 0, 2), ("redstone", 0, 2), ("spider_eye", 0, 2)]);
    public static readonly MobKind Slime = new("minecraft:slime", 2.04f, 2.04f, 16, 0.2, true, 4, [("slime_ball", 0, 2)]);
    public static readonly MobKind Phantom = new("minecraft:phantom", 0.9f, 0.5f, 20, 0.2, true, 2, [("phantom_membrane", 0, 1)]);
    public static readonly MobKind Silverfish = new("minecraft:silverfish", 0.4f, 0.3f, 8, 0.25, true, 1, []);
    public static readonly MobKind Endermite = new("minecraft:endermite", 0.4f, 0.3f, 8, 0.25, true, 2, []);
    public static readonly MobKind Guardian = new("minecraft:guardian", 0.85f, 0.85f, 30, 0.2, true, 6, [("prismarine_shard", 0, 2), ("prismarine_crystals", 0, 1), ("cod", 0, 1)]);
    public static readonly MobKind ElderGuardian = new("minecraft:elder_guardian", 1.9975f, 1.9975f, 80, 0.2, true, 8, [("prismarine_shard", 0, 2), ("prismarine_crystals", 0, 1), ("cod", 0, 1)]);
    public static readonly MobKind Shulker = new("minecraft:shulker", 1.0f, 1.0f, 30, 0.0, true, 4, [("shulker_shell", 0, 1)]);
    public static readonly MobKind Vindicator = new("minecraft:vindicator", 0.6f, 1.95f, 24, 0.35, true, 5, [("emerald", 0, 1)]);
    public static readonly MobKind Evoker = new("minecraft:evoker", 0.6f, 1.95f, 24, 0.2, true, 6, [("totem_of_undying", 1, 1), ("emerald", 0, 1)]);
    public static readonly MobKind Pillager = new("minecraft:pillager", 0.6f, 1.95f, 24, 0.35, true, 5, [("arrow", 0, 2)]);
    public static readonly MobKind Ravager = new("minecraft:ravager", 1.95f, 2.2f, 100, 0.3, true, 12, [("saddle", 1, 1)]);
    public static readonly MobKind Vex = new("minecraft:vex", 0.4f, 0.8f, 14, 0.4, true, 3, []);
    public static readonly MobKind Warden = new("minecraft:warden", 0.9f, 2.9f, 500, 0.3, true, 30, [("sculk_catalyst", 1, 1)]);
    
    // Monstres hostiles - Nether
    public static readonly MobKind Ghast = new("minecraft:ghast", 4.0f, 4.0f, 10, 0.2, true, 0, [("ghast_tear", 0, 1), ("gunpowder", 0, 2)]);
    public static readonly MobKind ZombiePigman = new("minecraft:zombie_pigman", 0.6f, 1.95f, 20, 0.23, true, 5, [("rotten_flesh", 0, 1), ("gold_nugget", 0, 1), ("gold_ingot", 0, 1)]);
    public static readonly MobKind Piglin = new("minecraft:piglin", 0.6f, 1.95f, 16, 0.35, true, 5, [("porkchop", 1, 3)]);
    public static readonly MobKind PiglinBrute = new("minecraft:piglin_brute", 0.6f, 1.95f, 50, 0.35, true, 7, []);
    public static readonly MobKind Hoglin = new("minecraft:hoglin", 1.4f, 1.4f, 40, 0.3, true, 6, [("porkchop", 2, 4), ("leather", 0, 1)]);
    public static readonly MobKind Zoglin = new("minecraft:zoglin", 1.4f, 1.4f, 40, 0.3, true, 6, [("rotten_flesh", 1, 3)]);
    public static readonly MobKind Blaze = new("minecraft:blaze", 0.6f, 1.8f, 20, 0.23, true, 6, [("blaze_rod", 0, 1)]);
    public static readonly MobKind MagmaCube = new("minecraft:magma_cube", 2.04f, 2.04f, 16, 0.2, true, 6, [("magma_cream", 0, 1)]);
    public static readonly MobKind WitherSkeleton = new("minecraft:wither_skeleton", 0.7f, 2.4f, 20, 0.25, true, 8, [("coal", 0, 1), ("bone", 0, 2), ("wither_skeleton_skull", 0, 1)]);
    
    // Monstres hostiles - End
    public static readonly MobKind EnderDragon = new("minecraft:ender_dragon", 16.0f, 8.0f, 200, 0.2, true, 10, [("dragon_egg", 1, 1)]);
    
    // Boss
    public static readonly MobKind Wither = new("minecraft:wither", 0.9f, 3.5f, 300, 0.25, true, 8, [("nether_star", 1, 1)]);

    public static readonly MobKind[] Passive = [Cow, Pig, Sheep, Chicken, Rabbit, Horse, Donkey, Llama, Mooshroom, Panda, Fox, Bee, Goat, Camel];
    public static readonly MobKind[] HostileKinds = [Zombie, Zombie, Skeleton, Skeleton, Creeper, Spider, Enderman, Witch, Phantom, Husk, Drowned, Stray, CaveSpider];
    
    // Dictionnaire pour trouver les mobs par leur ID
    public static readonly Dictionary<string, MobKind> ById = new()
    {
        ["cow"] = Cow, ["pig"] = Pig, ["sheep"] = Sheep, ["chicken"] = Chicken, ["rabbit"] = Rabbit,
        ["horse"] = Horse, ["donkey"] = Donkey, ["mule"] = Mule, ["llama"] = Llama,
        ["wolf"] = Wolf, ["cat"] = Cat, ["ocelot"] = Ocelot, ["parrot"] = Parrot, ["bat"] = Bat,
        ["mooshroom"] = Mooshroom, ["glow_squid"] = SquidGlow, ["squid"] = Squid,
        ["cod"] = Cod, ["salmon"] = Salmon, ["tropical_fish"] = TropicalFish, ["pufferfish"] = Pufferfish,
        ["turtle"] = Turtle, ["panda"] = Panda, ["polar_bear"] = PolarBear, ["fox"] = Fox,
        ["bee"] = Bee, ["strider"] = Strider, ["axolotl"] = Axolotl, ["goat"] = Goat,
        ["frog"] = Frog, ["tadpole"] = Tadpole, ["allay"] = Allay, ["camel"] = Camel, ["sniffer"] = Sniffer,
        ["villager"] = Villager, ["zombie_villager"] = ZombieVillager, ["wandering_trader"] = WanderingTrader,
        ["iron_golem"] = IronGolem, ["snow_golem"] = SnowGolem,
        ["zombie"] = Zombie, ["husk"] = Husk, ["drowned"] = Drowned,
        ["skeleton"] = Skeleton, ["stray"] = Stray, ["creeper"] = Creeper,
        ["spider"] = Spider, ["cave_spider"] = CaveSpider, ["enderman"] = Enderman,
        ["witch"] = Witch, ["slime"] = Slime, ["phantom"] = Phantom,
        ["silverfish"] = Silverfish, ["endermite"] = Endermite,
        ["guardian"] = Guardian, ["elder_guardian"] = ElderGuardian, ["shulker"] = Shulker,
        ["vindicator"] = Vindicator, ["evoker"] = Evoker, ["pillager"] = Pillager,
        ["ravager"] = Ravager, ["vex"] = Vex, ["warden"] = Warden,
        ["ghast"] = Ghast, ["zombie_pigman"] = ZombiePigman, ["piglin"] = Piglin,
        ["piglin_brute"] = PiglinBrute, ["hoglin"] = Hoglin, ["zoglin"] = Zoglin,
        ["blaze"] = Blaze, ["magma_cube"] = MagmaCube, ["wither_skeleton"] = WitherSkeleton,
        ["ender_dragon"] = EnderDragon, ["wither"] = Wither
    };
}

/// <summary>One mob in the world. Positions are the feet; game thread only.</summary>
public sealed class Mob(long entityId, MobKind kind, double x, double y, double z)
{
    public long EntityId { get; } = entityId;
    public MobKind Kind { get; } = kind;
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Z { get; set; } = z;
    public double Vx, Vy, Vz;
    public float Yaw;
    public float Health { get; set; } = kind.MaxHealth;
    public bool OnGround;
    public bool Dead => Health <= 0;

    /// <summary>Wool colour of sheep (0 = white), random at spawn like vanilla.</summary>
    public int? Colour;

    internal int HurtTicks, DeathTicks, AttackCooldown, PanicTicks, FuseTicks, WanderTicks;
    internal (double X, double Z)? WanderTarget;
    internal (double X, double Z)? FleeFrom;
    internal bool Burning;
    /// <summary>Ticks left on fire (Fire Aspect).</summary>
    public int FireTicks;
    internal (double X, double Y, double Z, float Yaw) LastSent = (double.NaN, 0, 0, 0);
    internal readonly HashSet<BedrockConnection> Viewers = [];

    public long Flags => EntityPackets.FlagHasCollision | EntityPackets.FlagGravity | EntityPackets.FlagBreathing
                         | (Burning ? EntityPackets.FlagOnFire : 0);

    public byte[] AddPacket() =>
        EntityPackets.AddActor(EntityId, Kind.Id, X, Y, Z, Yaw, Health, Kind.MaxHealth, (float)Kind.Speed, Kind.Width, Kind.Height, Flags, Colour);
}

/// <summary>
/// Mobs: natural spawning around players (animals on grass, monsters at night), physics with block collisions,
/// simple vanilla-like AI (wander, panic, chase and attack, creeper fuse), daylight burning, damage, knockback,
/// death animation and loot. AI rules follow Cuberite's (Apache-2.0) passive/aggressive monster behaviour.
/// </summary>
/// <remarks>ponytail: mobs are not saved with the world; skeletons hit at range without arrow entities; no pathfinding
/// beyond stepping up one block and avoiding drops.</remarks>
public sealed class MobManager(BedrokiaServer server)
{
    public const int ViewDistance = 80, DespawnDistance = 128, PassiveCap = 12, HostileCap = 18;

    private readonly List<Mob> _mobs = [];
    private readonly Random _random = new();

    public IReadOnlyList<Mob> All => _mobs;

    public Mob? Find(long entityId) => _mobs.FirstOrDefault(m => m.EntityId == entityId && !m.Dead);

    public Mob? Spawn(MobKind kind, double x, double y, double z)
    {
        var mob = new Mob(server.NextEntityId(), kind, x, y, z) { Yaw = _random.Next(360) };
        if (kind == MobKind.Sheep)
            mob.Colour = _random.Next(100) switch { < 5 => 15, < 10 => 7, < 15 => 8, < 18 => 12, 18 => 6, _ => 0 };

        var spawnEvent = new EntitySpawnEvent(mob, x, y, z);
        if (!server.Events.OnEntitySpawn(spawnEvent))
            return null;

        mob.X = spawnEvent.X;
        mob.Y = spawnEvent.Y;
        mob.Z = spawnEvent.Z;

        _mobs.Add(mob);
        server.Logger.Debug($"Spawned {kind.Id} #{mob.EntityId} at ({mob.X:0.#}, {mob.Y:0.#}, {mob.Z:0.#}); {_mobs.Count} mobs.");
        return mob;
    }

    public void Tick(long tick)
    {
        var players = server.Players.Where(p => p.IsInGame).ToList();
        if (tick % 20 == 0)
            SpawnAround(players);

        foreach (var mob in _mobs.ToList())
        {
            if (mob.Dead)
            {
                if (++mob.DeathTicks >= 20)
                    Remove(mob, EntityDespawnReason.Death);
                continue;
            }
            var nearest = players.Where(p => !p.Dead).MinBy(p => Distance2(mob, p));
            var distance = nearest is null ? double.MaxValue : Math.Sqrt(Distance2(mob, nearest));
            if (players.Count > 0 && distance > DespawnDistance && (mob.Kind.Hostile || distance > DespawnDistance * 2))
            {
                Remove(mob, EntityDespawnReason.Distance);
                continue;
            }
            if (server.World.LoadedChunk((int)Math.Floor(mob.X) >> 4, (int)Math.Floor(mob.Z) >> 4) is null)
            {
                Remove(mob, EntityDespawnReason.ChunkUnload);
                continue;
            }
            Think(mob, nearest, distance, tick);
            Physics(mob);
            if (mob.HurtTicks > 0)
                mob.HurtTicks--;
            if (mob.AttackCooldown > 0)
                mob.AttackCooldown--;
        }

        if (tick % 2 == 0)
            foreach (var mob in _mobs)
                Sync(mob, players);
    }

    // --- Spawning ---

    private void SpawnAround(List<BedrockConnection> players)
    {
        var time = server.World.Time % 24000;
        var night = time is > 13000 and < 23000;
        foreach (var player in players)
        {
            var near = _mobs.Where(m => Distance2(m, player) < 64 * 64).ToList();
            var hostile = near.Count(m => m.Kind.Hostile);
            var config = server.Config;
            if (config.SpawnAnimals && near.Count - hostile < PassiveCap && _random.Next(4) == 0)
                TrySpawn(player, MobKind.Passive[_random.Next(MobKind.Passive.Length)], 1 + _random.Next(3), passive: true);
            if (night && hostile < HostileCap && config.SpawnMonsters && config.Difficulty != Difficulty.Peaceful)
                TrySpawn(player, MobKind.HostileKinds[_random.Next(MobKind.HostileKinds.Length)], 1, passive: false);
        }
    }

    private void TrySpawn(BedrockConnection player, MobKind kind, int group, bool passive)
    {
        var angle = _random.NextDouble() * Math.PI * 2;
        var radius = 24 + _random.Next(24);
        var x = (int)Math.Floor(player.Position.X + Math.Cos(angle) * radius);
        var z = (int)Math.Floor(player.Position.Z + Math.Sin(angle) * radius);
        for (var i = 0; i < group; i++)
        {
            int sx = x + _random.Next(5) - 2, sz = z + _random.Next(5) - 2;
            if (Surface(sx, sz, (int)player.Position.Y) is not { } ground)
                return;
            var below = server.World.GetBlock(sx, ground - 1, sz);
            if (below is null || (passive ? below != Blocks.GrassBlock : !BlockPhysics.IsSolid(below)))
                return;
            Spawn(kind, sx + 0.5, ground, sz + 0.5);
        }
    }

    // First free spot (two air blocks over a solid one) scanning down from above the player's height.
    private int? Surface(int x, int z, int aroundY)
    {
        for (var y = Math.Min(aroundY + 40, Chunk.MaxY - 2); y > Math.Max(aroundY - 40, Chunk.MinY); y--)
        {
            var block = server.World.GetBlock(x, y, z);
            if (block is null)
                return null;
            if (block == Blocks.Water || block.Name is "minecraft:lava" or "minecraft:flowing_lava")
                return null;
            if (BlockPhysics.IsSolid(block))
                return BlockPhysics.IsSolid(server.World.GetBlock(x, y + 1, z)) || BlockPhysics.IsSolid(server.World.GetBlock(x, y + 2, z)) ? null : y + 1;
        }
        return null;
    }

    // --- AI ---

    private void Think(Mob mob, BedrockConnection? player, double distance, long tick)
    {
        // The movement attribute is not blocks per tick: vanilla walking speed is about 0.45x it (zombie ~2 m/s).
        double targetX, targetZ, speed = mob.Kind.Speed * 0.45;
        var moving = false;
        var day = server.World.Time % 24000 is < 12500 or > 23500;

        if (mob.Kind.BurnsInDaylight || mob.FireTicks > 0)
        {
            if (mob.FireTicks > 0)
                mob.FireTicks--;
            var exposed = mob.FireTicks > 0 || mob.Kind.BurnsInDaylight && day && SkyAbove(mob);
            if (exposed != mob.Burning)
            {
                mob.Burning = exposed;
                Broadcast(mob, EntityPackets.SetData(mob.EntityId, mob.Flags, mob.Kind.Width, mob.Kind.Height));
            }
            if (exposed && tick % 20 == 0)
                Damage(mob, 1, null, source: DamageSource.Fire());
            if (mob.Dead)
                return;
        }

        var survivalTarget = player is not null && player.GameMode != GameMode.Creative;
        if (mob.Kind.Hostile && survivalTarget && distance < 16 && (!day || mob.Kind != MobKind.Spider || distance < 4))
        {
            if (server.Events.HasListeners(server.Events.EntityTarget))
            {
                var targetEv = new EntityTargetEvent(mob, player);
                if (!server.Events.OnEntityTarget(targetEv) || targetEv.Target is null)
                {
                    survivalTarget = false;
                    player = null;
                }
                else
                {
                    player = targetEv.Target;
                }
            }
        }
        if (mob.Kind.Hostile && survivalTarget && player is not null && distance < 16 && (!day || mob.Kind != MobKind.Spider || distance < 4))
        {
            targetX = player.Position.X;
            targetZ = player.Position.Z;
            moving = true;
            if (mob.Kind == MobKind.Skeleton && distance < 15)
            {
                moving = distance > 8;
                if (mob.AttackCooldown == 0 && Math.Abs(player.Position.Y - BedrockConnection.EyeHeight - mob.Y) < 6)
                {
                    mob.AttackCooldown = 40;
                    Broadcast(mob, EntityPackets.Swing(mob.EntityId));
                    if (_random.NextDouble() < 0.7)
                        player.Damage(Scaled(2 + _random.Next(3)), mob.X, mob.Z, source: DamageSource.EntityAttack(mob));
                }
            }
            else if (mob.Kind == MobKind.Creeper)
            {
                if (distance < 3)
                {
                    moving = false;
                    if (++mob.FuseTicks >= 30)
                    {
                        Explode(mob);
                        return;
                    }
                }
                else
                    mob.FuseTicks = Math.Max(0, mob.FuseTicks - 1);
            }
            else if (distance < 1.2 + mob.Kind.Width && mob.AttackCooldown == 0
                     && Math.Abs(player.Position.Y - BedrockConnection.EyeHeight - mob.Y) < 2)
            {
                mob.AttackCooldown = 20;
                Broadcast(mob, EntityPackets.Swing(mob.EntityId));
                player.Damage(Scaled(mob.Kind.Damage), mob.X, mob.Z, source: DamageSource.EntityAttack(mob));
            }
        }
        else if (mob.PanicTicks > 0 && mob.FleeFrom is { } from)
        {
            mob.PanicTicks--;
            targetX = mob.X + (mob.X - from.X);
            targetZ = mob.Z + (mob.Z - from.Z);
            speed *= 1.6;
            moving = true;
        }
        else
        {
            if (--mob.WanderTicks <= 0)
            {
                mob.WanderTicks = 60 + _random.Next(120);
                mob.WanderTarget = _random.Next(3) == 0 ? null : (mob.X + _random.Next(17) - 8, mob.Z + _random.Next(17) - 8);
            }
            if (mob.WanderTarget is { } wander && (Math.Abs(wander.X - mob.X) > 0.5 || Math.Abs(wander.Z - mob.Z) > 0.5))
            {
                (targetX, targetZ) = wander;
                speed *= 0.6;
                moving = true;
            }
            else
                (targetX, targetZ) = (mob.X, mob.Z);
        }

        if (!moving)
        {
            mob.Vx *= 0.5;
            mob.Vz *= 0.5;
            return;
        }
        var dx = targetX - mob.X;
        var dz = targetZ - mob.Z;
        var length = Math.Sqrt(dx * dx + dz * dz);
        if (length < 0.1)
            return;
        mob.Yaw = (float)(Math.Atan2(-dx, dz) * 180 / Math.PI);
        var stepX = dx / length * speed;
        var stepZ = dz / length * speed;
        // Do not walk off a ledge higher than 3 blocks.
        if (DropAhead(mob, stepX, stepZ) > 3)
        {
            mob.WanderTarget = null;
            mob.Vx = mob.Vz = 0;
            return;
        }
        mob.Vx = stepX;
        mob.Vz = stepZ;
    }

    private bool SkyAbove(Mob mob)
    {
        int x = (int)Math.Floor(mob.X), z = (int)Math.Floor(mob.Z);
        for (var y = (int)Math.Floor(mob.Y + mob.Kind.Height); y < Math.Min(Chunk.MaxY, (int)mob.Y + 64); y++)
            if (server.World.GetBlock(x, y, z) is { IsAir: false } block && block != Blocks.Water)
                return false; // any cover, leaves included, shades the mob
        return true;
    }

    private int DropAhead(Mob mob, double stepX, double stepZ)
    {
        int x = (int)Math.Floor(mob.X + stepX * 4), z = (int)Math.Floor(mob.Z + stepZ * 4), y = (int)Math.Floor(mob.Y);
        var drop = 0;
        while (drop < 5 && !BlockPhysics.IsSolid(server.World.GetBlock(x, y - 1 - drop, z)))
            drop++;
        return drop;
    }

    // --- Physics ---

    private void Physics(Mob mob)
    {
        var world = server.World;
        var feet = world.GetBlock((int)Math.Floor(mob.X), (int)Math.Floor(mob.Y), (int)Math.Floor(mob.Z));
        var inWater = feet == Blocks.Water;
        if (inWater)
            mob.Vy = Math.Min(mob.Vy + 0.06, 0.12);
        else
            mob.Vy = (mob.Vy - 0.08) * 0.98;

        // Step up one block when walking into a wall.
        if (mob.OnGround && (mob.Vx != 0 || mob.Vz != 0)
            && Collides(mob, mob.X + mob.Vx, mob.Y, mob.Z + mob.Vz) && !Collides(mob, mob.X + mob.Vx, mob.Y + 1.1, mob.Z + mob.Vz))
            mob.Vy = 0.42;

        var ny = mob.Y + mob.Vy;
        if (Collides(mob, mob.X, ny, mob.Z))
        {
            if (mob.Vy < 0)
            {
                mob.OnGround = true;
                mob.Y = Math.Floor(ny) + 1; // land on top of the block
                if (Collides(mob, mob.X, mob.Y, mob.Z))
                    mob.Y = Math.Floor(mob.Y);
            }
            mob.Vy = 0;
        }
        else
        {
            mob.Y = ny;
            mob.OnGround = false;
        }
        if (!Collides(mob, mob.X + mob.Vx, mob.Y, mob.Z))
            mob.X += mob.Vx;
        if (!Collides(mob, mob.X, mob.Y, mob.Z + mob.Vz))
            mob.Z += mob.Vz;
        var friction = mob.OnGround ? 0.6 : 0.91;
        mob.Vx *= friction;
        mob.Vz *= friction;
        if (mob.Y < Chunk.MinY - 32)
            Damage(mob, 1000, null, source: DamageSource.Void());
    }

    private bool Collides(Mob mob, double x, double y, double z)
    {
        var half = mob.Kind.Width / 2 - 0.01;
        for (var bx = (int)Math.Floor(x - half); bx <= (int)Math.Floor(x + half); bx++)
        for (var bz = (int)Math.Floor(z - half); bz <= (int)Math.Floor(z + half); bz++)
        for (var by = (int)Math.Floor(y); by <= (int)Math.Floor(y + mob.Kind.Height - 0.01); by++)
            if (BlockPhysics.IsSolid(server.World.GetBlock(bx, by, bz)))
                return true;
        return false;
    }

    // --- Combat ---

    /// <summary>Applies damage (with knockback away from the attacker); returns false if the mob is invulnerable now.</summary>
    public bool Damage(Mob mob, float amount, (double X, double Z)? from, BedrockConnection? killer = null, int knockback = 0, int looting = 0, DamageSource? source = null)
    {
        if (mob.Dead || mob.HurtTicks > 0 && from is not null)
            return false;

        source ??= killer is not null ? DamageSource.PlayerAttack(killer) : DamageSource.Custom();
        var ev = new EntityDamageEvent(mob, source, amount, from, killer);
        if (!server.Events.OnEntityDamage(ev))
            return false;

        amount = ev.Damage;
        if (amount <= 0)
            return false;

        mob.Health -= amount;
        mob.HurtTicks = 10;
        Broadcast(mob, EntityPackets.Event(mob.EntityId, EntityPackets.EventHurt));
        if (from is { } fromPos)
        {
            var dx = mob.X - fromPos.X;
            var dz = mob.Z - fromPos.Z;
            var length = Math.Max(0.01, Math.Sqrt(dx * dx + dz * dz));
            mob.Vx = dx / length * (0.4 + 0.5 * knockback);
            mob.Vz = dz / length * (0.4 + 0.5 * knockback);
            mob.Vy = 0.36;
            if (!mob.Kind.Hostile)
            {
                mob.PanicTicks = 100;
                mob.FleeFrom = fromPos;
            }
        }
        if (mob.Dead)
        {
            Broadcast(mob, EntityPackets.Event(mob.EntityId, EntityPackets.EventDeath));
            var drops = new List<ItemStack>();
            if (from is not null) // killed by a player: loot
            {
                foreach (var (item, min, max) in mob.Kind.Loot)
                {
                    var count = _random.Next(min, max + 1 + looting);
                    var name = item == "white_wool" && mob.Colour is { } c ? WoolColour(c) : item;
                    if (count > 0 && ItemStack.Of(name, count) is { } stack)
                        drops.Add(stack);
                }
            }
            var droppedXp = mob.Kind.Hostile ? 5 : 1 + _random.Next(3);
            if (server.Events.HasListeners(server.Events.EntityDeath))
            {
                var deathEv = new EntityDeathEvent(mob, killer, drops, droppedXp);
                server.Events.OnEntityDeath(deathEv);
                droppedXp = deathEv.DroppedXp;
            }
            killer?.AddExperience(droppedXp);
            foreach (var stack in drops)
                server.DropItem(stack, (float)mob.X, (float)mob.Y + 0.5f, (float)mob.Z);
        }
        return true;
    }

    private static string WoolColour(int colour) => colour switch
    {
        15 => "black_wool", 7 => "gray_wool", 8 => "light_gray_wool", 12 => "brown_wool", 6 => "pink_wool", _ => "white_wool",
    };

    private void Explode(Mob creeper)
    {
        Remove(creeper, EntityDespawnReason.Death);
        server.Explode(creeper.X, creeper.Y + 0.5, creeper.Z, 3);
    }

    // Vanilla difficulty scaling of mob damage to players.
    private float Scaled(float damage) => server.Config.Difficulty switch
    {
        Difficulty.Peaceful => 0,
        Difficulty.Easy => Math.Min(damage, damage / 2 + 1),
        Difficulty.Hard => damage * 1.5f,
        _ => damage,
    };

    // --- Visibility ---

    private void Sync(Mob mob, List<BedrockConnection> players)
    {
        foreach (var player in players)
        {
            var visible = Distance2(mob, player) < ViewDistance * ViewDistance;
            if (visible && mob.Viewers.Add(player))
                player.Send(mob.AddPacket());
            else if (!visible && mob.Viewers.Remove(player))
                player.Send(Bedrokia.Protocol.Packets.RemoveActor(mob.EntityId));
        }
        mob.Viewers.RemoveWhere(v => !v.IsInGame);
        var (lx, ly, lz, lyaw) = mob.LastSent;
        if (Math.Abs(mob.X - lx) + Math.Abs(mob.Y - ly) + Math.Abs(mob.Z - lz) < 0.01 && Math.Abs(mob.Yaw - lyaw) < 1)
            return;
        mob.LastSent = (mob.X, mob.Y, mob.Z, mob.Yaw);
        Broadcast(mob, EntityPackets.MoveAbsolute(mob.EntityId, mob.X, mob.Y, mob.Z, 0, mob.Yaw, mob.OnGround));
    }

    private void Broadcast(Mob mob, byte[] packet)
    {
        foreach (var viewer in mob.Viewers)
            viewer.Send(packet);
    }

    internal void Remove(Mob mob, EntityDespawnReason reason = EntityDespawnReason.Death)
    {
        server.Events.OnEntityDespawn(new EntityDespawnEvent(mob, reason));
        _mobs.Remove(mob);
        Broadcast(mob, Bedrokia.Protocol.Packets.RemoveActor(mob.EntityId));
        mob.Viewers.Clear();
    }

    /// <summary>A player left: forget it as a viewer.</summary>
    public void Forget(BedrockConnection player)
    {
        foreach (var mob in _mobs)
            mob.Viewers.Remove(player);
    }

    private static double Distance2(Mob mob, BedrockConnection player) => Distance2(mob, player.Position.X, player.Position.Z)
        + Math.Pow(player.Position.Y - BedrockConnection.EyeHeight - mob.Y, 2);

    private static double Distance2(Mob mob, double x, double z) => (mob.X - x) * (mob.X - x) + (mob.Z - z) * (mob.Z - z);
}

/// <summary>Which blocks entities collide with.</summary>
public static class BlockPhysics
{
    private static readonly HashSet<string> Passable =
    [
        "minecraft:air", "minecraft:water", "minecraft:flowing_water", "minecraft:lava", "minecraft:flowing_lava",
        "minecraft:short_grass", "minecraft:tall_grass", "minecraft:fern", "minecraft:large_fern", "minecraft:deadbush",
        "minecraft:dandelion", "minecraft:poppy", "minecraft:blue_orchid", "minecraft:allium", "minecraft:azure_bluet",
        "minecraft:red_tulip", "minecraft:orange_tulip", "minecraft:white_tulip", "minecraft:pink_tulip", "minecraft:oxeye_daisy",
        "minecraft:cornflower", "minecraft:lily_of_the_valley", "minecraft:sunflower", "minecraft:lilac", "minecraft:rose_bush",
        "minecraft:peony", "minecraft:reeds", "minecraft:snow_layer", "minecraft:brown_mushroom", "minecraft:red_mushroom",
        "minecraft:vine", "minecraft:torch", "minecraft:wheat", "minecraft:waterlily",
    ];

    public static bool IsSolid(BlockType? block) =>
        block is not null && !block.IsAir && !Passable.Contains(block.Name) && !block.Name.EndsWith("_sapling");
}
