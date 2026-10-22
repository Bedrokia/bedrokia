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
        return new ItemStack(type, count, 0, Blocks.ByName(type.Name)?.NetworkId ?? 0, Items.DefaultNbtOf(type.Name));
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
