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

/// <summary>
/// The "multi" recipes, whose result depends on the ingredients: firework rockets (flight = gunpowder, the stars put
/// in), firework stars (colours, shape, trail, twinkle) and map copies. Each ingredient slot is used once per craft.
/// </summary>
public static class SpecialRecipes
{
    // Bedrock dye colour index (FireworkColor) and RGB, by item.
    private static readonly Dictionary<string, (byte Index, int Rgb)> Dyes = new()
    {
        ["black_dye"] = (0, 0x1D1D21), ["ink_sac"] = (0, 0x1D1D21), ["red_dye"] = (1, 0xB02E26), ["green_dye"] = (2, 0x5E7C16),
        ["brown_dye"] = (3, 0x835432), ["cocoa_beans"] = (3, 0x835432), ["blue_dye"] = (4, 0x3C44AA), ["lapis_lazuli"] = (4, 0x3C44AA),
        ["purple_dye"] = (5, 0x8932B8), ["cyan_dye"] = (6, 0x169C9C), ["light_gray_dye"] = (7, 0x9D9D97), ["gray_dye"] = (8, 0x474F52),
        ["pink_dye"] = (9, 0xF38BAA), ["lime_dye"] = (10, 0x80C71F), ["yellow_dye"] = (11, 0xFED83D), ["light_blue_dye"] = (12, 0x3AB3DA),
        ["magenta_dye"] = (13, 0xC74EBD), ["orange_dye"] = (14, 0xF9801D), ["white_dye"] = (15, 0xF9FFFE), ["bone_meal"] = (15, 0xF9FFFE),
    };

    // Shape items of a firework star: large ball, star, creeper face, burst (none: small ball).
    private static readonly Dictionary<string, byte> Shapes = new()
    {
        ["fire_charge"] = 1, ["gold_nugget"] = 2, ["creeper_head"] = 3, ["skull"] = 3, ["feather"] = 4,
    };

    /// <summary>The result of a special recipe made of <paramref name="items"/> (one of each slot), or null if none fits.</summary>
    public static ItemStack? Craft(IReadOnlyList<ItemStack> items)
    {
        var names = items.Select(i => i.Type.Name.StartsWith("minecraft:") ? i.Type.Name["minecraft:".Length..] : i.Type.Name).ToList();
        int Count(string name) => names.Count(n => n == name);
        return Rocket(items, names, Count) ?? Star(items, names, Count) ?? MapCopy(items, names, Count);
    }

    // Paper + 1-3 gunpowder (+ firework stars): three rockets.
    private static ItemStack? Rocket(IReadOnlyList<ItemStack> items, List<string> names, Func<string, int> count)
    {
        var gunpowder = count("gunpowder");
        if (count("paper") != 1 || gunpowder is < 1 or > 3 || names.Any(n => n is not ("paper" or "gunpowder" or "firework_star")))
            return null;
        var explosions = items.Where(i => i.Type.Name == "minecraft:firework_star")
            .Select(i => i.Nbt?.GetValueOrDefault("FireworkExplosion"))
            .OfType<OrderedDictionary<string, object>>()
            .Cast<object>()
            .ToList();
        var nbt = new OrderedDictionary<string, object>
        {
            ["Fireworks"] = new OrderedDictionary<string, object>
            {
                ["Explosions"] = new NbtList(10, explosions), // 10: compound tag
                ["Flight"] = (byte)gunpowder,
            },
        };
        return Items.ByName("firework_rocket") is { } type ? new ItemStack(type, 3, 0, 0, nbt) : null;
    }

    // Gunpowder + dyes (+ one shape item, diamond = trail, glowstone dust = twinkle): one star.
    private static ItemStack? Star(IReadOnlyList<ItemStack> items, List<string> names, Func<string, int> count)
    {
        var dyes = names.Where(Dyes.ContainsKey).ToList();
        var shapes = names.Where(Shapes.ContainsKey).ToList();
        if (count("gunpowder") != 1 || dyes.Count == 0 || shapes.Count > 1 || count("diamond") > 1 || count("glowstone_dust") > 1
            || names.Any(n => n is not ("gunpowder" or "diamond" or "glowstone_dust") && !Dyes.ContainsKey(n) && !Shapes.ContainsKey(n)))
            return null;
        var colours = dyes.Select(d => Dyes[d].Index).ToArray();
        var explosion = new OrderedDictionary<string, object>
        {
            ["FireworkColor"] = colours,
            ["FireworkFade"] = Array.Empty<byte>(),
            ["FireworkFlicker"] = (byte)(count("glowstone_dust") > 0 ? 1 : 0),
            ["FireworkTrail"] = (byte)(count("diamond") > 0 ? 1 : 0),
            ["FireworkType"] = shapes.Count == 1 ? Shapes[shapes[0]] : (byte)0,
        };
        // The star shows the mix of its colours.
        int r = 0, g = 0, b = 0;
        foreach (var dye in dyes)
        {
            var rgb = Dyes[dye].Rgb;
            r += (rgb >> 16) & 0xFF;
            g += (rgb >> 8) & 0xFF;
            b += rgb & 0xFF;
        }
        var mixed = unchecked((int)0xFF000000) | (r / dyes.Count << 16) | (g / dyes.Count << 8) | (b / dyes.Count);
        var nbt = new OrderedDictionary<string, object> { ["FireworkExplosion"] = explosion, ["customColor"] = mixed };
        return Items.ByName("firework_star") is { } type ? new ItemStack(type, 1, colours[0], 0, nbt) : null;
    }

    // A filled map + empty maps: copies of it.
    private static ItemStack? MapCopy(IReadOnlyList<ItemStack> items, List<string> names, Func<string, int> count)
    {
        var empty = count("empty_map");
        if (count("filled_map") != 1 || empty < 1 || names.Any(n => n is not ("filled_map" or "empty_map")))
            return null;
        var map = items.First(i => i.Type.Name == "minecraft:filled_map");
        return new ItemStack(map.Type, empty + 1, map.Meta, 0, map.Nbt);
    }
}
