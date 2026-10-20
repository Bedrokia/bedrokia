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
using Bedrokia.Protocol;

namespace Bedrokia.Server.Game;

/// <summary>
/// A stack of items. Each stack placed into an empty slot gets a new <see cref="NetworkId"/>, which the client
/// echoes in inventory requests (ItemStackRequest) to prove it is in sync.
/// </summary>
public sealed class ItemStack
{
    private static int _nextNetworkId;
    private static readonly FrozenDictionary<string, int> MaxStackSizes = LoadStackSizes().ToFrozenDictionary();

    public ItemStack(ItemType type, int count, short meta = 0, uint blockRuntimeId = 0, OrderedDictionary<string, object>? nbt = null)
    {
        Type = type;
        Count = count;
        Meta = meta;
        BlockRuntimeId = blockRuntimeId;
        Nbt = nbt;
        NetworkId = Interlocked.Increment(ref _nextNetworkId);
    }

    public ItemType Type { get; }
    public int Count { get; private set; }
    public short Meta { get; }
    public uint BlockRuntimeId { get; }
    public OrderedDictionary<string, object>? Nbt { get; }
    public int NetworkId { get; }

    public int MaxCount => MaxStackSizes.GetValueOrDefault(Type.Name, 64);

    /// <summary>The block this item places, if any.</summary>
    public BlockType? Block => BlockRuntimeId == 0 ? null : Blocks.ByNetworkId(BlockRuntimeId);

    /// <summary>A stack of the given item name (block items get their default block state), or null if unknown.</summary>
    public static ItemStack? Of(string name, int count = 1)
    {
        if (Items.ByName(name) is not { } type)
            return null;
        return new ItemStack(type, count, 0, Blocks.ByName(type.Name)?.NetworkId ?? 0);
    }

    /// <summary>Same item, new count; keeps the network id (same stack in the same slot).</summary>
    internal void SetCount(int count) => Count = count;

    /// <summary>A copy with another count and a fresh network id (a new stack in another slot).</summary>
    public ItemStack Split(int count) => new(Type, count, Meta, BlockRuntimeId, Nbt);

    /// <summary>A copy with a custom name and lore lines (NBT "display", as vanilla anvils and /give do).</summary>
    public ItemStack WithName(string name, params string[] lore)
    {
        var nbt = Nbt is null ? new OrderedDictionary<string, object>() : new OrderedDictionary<string, object>(Nbt);
        var display = new OrderedDictionary<string, object> { ["Name"] = name };
        if (lore.Length > 0)
            display["Lore"] = new Protocol.NbtList(8, [.. lore]); // 8: string tag
        nbt["display"] = display;
        return new ItemStack(Type, Count, Meta, BlockRuntimeId, nbt);
    }

    /// <summary>Custom name given by <see cref="WithName"/> or an anvil; null if none.</summary>
    public string? CustomName => Nbt?.GetValueOrDefault("display") is OrderedDictionary<string, object> display
        ? display.GetValueOrDefault("Name") as string : null;

    /// <summary>Damage sustained by this item (0 = full durability).</summary>
    public int Damage
    {
        get
        {
            if (Nbt is not null && Nbt.TryGetValue("Damage", out var d) && d is IConvertible c)
                return Convert.ToInt32(c);
            return Meta > 0 && MaxDurability > 0 ? Meta : 0;
        }
    }

    /// <summary>Maximum durability for this item kind, or 0 if not durable.</summary>
    public int MaxDurability => GetMaxDurability(Type.Name);

    /// <summary>Returns a copy with the given damage amount.</summary>
    public ItemStack WithDamage(int damage)
    {
        var nbt = Nbt is null ? new OrderedDictionary<string, object>() : new OrderedDictionary<string, object>(Nbt);
        if (damage > 0)
            nbt["Damage"] = damage;
        else
            nbt.Remove("Damage");
        return new ItemStack(Type, Count, (short)Math.Min(damage, short.MaxValue), BlockRuntimeId, nbt.Count > 0 ? nbt : null);
    }

    /// <summary>Anvil cumulative repair penalty level cost (2^n - 1).</summary>
    public int GetRepairCost() =>
        Nbt?.TryGetValue("RepairCost", out var c) == true && c is IConvertible conv ? Convert.ToInt32(conv) : 0;

    /// <summary>Returns a copy with the given anvil repair penalty.</summary>
    public ItemStack WithRepairCost(int cost)
    {
        var nbt = Nbt is null ? new OrderedDictionary<string, object>() : new OrderedDictionary<string, object>(Nbt);
        if (cost > 0)
            nbt["RepairCost"] = cost;
        else
            nbt.Remove("RepairCost");
        return new ItemStack(Type, Count, Meta, BlockRuntimeId, nbt.Count > 0 ? nbt : null);
    }

    /// <summary>Enchantments carried on this stack.</summary>
    public IReadOnlyList<(int Id, int Level)> GetEnchantments() => Enchanting.Of(this);

    /// <summary>True if this item has the given enchantment id.</summary>
    public bool HasEnchantment(int id) => Enchanting.Of(this).Any(e => e.Id == id);

    /// <summary>Level of the named enchantment, or 0 if absent.</summary>
    public int GetEnchantmentLevel(string name) => Enchanting.Level(this, name);

    /// <summary>Returns a copy with the given enchantment added or updated.</summary>
    public ItemStack AddEnchantment(int id, int level)
    {
        var enchs = Enchanting.Of(this).Where(e => e.Id != id).ToList();
        if (level > 0)
            enchs.Add((id, level));
        return Enchanting.With(this, enchs);
    }

    /// <summary>Returns a copy without the given enchantment.</summary>
    public ItemStack RemoveEnchantment(int id) =>
        Enchanting.With(this, Enchanting.Of(this).Where(e => e.Id != id));

    /// <summary>Vanilla durability lookup by item name.</summary>
    public static int GetMaxDurability(string name)
    {
        if (name.StartsWith("minecraft:"))
            name = name["minecraft:".Length..];
        if (Custom.Items.CustomItemRegistry.Get(name)?.GetComponent<Custom.Items.DurabilityItemComponent>() is { } custom)
            return custom.MaxDurability;
        return name switch
        {
            "wooden_sword" or "wooden_shovel" or "wooden_pickaxe" or "wooden_axe" or "wooden_hoe" => 59,
            "stone_sword" or "stone_shovel" or "stone_pickaxe" or "stone_axe" or "stone_hoe" => 131,
            "iron_sword" or "iron_shovel" or "iron_pickaxe" or "iron_axe" or "iron_hoe" => 250,
            "golden_sword" or "golden_shovel" or "golden_pickaxe" or "golden_axe" or "golden_hoe" => 32,
            "diamond_sword" or "diamond_shovel" or "diamond_pickaxe" or "diamond_axe" or "diamond_hoe" => 1561,
            "netherite_sword" or "netherite_shovel" or "netherite_pickaxe" or "netherite_axe" or "netherite_hoe" => 2031,

            "leather_helmet" => 55, "leather_chestplate" => 80, "leather_leggings" => 75, "leather_boots" => 65,
            "chainmail_helmet" => 165, "chainmail_chestplate" => 240, "chainmail_leggings" => 225, "chainmail_boots" => 195,
            "iron_helmet" => 165, "iron_chestplate" => 240, "iron_leggings" => 225, "iron_boots" => 195,
            "golden_helmet" => 77, "golden_chestplate" => 112, "golden_leggings" => 105, "golden_boots" => 91,
            "diamond_helmet" => 363, "diamond_chestplate" => 528, "diamond_leggings" => 495, "diamond_boots" => 429,
            "netherite_helmet" => 407, "netherite_chestplate" => 592, "netherite_leggings" => 555, "netherite_boots" => 481,
            "turtle_helmet" => 275,

            "bow" => 384, "crossbow" => 465, "trident" => 250, "shield" => 336,
            "fishing_rod" => 64, "shears" => 238, "flint_and_steel" => 64, "elytra" => 432, "mace" => 500,
            "warped_fungus_on_a_stick" or "carrot_on_a_stick" => 25,
            _ => 0,
        };
    }

    /// <summary>True when the two stacks can be merged.</summary>
    public bool StacksWith(ItemStack other) =>
        other.Type == Type && other.Meta == Meta && other.BlockRuntimeId == BlockRuntimeId
        && (Nbt is null || Nbt.Count == 0) == (other.Nbt is null || other.Nbt.Count == 0)
        && (Nbt is null || Nbt.Count == 0 || Protocol.Nbt.Write(Nbt, false).AsSpan().SequenceEqual(Protocol.Nbt.Write(other.Nbt!, false)));

    public override string ToString() => $"{Count}x {Type.Name}";

    /// <summary>ItemInstance encoding (gophertunnel Writer.ItemInstance, MIT). Null or empty writes air.</summary>
    public static void WriteInstance(BedrockWriter w, ItemStack? stack)
    {
        if (stack is null || stack.Count <= 0)
        {
            w.Int16(0).UInt16(0).VarUInt32(0).Bool(false).VarUInt32(0).VarUInt32(0);
            return;
        }
        w.Int16(stack.Type.RuntimeId).UInt16((ushort)stack.Count).VarUInt32((ushort)stack.Meta)
            .Bool(true).VarInt32(stack.NetworkId)
            .VarUInt32(stack.BlockRuntimeId);
        Items.WriteUserData(w, stack.Nbt, stack.Type.RuntimeId == Items.ShieldRuntimeId);
    }

    private static Dictionary<string, int> LoadStackSizes()
    {
        using var stream = typeof(ItemStack).Assembly.GetManifestResourceStream("item_stack_sizes.tsv")
                           ?? throw new InvalidOperationException("item_stack_sizes.tsv resource is missing.");
        using var reader = new StreamReader(stream);
        var sizes = new Dictionary<string, int>();
        while (reader.ReadLine() is { } line)
        {
            var parts = line.Split('\t');
            if (parts.Length == 2 && int.TryParse(parts[1], out var size))
                sizes[parts[0]] = size;
        }
        return sizes;
    }
}

/// <summary>A fixed-size set of item slots (player inventory, armour, cursor...).</summary>
public sealed class Inventory(int size)
{
    private readonly ItemStack?[] _slots = new ItemStack?[size];

    public int Size => _slots.Length;

    public ItemStack? this[int slot]
    {
        get => slot >= 0 && slot < _slots.Length ? _slots[slot] : null;
        set
        {
            if (slot >= 0 && slot < _slots.Length)
                _slots[slot] = value is { Count: > 0 } ? value : null;
        }
    }

    public IEnumerable<ItemStack?> Slots => _slots;

    /// <summary>Adds as much of <paramref name="stack"/> as fits; returns the changed slots and the count left over.</summary>
    public (List<int> Changed, int Remaining) Add(ItemStack stack)
    {
        var remaining = stack.Count;
        var changed = new List<int>();
        for (var pass = 0; pass < 2 && remaining > 0; pass++)
        {
            for (var i = 0; i < _slots.Length && remaining > 0; i++)
            {
                var slot = _slots[i];
                if (pass == 0 && slot is not null && slot.StacksWith(stack) && slot.Count < slot.MaxCount)
                {
                    var moved = Math.Min(remaining, slot.MaxCount - slot.Count);
                    slot.SetCount(slot.Count + moved);
                    remaining -= moved;
                    changed.Add(i);
                }
                else if (pass == 1 && slot is null)
                {
                    var moved = Math.Min(remaining, stack.MaxCount);
                    _slots[i] = stack.Split(moved);
                    remaining -= moved;
                    changed.Add(i);
                }
            }
        }
        return (changed, remaining);
    }

    public void Clear() => Array.Clear(_slots);
}
