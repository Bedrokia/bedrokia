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

using System.Text.Json;
using Bedrokia.Protocol;

namespace Bedrokia.Server.Game;

/// <summary>
/// Extra data attached to a block (Bedrock "block actor"): container contents, furnace progress, bed colour.
/// The client needs one for chests, beds, furnaces... to draw them. Ids and NBT from Dragonfly (MIT) block/*.go.
/// </summary>
public sealed class BlockEntity(string id, int slots)
{
    /// <summary>Bedrock block actor id: Chest, Barrel, EnderChest, Furnace, BlastFurnace, Smoker, Bed.</summary>
    public string Id { get; } = id;

    /// <summary>Contents of containers (null for beds and ender chests, whose items belong to each player).</summary>
    public Inventory? Items { get; } = slots > 0 ? new Inventory(slots) : null;

    /// <summary>Bed colour (0 white .. 15 black, Bedrock dye order).</summary>
    public byte Colour { get; set; } = 14; // red

    // Furnaces: ticks cooked so far, fuel ticks left and the total of the current fuel.
    public int CookTicks, BurnTicks, BurnDuration;

    // Custom blocks that work as furnaces (plugins, e.g. AntaCore's tiered furnaces): their cooking speed.
    private static readonly Dictionary<string, float> FurnaceSpeeds = [];

    /// <summary>
    /// Makes the custom block <paramref name="blockId"/> a furnace: same window, fuels and recipes as the vanilla one,
    /// cooking <paramref name="speed"/> times as fast (AntaCore: 1 + 2 x (speed - 1) cook ticks a tick). A
    /// "...:lit" property (0/1) of the block, if any, is set while it burns.
    /// </summary>
    public static void RegisterFurnace(string blockId, float speed) => FurnaceSpeeds[blockId] = Math.Max(1, speed);

    /// <summary>The cooking speed of a custom furnace, or null when the block is not one.</summary>
    public static float? FurnaceSpeed(string? blockName) => blockName is not null && FurnaceSpeeds.TryGetValue(blockName, out var speed) ? speed : null;

    /// <summary>The block entity a block needs, or null.</summary>
    public static BlockEntity? For(BlockType block) => FurnaceSpeeds.ContainsKey(block.Name) ? new BlockEntity("Furnace", 3) :
        !block.Name.StartsWith("minecraft:") ? null : block.Name["minecraft:".Length..] switch
        {
            "chest" or "trapped_chest" => new BlockEntity("Chest", 27),
            "barrel" => new BlockEntity("Barrel", 27),
            "ender_chest" => new BlockEntity("EnderChest", 0),
            "furnace" or "lit_furnace" => new BlockEntity("Furnace", 3),
            "blast_furnace" or "lit_blast_furnace" => new BlockEntity("BlastFurnace", 3),
            "smoker" or "lit_smoker" => new BlockEntity("Smoker", 3),
            "bed" => new BlockEntity("Bed", 0),
            _ => null,
        };

    /// <summary>What the client receives (no contents: those are sent when the container is opened).</summary>
    public OrderedDictionary<string, object> NetworkNbt(int x, int y, int z)
    {
        var nbt = new OrderedDictionary<string, object> { ["id"] = Id, ["x"] = x, ["y"] = y, ["z"] = z, ["isMovable"] = (byte)1 };
        if (Id == "Bed")
            nbt["color"] = Colour;
        if (Id == "Chest")
            nbt["Findable"] = (byte)0;
        return nbt;
    }

    internal void Write(BinaryWriter w)
    {
        w.Write(Id);
        w.Write(Items?.Size ?? 0);
        w.Write(Colour);
        w.Write(CookTicks);
        w.Write(BurnTicks);
        w.Write(BurnDuration);
        w.Write(Items is null ? "" : JsonSerializer.Serialize(PlayerData.Save(Items)));
    }

    internal static BlockEntity Read(BinaryReader r)
    {
        var entity = new BlockEntity(r.ReadString(), r.ReadInt32())
        {
            Colour = r.ReadByte(),
            CookTicks = r.ReadInt32(),
            BurnTicks = r.ReadInt32(),
            BurnDuration = r.ReadInt32(),
        };
        var items = r.ReadString();
        if (entity.Items is not null && items.Length > 0)
            PlayerData.Load(entity.Items, JsonSerializer.Deserialize<List<PlayerData.SavedStack>>(items) ?? []);
        return entity;
    }

    /// <summary>BlockActorData packet (id 56): position and network NBT, sent when a block entity appears or changes.</summary>
    public static byte[] DataPacket(int x, int y, int z, BlockEntity entity) =>
        new BedrockWriter().VarUInt32(56).VarInt32(x).VarInt32(y).VarInt32(z) // BlockPos: signed varints (gophertunnel)
            .Bytes(Nbt.Write(entity.NetworkNbt(x, y, z), network: true)).ToArray();
}
