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
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Game;

/// <summary>A filled map: 128 x 128 pixels, one block each, around a centre aligned on the 128-block grid.</summary>
public sealed class MapData(long id, World world, int centerX, int centerZ)
{
    public const int Size = 128;

    public long Id { get; } = id;
    public World World { get; } = world;
    public int CenterX { get; } = centerX;
    public int CenterZ { get; } = centerZ;

    /// <summary>Colours as A | R &lt;&lt; 8 | G &lt;&lt; 16 | B &lt;&lt; 24 (the network order); 0 = not explored yet.</summary>
    public int[] Pixels { get; } = new int[Size * Size];

    internal bool Dirty;
}

/// <summary>
/// Maps: an empty map used in the air becomes a filled map of the area (vanilla grid), drawn from the top blocks of
/// the loaded chunks with vanilla shading (lighter where the ground rises northwards). The map fills in around its
/// holder as they walk, shows them as a marker, and is saved in the world (maps/&lt;id&gt;.json).
/// </summary>
/// <remarks>ponytail: scale 0 only (no zoomed-out maps, no cartography table), no other players or banners on the map.</remarks>
public sealed class MapManager(BedrokiaServer server)
{
    private const uint ClientboundMapItemDataId = 67;
    private const int UpdateRadius = 24;

    private readonly Dictionary<long, MapData> _maps = [];

    /// <summary>Makes a new map centred where <paramref name="player"/> stands, drawn at once, as a filled map item.</summary>
    public ItemStack? Create(BedrockConnection player)
    {
        if (Items.ByName("filled_map") is not { } type)
            return null;
        int px = (int)Math.Floor(player.Position.X), pz = (int)Math.Floor(player.Position.Z);
        // Vanilla grid: maps cover x in [-64, 64), [64, 192)... around the origin.
        var map = new MapData(Random.Shared.NextInt64(1, long.MaxValue), player.World,
            (int)Math.Floor((px + 64) / 128.0) * 128, (int)Math.Floor((pz + 64) / 128.0) * 128);
        _maps[map.Id] = map;
        Render(map, 0, 0, MapData.Size, MapData.Size);
        var nbt = new OrderedDictionary<string, object>
        {
            ["map_uuid"] = map.Id, ["map_scale"] = 0, ["map_is_init"] = (byte)1, ["map_display_players"] = (byte)1,
            ["map_name_index"] = 1,
        };
        return new ItemStack(type, 1, 0, 0, nbt);
    }

    /// <summary>The map of a filled map item, loaded from its world if needed; null if unknown.</summary>
    public MapData? Of(ItemStack? item) =>
        item?.Type.Name == "minecraft:filled_map" && item.Nbt?.GetValueOrDefault("map_uuid") is long id ? Get(id) : null;

    public MapData? Get(long id)
    {
        if (_maps.TryGetValue(id, out var map))
            return map;
        foreach (var world in server.Worlds)
        {
            var path = Path.Combine(world.Folder, "maps", $"{id}.json");
            if (!File.Exists(path))
                continue;
            try
            {
                var saved = JsonSerializer.Deserialize<SavedMap>(File.ReadAllBytes(path))!;
                map = new MapData(id, world, saved.CenterX, saved.CenterZ);
                Buffer.BlockCopy(Convert.FromBase64String(saved.Pixels), 0, map.Pixels, 0, map.Pixels.Length * 4);
                return _maps[id] = map;
            }
            catch (Exception e) when (e is JsonException or IOException or FormatException or ArgumentException)
            {
                server.Logger.Warn($"Could not read map {path}: {e.Message}");
            }
        }
        return null;
    }

    private sealed record SavedMap(int CenterX, int CenterZ, string Pixels);

    public void Save()
    {
        foreach (var map in _maps.Values.Where(m => m.Dirty))
        {
            var folder = Path.Combine(map.World.Folder, "maps");
            Directory.CreateDirectory(folder);
            var bytes = new byte[map.Pixels.Length * 4];
            Buffer.BlockCopy(map.Pixels, 0, bytes, 0, bytes.Length);
            var path = Path.Combine(folder, $"{map.Id}.json");
            File.WriteAllBytes(path + ".tmp", JsonSerializer.SerializeToUtf8Bytes(new SavedMap(map.CenterX, map.CenterZ, Convert.ToBase64String(bytes))));
            File.Move(path + ".tmp", path, overwrite: true);
            map.Dirty = false;
        }
    }

    /// <summary>Draws pixels [x0, x1) x [z0, z1) from the loaded chunks; returns true when a pixel changed.</summary>
    public bool Render(MapData map, int x0, int z0, int x1, int z1)
    {
        var changed = false;
        var nether = map.World.Dimension == 1;
        int originX = map.CenterX - 64, originZ = map.CenterZ - 64;
        for (var px = Math.Max(0, x0); px < Math.Min(MapData.Size, x1); px++)
        {
            var previousHeight = int.MinValue;
            for (var pz = Math.Max(0, z0 - 1); pz < Math.Min(MapData.Size, z1); pz++)
            {
                int wx = originX + px, wz = originZ + pz;
                if (map.World.LoadedChunk(wx >> 4, wz >> 4) is not { } chunk)
                {
                    previousHeight = int.MinValue;
                    continue;
                }
                var (block, y) = chunk.TopBlock(wx & 15, wz & 15, nether ? 100 : Chunk.MaxY);
                if (pz < z0)
                {
                    previousHeight = y; // the row above the area: only for the shading
                    continue;
                }
                var colour = block.IsAir ? 0 : Shade(ColourOf(block), previousHeight == int.MinValue ? 1 : Math.Sign(y - previousHeight));
                previousHeight = y;
                ref var pixel = ref map.Pixels[pz * MapData.Size + px];
                if (pixel == colour)
                    continue;
                pixel = colour;
                changed = true;
            }
        }
        map.Dirty |= changed;
        return changed;
    }

    // Vanilla map shading: 255 where the ground rises from the north, 220 level, 180 lower.
    private static int Shade(int rgb, int slope)
    {
        var factor = slope > 0 ? 255 : slope == 0 ? 220 : 180;
        int r = ((rgb >> 16) & 0xFF) * factor / 255, g = ((rgb >> 8) & 0xFF) * factor / 255, b = (rgb & 0xFF) * factor / 255;
        return unchecked(0xFF | (r << 8) | (g << 16) | (b << 24));
    }

    private static readonly Dictionary<string, int> NamedColours = new()
    {
        ["white"] = 0xFFFFFF, ["orange"] = 0xD87F33, ["magenta"] = 0xB24CD8, ["light_blue"] = 0x6699D8, ["yellow"] = 0xE5E533,
        ["lime"] = 0x7FCC19, ["pink"] = 0xF27FA5, ["gray"] = 0x4C4C4C, ["light_gray"] = 0x999999, ["cyan"] = 0x4C7F99,
        ["purple"] = 0x7F3FB2, ["blue"] = 0x334CB2, ["brown"] = 0x664C33, ["green"] = 0x667F33, ["red"] = 0x993333, ["black"] = 0x191919,
    };

    /// <summary>Vanilla map colour of a block (by its material, guessed from its name).</summary>
    public static int ColourOf(BlockType block)
    {
        var name = block.Name.StartsWith("minecraft:") ? block.Name["minecraft:".Length..] : block.Name;
        // Coloured blocks: wool, concrete, carpets, stained terracotta and glass...
        foreach (var (colour, rgb) in NamedColours.OrderByDescending(c => c.Key.Length))
            if (name.StartsWith(colour + "_") && !name.EndsWith("_dye"))
                return name.Contains("terracotta") ? Darker(rgb) : rgb;
        return name switch
        {
            "water" or "flowing_water" or "bubble_column" => 0x4040FF,
            "lava" or "flowing_lava" or "fire" or "magma" or "redstone_block" or "tnt" => 0xFF0000,
            "grass_block" or "slime" => 0x7FB238,
            "sand" or "sandstone" or "smooth_sandstone" or "cut_sandstone" or "birch_planks" or "end_stone" or "glowstone" => 0xF7E9A3,
            "red_sand" or "red_sandstone" or "acacia_planks" or "pumpkin" => 0xD87F33,
            "snow" or "snow_layer" or "powder_snow" or "white_wool" => 0xFFFFFF,
            "ice" or "packed_ice" or "blue_ice" or "frosted_ice" => 0xA0A0FF,
            "dirt" or "coarse_dirt" or "farmland" or "dirt_path" or "grass_path" or "rooted_dirt" or "jungle_planks" => 0x976D4D,
            "podzol" or "spruce_planks" or "mud" => 0x815631,
            "clay" => 0xA4A8B8,
            "netherrack" or "nether_bricks" or "crimson_nylium" or "nether_wart_block" => 0x700200,
            "warped_nylium" or "warped_wart_block" => 0x167E86,
            "soul_sand" or "soul_soil" => 0x664C33,
            "quartz_block" or "diorite" or "calcite" or "bone_block" => 0xFFFCF5,
            "iron_block" or "anvil" or "heavy_weighted_pressure_plate" => 0xA7A7A7,
            "gold_block" => 0xFAEE4D,
            "diamond_block" or "beacon" or "prismarine_bricks" => 0x5CDBD5,
            "emerald_block" => 0x00D93A,
            "lapis_block" => 0x4A80FF,
            "obsidian" or "crying_obsidian" or "coal_block" or "blackstone" or "basalt" => 0x191919,
            "deepslate" or "cobbled_deepslate" or "tuff" => 0x646464,
            "terracotta" => 0xD1B1A1,
            "mycelium" => 0x7F3FB2,
            "bedrock" => 0x707070,
            _ when name.EndsWith("leaves") || name.Contains("grass") || name.Contains("fern") || name.Contains("flower") || name.Contains("tulip")
                   || name.Contains("sapling") || name.Contains("vine") || name is "cactus" or "kelp" or "seagrass" or "waterlily" or "wheat"
                       or "carrots" or "potatoes" or "beetroot" or "melon_block" or "bamboo" or "sugar_cane" or "reeds" or "dandelion" or "poppy"
                       or "azure_bluet" or "allium" or "cornflower" or "oxeye_daisy" or "lily_of_the_valley" or "sweet_berry_bush" => 0x007C00,
            _ when name.EndsWith("_log") || name.EndsWith("_wood") || name.EndsWith("_planks") || name.EndsWith("_fence") || name.EndsWith("_door")
                   || name.EndsWith("_stairs") && name.Contains("oak") || name.Contains("chest") || name is "crafting_table" or "bookshelf" or "barrel" => 0x8F7748,
            _ => 0x707070, // stone
        };
    }

    private static int Darker(int rgb) => (((rgb >> 16) & 0xFF) * 3 / 4 << 16) | (((rgb >> 8) & 0xFF) * 3 / 4 << 8) | ((rgb & 0xFF) * 3 / 4);

    /// <summary>
    /// ClientBoundMapItemData: the pixels of [x0, x0 + width) x [z0, z0 + height) and the viewer's marker
    /// (gophertunnel packet/client_bound_map_item_data.go at protocol 2193).
    /// </summary>
    public static byte[] Packet(MapData map, BedrockConnection? viewer, int x0 = 0, int z0 = 0, int width = MapData.Size, int height = MapData.Size)
    {
        var w = new BedrockWriter(width * height * 4 + 64).VarUInt32(ClientboundMapItemDataId)
            .VarInt64(map.Id)
            .Byte((byte)map.World.Dimension)
            .Bool(false) // locked
            .BlockPos(map.CenterX, 64, map.CenterZ)
            .Bool(true).VarUInt32(1).VarInt64(map.Id) // maps included in
            .Bool(true).Byte(0) // scale
            .Bool(false); // tracked objects
        // The viewer as a marker, when on this map.
        if (viewer is not null && viewer.World == map.World
            && Math.Abs(viewer.Position.X - map.CenterX) < 64 && Math.Abs(viewer.Position.Z - map.CenterZ) < 64)
        {
            w.Bool(true).VarUInt32(1)
                .Byte(0) // player marker
                .Byte((byte)((int)Math.Floor((viewer.Yaw + 180) * 16 / 360 + 0.5) & 15))
                .Byte(unchecked((byte)(sbyte)Math.Clamp((viewer.Position.X - map.CenterX) * 2, -128, 127)))
                .Byte(unchecked((byte)(sbyte)Math.Clamp((viewer.Position.Z - map.CenterZ) * 2, -128, 127)))
                .String("")
                .Int32BE(unchecked((int)0xFFFFFFFF));
        }
        else
            w.Bool(true).VarUInt32(0);
        w.Bool(true).VarInt32(width).Bool(true).VarInt32(height).Bool(true).VarInt32(x0).Bool(true).VarInt32(z0);
        w.Bool(true).VarUInt32((uint)(width * height));
        for (var z = z0; z < z0 + height; z++)
            for (var x = x0; x < x0 + width; x++)
                w.Int32BE(map.Pixels[z * MapData.Size + x]);
        return w.ToArray();
    }

    /// <summary>Every second, the maps players hold fill in around them and show where they are.</summary>
    public void Tick(long tick)
    {
        if (tick % 20 != 0)
            return;
        foreach (var player in server.Players)
        {
            if (!player.IsInGame || (Of(player.HeldItem) ?? Of(player.Inventory.Offhand[0])) is not { } map || map.World != player.World)
                continue;
            int px = (int)Math.Floor(player.Position.X) - (map.CenterX - 64), pz = (int)Math.Floor(player.Position.Z) - (map.CenterZ - 64);
            if (px is < -UpdateRadius or >= MapData.Size + UpdateRadius || pz is < -UpdateRadius or >= MapData.Size + UpdateRadius)
            {
                player.Send(Packet(map, player, 0, 0, 1, 1)); // off the map: only its marker
                continue;
            }
            int x0 = Math.Clamp(px - UpdateRadius, 0, MapData.Size), z0 = Math.Clamp(pz - UpdateRadius, 0, MapData.Size);
            int x1 = Math.Clamp(px + UpdateRadius, 0, MapData.Size), z1 = Math.Clamp(pz + UpdateRadius, 0, MapData.Size);
            Render(map, x0, z0, x1, z1);
            player.Send(x1 > x0 && z1 > z0 ? Packet(map, player, x0, z0, x1 - x0, z1 - z0) : Packet(map, player, 0, 0, 1, 1));
        }
    }
}
