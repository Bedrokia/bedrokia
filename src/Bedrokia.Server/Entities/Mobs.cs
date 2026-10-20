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

using System.Collections.Frozen;
using Bedrokia.Server.Core;
using Bedrokia.Server.Events;
using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Entities;

/// <summary>How a mob moves: walks, swims (fish, squids, guardians...) or flies (bats, ghasts, phantoms...).</summary>
public enum MobMovement
{
    Ground,
    Swim,
    Fly,
}

/// <summary>A kind of mob with its vanilla (Bedrock) size, health, speed and loot.</summary>
public sealed record MobKind(string Id, float Width, float Height, float MaxHealth, double Speed, bool Hostile, float Damage,
    (string Item, int Min, int Max)[] Loot, bool BurnsInDaylight = false, bool Neutral = false,
    MobMovement Movement = MobMovement.Ground)
{
    /// <summary>A swimmer that also walks on land (drowned, axolotls).</summary>
    public bool Amphibious => Id is "minecraft:drowned" or "minecraft:axolotl";

    /// <summary>Attacks players on sight (hostile and not neutral). Neutral mobs only attack the player who hit them.</summary>
    public bool Aggressive => Hostile && !Neutral;

    // Animaux passifs
    public static readonly MobKind Cow = new("minecraft:cow", 0.9f, 1.3f, 10, 0.2, false, 0, [("beef", 1, 3), ("leather", 0, 2)]);
    public static readonly MobKind Pig = new("minecraft:pig", 0.9f, 0.9f, 10, 0.25, false, 0, [("porkchop", 1, 3)]);
    public static readonly MobKind Sheep = new("minecraft:sheep", 0.9f, 1.3f, 8, 0.23, false, 0, [("mutton", 1, 2), ("white_wool", 1, 1)]);
    public static readonly MobKind Chicken = new("minecraft:chicken", 0.6f, 0.8f, 4, 0.25, false, 0, [("chicken", 1, 1), ("feather", 0, 2)]);
    public static readonly MobKind Rabbit = new("minecraft:rabbit", 0.4f, 0.5f, 3, 0.3, false, 0, [("rabbit", 0, 1), ("rabbit_hide", 0, 1), ("rabbit_foot", 0, 1)]);
    public static readonly MobKind Horse = new("minecraft:horse", 1.4f, 1.6f, 15, 0.225, false, 0, [("leather", 0, 2)]);
    public static readonly MobKind Donkey = new("minecraft:donkey", 1.4f, 1.6f, 15, 0.175, false, 0, [("leather", 0, 2)]);
    public static readonly MobKind Mule = new("minecraft:mule", 1.4f, 1.6f, 15, 0.175, false, 0, [("leather", 0, 2)]);
    public static readonly MobKind Llama = new("minecraft:llama", 0.9f, 1.87f, 15, 0.2, false, 0, [("leather", 0, 2)], Neutral: true);
    public static readonly MobKind Wolf = new("minecraft:wolf", 0.6f, 0.85f, 8, 0.3, false, 2, [], Neutral: true);
    public static readonly MobKind Cat = new("minecraft:cat", 0.6f, 0.7f, 10, 0.3, false, 0, [("string", 0, 2)]);
    public static readonly MobKind Ocelot = new("minecraft:ocelot", 0.6f, 0.7f, 10, 0.3, false, 0, []);
    public static readonly MobKind Parrot = new("minecraft:parrot", 0.5f, 0.9f, 6, 0.2, false, 0, [("feather", 1, 2)], Movement: MobMovement.Fly);
    public static readonly MobKind Bat = new("minecraft:bat", 0.5f, 0.9f, 6, 0.25, false, 0, [], Movement: MobMovement.Fly);
    public static readonly MobKind Mooshroom = new("minecraft:mooshroom", 0.9f, 1.3f, 10, 0.2, false, 0, [("beef", 1, 3), ("leather", 0, 2)]);
    public static readonly MobKind SquidGlow = new("minecraft:glow_squid", 0.8f, 0.8f, 10, 0.2, false, 0, [("glow_ink_sac", 1, 3)], Movement: MobMovement.Swim);
    public static readonly MobKind Squid = new("minecraft:squid", 0.8f, 0.8f, 10, 0.2, false, 0, [("ink_sac", 1, 3)], Movement: MobMovement.Swim);
    public static readonly MobKind Cod = new("minecraft:cod", 0.5f, 0.3f, 3, 0.2, false, 0, [("cod", 1, 1)], Movement: MobMovement.Swim);
    public static readonly MobKind Salmon = new("minecraft:salmon", 0.7f, 0.4f, 3, 0.2, false, 0, [("salmon", 1, 1)], Movement: MobMovement.Swim);
    public static readonly MobKind TropicalFish = new("minecraft:tropicalfish", 0.5f, 0.4f, 3, 0.2, false, 0, [("tropical_fish", 1, 1)], Movement: MobMovement.Swim);
    public static readonly MobKind Pufferfish = new("minecraft:pufferfish", 0.7f, 0.7f, 3, 0.2, false, 0, [("pufferfish", 1, 1)], Movement: MobMovement.Swim);
    public static readonly MobKind Dolphin = new("minecraft:dolphin", 0.9f, 0.6f, 10, 0.4, false, 3, [("cod", 0, 1)], Neutral: true, Movement: MobMovement.Swim);
    public static readonly MobKind Turtle = new("minecraft:turtle", 1.2f, 0.4f, 30, 0.1, false, 0, [("seagrass", 0, 2)]);
    public static readonly MobKind Panda = new("minecraft:panda", 1.3f, 1.25f, 20, 0.15, false, 6, [("bamboo", 0, 2)], Neutral: true);
    public static readonly MobKind PolarBear = new("minecraft:polar_bear", 1.3f, 1.4f, 30, 0.25, false, 6, [("cod", 0, 2), ("salmon", 0, 2)], Neutral: true);
    public static readonly MobKind Fox = new("minecraft:fox", 0.6f, 0.7f, 10, 0.3, false, 2, []);
    public static readonly MobKind Bee = new("minecraft:bee", 0.7f, 0.6f, 10, 0.3, false, 2, [("honeycomb", 1, 3)], Neutral: true, Movement: MobMovement.Fly);
    public static readonly MobKind Strider = new("minecraft:strider", 0.9f, 1.7f, 20, 0.16, false, 0, [("string", 2, 5)]);
    public static readonly MobKind Axolotl = new("minecraft:axolotl", 0.75f, 0.42f, 14, 0.2, false, 2, [], Movement: MobMovement.Swim);
    public static readonly MobKind Goat = new("minecraft:goat", 0.9f, 1.3f, 10, 0.25, false, 2, [("mutton", 1, 2)], Neutral: true);
    public static readonly MobKind Frog = new("minecraft:frog", 0.5f, 0.5f, 10, 0.2, false, 0, []);
    public static readonly MobKind Tadpole = new("minecraft:tadpole", 0.4f, 0.3f, 6, 0.2, false, 0, []);
    public static readonly MobKind Allay = new("minecraft:allay", 0.35f, 0.6f, 20, 0.25, false, 0, [], Movement: MobMovement.Fly);
    public static readonly MobKind Camel = new("minecraft:camel", 1.7f, 2.375f, 32, 0.09, false, 0, []);
    public static readonly MobKind Sniffer = new("minecraft:sniffer", 1.9f, 1.75f, 14, 0.1, false, 0, []);
    
    // Villageois et commerce
    public static readonly MobKind Villager = new("minecraft:villager", 0.6f, 1.95f, 20, 0.2, false, 0, []);
    public static readonly MobKind ZombieVillager = new("minecraft:zombie_villager", 0.6f, 1.95f, 20, 0.23, true, 3, [("rotten_flesh", 0, 2)], BurnsInDaylight: true);
    public static readonly MobKind WanderingTrader = new("minecraft:wandering_trader", 0.6f, 1.95f, 20, 0.2, false, 0, []);
    
    // Golems
    public static readonly MobKind IronGolem = new("minecraft:iron_golem", 1.4f, 2.7f, 100, 0.25, false, 7, [("iron_ingot", 3, 5), ("poppy", 0, 2)], Neutral: true);
    public static readonly MobKind SnowGolem = new("minecraft:snow_golem", 0.7f, 1.9f, 4, 0.2, false, 0, [("snowball", 0, 15)]);
    
    // Monstres hostiles - Overworld
    public static readonly MobKind Zombie = new("minecraft:zombie", 0.6f, 1.9f, 20, 0.23, true, 3, [("rotten_flesh", 0, 2)], BurnsInDaylight: true);
    public static readonly MobKind Husk = new("minecraft:husk", 0.6f, 1.9f, 20, 0.23, true, 3, [("rotten_flesh", 0, 2)], BurnsInDaylight: false);
    public static readonly MobKind Drowned = new("minecraft:drowned", 0.6f, 1.95f, 20, 0.23, true, 3, [("rotten_flesh", 0, 2), ("copper_ingot", 0, 1)], Movement: MobMovement.Swim);
    public static readonly MobKind Skeleton = new("minecraft:skeleton", 0.6f, 1.99f, 20, 0.25, true, 3, [("bone", 0, 2), ("arrow", 0, 2)], BurnsInDaylight: true);
    public static readonly MobKind Stray = new("minecraft:stray", 0.6f, 1.99f, 20, 0.25, true, 3, [("bone", 0, 2), ("arrow", 0, 2)], BurnsInDaylight: true);
    public static readonly MobKind Creeper = new("minecraft:creeper", 0.6f, 1.7f, 20, 0.2, true, 0, [("gunpowder", 0, 2)]);
    public static readonly MobKind Spider = new("minecraft:spider", 1.4f, 0.9f, 16, 0.3, true, 2, [("string", 0, 2), ("spider_eye", 0, 1)]);
    public static readonly MobKind CaveSpider = new("minecraft:cave_spider", 0.7f, 0.5f, 12, 0.3, true, 2, [("string", 0, 2), ("spider_eye", 0, 1)]);
    public static readonly MobKind Enderman = new("minecraft:enderman", 0.6f, 2.9f, 40, 0.3, true, 7, [("ender_pearl", 0, 1)], Neutral: true);
    public static readonly MobKind Witch = new("minecraft:witch", 0.6f, 1.95f, 26, 0.25, true, 0, [("glowstone_dust", 0, 2), ("sugar", 0, 2), ("redstone", 0, 2), ("spider_eye", 0, 2)]);
    public static readonly MobKind Slime = new("minecraft:slime", 2.04f, 2.04f, 16, 0.2, true, 4, [("slime_ball", 0, 2)]);
    public static readonly MobKind Phantom = new("minecraft:phantom", 0.9f, 0.5f, 20, 0.2, true, 2, [("phantom_membrane", 0, 1)], BurnsInDaylight: true, Movement: MobMovement.Fly);
    public static readonly MobKind Silverfish = new("minecraft:silverfish", 0.4f, 0.3f, 8, 0.25, true, 1, []);
    public static readonly MobKind Endermite = new("minecraft:endermite", 0.4f, 0.3f, 8, 0.25, true, 2, []);
    public static readonly MobKind Guardian = new("minecraft:guardian", 0.85f, 0.85f, 30, 0.2, true, 6, [("prismarine_shard", 0, 2), ("prismarine_crystals", 0, 1), ("cod", 0, 1)], Movement: MobMovement.Swim);
    public static readonly MobKind ElderGuardian = new("minecraft:elder_guardian", 1.9975f, 1.9975f, 80, 0.2, true, 8, [("prismarine_shard", 0, 2), ("prismarine_crystals", 0, 1), ("cod", 0, 1)], Movement: MobMovement.Swim);
    public static readonly MobKind Shulker = new("minecraft:shulker", 1.0f, 1.0f, 30, 0.0, true, 4, [("shulker_shell", 0, 1)]);
    public static readonly MobKind Vindicator = new("minecraft:vindicator", 0.6f, 1.95f, 24, 0.35, true, 5, [("emerald", 0, 1)]);
    public static readonly MobKind Evoker = new("minecraft:evoker", 0.6f, 1.95f, 24, 0.2, true, 6, [("totem_of_undying", 1, 1), ("emerald", 0, 1)]);
    public static readonly MobKind Pillager = new("minecraft:pillager", 0.6f, 1.95f, 24, 0.35, true, 5, [("arrow", 0, 2)]);
    public static readonly MobKind Ravager = new("minecraft:ravager", 1.95f, 2.2f, 100, 0.3, true, 12, [("saddle", 1, 1)]);
    public static readonly MobKind Vex = new("minecraft:vex", 0.4f, 0.8f, 14, 0.4, true, 3, [], Movement: MobMovement.Fly);
    public static readonly MobKind Warden = new("minecraft:warden", 0.9f, 2.9f, 500, 0.3, true, 30, [("sculk_catalyst", 1, 1)]);
    
    // Monstres hostiles - Nether
    public static readonly MobKind Ghast = new("minecraft:ghast", 4.0f, 4.0f, 10, 0.2, true, 0, [("ghast_tear", 0, 1), ("gunpowder", 0, 2)], Movement: MobMovement.Fly);
    public static readonly MobKind ZombiePigman = new("minecraft:zombie_pigman", 0.6f, 1.95f, 20, 0.23, true, 5, [("rotten_flesh", 0, 1), ("gold_nugget", 0, 1), ("gold_ingot", 0, 1)], Neutral: true);
    public static readonly MobKind Piglin = new("minecraft:piglin", 0.6f, 1.95f, 16, 0.35, true, 5, [("porkchop", 1, 3)]);
    public static readonly MobKind PiglinBrute = new("minecraft:piglin_brute", 0.6f, 1.95f, 50, 0.35, true, 7, []);
    public static readonly MobKind Hoglin = new("minecraft:hoglin", 1.4f, 1.4f, 40, 0.3, true, 6, [("porkchop", 2, 4), ("leather", 0, 1)]);
    public static readonly MobKind Zoglin = new("minecraft:zoglin", 1.4f, 1.4f, 40, 0.3, true, 6, [("rotten_flesh", 1, 3)]);
    public static readonly MobKind Blaze = new("minecraft:blaze", 0.6f, 1.8f, 20, 0.23, true, 6, [("blaze_rod", 0, 1)], Movement: MobMovement.Fly);
    public static readonly MobKind MagmaCube = new("minecraft:magma_cube", 2.04f, 2.04f, 16, 0.2, true, 6, [("magma_cream", 0, 1)]);
    public static readonly MobKind WitherSkeleton = new("minecraft:wither_skeleton", 0.7f, 2.4f, 20, 0.25, true, 8, [("coal", 0, 1), ("bone", 0, 2), ("wither_skeleton_skull", 0, 1)]);
    
    // Monstres hostiles - End
    public static readonly MobKind EnderDragon = new("minecraft:ender_dragon", 16.0f, 8.0f, 200, 0.2, true, 10, [("dragon_egg", 1, 1)], Movement: MobMovement.Fly);
    
    // Boss
    public static readonly MobKind Wither = new("minecraft:wither", 0.9f, 3.5f, 300, 0.25, true, 8, [("nether_star", 1, 1)], Movement: MobMovement.Fly);

    public static readonly MobKind[] Passive = [Cow, Pig, Sheep, Chicken, Rabbit, Horse, Donkey, Llama, Mooshroom, Panda, Fox, Bee, Goat, Camel];
    public static readonly MobKind[] HostileKinds = [Zombie, Zombie, Skeleton, Skeleton, Creeper, Spider, Enderman, Witch, Phantom, Husk, Drowned, Stray, CaveSpider];
    
    // Dictionnaire pour trouver les mobs par leur ID
    public static readonly Dictionary<string, MobKind> ById = new()
    {
        ["cow"] = Cow, ["pig"] = Pig, ["sheep"] = Sheep, ["chicken"] = Chicken, ["rabbit"] = Rabbit,
        ["horse"] = Horse, ["donkey"] = Donkey, ["mule"] = Mule, ["llama"] = Llama,
        ["wolf"] = Wolf, ["cat"] = Cat, ["ocelot"] = Ocelot, ["parrot"] = Parrot, ["bat"] = Bat,
        ["mooshroom"] = Mooshroom, ["glow_squid"] = SquidGlow, ["squid"] = Squid,
        ["cod"] = Cod, ["salmon"] = Salmon, ["tropical_fish"] = TropicalFish, ["pufferfish"] = Pufferfish, ["dolphin"] = Dolphin,
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

    /// <summary>The world (overworld or Nether) it lives in; null only on a server without a world (tests).</summary>
    public World? World { get; internal set; }

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

    /// <summary>A baby: half size, no loot, grows up after 20 minutes (feeding it speeds that up).</summary>
    public bool Baby { get; internal set; }

    /// <summary>A sheared sheep: no wool until it has eaten grass.</summary>
    public bool Sheared { get; internal set; }

    /// <summary>Name given with a name tag, shown above it; a named mob never despawns.</summary>
    public string? NameTag { get; set; }

    /// <summary>Kept when its chunk unloads and saved with the world (animals and other non-hostile mobs).</summary>
    public bool Persistent { get; set; }

    internal int HurtTicks, DeathTicks, AttackCooldown, PanicTicks, FuseTicks, WanderTicks;
    internal int AgeTicks; // babies: negative, adult at 0; adults: breeding cooldown
    internal int LoveTicks, EggTicks, AngerTicks, PathTicks;
    internal BedrockConnection? LoveCause, AngryAt;
    internal double FallDistance;
    internal (double X, double Z)? WanderTarget;
    internal (double X, double Z)? FleeFrom;
    internal List<(int X, int Y, int Z)>? Path;
    internal (double X, double Y, double Z)? FlyTarget; // flyers and swimmers: where they head
    internal int SwoopTicks, Burst, DryTicks;
    internal double CircleAngle;
    internal int PathIndex;
    internal (int X, int Y, int Z) PathGoal;
    internal bool Burning;
    /// <summary>Ticks left on fire (Fire Aspect, lava, fire).</summary>
    public int FireTicks;
    internal (double X, double Y, double Z, float Yaw) LastSent = (double.NaN, 0, 0, 0);
    internal readonly HashSet<BedrockConnection> Viewers = [];

    public float Width => Baby ? Kind.Width / 2 : Kind.Width;
    public float Height => Baby ? Kind.Height / 2 : Kind.Height;

    public long Flags => EntityPackets.FlagHasCollision | EntityPackets.FlagGravity | EntityPackets.FlagBreathing
                         | (Burning ? EntityPackets.FlagOnFire : 0)
                         | (Baby ? EntityPackets.FlagBaby : 0)
                         | (LoveTicks > 0 ? EntityPackets.FlagInLove : 0)
                         | (Sheared ? EntityPackets.FlagSheared : 0)
                         | (FuseTicks > 0 ? EntityPackets.FlagIgnited : 0)
                         | (AngerTicks > 0 ? EntityPackets.FlagAngry : 0);

    public byte[] AddPacket() =>
        EntityPackets.AddActor(EntityId, Kind.Id, X, Y, Z, Yaw, Health, Kind.MaxHealth, (float)Kind.Speed, Width, Height, Flags, Colour,
            scale: Baby ? 0.5f : 1, nameTag: NameTag);

    internal byte[] DataPacket() => EntityPackets.SetData(EntityId, Flags, Width, Height, Colour, Baby ? 0.5f : 1, NameTag);
}

/// <summary>
/// Mobs, vanilla-like: natural spawning (animals on grass by biome; monsters in the dark, at night or in caves, by biome;
/// Nether mobs in the Nether), A* pathfinding, hostile / neutral / passive behaviour, breeding and babies, shearing,
/// milking, eggs, burning in daylight, fall, fire and lava damage, loot (cooked when burning) and despawning.
/// Animals are saved with their world (entities.json). AI rules follow Cuberite's (Apache-2.0) monster behaviour.
/// </summary>
/// <remarks>
/// ponytail: light is approximated from light sources within 14 blocks (walls do not block it); no water, flying or
/// boss AI (squids, fish, bats, ghasts, phantoms only come from spawn eggs and plugins); skeletons hit at range without
/// arrow entities; no taming, villager trading or raids.
/// </remarks>
public sealed class MobManager(BedrokiaServer server)
{
    public const int ViewDistance = 80, DespawnDistance = 128, PassiveCap = 12, HostileCap = 18, WaterCap = 8, AmbientCap = 4;
    private const int ActiveRange = 64, AdultAgeTicks = 24000, BreedCooldownTicks = 6000, LoveTicks = 600, AngerTicks = 600;
    private const int MaxPathNodes = 600;

    private readonly List<Mob> _mobs = [];
    private readonly List<Mob> _mobScratch = [];
    private readonly List<BedrockConnection> _playerScratch = [];
    private readonly Dictionary<World, List<SavedMob>> _dormant = [];
    private readonly Random _random = new();

    private static readonly FrozenDictionary<string, MobKind> ByFullId =
        MobKind.ById.Values.DistinctBy(k => k.Id).ToFrozenDictionary(k => k.Id);

    // What each animal eats: breeding (adults), growing up (babies) and following the player holding it.
    private static readonly FrozenDictionary<string, string[]> Food = new Dictionary<string, string[]>
    {
        ["minecraft:cow"] = ["wheat"], ["minecraft:mooshroom"] = ["wheat"], ["minecraft:sheep"] = ["wheat"], ["minecraft:goat"] = ["wheat"],
        ["minecraft:pig"] = ["carrot", "potato", "beetroot"],
        ["minecraft:chicken"] = ["wheat_seeds", "pumpkin_seeds", "melon_seeds", "beetroot_seeds", "torchflower_seeds", "pitcher_pod"],
        ["minecraft:rabbit"] = ["carrot", "golden_carrot", "dandelion"],
        ["minecraft:horse"] = ["golden_apple", "golden_carrot"], ["minecraft:donkey"] = ["golden_apple", "golden_carrot"],
        ["minecraft:llama"] = ["hay_block"], ["minecraft:fox"] = ["sweet_berries", "glow_berries"], ["minecraft:panda"] = ["bamboo"],
        ["minecraft:turtle"] = ["seagrass"], ["minecraft:bee"] = ["dandelion", "poppy", "cornflower", "allium", "oxeye_daisy"],
        ["minecraft:frog"] = ["slime_ball"], ["minecraft:camel"] = ["cactus"], ["minecraft:sniffer"] = ["torchflower_seeds"],
        ["minecraft:strider"] = ["warped_fungus"], ["minecraft:hoglin"] = ["crimson_fungus"],
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, string> Cooked = new Dictionary<string, string>
    {
        ["beef"] = "cooked_beef", ["porkchop"] = "cooked_porkchop", ["chicken"] = "cooked_chicken", ["mutton"] = "cooked_mutton",
        ["rabbit"] = "cooked_rabbit", ["cod"] = "cooked_cod", ["salmon"] = "cooked_salmon",
    }.ToFrozenDictionary();

    // Light level of the blocks that emit light (Bedrock values).
    private static readonly FrozenDictionary<string, int> Emission = new Dictionary<string, int>
    {
        ["minecraft:torch"] = 14, ["minecraft:soul_torch"] = 10, ["minecraft:redstone_torch"] = 7, ["minecraft:lantern"] = 15,
        ["minecraft:soul_lantern"] = 10, ["minecraft:glowstone"] = 15, ["minecraft:sea_lantern"] = 15, ["minecraft:lit_pumpkin"] = 15,
        ["minecraft:shroomlight"] = 15, ["minecraft:lava"] = 15, ["minecraft:flowing_lava"] = 15, ["minecraft:fire"] = 15,
        ["minecraft:soul_fire"] = 10, ["minecraft:campfire"] = 15, ["minecraft:soul_campfire"] = 10, ["minecraft:end_rod"] = 14,
        ["minecraft:beacon"] = 15, ["minecraft:lit_redstone_lamp"] = 15, ["minecraft:ochre_froglight"] = 15,
        ["minecraft:verdant_froglight"] = 15, ["minecraft:pearlescent_froglight"] = 15, ["minecraft:conduit"] = 15,
        ["minecraft:lit_furnace"] = 13, ["minecraft:lit_blast_furnace"] = 13, ["minecraft:lit_smoker"] = 13,
        ["minecraft:glow_lichen"] = 7, ["minecraft:crying_obsidian"] = 10, ["minecraft:magma"] = 3, ["minecraft:amethyst_cluster"] = 5,
        ["minecraft:copper_torch"] = 14, ["minecraft:enchanting_table"] = 7, ["minecraft:ender_chest"] = 7,
    }.ToFrozenDictionary();

    /// <summary>A mob kept on disk or while its chunk is unloaded.</summary>
    private sealed record SavedMob(string Kind, double X, double Y, double Z, float Yaw, float Health, int? Colour, bool Baby, int Age, bool Sheared,
        string? NameTag = null);

    public IReadOnlyList<Mob> All => _mobs;

    public Mob? Find(long entityId) => _mobs.FirstOrDefault(m => m.EntityId == entityId && !m.Dead);

    /// <summary>Spawns a mob (null when mobs=off or a plugin cancelled it). Default world: the overworld.</summary>
    public Mob? Spawn(MobKind kind, double x, double y, double z, World? world = null)
    {
        if (!server.Config.Mobs)
            return null;
        var mob = new Mob(server.NextEntityId(), kind, x, y, z)
        {
            Yaw = _random.Next(360),
            World = world ?? server.World,
            Persistent = !kind.Hostile && kind.Movement == MobMovement.Ground,
            EggTicks = 6000 + _random.Next(6000),
        };
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
        if (!server.Config.Mobs)
        {
            // mobs=off: none in the world. Animals are kept on disk and come back when mobs are on again.
            foreach (var mob in _mobs.ToList())
                Sleep(mob);
            return;
        }

        _playerScratch.Clear();
        var allPlayers = server.Players;
        for (var i = 0; i < allPlayers.Count; i++)
            if (allPlayers[i].IsInGame)
                _playerScratch.Add(allPlayers[i]);

        if (tick % 20 == 0)
        {
            foreach (var world in server.Worlds)
            {
                Wake(world);
                var here = _playerScratch.Where(p => p.World == world).ToList();
                if (here.Count > 0)
                    SpawnAround(world, here);
            }
        }

        _mobScratch.Clear();
        _mobScratch.AddRange(_mobs);

        for (var mIdx = 0; mIdx < _mobScratch.Count; mIdx++)
        {
            var mob = _mobScratch[mIdx];
            if (mob.World is not { } world)
                continue; // no world (tests): nothing to simulate
            if (mob.Dead)
            {
                if (++mob.DeathTicks >= 20)
                    Remove(mob, EntityDespawnReason.Death);
                continue;
            }

            BedrockConnection? nearest = null;
            var nearestDist2 = double.MaxValue;
            var playersHere = 0;
            for (var pIdx = 0; pIdx < _playerScratch.Count; pIdx++)
            {
                var p = _playerScratch[pIdx];
                if (p.World != world)
                    continue;
                playersHere++;
                if (p.Dead)
                    continue;
                var d2 = Distance2(mob, p);
                if (d2 < nearestDist2)
                {
                    nearestDist2 = d2;
                    nearest = p;
                }
            }

            // Vanilla despawning: monsters far from every player go away, at once beyond 128 blocks, randomly beyond 32.
            if (!mob.Persistent && mob.NameTag is null && playersHere > 0
                && (nearestDist2 > DespawnDistance * DespawnDistance || nearestDist2 > 32 * 32 && _random.Next(800) == 0))
            {
                Remove(mob, EntityDespawnReason.Distance);
                continue;
            }

            if (world.LoadedChunk((int)Math.Floor(mob.X) >> 4, (int)Math.Floor(mob.Z) >> 4) is null)
            {
                Sleep(mob);
                continue;
            }

            var active = nearestDist2 <= ActiveRange * ActiveRange;
            // Entity Activation Range: full AI within 32 blocks, a quarter of the ticks up to 64, none beyond.
            if (active && (nearestDist2 <= 32 * 32 || ((tick + mob.EntityId) & 3) == 0))
                Think(mob, nearest, Math.Sqrt(nearestDist2), tick);
            Live(mob, tick);
            if (mob.Dead)
                continue;

            // A far mob standing still costs nothing.
            if (active || !mob.OnGround || Math.Abs(mob.Vx) + Math.Abs(mob.Vz) > 0.003 || mob.FireTicks > 0)
                Physics(mob, tick);
            if (mob.HurtTicks > 0)
                mob.HurtTicks--;
            if (mob.AttackCooldown > 0)
                mob.AttackCooldown--;
        }

        if (tick % 2 == 0)
        {
            for (var i = 0; i < _mobs.Count; i++)
                Sync(_mobs[i], _playerScratch);
        }
    }

    // --- Spawning ---

    private void SpawnAround(World world, List<BedrockConnection> players)
    {
        var config = server.Config;
        var overworld = world.Dimension == 0;
        var time = world.Time % 24000;
        var night = overworld && time is > 13000 and < 23000;
        foreach (var player in players)
        {
            int passive = 0, hostile = 0, water = 0, ambient = 0, phantoms = 0;
            foreach (var m in _mobs)
            {
                if (m.World != world || Distance2(m, player) >= ActiveRange * ActiveRange)
                    continue;
                if (m.Kind.Hostile)
                    hostile++;
                else if (m.Kind == MobKind.Bat)
                    ambient++;
                else if (m.Kind.Movement == MobMovement.Swim)
                    water++;
                else
                    passive++;
                if (m.Kind == MobKind.Phantom)
                    phantoms++;
            }
            if (overworld && config.SpawnAnimals && passive < PassiveCap && _random.Next(4) == 0)
                TrySpawnAnimals(world, player);
            if (overworld && config.SpawnAnimals && water < WaterCap && _random.Next(2) == 0)
                TrySpawnWater(world, player);
            if (overworld && config.SpawnAnimals && ambient < AmbientCap && _random.Next(4) == 0)
                TrySpawnBat(world, player);
            if (config.SpawnMonsters && config.Difficulty != Difficulty.Peaceful && hostile < HostileCap)
            {
                TrySpawnMonsters(world, player, night);
                // Phantoms: at night after three days, above a player under the open sky.
                if (night && world.Time >= 72000 && phantoms < 3 && _random.Next(60) == 0)
                    TrySpawnPhantoms(world, player);
            }
        }
    }

    private (int X, int Z) RandomColumn(BedrockConnection player)
    {
        var angle = _random.NextDouble() * Math.PI * 2;
        var radius = 24 + _random.Next(24);
        return ((int)Math.Floor(player.Position.X + Math.Cos(angle) * radius), (int)Math.Floor(player.Position.Z + Math.Sin(angle) * radius));
    }

    private static VanillaBiome? BiomeAt(World world, int x, int z) =>
        world.LoadedChunk(x >> 4, z >> 4) is { } chunk && VanillaBiomes.ById.TryGetValue(chunk.GetBiome(x & 15, z & 15), out var biome)
            ? biome
            : null;

    private static bool Tagged(VanillaBiome? biome, string tag) => biome is not null && biome.Tags.Contains(tag);

    private T? Pick<T>(IReadOnlyList<(T Value, int Weight)> table) where T : class
    {
        var total = table.Sum(e => e.Weight);
        if (total <= 0)
            return null;
        var roll = _random.Next(total);
        foreach (var (value, weight) in table)
        {
            if (roll < weight)
                return value;
            roll -= weight;
        }
        return null;
    }

    private void TrySpawnAnimals(World world, BedrockConnection player)
    {
        var (x, z) = RandomColumn(player);
        var biome = BiomeAt(world, x, z);
        var table = AnimalsOf(biome);
        if (Pick(table) is not { } kind)
            return;
        var group = kind == MobKind.PolarBear ? 1 : 2 + _random.Next(3);
        for (var i = 0; i < group; i++)
        {
            int sx = x + _random.Next(5) - 2, sz = z + _random.Next(5) - 2;
            if (Surface(world, sx, sz, (int)player.Position.Y) is not { } ground)
                return;
            var below = world.GetBlock(sx, ground - 1, sz);
            if (below is null || !AnimalGround(below, biome) || !SkyAccess(world, sx, ground, sz))
                return;
            Spawn(kind, sx + 0.5, ground, sz + 0.5, world);
        }
    }

    private static bool AnimalGround(BlockType block, VanillaBiome? biome) =>
        block == Blocks.GrassBlock
        || Tagged(biome, "desert") && block == Blocks.Sand
        || (Tagged(biome, "frozen") || Tagged(biome, "ice")) && block.Name is "minecraft:snow" or "minecraft:snow_layer"
        || Tagged(biome, "mooshroom_island") && block.Name == "minecraft:mycelium";

    private static List<(MobKind, int)> AnimalsOf(VanillaBiome? biome)
    {
        if (biome is null)
            return [];
        if (Tagged(biome, "mooshroom_island"))
            return [(MobKind.Mooshroom, 8)];
        if (Tagged(biome, "frozen") || Tagged(biome, "ice"))
            return [(MobKind.Rabbit, 10), (MobKind.Fox, Tagged(biome, "spawns_snow_foxes") ? 6 : 0), (MobKind.PolarBear, 1)];
        if (Tagged(biome, "desert"))
            return [(MobKind.Rabbit, 4)];
        if (!Tagged(biome, "animal"))
            return [];
        List<(MobKind, int)> table = [(MobKind.Sheep, 12), (MobKind.Pig, 10), (MobKind.Chicken, 10), (MobKind.Cow, 8)];
        if (Tagged(biome, "plains"))
            table.AddRange([(MobKind.Horse, 5), (MobKind.Donkey, 1)]);
        if (Tagged(biome, "savanna"))
            table.AddRange([(MobKind.Horse, 1), (MobKind.Donkey, 1), (MobKind.Llama, 2)]);
        if (Tagged(biome, "taiga"))
            table.AddRange([(MobKind.Wolf, 8), (MobKind.Fox, 8), (MobKind.Rabbit, 4)]);
        else if (Tagged(biome, "forest"))
            table.AddRange([(MobKind.Wolf, 4), (MobKind.Rabbit, 2)]);
        if (Tagged(biome, "extreme_hills") || Tagged(biome, "mountain"))
            table.AddRange([(MobKind.Llama, 5), (MobKind.Goat, 5)]);
        if (Tagged(biome, "jungle"))
            table.AddRange([(MobKind.Ocelot, 2), (MobKind.Panda, 1)]);
        if (Tagged(biome, "swamp"))
            table.Add((MobKind.Frog, 10));
        return table;
    }

    private static List<(MobKind, int)> MonstersOf(VanillaBiome? biome, bool nether)
    {
        if (nether)
        {
            if (Tagged(biome, "crimson_forest"))
                return [(MobKind.Piglin, 5), (MobKind.Hoglin, 9), (MobKind.ZombiePigman, 1)];
            if (Tagged(biome, "warped_forest"))
                return [(MobKind.Enderman, 1)];
            if (Tagged(biome, "soulsand_valley"))
                return [(MobKind.Skeleton, 2)];
            if (Tagged(biome, "basalt_deltas"))
                return [(MobKind.MagmaCube, 100)];
            return [(MobKind.ZombiePigman, 100), (MobKind.Piglin, 15), (MobKind.MagmaCube, 2), (MobKind.Enderman, 1)];
        }
        if (Tagged(biome, "mooshroom_island"))
            return [];
        var desert = Tagged(biome, "desert");
        var frozen = Tagged(biome, "frozen") || Tagged(biome, "ice");
        List<(MobKind, int)> table =
        [
            (desert ? MobKind.Husk : MobKind.Zombie, 95), (MobKind.ZombieVillager, 5),
            (frozen ? MobKind.Stray : MobKind.Skeleton, 100), (MobKind.Creeper, 100), (MobKind.Spider, 100),
            (MobKind.Enderman, 10), (MobKind.Witch, 5),
        ];
        if (Tagged(biome, "swamp"))
            table.Add((MobKind.Slime, 20));
        return table;
    }

    private void TrySpawnMonsters(World world, BedrockConnection player, bool night)
    {
        var nether = world.Dimension == 1;
        var (x, z) = RandomColumn(player);
        var biome = BiomeAt(world, x, z);
        if (nether && Tagged(biome, "spawn_ghast") && _random.Next(20) == 0)
        {
            TrySpawnGhast(world, player, x, z);
            return;
        }
        if (!nether && (Tagged(biome, "ocean") || Tagged(biome, "river")) && _random.Next(Tagged(biome, "river") ? 2 : 4) == 0)
        {
            TrySpawnDrowned(world, player, x, z, night);
            return;
        }
        if (Pick(MonstersOf(biome, nether)) is not { } kind)
            return;
        var feet = (int)Math.Floor(player.Position.Y - BedrockConnection.EyeHeight);
        var height = (int)Math.Ceiling(kind.Height);
        var group = kind == MobKind.Witch ? 1 : 1 + _random.Next(4);
        for (var i = 0; i < group; i++)
        {
            int sx = x + _random.Next(5) - 2, sz = z + _random.Next(5) - 2;
            // Caves too: a random height around the player, then down to the first floor.
            if (FindFloor(world, sx, feet + _random.Next(-16, 17), sz, 16, height) is not { } y)
                continue;
            if (!MonsterGround(world.GetBlock(sx, y - 1, sz)) || !Dark(world, sx, y, sz, night, nether))
                continue;
            Spawn(kind, sx + 0.5, y, sz + 0.5, world);
        }
    }

    private static bool MonsterGround(BlockType? block) =>
        BlockPhysics.IsSolid(block) && !block!.Name.Contains("glass") && !block.Name.Contains("leaves") && !block.Name.Contains("slab")
        && !block.Name.Contains("stairs") && !block.Name.Contains("fence") && !block.Name.Contains("carpet")
        && block.Name is not ("minecraft:barrier" or "minecraft:magma" or "minecraft:ice");

    /// <summary>
    /// Monsters spawn in the dark: no light source close enough to light the spot (block light 0, 11 in the Nether)
    /// and, in the overworld, no open sky in the daytime.
    /// </summary>
    private static bool Dark(World world, int x, int y, int z, bool night, bool nether)
    {
        if (!nether && !night && SkyAccess(world, x, y, z))
            return false;
        var maxLight = nether ? 11 : 0;
        const int reach = 14;
        for (var dx = -reach; dx <= reach; dx++)
        for (var dy = -reach + Math.Abs(dx); dy <= reach - Math.Abs(dx); dy++)
        {
            var left = reach - Math.Abs(dx) - Math.Abs(dy);
            for (var dz = -left; dz <= left; dz++)
            {
                if (world.GetBlock(x + dx, y + dy, z + dz) is not { } block || block.IsAir
                    || !Emission.TryGetValue(block.Name, out var emitted))
                    continue;
                if (emitted - Math.Abs(dx) - Math.Abs(dy) - Math.Abs(dz) > maxLight)
                    return false;
            }
        }
        return true;
    }

    private static bool SkyAccess(World world, int x, int y, int z)
    {
        for (var by = y; by < Chunk.MaxY; by++)
            if (world.GetBlock(x, by, z) is { IsAir: false } block && block != Blocks.Water)
                return false;
        return true;
    }

    // First floor at or below startY: a block to stand on and room for the mob's height, no liquid.
    private static int? FindFloor(World world, int x, int startY, int z, int depth, int height)
    {
        for (var y = Math.Min(startY, Chunk.MaxY - height); y > Math.Max(startY - depth, Chunk.MinY + 1); y--)
        {
            if (!BlockPhysics.IsSolid(world.GetBlock(x, y - 1, z)))
                continue;
            var free = true;
            for (var h = 0; h < height && free; h++)
                free = world.GetBlock(x, y + h, z) is { } b && !BlockPhysics.IsSolid(b) && !Fluids.IsLiquid(b);
            if (free)
                return y;
        }
        return null;
    }

    // First free spot (two air blocks over a solid one) scanning down from above the player's height.
    private static int? Surface(World world, int x, int z, int aroundY)
    {
        for (var y = Math.Min(aroundY + 40, Chunk.MaxY - 2); y > Math.Max(aroundY - 40, Chunk.MinY); y--)
        {
            var block = world.GetBlock(x, y, z);
            if (block is null)
                return null;
            if (Fluids.IsLiquid(block))
                return null;
            if (BlockPhysics.IsSolid(block))
                return BlockPhysics.IsSolid(world.GetBlock(x, y + 1, z)) || BlockPhysics.IsSolid(world.GetBlock(x, y + 2, z)) ? null : y + 1;
        }
        return null;
    }

    // --- Life: growing up, love, anger, eggs, wool ---

    private void Live(Mob mob, long tick)
    {
        var changed = false;
        if (mob.Baby)
        {
            if (++mob.AgeTicks >= 0)
            {
                mob.Baby = false;
                mob.AgeTicks = 0;
                changed = true;
            }
        }
        else if (mob.AgeTicks > 0)
            mob.AgeTicks--;

        if (mob.LoveTicks > 0)
        {
            if (--mob.LoveTicks == 0)
                changed = true;
            else if (tick % 10 == 0)
                Broadcast(mob, EntityPackets.Event(mob.EntityId, EntityPackets.EventLoveParticles));
        }
        if (mob.AngerTicks > 0 && --mob.AngerTicks == 0)
        {
            mob.AngryAt = null;
            changed = true;
        }

        if (mob.Kind == MobKind.Chicken && !mob.Baby && --mob.EggTicks <= 0)
        {
            mob.EggTicks = 6000 + _random.Next(6000);
            if (ItemStack.Of("egg") is { } egg)
                server.DropItem(egg, (float)mob.X, (float)mob.Y + 0.3f, (float)mob.Z, world: mob.World);
        }

        // A sheared sheep eats grass now and then, which grows its wool back.
        if (mob.Sheared && mob.OnGround && _random.Next(1000) == 0)
        {
            int bx = (int)Math.Floor(mob.X), by = (int)Math.Floor(mob.Y) - 1, bz = (int)Math.Floor(mob.Z);
            if (mob.World!.GetBlock(bx, by, bz) == Blocks.GrassBlock)
            {
                server.SetBlockAndUpdate(bx, by, bz, Blocks.Dirt, mob.World);
                Broadcast(mob, EntityPackets.Event(mob.EntityId, 10)); // eat grass animation
                mob.Sheared = false;
                changed = true;
            }
        }
        if (changed)
            Broadcast(mob, mob.DataPacket());
    }

    // --- AI ---

    private void Think(Mob mob, BedrockConnection? player, double distance, long tick)
    {
        // The movement attribute is not blocks per tick: vanilla walking speed is about 0.45x it (zombie ~2 m/s).
        var speed = mob.Kind.Speed * 0.45;
        var world = mob.World!;
        var day = world.Dimension == 0 && world.Time % 24000 is < 12500 or > 23500;
        if (mob.Kind.Movement == MobMovement.Fly)
        {
            ThinkFlying(mob, player, distance, speed, day);
            return;
        }
        if (mob.Kind.Movement == MobMovement.Swim && (InWater(mob) || !mob.Kind.Amphibious))
        {
            ThinkSwimming(mob, player, distance, speed, day);
            return;
        }

        var target = Target(mob, player, distance, day);
        if (target is not null)
        {
            var dist = Math.Sqrt(Distance2(mob, target));
            var feetY = target.Position.Y - BedrockConnection.EyeHeight;
            var moving = true;
            if (mob.Kind == MobKind.Skeleton || mob.Kind == MobKind.Stray || mob.Kind == MobKind.Pillager)
            {
                var sees = LineOfSight(mob, target);
                moving = dist > 8 || !sees;
                if (dist < 15 && sees && mob.AttackCooldown == 0 && Math.Abs(feetY - mob.Y) < 6)
                {
                    mob.AttackCooldown = 40;
                    Broadcast(mob, EntityPackets.Swing(mob.EntityId));
                    ShootArrow(mob, target);
                }
            }
            else if (mob.Kind == MobKind.Creeper)
            {
                if (dist < 3 && LineOfSight(mob, target))
                {
                    moving = false;
                    if (mob.FuseTicks++ == 0)
                        Broadcast(mob, mob.DataPacket()); // starts swelling
                    if (mob.FuseTicks >= 30)
                    {
                        Explode(mob);
                        return;
                    }
                }
                else if (mob.FuseTicks > 0 && --mob.FuseTicks == 0)
                    Broadcast(mob, mob.DataPacket());
            }
            else if (dist < 1.2 + mob.Width && mob.AttackCooldown == 0 && Math.Abs(feetY - mob.Y) < 2)
            {
                mob.AttackCooldown = 20;
                Broadcast(mob, EntityPackets.Swing(mob.EntityId));
                Hit(mob, target, Scaled(mob.Kind.Damage));
            }
            if (moving)
                MoveTowards(mob, target.Position.X, feetY, target.Position.Z, speed, pathfind: true);
            else
                Stop(mob, target.Position.X, target.Position.Z);
            return;
        }
        if (mob.FuseTicks > 0 && --mob.FuseTicks == 0)
            Broadcast(mob, mob.DataPacket());

        if (mob.PanicTicks > 0 && mob.FleeFrom is { } from)
        {
            mob.PanicTicks--;
            MoveTowards(mob, mob.X + (mob.X - from.X), mob.Y, mob.Z + (mob.Z - from.Z), speed * 1.6, pathfind: false);
            return;
        }

        // In love: walk to a partner and breed.
        if (mob.LoveTicks > 0 && FindPartner(mob) is { } partner)
        {
            if (Math.Sqrt(Distance2(mob, partner.X, partner.Z)) < 1.5 + mob.Width)
                Breed(mob, partner);
            else
                MoveTowards(mob, partner.X, partner.Y, partner.Z, speed, pathfind: true);
            return;
        }

        // Animals follow a nearby player holding their food.
        if (player is not null && distance < 8 && !mob.Kind.Hostile && Eats(mob.Kind, player.HeldItem))
        {
            if (distance > 2.5)
                MoveTowards(mob, player.Position.X, player.Position.Y - BedrockConnection.EyeHeight, player.Position.Z, speed, pathfind: true);
            else
                Stop(mob, player.Position.X, player.Position.Z);
            return;
        }

        if (--mob.WanderTicks <= 0)
        {
            mob.WanderTicks = 60 + _random.Next(120);
            mob.WanderTarget = _random.Next(3) == 0 ? null : (mob.X + _random.Next(17) - 8, mob.Z + _random.Next(17) - 8);
        }
        if (mob.WanderTarget is { } wander && (Math.Abs(wander.X - mob.X) > 0.5 || Math.Abs(wander.Z - mob.Z) > 0.5))
            MoveTowards(mob, wander.X, mob.Y, wander.Z, speed * 0.6, pathfind: false);
        else
            Stop(mob, null, null);
    }

    // Who the mob attacks: the player that angered a neutral mob, or the nearest survival player for monsters.
    private BedrockConnection? Target(Mob mob, BedrockConnection? player, double distance, bool day)
    {
        if (mob.AngerTicks > 0 && mob.AngryAt is { IsInGame: true, Dead: false } angry && angry.World == mob.World
            && angry.GameMode is not (GameMode.Creative or GameMode.Spectator) && Distance2(mob, angry) < 32 * 32)
            return angry;
        if (!mob.Kind.Aggressive || player is null || player.GameMode is GameMode.Creative or GameMode.Spectator || distance >= 16)
            return null;
        if (mob.Kind == MobKind.Spider && day && distance >= 4)
            return null; // spiders are neutral in daylight
        if (distance > 4 && mob.Path is null && !LineOfSight(mob, player))
            return null; // does not notice a player behind a wall

        if (server.Events.HasEntityTarget)
        {
            var targetEv = new EntityTargetEvent(mob, player);
            if (!server.Events.OnEntityTarget(targetEv) || targetEv.Target is null)
                return null;
            player = targetEv.Target;
        }
        if (server.Events.HasMobTarget)
        {
            var mobTargetEv = new MobTargetEvent((ulong)mob.EntityId, mob.Kind.Id, (ulong)player.EntityId);
            if (!server.Events.OnMobTarget(mobTargetEv))
                return null;
            if (mobTargetEv.TargetEntityId != (ulong)player.EntityId)
                player = server.Players.FirstOrDefault(p => (ulong)p.EntityId == mobTargetEv.TargetEntityId);
        }
        return player;
    }

    private void Hit(Mob mob, BedrockConnection player, float damage)
    {
        if (server.Events.HasMobAttack)
        {
            var attackEv = new MobAttackEvent((ulong)mob.EntityId, mob.Kind.Id, (ulong)player.EntityId, damage);
            if (!server.Events.OnMobAttack(attackEv))
                return;
            damage = attackEv.Damage;
        }
        player.Damage(damage, mob.X, mob.Z, source: DamageSource.EntityAttack(mob));
    }

    private void Stop(Mob mob, double? lookX, double? lookZ)
    {
        mob.Vx *= 0.5;
        mob.Vz *= 0.5;
        mob.Path = null;
        if (lookX is { } lx && lookZ is { } lz)
            mob.Yaw = (float)(Math.Atan2(-(lx - mob.X), lz - mob.Z) * 180 / Math.PI);
    }

    // Walks towards (x, y, z), along an A* path when asked (chasing, joining a partner), else straight on.
    private void MoveTowards(Mob mob, double x, double y, double z, double speed, bool pathfind)
    {
        double gx = x, gz = z;
        if (pathfind)
        {
            var goal = ((int)Math.Floor(x), (int)Math.Floor(y + 0.01), (int)Math.Floor(z));
            if (--mob.PathTicks <= 0
                || Math.Abs(goal.Item1 - mob.PathGoal.X) + Math.Abs(goal.Item2 - mob.PathGoal.Y) + Math.Abs(goal.Item3 - mob.PathGoal.Z) > 2)
            {
                mob.PathTicks = 20;
                mob.PathGoal = goal;
                mob.Path = FindPath(mob, goal);
                mob.PathIndex = 0;
            }
            if (mob.Path is { } path)
            {
                while (mob.PathIndex < path.Count && Math.Abs(path[mob.PathIndex].X + 0.5 - mob.X) < 0.4
                       && Math.Abs(path[mob.PathIndex].Z + 0.5 - mob.Z) < 0.4)
                    mob.PathIndex++;
                if (mob.PathIndex < path.Count)
                    (gx, gz) = (path[mob.PathIndex].X + 0.5, path[mob.PathIndex].Z + 0.5);
            }
        }
        else
            mob.Path = null;

        var dx = gx - mob.X;
        var dz = gz - mob.Z;
        var length = Math.Sqrt(dx * dx + dz * dz);
        if (length < 0.1)
            return;
        mob.Yaw = (float)(Math.Atan2(-dx, dz) * 180 / Math.PI);
        var stepX = dx / length * speed;
        var stepZ = dz / length * speed;
        // Without a path: never walk off a ledge higher than 3 blocks (a path only has safe drops).
        if (mob.Path is null && DropAhead(mob, stepX, stepZ) > 3)
        {
            mob.WanderTarget = null;
            mob.Vx = mob.Vz = 0;
            return;
        }
        mob.Vx = stepX;
        mob.Vz = stepZ;
    }

    /// <summary>
    /// A* over the blocks the mob can stand on: 8 directions, one block up, three blocks down, no lava, fire or
    /// cactus. Returns the best path found within <see cref="MaxPathNodes"/> nodes (towards the goal if unreachable).
    /// </summary>
    internal List<(int X, int Y, int Z)>? FindPath(Mob mob, (int X, int Y, int Z) goal)
    {
        var world = mob.World!;
        var start = ((int)Math.Floor(mob.X), (int)Math.Floor(mob.Y + 0.01), (int)Math.Floor(mob.Z));
        if (Math.Abs(goal.X - start.Item1) + Math.Abs(goal.Z - start.Item3) > 48)
            return null;
        var height = Math.Max(1, (int)Math.Ceiling(mob.Height));

        static double Heuristic((int X, int Y, int Z) a, (int X, int Y, int Z) b)
        {
            int dx = Math.Abs(a.X - b.X), dz = Math.Abs(a.Z - b.Z);
            return Math.Max(dx, dz) + 0.414 * Math.Min(dx, dz) + Math.Abs(a.Y - b.Y);
        }

        var open = new PriorityQueue<(int X, int Y, int Z), double>();
        var cost = new Dictionary<(int, int, int), double> { [start] = 0 };
        var cameFrom = new Dictionary<(int, int, int), (int, int, int)>();
        open.Enqueue(start, Heuristic(start, goal));
        var best = start;
        var bestH = Heuristic(start, goal);
        var expanded = 0;
        (int X, int Z)[] directions = [(1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)];

        while (open.TryDequeue(out var node, out _) && expanded++ < MaxPathNodes)
        {
            var h = Heuristic(node, goal);
            if (h < bestH)
                (best, bestH) = (node, h);
            if (Math.Abs(node.X - goal.X) <= 1 && Math.Abs(node.Z - goal.Z) <= 1 && Math.Abs(node.Y - goal.Y) <= 1)
            {
                best = node;
                break;
            }
            foreach (var (ddx, ddz) in directions)
            {
                int nx = node.X + ddx, nz = node.Z + ddz;
                var diagonal = ddx != 0 && ddz != 0;
                // No cutting corners: both sides of a diagonal step must be free.
                if (diagonal && (!Passable(world, node.X + ddx, node.Y, node.Z, height) || !Passable(world, node.X, node.Y, node.Z + ddz, height)))
                    continue;
                int? ny = null;
                foreach (var dy in (ReadOnlySpan<int>)[0, 1, -1, -2, -3])
                {
                    var y = node.Y + dy;
                    if (dy == 1 && !Passable(world, node.X, node.Y + height, node.Z, 1))
                        continue; // no room to jump
                    if (dy < 0 && !Passable(world, nx, y + 1, nz, -dy + height - 1))
                        break; // the drop is blocked
                    if (Standable(world, nx, y, nz, height))
                    {
                        ny = y;
                        break;
                    }
                }
                if (ny is not { } stepY)
                    continue;
                var next = (nx, stepY, nz);
                var stepCost = (diagonal ? 1.414 : 1) + (stepY > node.Y ? 1 : 0) + (node.Y - stepY) * 0.5
                               + (world.GetBlock(nx, stepY, nz) == Blocks.Water ? 2 : 0);
                var newCost = cost[node] + stepCost;
                if (cost.TryGetValue(next, out var old) && old <= newCost)
                    continue;
                cost[next] = newCost;
                cameFrom[next] = node;
                open.Enqueue(next, newCost + Heuristic(next, goal));
            }
        }
        if (best == start)
            return null;
        var path = new List<(int X, int Y, int Z)>();
        for (var at = best; at != start; at = cameFrom[at])
            path.Add(at);
        path.Reverse();
        return path;
    }

    private static bool Passable(World world, int x, int y, int z, int height)
    {
        for (var h = 0; h < height; h++)
            if (world.GetBlock(x, y + h, z) is not { } block || BlockPhysics.IsSolid(block) || Dangerous(block))
                return false;
        return true;
    }

    private static bool Standable(World world, int x, int y, int z, int height) =>
        world.GetBlock(x, y - 1, z) is { } floor && BlockPhysics.IsSolid(floor) && !Dangerous(floor) && Passable(world, x, y, z, height);

    private static bool Dangerous(BlockType block) => block.Name is "minecraft:lava" or "minecraft:flowing_lava" or "minecraft:fire"
        or "minecraft:soul_fire" or "minecraft:cactus" or "minecraft:magma" or "minecraft:sweet_berry_bush" or "minecraft:campfire";

    private static bool LineOfSight(Mob mob, BedrockConnection player)
    {
        var world = mob.World!;
        double sx = mob.X, sy = mob.Y + mob.Height * 0.85, sz = mob.Z;
        double dx = player.Position.X - sx, dy = player.Position.Y - sy, dz = player.Position.Z - sz;
        var length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        var steps = (int)(length / 0.3);
        for (var i = 1; i < steps; i++)
        {
            var t = i / (double)steps;
            if (BlockPhysics.IsSolid(world.GetBlock((int)Math.Floor(sx + dx * t), (int)Math.Floor(sy + dy * t), (int)Math.Floor(sz + dz * t))))
                return false;
        }
        return true;
    }

    private int DropAhead(Mob mob, double stepX, double stepZ)
    {
        int x = (int)Math.Floor(mob.X + stepX * 4), z = (int)Math.Floor(mob.Z + stepZ * 4), y = (int)Math.Floor(mob.Y);
        var drop = 0;
        while (drop < 5 && !BlockPhysics.IsSolid(mob.World!.GetBlock(x, y - 1 - drop, z)))
            drop++;
        return drop;
    }

    // --- Ranged attacks ---

    // A real arrow with vanilla's aim (a third of the target's height), arc (+20 % of the distance) and spread (less in hard).
    private void ShootArrow(Mob mob, BedrockConnection target)
    {
        var from = new System.Numerics.Vector3((float)mob.X, (float)(mob.Y + mob.Height * 0.85), (float)mob.Z);
        float dx = target.Position.X - from.X, dz = target.Position.Z - from.Z;
        var dy = target.Position.Y - BedrockConnection.EyeHeight + 0.6f - from.Y; // a third of the height: the arc lifts it to the chest
        var horizontal = MathF.Sqrt(dx * dx + dz * dz);
        var direction = System.Numerics.Vector3.Normalize(new(dx, dy + horizontal * 0.2f, dz));
        var spread = 0.0075f * (14 - 4 * (int)server.Config.Difficulty);
        direction += new System.Numerics.Vector3(Gaussian(), Gaussian(), Gaussian()) * spread;
        server.Projectiles.Shoot(mob, ProjectileManager.Arrow, from + direction * 0.5f, direction * 1.6f, Scaled(2 + _random.Next(3)));
        server.PlaySound("random.bow", mob.X, mob.Y, mob.Z, mob.World);
    }

    private void ShootFireball(Mob mob, BedrockConnection target, string identifier, float speed, float damage, float spread)
    {
        var from = new System.Numerics.Vector3((float)mob.X, (float)(mob.Y + mob.Height / 2), (float)mob.Z);
        var to = new System.Numerics.Vector3(target.Position.X, target.Position.Y - 0.6f, target.Position.Z);
        var direction = System.Numerics.Vector3.Normalize(to - from)
                        + new System.Numerics.Vector3(Gaussian(), Gaussian(), Gaussian()) * spread;
        direction = System.Numerics.Vector3.Normalize(direction);
        server.Projectiles.Shoot(mob, identifier, from + direction * (mob.Width / 2 + 0.6f), direction * speed, damage);
        server.PlaySound(identifier == ProjectileManager.Fireball ? "mob.ghast.fireball" : "mob.blaze.shoot", mob.X, mob.Y, mob.Z, mob.World);
    }

    private float Gaussian() => (float)(_random.NextDouble() + _random.NextDouble() + _random.NextDouble() - 1.5);

    // --- Flying mobs: bats, phantoms, ghasts, blazes, bees, allays, vexes ---

    private void ThinkFlying(Mob mob, BedrockConnection? player, double distance, double speed, bool day)
    {
        var survival = player is not null && player.GameMode is not (GameMode.Creative or GameMode.Spectator) && !player.Dead;
        if (mob.Kind == MobKind.Bat)
        {
            // Erratic flight in caves.
            if (mob.FlyTarget is null || _random.Next(30) == 0 || Reached(mob, mob.FlyTarget.Value))
                mob.FlyTarget = RandomAir(mob, 6, 3);
            if (mob.FlyTarget is { } bat)
                HeadTo(mob, bat.X, bat.Y, bat.Z, 0.12);
            return;
        }
        if (mob.Kind == MobKind.Phantom)
        {
            ThinkPhantom(mob, survival && distance < 64 && SkyAccess(mob.World!, (int)Math.Floor(player!.Position.X),
                (int)Math.Floor(player.Position.Y), (int)Math.Floor(player.Position.Z)) ? player : null);
            return;
        }
        if (mob.Kind == MobKind.Ghast)
        {
            if (survival && distance < 64 && LineOfSight(mob, player!))
            {
                Look(mob, player!.Position.X, player.Position.Z);
                if (mob.AttackCooldown == 0)
                {
                    mob.AttackCooldown = 60;
                    ShootFireball(mob, player, ProjectileManager.Fireball, 0.9f, 6, 0.02f);
                }
            }
            if (mob.FlyTarget is null || _random.Next(80) == 0 || Reached(mob, mob.FlyTarget.Value))
                mob.FlyTarget = RandomAir(mob, 16, 6);
            if (mob.FlyTarget is { } ghast)
                HeadTo(mob, ghast.X, ghast.Y, ghast.Z, 0.05, look: !(survival && distance < 64));
            return;
        }

        var target = Target(mob, player, distance, day);
        if (target is not null)
        {
            var feet = target.Position.Y - BedrockConnection.EyeHeight;
            var dist = Math.Sqrt(Distance2(mob, target));
            if (mob.Kind == MobKind.Blaze)
            {
                // Hovers two blocks above the player's feet and fires three small fireballs every five seconds.
                if (dist > 6)
                    HeadTo(mob, target.Position.X, feet + 2, target.Position.Z, speed);
                else
                {
                    HeadTo(mob, mob.X, feet + 2, mob.Z, speed * 0.5, look: false);
                    Look(mob, target.Position.X, target.Position.Z);
                }
                if (mob.AttackCooldown == 0 && LineOfSight(mob, target))
                {
                    if (mob.Burst == 0)
                        mob.Burst = 3;
                    ShootFireball(mob, target, ProjectileManager.SmallFireball, 0.8f, Scaled(5), 0.08f);
                    mob.AttackCooldown = --mob.Burst > 0 ? 6 : 100;
                }
                return;
            }
            HeadTo(mob, target.Position.X, target.Position.Y - 0.6, target.Position.Z, speed * 1.4);
            if (dist < 1.2 + mob.Width && mob.AttackCooldown == 0)
            {
                mob.AttackCooldown = 20;
                Hit(mob, target, Scaled(mob.Kind.Damage));
            }
            return;
        }
        // Bees, allays, parrots...: hover around, close to the ground.
        if (mob.FlyTarget is null || _random.Next(60) == 0 || Reached(mob, mob.FlyTarget.Value))
            mob.FlyTarget = RandomAir(mob, 8, 2);
        if (mob.FlyTarget is { } around)
            HeadTo(mob, around.X, around.Y, around.Z, speed * 0.6);
    }

    // Phantoms circle high above their target, then swoop down to bite and climb back.
    private void ThinkPhantom(Mob mob, BedrockConnection? target)
    {
        if (target is null)
        {
            mob.SwoopTicks = 0;
            mob.CircleAngle += 0.04;
            HeadTo(mob, mob.X + Math.Cos(mob.CircleAngle) * 4, mob.Y + (_random.NextDouble() - 0.5) * 0.5, mob.Z + Math.Sin(mob.CircleAngle) * 4, 0.15);
            return;
        }
        var (px, py, pz) = target.Position;
        if (mob.SwoopTicks > 0)
        {
            mob.SwoopTicks--;
            HeadTo(mob, px, py - 0.8, pz, 0.35);
            if (Math.Sqrt(Distance2(mob, target)) < 1.6)
            {
                Hit(mob, target, Scaled(mob.Kind.Damage));
                mob.SwoopTicks = 0;
                mob.AttackCooldown = 80 + _random.Next(60);
            }
            return;
        }
        mob.CircleAngle += 0.05;
        HeadTo(mob, px + Math.Cos(mob.CircleAngle) * 12, py + 10, pz + Math.Sin(mob.CircleAngle) * 12, 0.22);
        if (mob.AttackCooldown == 0 && _random.Next(40) == 0)
            mob.SwoopTicks = 60;
    }

    // --- Swimming mobs: fish, squids, dolphins, guardians, drowned ---

    private void ThinkSwimming(Mob mob, BedrockConnection? player, double distance, double speed, bool day)
    {
        var target = Target(mob, player, distance, day);
        if (target is not null)
        {
            var dist = Math.Sqrt(Distance2(mob, target));
            if (mob.Kind == MobKind.Guardian || mob.Kind == MobKind.ElderGuardian)
            {
                // Laser beam: hits after the charge when the player stays in sight.
                Look(mob, target.Position.X, target.Position.Z);
                if (dist < 15 && mob.AttackCooldown == 0 && LineOfSight(mob, target))
                {
                    mob.AttackCooldown = 60;
                    Hit(mob, target, Scaled(mob.Kind == MobKind.ElderGuardian ? 8 : 6));
                }
                if (dist > 8)
                    HeadTo(mob, target.Position.X, target.Position.Y - 1, target.Position.Z, speed, water: true);
                return;
            }
            HeadTo(mob, target.Position.X, target.Position.Y - 1, target.Position.Z, speed * 1.3, water: !mob.Kind.Amphibious);
            if (dist < 1.2 + mob.Width && mob.AttackCooldown == 0)
            {
                mob.AttackCooldown = 20;
                Broadcast(mob, EntityPackets.Swing(mob.EntityId));
                Hit(mob, target, Scaled(mob.Kind.Damage));
            }
            return;
        }
        if (mob.PanicTicks > 0 && mob.FleeFrom is { } from)
        {
            mob.PanicTicks--;
            HeadTo(mob, mob.X + (mob.X - from.X), mob.Y, mob.Z + (mob.Z - from.Z), speed * 1.6, water: true);
            return;
        }
        if (mob.FlyTarget is null || _random.Next(80) == 0 || Reached(mob, mob.FlyTarget.Value))
            mob.FlyTarget = RandomWater(mob, 7, 3);
        if (mob.FlyTarget is { } swim)
            HeadTo(mob, swim.X, swim.Y, swim.Z, speed * 0.7, water: true);
        else
            mob.Vx = mob.Vy = mob.Vz = 0;
    }

    private static bool InWater(Mob mob) => WaterAt(mob.World!, mob.X, mob.Y + mob.Height / 2, mob.Z);

    private static bool WaterAt(World world, double x, double y, double z) =>
        world.GetBlock((int)Math.Floor(x), (int)Math.Floor(y), (int)Math.Floor(z)) is { } b
        && (b == Blocks.Water || b.Name == "minecraft:flowing_water");

    private static bool Reached(Mob mob, (double X, double Y, double Z) at) =>
        Math.Abs(at.X - mob.X) < 0.6 && Math.Abs(at.Y - mob.Y) < 0.6 && Math.Abs(at.Z - mob.Z) < 0.6;

    private (double X, double Y, double Z)? RandomAir(Mob mob, int horizontal, int vertical)
    {
        for (var attempt = 0; attempt < 6; attempt++)
        {
            double x = mob.X + _random.Next(-horizontal, horizontal + 1), y = mob.Y + _random.Next(-vertical, vertical + 2),
                z = mob.Z + _random.Next(-horizontal, horizontal + 1);
            if (!Collides(mob, x, y, z) && !WaterAt(mob.World!, x, y, z) && y > Chunk.MinY + 2)
                return (x, y, z);
        }
        return null;
    }

    private (double X, double Y, double Z)? RandomWater(Mob mob, int horizontal, int vertical)
    {
        for (var attempt = 0; attempt < 6; attempt++)
        {
            double x = mob.X + _random.Next(-horizontal, horizontal + 1), y = mob.Y + _random.Next(-vertical, vertical + 1),
                z = mob.Z + _random.Next(-horizontal, horizontal + 1);
            if (WaterAt(mob.World!, x, y, z) && WaterAt(mob.World!, x, y + mob.Height, z) && !Collides(mob, x, y, z))
                return (x, y, z);
        }
        return null;
    }

    private static void Look(Mob mob, double x, double z) => mob.Yaw = (float)(Math.Atan2(-(x - mob.X), z - mob.Z) * 180 / Math.PI);

    // Flyers and swimmers steer in three dimensions; a swimmer (not amphibious) never leaves the water upwards.
    private static void HeadTo(Mob mob, double x, double y, double z, double speed, bool water = false, bool look = true)
    {
        double dx = x - mob.X, dy = y - mob.Y, dz = z - mob.Z;
        var length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (length < 0.05)
        {
            mob.Vx = mob.Vy = mob.Vz = 0;
            return;
        }
        // Turn smoothly instead of snapping.
        mob.Vx = mob.Vx * 0.6 + dx / length * speed * 0.4;
        mob.Vy = mob.Vy * 0.6 + dy / length * speed * 0.4;
        mob.Vz = mob.Vz * 0.6 + dz / length * speed * 0.4;
        if (water && !WaterAt(mob.World!, mob.X + mob.Vx, mob.Y + mob.Height + mob.Vy, mob.Z + mob.Vz))
            mob.Vy = Math.Min(mob.Vy, 0);
        if (look && Math.Abs(dx) + Math.Abs(dz) > 0.05)
            Look(mob, x, z);
    }

    // No gravity: flyers and swimmers in water keep their speed (with drag) and stop against blocks.
    private void FreePhysics(Mob mob)
    {
        if (!Collides(mob, mob.X + mob.Vx, mob.Y, mob.Z))
            mob.X += mob.Vx;
        else
            (mob.Vx, mob.FlyTarget) = (0, null);
        if (!Collides(mob, mob.X, mob.Y + mob.Vy, mob.Z))
            mob.Y += mob.Vy;
        else
            (mob.Vy, mob.FlyTarget) = (0, null);
        if (!Collides(mob, mob.X, mob.Y, mob.Z + mob.Vz))
            mob.Z += mob.Vz;
        else
            (mob.Vz, mob.FlyTarget) = (0, null);
        mob.Vx *= 0.91;
        mob.Vy *= 0.91;
        mob.Vz *= 0.91;
        mob.FallDistance = 0;
        mob.OnGround = Collides(mob, mob.X, mob.Y - 0.05, mob.Z);
        if (mob.Y < Chunk.MinY - 32)
            Damage(mob, 1000, null, source: DamageSource.Void());
    }

    // --- Spawning of water, flying and Nether mobs ---

    private void TrySpawnWater(World world, BedrockConnection player)
    {
        var (x, z) = RandomColumn(player);
        var biome = BiomeAt(world, x, z);
        var feet = (int)Math.Floor(player.Position.Y - BedrockConnection.EyeHeight);
        // A water block: under the surface around the player's height, or in an underground lake.
        int? found = null;
        for (var y = Math.Min(feet + 12, Chunk.MaxY - 2); y > Math.Max(feet - 24, Chunk.MinY + 1) && found is null; y--)
            if (WaterAt(world, x, y, z) && WaterAt(world, x, y + 1, z))
                found = y - _random.Next(3);
        if (found is not { } wy || !WaterAt(world, x, wy, z))
            return;

        List<(MobKind, int)> table;
        if (wy < 30 && !SkyAccess(world, x, wy + 1, z))
            table = [(MobKind.SquidGlow, 10)]; // glow squids live in dark underground water
        else if (Tagged(biome, "ocean"))
        {
            var warm = Tagged(biome, "warm") || Tagged(biome, "lukewarm");
            var cold = Tagged(biome, "cold") || Tagged(biome, "frozen");
            table = warm
                ? [(MobKind.TropicalFish, 25), (MobKind.Pufferfish, 3), (MobKind.Dolphin, 2)]
                : cold
                    ? [(MobKind.Salmon, 15), (MobKind.Cod, 5), (MobKind.Squid, 3)]
                    : [(MobKind.Cod, 10), (MobKind.Squid, 4), (MobKind.Dolphin, 1)];
        }
        else if (Tagged(biome, "river"))
            table = [(MobKind.Salmon, 5), (MobKind.Squid, 2)];
        else
            return;
        if (Pick(table) is not { } kind)
            return;
        var group = kind == MobKind.Dolphin ? 1 + _random.Next(2) : 2 + _random.Next(3);
        for (var i = 0; i < group; i++)
        {
            int sx = x + _random.Next(5) - 2, sz = z + _random.Next(5) - 2;
            if (WaterAt(world, sx, wy, sz) && WaterAt(world, sx, wy + Math.Ceiling(kind.Height), sz))
                Spawn(kind, sx + 0.5, wy, sz + 0.5, world);
        }
    }

    private void TrySpawnBat(World world, BedrockConnection player)
    {
        var (x, z) = RandomColumn(player);
        var y = (int)Math.Floor(player.Position.Y) + _random.Next(-16, 9);
        if (y >= 63 || world.GetBlock(x, y, z) is not { IsAir: true } || world.GetBlock(x, y + 1, z) is not { IsAir: true })
            return; // bats live in caves, below sea level
        if (SkyAccess(world, x, y, z) || !Dark(world, x, y, z, night: true, nether: false))
            return;
        for (var i = 0; i < 1 + _random.Next(2); i++)
            Spawn(MobKind.Bat, x + 0.5, y, z + 0.5, world);
    }

    private void TrySpawnPhantoms(World world, BedrockConnection player)
    {
        var (px, py, pz) = player.Position;
        if (player.GameMode is GameMode.Creative or GameMode.Spectator || !SkyAccess(world, (int)Math.Floor(px), (int)Math.Floor(py), (int)Math.Floor(pz)))
            return;
        var height = py + 20 + _random.Next(15);
        for (var i = 0; i < 1 + _random.Next(3); i++)
            Spawn(MobKind.Phantom, px + _random.Next(-4, 5), Math.Min(height, Chunk.MaxY - 2), pz + _random.Next(-4, 5), world);
    }

    // Ghasts need room: open air all around the spot.
    private void TrySpawnGhast(World world, BedrockConnection player, int x, int z)
    {
        var y = 20 + _random.Next(90);
        for (var dx = -2; dx <= 2; dx += 2)
        for (var dy = 0; dy <= 4; dy += 2)
        for (var dz = -2; dz <= 2; dz += 2)
            if (world.GetBlock(x + dx, y + dy, z + dz) is not { IsAir: true })
                return;
        if (_mobs.Count(m => m.Kind == MobKind.Ghast && m.World == world && Distance2(m, player) < ActiveRange * ActiveRange) < 2)
            Spawn(MobKind.Ghast, x + 0.5, y, z + 0.5, world);
    }

    private void TrySpawnDrowned(World world, BedrockConnection player, int x, int z, bool night)
    {
        var feet = (int)Math.Floor(player.Position.Y - BedrockConnection.EyeHeight);
        for (var y = Math.Min(feet + 8, Chunk.MaxY - 3); y > Math.Max(feet - 24, Chunk.MinY + 1); y--)
        {
            // Two blocks of water over a floor, in the dark (at night or deep enough to be out of the sun).
            if (!BlockPhysics.IsSolid(world.GetBlock(x, y - 1, z)) || !WaterAt(world, x, y, z) || !WaterAt(world, x, y + 1, z))
                continue;
            if (Dark(world, x, y, z, night, nether: false) || !night && !SkyAccess(world, x, y + 2, z))
                Spawn(MobKind.Drowned, x + 0.5, y, z + 0.5, world);
            return;
        }
    }

    // --- Breeding and interactions ---

    private static string? BucketFor(MobKind kind) =>
        kind == MobKind.Cod ? "cod_bucket" : kind == MobKind.Salmon ? "salmon_bucket" : kind == MobKind.TropicalFish ? "tropical_fish_bucket"
        : kind == MobKind.Pufferfish ? "pufferfish_bucket" : kind == MobKind.Axolotl ? "axolotl_bucket" : kind == MobKind.Tadpole ? "tadpole_bucket" : null;

    private static bool Eats(MobKind kind, ItemStack? item) =>
        item is not null && Food.TryGetValue(kind.Id, out var food) && food.Contains(item.Type.Name["minecraft:".Length..]);

    private Mob? FindPartner(Mob mob)
    {
        Mob? best = null;
        var bestDist = 8.0 * 8;
        foreach (var other in _mobs)
        {
            if (other == mob || other.Kind != mob.Kind || other.World != mob.World || other.LoveTicks <= 0 || other.Baby || other.Dead)
                continue;
            var d = Distance2(other, mob.X, mob.Z);
            if (d < bestDist)
                (best, bestDist) = (other, d);
        }
        return best;
    }

    private void Breed(Mob a, Mob b)
    {
        foreach (var parent in new[] { a, b })
        {
            parent.LoveTicks = 0;
            parent.AgeTicks = BreedCooldownTicks;
            Broadcast(parent, parent.DataPacket());
        }
        var baby = Spawn(a.Kind, (a.X + b.X) / 2, Math.Max(a.Y, b.Y), (a.Z + b.Z) / 2, a.World);
        if (baby is null)
            return;
        baby.Baby = true;
        baby.AgeTicks = -AdultAgeTicks;
        baby.Persistent = true;
        if (a.Kind == MobKind.Sheep)
            baby.Colour = _random.Next(2) == 0 ? a.Colour : b.Colour;
        (a.LoveCause ?? b.LoveCause)?.AddExperience(1 + _random.Next(7));
        a.LoveCause = b.LoveCause = null;
    }

    /// <summary>
    /// A player right-clicks a mob: food (breeding, babies grow faster), shears on a sheep, a bucket on a cow, a bowl on a
    /// mooshroom. Returns false when nothing happens.
    /// </summary>
    public bool Interact(BedrockConnection player, Mob mob)
    {
        if (mob.Dead || mob.World != player.World || Distance2(mob, player) > 8 * 8)
            return false;
        var held = player.HeldItem;
        var name = held?.Type.Name;
        var creative = player.GameMode == GameMode.Creative;

        // A name tag renamed on an anvil names the mob, which then stays in the world.
        if (name == "minecraft:name_tag" && held!.CustomName is { Length: > 0 } tag)
        {
            mob.NameTag = tag;
            mob.Persistent = true;
            Broadcast(mob, mob.DataPacket());
            if (!creative)
                player.Send(player.Inventory.ConsumeHeld());
            return true;
        }
        // A water bucket scoops up a fish (or an axolotl, a tadpole).
        if (name == "minecraft:water_bucket" && BucketFor(mob.Kind) is { } bucketName && ItemStack.Of(bucketName) is { } bucket)
        {
            Remove(mob, EntityDespawnReason.Removed);
            if (!creative)
                player.Send(player.Inventory.ConsumeHeld());
            player.GiveItem(bucket);
            return true;
        }
        if (name == "minecraft:shears" && mob.Kind == MobKind.Sheep && !mob.Sheared && !mob.Baby)
        {
            player.WearHeld(1);
            mob.Sheared = true;
            Broadcast(mob, mob.DataPacket());
            if (ItemStack.Of(WoolColour(mob.Colour ?? 0), 1 + _random.Next(3)) is { } wool)
                server.DropItem(wool, (float)mob.X, (float)mob.Y + 1, (float)mob.Z, world: mob.World);
            return true;
        }
        var milk = name == "minecraft:bucket" && (mob.Kind == MobKind.Cow || mob.Kind == MobKind.Mooshroom) ? "milk_bucket"
            : name == "minecraft:bowl" && mob.Kind == MobKind.Mooshroom ? "mushroom_stew" : null;
        if (milk is not null && !mob.Baby)
        {
            if (!creative)
                player.Send(player.Inventory.ConsumeHeld());
            if (ItemStack.Of(milk) is { } filled)
                player.GiveItem(filled);
            return true;
        }
        if (!Eats(mob.Kind, held))
            return false;
        if (mob.Baby)
            mob.AgeTicks = Math.Min(0, mob.AgeTicks + AdultAgeTicks / 10);
        else if (mob.AgeTicks > 0 || mob.LoveTicks > 0)
            return false; // cooling down after breeding, or already in love
        else
        {
            mob.LoveTicks = LoveTicks;
            mob.LoveCause = player;
            Broadcast(mob, mob.DataPacket());
            Broadcast(mob, EntityPackets.Event(mob.EntityId, EntityPackets.EventLoveParticles));
        }
        if (!creative)
            player.Send(player.Inventory.ConsumeHeld());
        return true;
    }

    // --- Physics ---

    private void Physics(Mob mob, long tick)
    {
        var world = mob.World!;
        var feet = world.GetBlock((int)Math.Floor(mob.X), (int)Math.Floor(mob.Y), (int)Math.Floor(mob.Z));
        var inWater = feet == Blocks.Water || feet?.Name == "minecraft:flowing_water";
        Environment(mob, feet, inWater, tick);
        if (mob.Dead)
            return;
        if (mob.Kind.Movement == MobMovement.Fly || mob.Kind.Movement == MobMovement.Swim && InWater(mob))
        {
            mob.DryTicks = 0;
            FreePhysics(mob);
            return;
        }
        // Fish, squids and dolphins out of water: they flop and suffocate.
        if (mob.Kind.Movement == MobMovement.Swim && !mob.Kind.Amphibious)
        {
            if (++mob.DryTicks > 60 && mob.DryTicks % 20 == 0)
                Damage(mob, 1, null, source: DamageSource.Drowning());
            if (mob.OnGround && _random.Next(10) == 0)
                (mob.Vx, mob.Vy, mob.Vz) = ((_random.NextDouble() - 0.5) * 0.2, 0.3, (_random.NextDouble() - 0.5) * 0.2);
            if (mob.Dead)
                return;
        }
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
                Land(mob, inWater);
            }
            mob.Vy = 0;
        }
        else
        {
            if (mob.Vy < 0 && !inWater)
                mob.FallDistance -= mob.Vy;
            else if (inWater)
                mob.FallDistance = 0;
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

    // Vanilla fall damage: 1 per block beyond 3 (chickens flutter down).
    private void Land(Mob mob, bool inWater)
    {
        var fallen = mob.FallDistance;
        mob.FallDistance = 0;
        if (fallen > 3 && !inWater && mob.Kind != MobKind.Chicken)
            Damage(mob, (float)Math.Floor(fallen - 3), null, source: DamageSource.Fall((float)fallen));
    }

    // Lava, fire, water and daylight.
    private void Environment(Mob mob, BlockType? feet, bool inWater, long tick)
    {
        var name = feet?.Name;
        if (name is "minecraft:lava" or "minecraft:flowing_lava")
        {
            mob.FireTicks = Math.Max(mob.FireTicks, 300);
            if (tick % 10 == 0)
                Damage(mob, 4, null, source: DamageSource.Lava());
        }
        else if (name is "minecraft:fire" or "minecraft:soul_fire")
            mob.FireTicks = Math.Max(mob.FireTicks, 160);
        if (inWater)
            mob.FireTicks = 0;
        if (mob.Dead)
            return;

        var world = mob.World!;
        var sunburn = mob.Burning && mob.FireTicks <= 0 && mob.Kind.BurnsInDaylight; // keeps burning until the sun check below
        if (tick % 20 == 0 && mob.Kind.BurnsInDaylight)
            sunburn = !inWater && world.Dimension == 0 && world.Time % 24000 is < 12500 or > 23500 && SkyAbove(mob);
        if (mob.FireTicks > 0)
            mob.FireTicks--;
        var burning = mob.FireTicks > 0 || sunburn;
        if (burning != mob.Burning)
        {
            mob.Burning = burning;
            Broadcast(mob, mob.DataPacket());
        }
        if (burning && tick % 20 == 0)
            Damage(mob, 1, null, source: DamageSource.Fire());
    }

    private bool SkyAbove(Mob mob)
    {
        int x = (int)Math.Floor(mob.X), z = (int)Math.Floor(mob.Z);
        for (var y = (int)Math.Floor(mob.Y + mob.Height); y < Math.Min(Chunk.MaxY, (int)mob.Y + 64); y++)
            if (mob.World!.GetBlock(x, y, z) is { IsAir: false } block && block != Blocks.Water)
                return false; // any cover, leaves included, shades the mob
        return true;
    }

    private bool Collides(Mob mob, double x, double y, double z)
    {
        var half = mob.Width / 2 - 0.01;
        for (var bx = (int)Math.Floor(x - half); bx <= (int)Math.Floor(x + half); bx++)
        for (var bz = (int)Math.Floor(z - half); bz <= (int)Math.Floor(z + half); bz++)
        for (var by = (int)Math.Floor(y); by <= (int)Math.Floor(y + mob.Height - 0.01); by++)
            if (BlockPhysics.IsSolid(mob.World!.GetBlock(bx, by, bz)))
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
            if (!mob.Kind.Hostile && !mob.Kind.Neutral)
            {
                mob.PanicTicks = 100;
                mob.FleeFrom = fromPos;
            }
        }
        if (killer is not null && mob.Kind.Neutral && killer.GameMode is not (GameMode.Creative or GameMode.Spectator))
            Anger(mob, killer);
        if (mob.Dead)
            Die(mob, killer, looting);
        return true;
    }

    // A neutral mob attacks the player who hit it; zombie pigmen, wolves and bees call the others around.
    private void Anger(Mob mob, BedrockConnection player)
    {
        var group = mob.Kind == MobKind.ZombiePigman ? 32 : mob.Kind == MobKind.Wolf || mob.Kind == MobKind.Bee ? 16 : 0;
        foreach (var other in group > 0 ? _mobs.Where(m => m.Kind == mob.Kind && m.World == mob.World && Distance2(m, mob.X, mob.Z) < group * group) : [mob])
        {
            var wasAngry = other.AngerTicks > 0;
            other.AngryAt = player;
            other.AngerTicks = AngerTicks;
            if (!wasAngry)
                Broadcast(other, other.DataPacket());
        }
    }

    private void Die(Mob mob, BedrockConnection? killer, int looting)
    {
        // Loot on any death (babies drop nothing); cooked when the mob was burning. Experience only for a player kill.
        var drops = new List<ItemStack>();
        if (!mob.Baby)
        {
            foreach (var (item, min, max) in mob.Kind.Loot)
            {
                if (item == "white_wool" && mob.Sheared)
                    continue;
                var count = _random.Next(min, max + 1 + (killer is null ? 0 : looting));
                var name = item == "white_wool" && mob.Colour is { } c ? WoolColour(c)
                    : mob.Burning && Cooked.TryGetValue(item, out var cooked) ? cooked : item;
                if (count > 0 && ItemStack.Of(name, count) is { } stack)
                    drops.Add(stack);
            }
        }
        var droppedXp = killer is null || mob.Baby ? 0 : mob.Kind.Hostile ? 5 : 1 + _random.Next(3);
        var dropItems = true;

        if (server.Events.HasEntityKilled)
        {
            var killerId = killer is not null ? (ulong)killer.EntityId : 0UL;
            var killedEv = new EntityKilledEvent((ulong)mob.EntityId, mob.Kind.Id, killerId, drops, droppedXp, dropItems);
            if (!server.Events.OnEntityKilled(killedEv))
            {
                mob.Health = 1; // survives with 1 HP
                return;
            }
            droppedXp = killedEv.Experience;
            dropItems = killedEv.DropItems;
        }

        Broadcast(mob, EntityPackets.Event(mob.EntityId, EntityPackets.EventDeath));
        if (server.Events.HasEntityDeath)
        {
            var deathEv = new EntityDeathEvent(mob, killer, drops, droppedXp);
            server.Events.OnEntityDeath(deathEv);
            droppedXp = deathEv.DroppedXp;
        }
        killer?.AddExperience(droppedXp);
        if (dropItems)
        {
            foreach (var stack in drops)
                server.DropItem(stack, (float)mob.X, (float)mob.Y + 0.5f, (float)mob.Z, world: mob.World);
        }
    }

    private static string WoolColour(int colour) => colour switch
    {
        15 => "black_wool", 7 => "gray_wool", 8 => "light_gray_wool", 12 => "brown_wool", 6 => "pink_wool", _ => "white_wool",
    };

    private void Explode(Mob creeper)
    {
        Remove(creeper, EntityDespawnReason.Death);
        server.Explode(creeper.X, creeper.Y + 0.5, creeper.Z, 3, creeper.World);
    }

    // Vanilla difficulty scaling of mob damage to players.
    private float Scaled(float damage) => server.Config.Difficulty switch
    {
        Difficulty.Peaceful => 0,
        Difficulty.Easy => Math.Min(damage, damage / 2 + 1),
        Difficulty.Hard => damage * 1.5f,
        _ => damage,
    };

    // --- Saving: animals stay in the world ---

    /// <summary>Reads the mobs saved with <paramref name="world"/>; they appear when their chunk is loaded.</summary>
    public void Load(World world)
    {
        var path = Path.Combine(world.Folder, "entities.json");
        if (!File.Exists(path))
            return;
        try
        {
            var saved = System.Text.Json.JsonSerializer.Deserialize<List<SavedMob>>(File.ReadAllBytes(path)) ?? [];
            Dormant(world).AddRange(saved.Where(s => ByFullId.ContainsKey(s.Kind)));
            server.Logger.Debug($"{saved.Count} mob(s) saved in {path}.");
        }
        catch (Exception e) when (e is System.Text.Json.JsonException or IOException)
        {
            server.Logger.Warn($"Could not read {path} ({e.Message}): its mobs are lost.");
        }
    }

    /// <summary>Writes the persistent mobs of <paramref name="world"/> (in loaded chunks or not) to entities.json.</summary>
    public void Save(World world)
    {
        var mobs = _mobs.Where(m => m.World == world && m.Persistent && !m.Dead).Select(Snapshot).Concat(Dormant(world)).ToList();
        var path = Path.Combine(world.Folder, "entities.json");
        if (mobs.Count == 0 && !File.Exists(path))
            return;
        var temp = path + ".tmp";
        File.WriteAllBytes(temp, System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(mobs));
        File.Move(temp, path, overwrite: true);
    }

    private List<SavedMob> Dormant(World world) =>
        _dormant.TryGetValue(world, out var list) ? list : _dormant[world] = [];

    private static SavedMob Snapshot(Mob m) => new(m.Kind.Id, m.X, m.Y, m.Z, m.Yaw, m.Health, m.Colour, m.Baby, m.AgeTicks, m.Sheared, m.NameTag);

    // Out of the simulation: persistent mobs wait on the side (and in entities.json), the others are gone.
    private void Sleep(Mob mob)
    {
        if (mob.Persistent && !mob.Dead && mob.World is { } world)
            Dormant(world).Add(Snapshot(mob));
        Remove(mob, EntityDespawnReason.ChunkUnload);
    }

    // Mobs waiting for their chunk come back once it is loaded.
    private void Wake(World world)
    {
        if (!_dormant.TryGetValue(world, out var list) || list.Count == 0)
            return;
        for (var i = list.Count - 1; i >= 0; i--)
        {
            var s = list[i];
            if (world.LoadedChunk((int)Math.Floor(s.X) >> 4, (int)Math.Floor(s.Z) >> 4) is null)
                continue;
            list.RemoveAt(i);
            _mobs.Add(new Mob(server.NextEntityId(), ByFullId[s.Kind], s.X, s.Y, s.Z)
            {
                World = world, Yaw = s.Yaw, Health = s.Health, Colour = s.Colour, Baby = s.Baby, AgeTicks = s.Age, Sheared = s.Sheared, NameTag = s.NameTag,
                Persistent = true, EggTicks = 6000 + _random.Next(6000),
            });
        }
    }

    // --- Visibility ---

    private void Sync(Mob mob, List<BedrockConnection> players)
    {
        foreach (var player in players)
        {
            var visible = player.World == mob.World && Distance2(mob, player) < ViewDistance * ViewDistance;
            if (visible && mob.Viewers.Add(player))
                player.Send(mob.AddPacket());
            else if (!visible && mob.Viewers.Remove(player))
                player.Send(Bedrokia.Protocol.Packets.RemoveActor(mob.EntityId));
        }
        mob.Viewers.RemoveWhere(v => !v.IsInGame || v.World != mob.World);
        var (lx, ly, lz, lyaw) = mob.LastSent;
        if (Math.Abs(mob.X - lx) + Math.Abs(mob.Y - ly) + Math.Abs(mob.Z - lz) < 0.001 && Math.Abs(mob.Yaw - lyaw) < 0.5)
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
        {
            mob.Viewers.Remove(player);
            if (mob.AngryAt == player)
                mob.AngryAt = null;
            if (mob.LoveCause == player)
                mob.LoveCause = null;
        }
    }

    private static double Distance2(Mob mob, BedrockConnection player) => Distance2(mob, player.Position.X, player.Position.Z)
        + Math.Pow(player.Position.Y - BedrockConnection.EyeHeight - mob.Y, 2);

    private static double Distance2(Mob mob, double x, double z) => (mob.X - x) * (mob.X - x) + (mob.Z - z) * (mob.Z - z);
}

/// <summary>Which blocks entities collide with.</summary>
public static class BlockPhysics
{
    private static readonly FrozenSet<string> Passable = new[]
    {
        "minecraft:air", "minecraft:water", "minecraft:flowing_water", "minecraft:lava", "minecraft:flowing_lava",
        "minecraft:short_grass", "minecraft:tall_grass", "minecraft:fern", "minecraft:large_fern", "minecraft:deadbush",
        "minecraft:dandelion", "minecraft:poppy", "minecraft:blue_orchid", "minecraft:allium", "minecraft:azure_bluet",
        "minecraft:red_tulip", "minecraft:orange_tulip", "minecraft:white_tulip", "minecraft:pink_tulip", "minecraft:oxeye_daisy",
        "minecraft:cornflower", "minecraft:lily_of_the_valley", "minecraft:sunflower", "minecraft:lilac", "minecraft:rose_bush",
        "minecraft:peony", "minecraft:reeds", "minecraft:snow_layer", "minecraft:brown_mushroom", "minecraft:red_mushroom",
        "minecraft:vine", "minecraft:torch", "minecraft:wheat", "minecraft:waterlily",
    }.ToFrozenSet();

    public static bool IsSolid(BlockType? block) =>
        block is not null && !block.IsAir && !Passable.Contains(block.Name) && !block.Name.EndsWith("_sapling");
}
