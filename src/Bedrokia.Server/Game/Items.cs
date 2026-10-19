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

/// <summary>A vanilla item as the client registry knows it.</summary>
public sealed record ItemType(string Name, short RuntimeId, bool ComponentBased, int Version, OrderedDictionary<string, object> Data);

/// <summary>
/// Every vanilla item and the creative inventory, from Dragonfly's vanilla_items.nbt and creative_items.nbt (MIT).
/// The encoded packets are built once and shared by every connection.
/// </summary>
public static class Items
{
    private static readonly Dictionary<string, ItemType> ByNameMap = [];
    private static readonly Dictionary<short, ItemType> ByRuntimeIdMap = [];

    static Items()
    {
        var root = Nbt.ReadAll(Resource("vanilla_items.nbt"), network: true)[0];
        foreach (var (name, value) in root)
        {
            var entry = (OrderedDictionary<string, object>)value;
            var item = new ItemType(name, (short)(int)entry["runtime_id"], entry.TryGetValue("component_based", out var cb) && (byte)cb != 0,
                (int)entry["version"], entry.TryGetValue("data", out var data) ? (OrderedDictionary<string, object>)data : []);
            ByNameMap[name] = item;
            ByRuntimeIdMap[item.RuntimeId] = item;
        }
        ShieldRuntimeId = ByNameMap.TryGetValue("minecraft:shield", out var shield) ? shield.RuntimeId : (short)-1;
        RegistryPacket = BuildRegistry();
        CreativeContentPacket = BuildCreativeContent(out var count);
        CreativeItemCount = count;
    }

    public static int Count => ByNameMap.Count;

    /// <summary>ItemRegistry packet with every vanilla item.</summary>
    public static byte[] RegistryPacket { get; private set; }

    /// <summary>CreativeContent packet with the vanilla creative inventory.</summary>
    public static byte[] CreativeContentPacket { get; private set; }

    public static int CreativeItemCount { get; private set; }

    /// <summary>Creative inventory entries; creative network id N is index N - 1.</summary>
    internal static List<ItemStack> CreativeTemplates { get; } = [];

    private static readonly List<(ItemStack Stack, int Category)> CustomCreativeEntries = [];

    internal static short ShieldRuntimeId { get; }

    public static ItemType? ByName(string name) => ByNameMap.GetValueOrDefault(name.Contains(':') ? name : "minecraft:" + name);

    public static ItemType? ByRuntimeId(short runtimeId) => ByRuntimeIdMap.GetValueOrDefault(runtimeId);

    /// <param name="creativeCategory">Creative tab: 1 construction, 2 nature, 3 equipment, 4 items.</param>
    public static void RegisterCustomItem(ItemType item, bool isCreative = true, int creativeCategory = 4)
    {
        ByNameMap[item.Name] = item;
        ByRuntimeIdMap[item.RuntimeId] = item;
        RegistryPacket = BuildRegistry();
        if (isCreative)
        {
            var template = new ItemStack(item, 1, 0, Blocks.ByName(item.Name)?.NetworkId ?? 0);
            CustomCreativeEntries.Add((template, creativeCategory));
            CreativeTemplates.Clear();
            CreativeContentPacket = BuildCreativeContent(out var count);
            CreativeItemCount = count;
        }
    }

    private static byte[] BuildRegistry()
    {
        var w = new BedrockWriter(256 * 1024).VarUInt32(PacketId.ItemRegistry).VarUInt32((uint)ByNameMap.Count);
        foreach (var item in ByNameMap.Values.OrderBy(i => i.RuntimeId))
        {
            w.String(item.Name).Int16(item.RuntimeId).Bool(item.ComponentBased).VarInt32(item.Version)
                .Bytes(Nbt.Write(item.Data, network: true));
        }
        return w.ToArray();
    }

    private static byte[] BuildCreativeContent(out int itemCount)
    {
        var root = Nbt.ReadAll(Resource("creative_items.nbt"), network: true)[0];
        var groups = ((NbtList)root["groups"]).Values.Cast<OrderedDictionary<string, object>>().ToList();
        var items = ((NbtList)root["items"]).Values.Cast<OrderedDictionary<string, object>>().ToList();

        var w = new BedrockWriter(512 * 1024).VarUInt32(PacketId.CreativeContent).VarUInt32((uint)groups.Count);
        foreach (var group in groups)
        {
            w.Byte((byte)(int)group["category"]).String(group.TryGetValue("name", out var n) ? (string)n : "");
            WriteStack(w, group.TryGetValue("icon", out var icon) ? (OrderedDictionary<string, object>)icon : [], out _);
        }

        var body = new BedrockWriter(512 * 1024);
        itemCount = 0;
        foreach (var item in items)
        {
            var groupIndex = item.TryGetValue("group_index", out var g) ? (int)g : 0;
            var stack = new BedrockWriter();
            if (!WriteStack(stack, item, out var template) || template is null)
                continue; // item or block state unknown to this build
            itemCount++;
            CreativeTemplates.Add(template);
            body.VarUInt32((uint)itemCount).Bytes(stack.ToArray()).VarUInt32((uint)groupIndex);
        }

        foreach (var (stack, category) in CustomCreativeEntries)
        {
            itemCount++;
            CreativeTemplates.Add(stack);
            var stackWriter = new BedrockWriter();
            stackWriter.VarInt32(stack.Type.RuntimeId).UInt16((ushort)stack.Count).VarUInt32((ushort)stack.Meta).VarInt32(unchecked((int)stack.BlockRuntimeId));
            WriteUserData(stackWriter, stack.Nbt, stack.Type.RuntimeId == ShieldRuntimeId);
            // Like Dragonfly session/player.go creativeContent: a group of the item's own tab (an anonymous one if possible).
            var groupIndex = groups.FindIndex(g => (int)g["category"] == category && (g.TryGetValue("name", out var n) ? (string)n : "") == "");
            if (groupIndex < 0)
                groupIndex = Math.Max(0, groups.FindIndex(g => (int)g["category"] == category));
            body.VarUInt32((uint)itemCount).Bytes(stackWriter.ToArray()).VarUInt32((uint)groupIndex);
        }

        w.VarUInt32((uint)itemCount).Bytes(body.ToArray());
        return w.ToArray();
    }

    /// <summary>
    /// "Item" stack encoding (gophertunnel Writer.Item, MIT): network id, count, meta, block runtime id, user data.
    /// Returns false when the entry cannot be resolved.
    /// </summary>
    private static bool WriteStack(BedrockWriter w, OrderedDictionary<string, object> entry, out ItemStack? template)
    {
        template = null;
        var name = entry.TryGetValue("name", out var n) ? (string)n : "";
        if (name.Length == 0 || ByName(name) is not { } item)
        {
            w.VarInt32(0).UInt16(0).VarUInt32(0).VarInt32(0).VarUInt32(0); // air
            return name.Length == 0;
        }
        var meta = entry.TryGetValue("meta", out var m) ? (short)m : (short)0;

        uint blockRuntimeId = 0;
        if (entry.TryGetValue("block_properties", out var props) && props is OrderedDictionary<string, object> { Count: > 0 } states)
        {
            var key = $"{name}[{string.Join(',', states.OrderBy(s => s.Key, StringComparer.Ordinal).Select(s => $"{s.Key}={s.Value}"))}]";
            if (Blocks.ByKey(key) is not { } block)
                return false;
            blockRuntimeId = block.NetworkId;
        }
        else if (Blocks.ByName(name) is { } block)
        {
            blockRuntimeId = block.NetworkId;
        }

        w.VarInt32(item.RuntimeId).UInt16(1).VarUInt32((ushort)meta).VarInt32(unchecked((int)blockRuntimeId));
        var nbt = entry.TryGetValue("nbt", out var raw) ? raw as OrderedDictionary<string, object> : null;
        WriteUserData(w, nbt, item.RuntimeId == ShieldRuntimeId);
        template = new ItemStack(item, 1, meta, blockRuntimeId, nbt);
        return true;
    }

    // Length-prefixed: [-1, version 1, LE NBT | 0], can-place-on (u32 0), can-break (u32 0), [shield blocking tick].
    internal static void WriteUserData(BedrockWriter w, OrderedDictionary<string, object>? nbt, bool shield)
    {
        var data = new BedrockWriter();
        if (nbt is { Count: > 0 })
            data.Int16(-1).Byte(1).Bytes(Nbt.Write(nbt, network: false));
        else
            data.Int16(0);
        data.UInt32(0).UInt32(0);
        if (shield)
            data.Int64(0);
        w.ByteArray(data.ToArray());
    }

    private static byte[] Resource(string name)
    {
        using var stream = typeof(Items).Assembly.GetManifestResourceStream(name)
                           ?? throw new InvalidOperationException($"{name} resource is missing.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
