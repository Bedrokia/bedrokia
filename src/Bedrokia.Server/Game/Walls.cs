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

namespace Bedrokia.Server.Game;

/// <summary>
/// Wall connections, which Bedrock leaves to the server (fences, panes and bars are drawn by the client):
/// "wall_connection_type_*" none/short/tall and "wall_post_bit", as in Dragonfly's (MIT) block/wall.go.
/// </summary>
public static class Walls
{
    private static readonly (string Side, int Dx, int Dz)[] Sides = [("north", 0, -1), ("east", 1, 0), ("south", 0, 1), ("west", -1, 0)];

    public static bool IsWall(BlockType? block) => block is not null && block.States.ContainsKey("wall_post_bit");

    /// <summary>The wall at (x, y, z) shaped after its neighbours.</summary>
    public static BlockType Connect(BlockType wall, Func<int, int, int, BlockType?> get, int x, int y, int z)
    {
        var above = get(x, y + 1, z);
        var states = new OrderedDictionary<string, object>(wall.States);
        var connected = new bool[4];
        for (var i = 0; i < 4; i++)
        {
            var (side, dx, dz) = Sides[i];
            var key = $"wall_connection_type_{side}";
            connected[i] = ConnectsTo(get(x + dx, y, z + dz));
            var tall = connected[i] && above is not null
                       && (IsWall(above) ? above.States[key] is not "none" : BlockPhysics.IsSolid(above));
            states[key] = !connected[i] ? "none" : tall ? "tall" : "short";
        }
        var straight = connected is [true, false, true, false] or [false, true, false, true];
        var post = !straight || (IsWall(above) ? Convert.ToInt32(above!.States["wall_post_bit"]) == 1
            : above is { IsAir: false } && !BlockPhysics.IsSolid(above)); // torches, lanterns... stand on a post
        states["wall_post_bit"] = (byte)(post ? 1 : 0);
        return Blocks.ByKey(new BlockType(0, wall.Name, states).Key) ?? wall;
    }

    private static bool ConnectsTo(BlockType? block) => block is not null && !block.IsAir && (IsWall(block)
        || block.Name.Contains("fence_gate") || block.Name.EndsWith("glass_pane") || block.Name == "minecraft:iron_bars"
        || BlockPhysics.IsSolid(block) && !block.Name.EndsWith("_fence") && !block.Name.Contains("leaves"));
}
