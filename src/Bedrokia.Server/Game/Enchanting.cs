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


/// <summary>An enchantment kind (Bedrock network id), ported from Dragonfly (MIT) server/item/enchantment/*.go.</summary>
public sealed record Enchantment(int Id, string Name, int MaxLevel, int Weight, Func<int, (int Min, int Max)> Cost, Func<string, bool> Fits,
    string? Group = null, bool Treasure = false);

/// <summary>Enchantments: registry, item NBT ("ench" list), enchanting-table options and the vanilla effects.</summary>
public static class Enchanting
{
    private const int Common = 10, Uncommon = 5, Rare = 2, VeryRare = 1;

    private static bool Armour(string n) => n.EndsWith("_helmet") || n.EndsWith("_chestplate") || n.EndsWith("_leggings") || n.EndsWith("_boots") || n == "turtle_helmet";
    private static bool Sword(string n) => n.EndsWith("_sword");
    private static bool Axe(string n) => n.EndsWith("_axe");
    private static bool Digger(string n) => n.EndsWith("_pickaxe") || n.EndsWith("_shovel") || Axe(n) || n.EndsWith("_hoe");
    private static bool Durable(string n) => Armour(n) || Sword(n) || Digger(n) || n is "bow" or "crossbow" or "trident" or "shears" or "fishing_rod" or "flint_and_steel" or "shield" or "elytra";

    public static readonly Enchantment[] All =
    [
        // Armure - Protection
        new(0, "protection", 4, Common, l => (1 + (l - 1) * 11, 12 + (l - 1) * 11), Armour, "protection"),
        new(1, "fire_protection", 4, Uncommon, l => (10 + (l - 1) * 8, 18 + (l - 1) * 8), Armour, "protection"),
        new(2, "feather_falling", 4, Uncommon, l => (5 + (l - 1) * 6, 11 + (l - 1) * 6), n => n.EndsWith("_boots")),
        new(3, "blast_protection", 4, Rare, l => (5 + (l - 1) * 8, 13 + (l - 1) * 8), Armour, "protection"),
        new(4, "projectile_protection", 4, Uncommon, l => (3 + (l - 1) * 6, 9 + (l - 1) * 6), Armour, "protection"),
        new(5, "thorns", 3, VeryRare, l => (10 + 20 * (l - 1), 60 + 20 * (l - 1)), Armour),
        new(6, "respiration", 3, Rare, l => (10 * l, 10 * l + 30), n => n.EndsWith("_helmet")),
        new(7, "depth_strider", 3, Rare, l => (10 * l, 10 * l + 15), n => n.EndsWith("_boots"), "water_movement"),
        new(8, "aqua_affinity", 1, Rare, _ => (1, 41), n => n.EndsWith("_helmet")),
        
        // Épées et haches - Dégâts
        new(9, "sharpness", 5, Common, l => (1 + (l - 1) * 11, 21 + (l - 1) * 11), n => Sword(n) || Axe(n), "damage"),
        new(10, "smite", 5, Uncommon, l => (5 + (l - 1) * 8, 25 + (l - 1) * 8), n => Sword(n) || Axe(n), "damage"),
        new(11, "bane_of_arthropods", 5, Uncommon, l => (5 + (l - 1) * 8, 25 + (l - 1) * 8), n => Sword(n) || Axe(n), "damage"),
        new(12, "knockback", 2, Uncommon, l => (5 + (l - 1) * 20, 55 + (l - 1) * 20), Sword),
        new(13, "fire_aspect", 2, Rare, l => (10 + (l - 1) * 20, 60 + (l - 1) * 20), Sword),
        new(14, "looting", 3, Rare, l => (15 + (l - 1) * 9, 65 + (l - 1) * 9), Sword),
        
        // Outils
        new(15, "efficiency", 5, Common, l => (1 + 10 * (l - 1), 51 + 10 * (l - 1)), Digger),
        new(16, "silk_touch", 1, VeryRare, _ => (15, 65), Digger, "mining"),
        new(17, "unbreaking", 3, Uncommon, l => (5 + 8 * (l - 1), 55 + 8 * (l - 1)), Durable),
        new(18, "fortune", 3, Rare, l => (15 + (l - 1) * 9, 65 + (l - 1) * 9 + l), Digger, "mining"),
        
        // Arc
        new(19, "power", 5, Common, l => (1 + (l - 1) * 10, 16 + (l - 1) * 10), n => n == "bow"),
        new(20, "punch", 2, Rare, l => (12 + (l - 1) * 20, 37 + (l - 1) * 20), n => n == "bow"),
        new(21, "flame", 1, Rare, _ => (20, 50), n => n == "bow"),
        new(22, "infinity", 1, VeryRare, _ => (20, 50), n => n == "bow", "infinity"),
        
        // Canne à pêche
        new(23, "luck_of_the_sea", 3, Rare, l => (15 + (l - 1) * 9, 65 + (l - 1) * 9), n => n == "fishing_rod"),
        new(24, "lure", 3, Rare, l => (15 + (l - 1) * 9, 65 + (l - 1) * 9), n => n == "fishing_rod"),
        
        // Bottes - Gel
        new(25, "frost_walker", 2, Rare, l => (10 * l, 10 * l + 15), n => n.EndsWith("_boots"), "water_movement", Treasure: true),
        
        // Réparation
        new(26, "mending", 1, Rare, l => (25 * l, 25 * l + 50), Durable, "infinity", Treasure: true),
        
        // Malédiction
        new(27, "binding_curse", 1, VeryRare, _ => (25, 50), Armour, null, Treasure: true),
        new(28, "vanishing_curse", 1, VeryRare, _ => (25, 50), Durable, null, Treasure: true),
        
        // Trident
        new(29, "impaling", 5, Rare, l => (1 + (l - 1) * 8, 21 + (l - 1) * 8), n => n == "trident"),
        new(30, "riptide", 3, Rare, l => (10 + (l - 1) * 7, 50 + (l - 1) * 7), n => n == "trident", "trident_exclusive"),
        new(31, "loyalty", 3, Uncommon, l => (12 + (l - 1) * 7, 50), n => n == "trident"),
        new(32, "channeling", 1, VeryRare, _ => (25, 50), n => n == "trident", "trident_exclusive"),
        
        // Arbalète
        new(33, "multishot", 1, Rare, _ => (20, 50), n => n == "crossbow", "crossbow_exclusive"),
        new(34, "piercing", 4, Common, l => (1 + (l - 1) * 10, 50), n => n == "crossbow"),
        new(35, "quick_charge", 3, Uncommon, l => (12 + (l - 1) * 20, 50), n => n == "crossbow"),
        
        // Bottes - Âme
        new(36, "soul_speed", 3, VeryRare, l => (10 * l, 10 * l + 15), n => n.EndsWith("_boots"), null, Treasure: true),
        
        // Haches - Sweeping (Java uniquement mais inclus pour compatibilité)
        new(37, "sweeping", 3, Rare, l => (5 + (l - 1) * 9, 20 + (l - 1) * 9), Sword),
        
        // Nouveaux enchantements 1.19+
        new(38, "swift_sneak", 3, VeryRare, l => (25 * l, 25 * l + 50), n => n.EndsWith("_leggings"), null, Treasure: true),
        
        // Enchantements Bedrock Edition spécifiques (IDs potentiellement différents)
        new(39, "wind_burst", 3, VeryRare, l => (10 + (l - 1) * 10, 40 + (l - 1) * 10), n => n == "mace", null, Treasure: true),
        new(40, "density", 5, Common, l => (1 + (l - 1) * 10, 21 + (l - 1) * 10), n => n == "mace"),
        new(41, "breach", 4, Rare, l => (15 + (l - 1) * 9, 65 + (l - 1) * 9), n => n == "mace"),
    ];

    public static Enchantment? ById(int id) => All.FirstOrDefault(e => e.Id == id);

    /// <summary>Enchantments of a stack, read from its "ench" NBT list.</summary>
    public static List<(int Id, int Level)> Of(ItemStack? stack)
    {
        var result = new List<(int, int)>();
        if (stack?.Nbt is { } nbt && nbt.TryGetValue("ench", out var list) && list is NbtList ench)
            foreach (var entry in ench.Values.OfType<OrderedDictionary<string, object>>())
                result.Add((Convert.ToInt32(entry["id"]), Convert.ToInt32(entry["lvl"])));
        return result;
    }

    public static int Level(ItemStack? stack, string name) =>
        Of(stack).Where(e => ById(e.Id)?.Name == name).Select(e => e.Level).DefaultIfEmpty(0).Max();

    /// <summary>A copy of the stack carrying these enchantments (written as Bedrock's "ench" list).</summary>
    public static ItemStack With(ItemStack stack, IEnumerable<(int Id, int Level)> enchantments)
    {
        var nbt = stack.Nbt is null ? new OrderedDictionary<string, object>() : new OrderedDictionary<string, object>(stack.Nbt);
        nbt["ench"] = new NbtList(Nbt.Compound, enchantments.Select(e => (object)new OrderedDictionary<string, object>
        {
            ["id"] = (short)e.Id, ["lvl"] = (short)e.Level,
        }).ToList());
        return new ItemStack(stack.Type, stack.Count, stack.Meta, stack.BlockRuntimeId, nbt);
    }

    /// <summary>Material enchantability (Dragonfly EnchantmentValue); 0 = not enchantable at a table.</summary>
    public static int Enchantability(ItemStack stack)
    {
        var name = stack.Type.Name["minecraft:".Length..];
        if (name == "book")
            return 1;
        var armour = Armour(name);
        if (!armour && !Sword(name) && !Digger(name) && name is not ("bow" or "crossbow" or "trident" or "fishing_rod"))
            return 0;
        return name.Split('_')[0] switch
        {
            "wooden" => 15, "stone" => 5, "iron" => armour ? 9 : 14, "golden" => armour ? 25 : 22, "diamond" => 10,
            "netherite" => 15, "leather" => 15, "chainmail" => 12, "turtle" => 9, _ => 1,
        };
    }

    /// <summary>The three enchanting-table options (costs and enchantments), Dragonfly's port of the vanilla algorithm.</summary>
    public static (int[] Costs, List<(int Id, int Level)>[] Options)? Options(ItemStack stack, int bookshelves, int seed)
    {
        var value = Enchantability(stack);
        if (value == 0 || stack.Count != 1 || Of(stack).Count > 0)
            return null;
        var random = new Random(seed);
        var baseCost = random.Next(8) + 1 + (bookshelves >> 1) + random.Next(bookshelves + 1);
        int[] costs = [Math.Max(baseCost / 3, 1), baseCost * 2 / 3 + 1, Math.Max(baseCost, bookshelves * 2)];
        var options = costs.Select(c => Pick(random, stack.Type.Name["minecraft:".Length..], value, c)).ToArray();
        return options.All(o => o.Count == 0) ? null : (costs, options);
    }

    private static List<(int, int)> Pick(Random random, string item, int value, int level)
    {
        var bonus = (random.NextDouble() + random.NextDouble() - 1.0) * 0.15;
        var cost = level + 1 + random.Next(value / 4 + 1) + random.Next(value / 4 + 1);
        cost = Math.Max(1, (int)Math.Round(cost + cost * bonus));
        var book = item == "book";

        var available = new List<(Enchantment Kind, int Level)>();
        foreach (var enchant in All.Where(e => !e.Treasure && (book || e.Fits(item))))
            for (var l = enchant.MaxLevel; l > 0; l--)
            {
                var (min, max) = enchant.Cost(l);
                if (cost >= min && cost <= max)
                {
                    available.Add((enchant, l));
                    break;
                }
            }
        var selected = new List<(int, int)>();
        while (available.Count > 0)
        {
            var total = available.Sum(a => a.Kind.Weight);
            var roll = random.Next(total);
            var chosen = available.First(a => (roll -= a.Kind.Weight) < 0);
            selected.Add((chosen.Kind.Id, chosen.Level));
            available.RemoveAll(a => a.Kind == chosen.Kind || (a.Kind.Group is not null && a.Kind.Group == chosen.Kind.Group));
            if (random.Next(50) > cost)
                break;
            cost /= 2;
        }
        return selected;
    }

    private static readonly string[] Words = ["the", "elder", "scrolls", "klaatu", "berata", "niktu", "xyzzy", "bless", "curse", "light",
        "darkness", "fire", "air", "earth", "water", "hot", "dry", "cold", "wet", "ignite", "snuff", "embiggen", "twist", "shorten"];

    /// <summary>PlayerEnchantOptions packet (id 146, gophertunnel layout).</summary>
    public static byte[] OptionsPacket(int[] costs, List<(int Id, int Level)>[] options)
    {
        var w = new BedrockWriter().VarUInt32(146).VarUInt32(3);
        for (var i = 0; i < 3; i++)
        {
            w.Byte((byte)Math.Min(costs[i], 255)).Int32(i).VarUInt32(0).VarUInt32((uint)options[i].Count);
            foreach (var (id, level) in options[i])
                w.Byte((byte)id).Byte((byte)level);
            w.VarUInt32(0)
                .String($"{Words[(costs[i] * 7 + i) % Words.Length]} {Words[(costs[i] * 13 + i * 5) % Words.Length]}")
                .VarUInt32((uint)i);
        }
        return w.ToArray();
    }
}

/// <summary>What the inventory needs from the player while an enchanting table is open.</summary>
public interface IEnchanter
{
    /// <summary>Costs and enchantments offered for this item at the open table, or null if it cannot be enchanted.</summary>
    (int[] Costs, List<(int Id, int Level)>[] Options)? Options(ItemStack item);

    /// <summary>Spends <paramref name="cost"/> levels if the player has at least <paramref name="requirement"/>.</summary>
    bool SpendLevels(int requirement, int cost);

    /// <summary>New enchantment seed after an enchantment, so the next options differ.</summary>
    void Reseed();
}
