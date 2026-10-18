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
/// Placement rules: the state a block gets from the clicked face, where on it, and where the player looks (logs,
/// stairs, slabs, furnaces, chests, torches, ladders, buttons, signs, doors, beds...), following Dragonfly's (MIT)
/// block UseOnBlock logic per state. Faces: 0 down, 1 up, 2 north, 3 south, 4 west, 5 east.
/// </summary>
public static class BlockPlacement
{
    private static readonly string[] Cardinals = ["south", "west", "north", "east"]; // index = Bedrock yaw quadrant

    /// <summary>Horizontal direction the player looks at: 0 south, 1 west, 2 north, 3 east.</summary>
    public static int Look(float yaw) => (int)Math.Floor(yaw * 4 / 360 + 0.5) & 3;

    private static (int X, int Z) Offset(int look) => look switch { 0 => (0, 1), 1 => (-1, 0), 2 => (0, -1), _ => (1, 0) };

    /// <summary>
    /// The blocks to set for placing <paramref name="block"/> at (x, y, z) (two for doors, beds and tall plants),
    /// or an empty list if the second half has no room. <paramref name="clickY"/> is where on the face (0..1).
    /// </summary>
    public static List<(int X, int Y, int Z, BlockType Block)> Place(BlockType block, int x, int y, int z, int face, float clickY,
        float yaw, float pitch, Func<int, int, int, bool> free)
    {
        var look = Look(yaw);
        var name = block.Name;
        var side = face >= 2;
        var upper = face == 0 || side && clickY > 0.5f;

        // Standing items placed on a wall become their wall variant.
        if (side && name.EndsWith("standing_sign") && Blocks.ByName(name.Replace("standing_sign", "wall_sign")) is { } wallSign)
            block = wallSign;

        var states = new OrderedDictionary<string, object>(block.States);
        foreach (var key in block.States.Keys)
        {
            object? value = key switch
            {
                "pillar_axis" => face switch { 0 or 1 => "y", 2 or 3 => "z", _ => "x" },
                // Doors and gates face where the player looks; containers, pumpkins... face the player.
                "minecraft:cardinal_direction" => name.EndsWith("_door") || name.Contains("fence_gate") || name == "minecraft:anvil"
                    ? Cardinals[look] : Cardinals[(look + 2) & 3],
                "weirdo_direction" => look switch { 0 => 2, 1 => 1, 2 => 3, _ => 0 }, // stairs climb away from the player
                "upside_down_bit" => (byte)(upper ? 1 : 0),
                "minecraft:vertical_half" => upper ? "top" : "bottom",
                "torch_facing_direction" => face switch { 2 => "north", 3 => "south", 4 => "west", 5 => "east", _ => "top" },
                "facing_direction" => FacingDirection(block, face, look, pitch),
                "minecraft:facing_direction" => pitch > 45 ? "up" : pitch < -45 ? "down" : Cardinals[(look + 2) & 3],
                "direction" => name.Contains("trapdoor") ? look switch { 0 => 3, 1 => 0, 2 => 2, _ => 1 } : look,
                "ground_sign_direction" => (int)Math.Floor((yaw + 180) * 16 / 360 + 0.5) & 15,
                "lever_direction" => face switch
                {
                    0 => look % 2 == 0 ? "down_north_south" : "down_east_west",
                    1 => look % 2 == 0 ? "up_north_south" : "up_east_west",
                    2 => "north", 3 => "south", 4 => "west", _ => "east",
                },
                "attachment" => face switch { 0 => "hanging", 1 => "standing", _ => "side" },
                "hanging" => (byte)(face == 0 ? 1 : 0),
                "attached_bit" => (byte)0,
                "upper_block_bit" or "head_piece_bit" => (byte)0,
                _ => null,
            };
            if (value is not null)
                states[key] = value;
        }
        var placed = Resolve(block, states);

        var result = new List<(int, int, int, BlockType)> { (x, y, z, placed) };
        if (placed.States.ContainsKey("upper_block_bit"))
        {
            if (!free(x, y + 1, z))
                return [];
            var top = new OrderedDictionary<string, object>(placed.States) { ["upper_block_bit"] = (byte)1 };
            result.Add((x, y + 1, z, Resolve(placed, top)));
        }
        else if (placed.States.ContainsKey("head_piece_bit"))
        {
            var (dx, dz) = Offset(look);
            if (!free(x + dx, y, z + dz))
                return [];
            var head = new OrderedDictionary<string, object>(placed.States) { ["head_piece_bit"] = (byte)1 };
            result.Add((x + dx, y, z + dz, Resolve(placed, head)));
        }
        return result;
    }

    /// <summary>The other half of a door, tall plant or bed, if <paramref name="block"/> is one.</summary>
    public static (int X, int Y, int Z)? OtherHalf(BlockType block, int x, int y, int z)
    {
        if (block.States.TryGetValue("upper_block_bit", out var upper))
            return (x, Convert.ToInt32(upper) == 1 ? y - 1 : y + 1, z);
        if (block.States.TryGetValue("head_piece_bit", out var head) && block.States.TryGetValue("direction", out var direction))
        {
            var (dx, dz) = Offset(Convert.ToInt32(direction));
            return Convert.ToInt32(head) == 1 ? (x - dx, y, z - dz) : (x + dx, y, z + dz);
        }
        return null;
    }

    // Wall-attached blocks face away from the wall they are on; machines face the player (up/down too).
    private static int FacingDirection(BlockType block, int face, int look, float pitch)
    {
        var name = block.Name;
        if (name.Contains("ladder") || name.Contains("wall_sign") || name.Contains("button") || name.Contains("end_rod")
            || name.Contains("lightning_rod") || name.Contains("wall_banner") || name.Contains("hanging_sign"))
            return face;
        if (name == "minecraft:hopper")
            return face == 1 ? 0 : face switch { 2 => 3, 3 => 2, 4 => 5, 5 => 4, _ => 0 };
        if (pitch > 45)
            return 1;
        if (pitch < -45)
            return 0;
        return ((look + 2) & 3) switch { 0 => 3, 1 => 4, 2 => 2, _ => 5 };
    }

    private static BlockType Resolve(BlockType block, OrderedDictionary<string, object> states) =>
        Blocks.ByKey(new BlockType(0, block.Name, states).Key) ?? block;
}
