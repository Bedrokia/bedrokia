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

namespace Bedrokia.Server.Game;

/// <summary>One block state: a block name plus its state values (byte, int or string).</summary>
public sealed class BlockType
{
    internal BlockType(ushort id, string name, OrderedDictionary<string, object> states)
    {
        Id = id;
        Name = name;
        var sorted = new OrderedDictionary<string, object>();
        foreach (var key in states.Keys.Order(StringComparer.Ordinal))
            sorted[key] = states[key];
        States = sorted;
        Key = sorted.Count == 0 ? name : $"{name}[{string.Join(',', sorted.Select(s => $"{s.Key}={s.Value}"))}]";

        // Network id with hashed ids: FNV-1a 32 of the little-endian NBT {name, states (sorted)}.
        var hash = 0x811C9DC5u;
        foreach (var b in Nbt.Write(new OrderedDictionary<string, object> { ["name"] = name, ["states"] = sorted }, network: false))
            hash = (hash ^ b) * 0x01000193u;
        NetworkId = hash;
    }

    /// <summary>Index in <see cref="Blocks"/>; only valid inside one server run (saves use <see cref="Key"/>).</summary>
    public ushort Id { get; }
    public string Name { get; }
    public OrderedDictionary<string, object> States { get; }

    /// <summary>Stable text form used in save files, e.g. "minecraft:oak_log[pillar_axis=y]".</summary>
    public string Key { get; }

    public uint NetworkId { get; }

    public bool IsAir => Id == 0;

    /// <summary>Cannot be broken in survival.</summary>
    public bool Unbreakable => Blocks.UnbreakableNames.Contains(Name);

    public override string ToString() => Key;
}

/// <summary>
/// Every vanilla block state (≈15 000), loaded from Dragonfly's block_states.nbt (MIT).
/// Air is always id 0.
/// </summary>
public static class Blocks
{
    internal static readonly HashSet<string> UnbreakableNames =
    [
        "minecraft:bedrock", "minecraft:barrier", "minecraft:command_block", "minecraft:chain_command_block",
        "minecraft:repeating_command_block", "minecraft:structure_block", "minecraft:jigsaw", "minecraft:end_portal",
        "minecraft:end_portal_frame", "minecraft:portal", "minecraft:end_gateway", "minecraft:border_block",
        "minecraft:allow", "minecraft:deny", "minecraft:invisible_bedrock", "minecraft:light_block",
    ];

    private static readonly List<BlockType> All = [];
    private static readonly Dictionary<string, BlockType> ByKeyMap = [];
    private static readonly Dictionary<uint, BlockType> ByNetworkIdMap = [];
    private static readonly Dictionary<string, BlockType> FirstByName = [];

    static Blocks()
    {
        Register("minecraft:air", []);
        using var stream = typeof(Blocks).Assembly.GetManifestResourceStream("block_states.nbt")
                           ?? throw new InvalidOperationException("block_states.nbt resource is missing.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        foreach (var entry in Nbt.ReadAll(buffer.ToArray(), network: true))
        {
            var name = (string)entry["name"];
            var states = (OrderedDictionary<string, object>)entry["states"];
            if (name != "minecraft:air")
                Register(name, states);
        }

        Air = ById(0);
        Bedrock = Get("bedrock", ("infiniburn_bit", (byte)0));
        Stone = Get("stone");
        Granite = Get("granite");
        Diorite = Get("diorite");
        Andesite = Get("andesite");
        Deepslate = Get("deepslate", ("pillar_axis", "y"));
        Dirt = Get("dirt");
        GrassBlock = Get("grass_block");
        Sand = Get("sand");
        Sandstone = Get("sandstone");
        Gravel = Get("gravel");
        Clay = Get("clay");
        Water = Get("water", ("liquid_depth", 0));
        CoalOre = Get("coal_ore");
        IronOre = Get("iron_ore");
        CopperOre = Get("copper_ore");
        OakLog = Get("oak_log", ("pillar_axis", "y"));
        OakLeaves = Get("oak_leaves", ("persistent_bit", (byte)0), ("update_bit", (byte)0));
        BirchLog = Get("birch_log", ("pillar_axis", "y"));
        BirchLeaves = Get("birch_leaves", ("persistent_bit", (byte)0), ("update_bit", (byte)0));
        SpruceLog = Get("spruce_log", ("pillar_axis", "y"));
        SpruceLeaves = Get("spruce_leaves", ("persistent_bit", (byte)0), ("update_bit", (byte)0));
        ShortGrass = Get("short_grass");
        Dandelion = Get("dandelion");
        Poppy = Get("poppy");
        Cactus = Get("cactus", ("age", 0));
        SnowLayer = Get("snow_layer", ("covered_bit", (byte)0), ("height", 0));
        Cobblestone = Get("cobblestone");
        OakPlanks = Get("oak_planks");
        Glass = Get("glass");
    }

    public static BlockType Air { get; }
    public static BlockType Bedrock { get; }
    public static BlockType Stone { get; }
    public static BlockType Granite { get; }
    public static BlockType Diorite { get; }
    public static BlockType Andesite { get; }
    public static BlockType Deepslate { get; }
    public static BlockType Dirt { get; }
    public static BlockType GrassBlock { get; }
    public static BlockType Sand { get; }
    public static BlockType Sandstone { get; }
    public static BlockType Gravel { get; }
    public static BlockType Clay { get; }
    public static BlockType Water { get; }
    public static BlockType CoalOre { get; }
    public static BlockType IronOre { get; }
    public static BlockType CopperOre { get; }
    public static BlockType OakLog { get; }
    public static BlockType OakLeaves { get; }
    public static BlockType BirchLog { get; }
    public static BlockType BirchLeaves { get; }
    public static BlockType SpruceLog { get; }
    public static BlockType SpruceLeaves { get; }
    public static BlockType ShortGrass { get; }
    public static BlockType Dandelion { get; }
    public static BlockType Poppy { get; }
    public static BlockType Cactus { get; }
    public static BlockType SnowLayer { get; }
    public static BlockType Cobblestone { get; }
    public static BlockType OakPlanks { get; }
    public static BlockType Glass { get; }

    public static int Count => All.Count;

    public static BlockType ById(ushort id) => All[id];

    public static BlockType? ByKey(string key) => ByKeyMap.GetValueOrDefault(key);

    public static BlockType? ByNetworkId(uint networkId) => ByNetworkIdMap.GetValueOrDefault(networkId);

    /// <summary>First registered state of a block (its usual default), e.g. "stone" or "minecraft:stone".</summary>
    public static BlockType? ByName(string name) =>
        FirstByName.GetValueOrDefault(name.Contains(':') ? name : "minecraft:" + name);

    /// <summary>A state by name and exact state values.</summary>
    /// <exception cref="KeyNotFoundException">No such state in the vanilla data.</exception>
    public static BlockType Get(string name, params (string Key, object Value)[] states)
    {
        var full = "minecraft:" + name;
        var key = states.Length == 0
            ? full
            : $"{full}[{string.Join(',', states.OrderBy(s => s.Key, StringComparer.Ordinal).Select(s => $"{s.Key}={s.Value}"))}]";
        return ByKey(key) ?? throw new KeyNotFoundException($"Unknown block state {key}.");
    }

    public static BlockType RegisterCustom(string name, OrderedDictionary<string, object> states)
    {
        var block = new BlockType((ushort)All.Count, name, states);
        if (ByKeyMap.TryGetValue(block.Key, out var existing))
            return existing;
        All.Add(block);
        ByKeyMap[block.Key] = block;
        ByNetworkIdMap.TryAdd(block.NetworkId, block);
        FirstByName.TryAdd(name, block);
        return block;
    }

    private static void Register(string name, OrderedDictionary<string, object> states)
    {
        var block = new BlockType((ushort)All.Count, name, states);
        if (!ByKeyMap.TryAdd(block.Key, block))
            return; // duplicate state in the data file
        All.Add(block);
        ByNetworkIdMap.TryAdd(block.NetworkId, block);
        FirstByName.TryAdd(name, block);
    }
}
