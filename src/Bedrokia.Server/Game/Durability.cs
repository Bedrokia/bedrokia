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

using Bedrokia.Server.Game.Custom.Items;

namespace Bedrokia.Server.Game;

/// <summary>
/// Tool, weapon and armour wear: the "Damage" NBT tag the client shows as the durability bar (Bedrock values).
/// Unbreaking skips a point with a chance of level / (level + 1) (armour: 40 % of that).
/// </summary>
public static class Durability
{
    private static readonly Dictionary<string, int> Tiers = new()
    {
        ["wooden"] = 59, ["stone"] = 131, ["iron"] = 250, ["golden"] = 32, ["diamond"] = 1561, ["netherite"] = 2031,
    };

    private static readonly Dictionary<string, int> Fixed = new()
    {
        ["bow"] = 384, ["crossbow"] = 464, ["trident"] = 250, ["shears"] = 238, ["fishing_rod"] = 384,
        ["flint_and_steel"] = 64, ["shield"] = 336, ["elytra"] = 432, ["mace"] = 500, ["carrot_on_a_stick"] = 25,
        ["warped_fungus_on_a_stick"] = 100, ["brush"] = 64, ["turtle_helmet"] = 275,
    };

    // Armour: base per material x factor per piece (helmet 11, chestplate 16, leggings 15, boots 13).
    private static readonly Dictionary<string, int> ArmourMaterial = new()
    {
        ["leather"] = 5, ["chainmail"] = 15, ["iron"] = 15, ["golden"] = 7, ["diamond"] = 33, ["netherite"] = 37,
    };

    private static readonly Dictionary<string, int> ArmourPiece = new()
    {
        ["helmet"] = 11, ["chestplate"] = 16, ["leggings"] = 15, ["boots"] = 13,
    };

    /// <summary>Uses before the item breaks, 0 if it does not wear out.</summary>
    public static int Max(ItemStack? stack)
    {
        if (stack is null)
            return 0;
        if (CustomItemRegistry.Get(stack.Type.Name)?.GetComponent<DurabilityItemComponent>() is { } custom)
            return custom.MaxDurability;
        if (!stack.Type.Name.StartsWith("minecraft:"))
            return 0;
        var name = stack.Type.Name["minecraft:".Length..];
        if (Fixed.TryGetValue(name, out var max))
            return max;
        var parts = name.Split('_');
        if (parts.Length != 2)
            return 0;
        if (parts[1] is "sword" or "pickaxe" or "axe" or "shovel" or "hoe" && Tiers.TryGetValue(parts[0], out var tier))
            return tier;
        if (ArmourMaterial.TryGetValue(parts[0], out var material) && ArmourPiece.TryGetValue(parts[1], out var piece))
            return material * piece;
        return 0;
    }

    /// <summary>Points of wear already taken.</summary>
    public static int Damage(ItemStack? stack) => stack?.Nbt?.GetValueOrDefault("Damage") is int damage ? damage : 0;

    /// <summary>
    /// The stack after <paramref name="amount"/> uses: a copy with more wear, the stack itself when nothing changed
    /// (no durability or Unbreaking saved it), or null when it broke.
    /// </summary>
    public static ItemStack? Wear(ItemStack stack, int amount, Random random, bool armour = false)
    {
        var max = Max(stack);
        if (max <= 0 || amount <= 0 || stack.Nbt?.GetValueOrDefault("Unbreakable") is byte and not 0)
            return stack;
        var unbreaking = Enchanting.Level(stack, "unbreaking");
        var taken = 0;
        for (var i = 0; i < amount; i++)
        {
            var saved = unbreaking > 0 && random.NextDouble() < (armour ? 0.4 : 1.0) * unbreaking / (unbreaking + 1.0);
            if (!saved)
                taken++;
        }
        if (taken == 0)
            return stack;
        var damage = Damage(stack) + taken;
        if (damage >= max)
            return null;
        var nbt = stack.Nbt is null ? new OrderedDictionary<string, object>() : new OrderedDictionary<string, object>(stack.Nbt);
        nbt["Damage"] = damage;
        return new ItemStack(stack.Type, stack.Count, stack.Meta, stack.BlockRuntimeId, nbt);
    }
}
