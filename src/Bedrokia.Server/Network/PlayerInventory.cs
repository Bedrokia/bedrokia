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
using Bedrokia.Server.Core;
using Bedrokia.Server.Game;

namespace Bedrokia.Server.Network;

/// <summary>
/// Server-authoritative player inventory: answers ItemStackRequest packets (take, place, swap, drop, destroy,
/// creative). Logic ported from Dragonfly (MIT) session/handler_item_stack_request.go. Game thread only.
/// </summary>
public sealed class PlayerInventory
{
    // Container ids (gophertunnel protocol/container.go).
    private const byte ContainerArmor = 6, ContainerCombined = 12, ContainerCraftingInput = 13, ContainerHotBar = 28,
        ContainerInventory = 29, ContainerOffhand = 34, ContainerCursor = 59, ContainerCreatedOutput = 60;
    private const int CreatedOutputSlot = 50;
    private const int RememberedRequests = 64;

    private readonly Dictionary<int, Dictionary<(Inventory, int), int>> _results = []; // request id → slot → stack id
    private readonly Queue<int> _resultOrder = new();

    public Inventory Main { get; } = new(36); // 0-8 hotbar
    public Inventory Armour { get; } = new(4);
    public Inventory Offhand { get; } = new(1);
    public Inventory Ui { get; } = new(54); // 0 cursor, 28-40 crafting grid, 50 created output

    public BedrockConnection? Player { get; set; }

    /// <summary>This player's ender chest (the same in every ender chest block).</summary>
    public Inventory EnderChest { get; } = new(27);

    /// <summary>The container window the player has open (window 3), or null.</summary>
    public (Inventory Items, string Kind)? OpenContainer { get; set; }

    /// <summary>For a menu (<see cref="InventoryMenu"/>): slot, item in it, item put there; false refuses the change.</summary>
    public Func<int, ItemStack?, ItemStack?, bool>? MenuListener { get; set; }

    private const byte ContainerLevelEntity = 7, ContainerFurnaceFuel = 24, ContainerFurnaceIngredient = 25, ContainerFurnaceResult = 26,
        ContainerBlastFurnaceIngredient = 45, ContainerSmokerIngredient = 46, ContainerBarrel = 58;

    /// <summary>Hotbar slot in hand.</summary>
    public int SelectedSlot { get; set; }

    public ItemStack? Held => Main[SelectedSlot];

    /// <summary>InventoryContent packets for a full resync.</summary>
    public IEnumerable<byte[]> ContentPackets()
    {
        yield return InventoryPackets.InventoryContent(InventoryPackets.WindowInventory, Main);
        yield return InventoryPackets.InventoryContent(InventoryPackets.WindowArmour, Armour);
        yield return InventoryPackets.InventoryContent(InventoryPackets.WindowOffhand, Offhand);
        yield return InventoryPackets.InventoryContent(InventoryPackets.WindowUi, Ui);
        if (OpenContainer is { } open)
            yield return InventoryPackets.InventoryContent(BedrockConnection.WindowContainer, open.Items);
    }

    private sealed record SlotRef(byte Container, byte Slot, int ClientStackId);

    private abstract record Op;
    private sealed record Transfer(int Count, SlotRef From, SlotRef To) : Op;
    private sealed record Swap(SlotRef A, SlotRef B) : Op;
    private sealed record Drop(int Count, SlotRef From) : Op;
    private sealed record Destroy(int Count, SlotRef From) : Op;
    private sealed record Verify(SlotRef Slot) : Op;
    private sealed record CreativeCraft(uint CreativeId) : Op;
    private sealed record Craft(uint RecipeId, int Times, bool Auto) : Op;
    private sealed record Create(int ResultSlot) : Op;

    // Crafting grid in the UI inventory (Dragonfly session/player.go): 2x2 at 28, 3x3 (crafting table) at 32.
    private const int SmallGridOffset = 28, SmallGridSize = 4, LargeGridOffset = 32, LargeGridSize = 9;

    /// <summary>True while a crafting table is open (3x3 grid).</summary>
    public bool CraftingTableOpen { get; set; }

    /// <summary>The crafting tags of the open table ("crafting_table" for a vanilla one and the inventory grid).</summary>
    public IReadOnlyList<string> CraftingTags { get; set; } = ["crafting_table"];

    // The 3x3 grid is in use when a table is open, or when it holds items: the open flag can be cleared by a close the
    // client sends for another window while its table stays open, and its crafts then looked in the 2x2 grid.
    private bool LargeGrid => CraftingTableOpen || Enumerable.Range(LargeGridOffset, LargeGridSize).Any(s => Ui[s] is not null);

    private const byte ContainerEnchantingInput = 22, ContainerEnchantingMaterial = 23;
    public const int EnchantInputSlot = 14, EnchantLapisSlot = 15;

    /// <summary>The open enchanting table (options, level costs), or null.</summary>
    public IEnchanter? Enchanter { get; set; }

    // The anvil window: input and material in the UI inventory (Dragonfly anvilInputSlot, anvilMaterialSlot).
    private const byte ContainerAnvilInput = 0, ContainerAnvilMaterial = 1;
    public const int AnvilInputSlot = 1, AnvilMaterialSlot = 2;

    /// <summary>Set while an anvil is open: called after each use (sound, the anvil wearing out).</summary>
    public Action? AnvilUsed { get; set; }

    private sealed record AnvilCraft(int FilterIndex) : Op;
    private readonly List<string> _filters = [];

    /// <summary>Called with the reason when a request is refused (for the server log).</summary>
    public Action<string>? Rejected { get; set; }

    /// <summary>Called with a summary of each accepted request (debug log).</summary>
    public Action<string>? Accepted { get; set; }
    private sealed record Ignored : Op;
    private sealed record Unsupported(string Name) : Op;

    /// <summary>Handles an ItemStackRequest packet body; returns the response packets.</summary>
    /// <param name="drop">Spawns a dropped stack in the world.</param>
    public List<byte[]> HandleRequests(ref BedrockReader reader, GameMode mode, Action<ItemStack> drop)
    {
        var responses = new List<byte[]>();
        var count = reader.VarUInt32();
        for (var i = 0; i < count; i++)
            if (!HandleOne(ref reader, mode, drop, responses))
                break;
        return responses;
    }

    /// <summary>Handles one request (as embedded in PlayerAuthInput); returns the response packets.</summary>
    public List<byte[]> HandleRequest(ref BedrockReader reader, GameMode mode, Action<ItemStack> drop)
    {
        var responses = new List<byte[]>();
        HandleOne(ref reader, mode, drop, responses);
        return responses;
    }

    // Returns false when the request could not be read to its end (the rest of the packet is unreadable).
    private bool HandleOne(ref BedrockReader reader, GameMode mode, Action<ItemStack> drop, List<byte[]> responses)
    {
        var requestId = reader.VarInt32();
        var actions = new List<Op>();
        var actionCount = reader.VarUInt32();
        for (var a = 0; a < actionCount; a++)
        {
            var action = ReadAction(ref reader);
            if (action is Unsupported { Name: "unparsable" })
            {
                Rejected?.Invoke($"inventory request {requestId}: unknown action, cannot read the rest");
                responses.Add(InventoryPackets.ItemStackResponse(requestId, false, []));
                responses.AddRange(ContentPackets());
                return false;
            }
            actions.Add(action);
        }
        var filters = reader.VarUInt32();
        _filters.Clear();
        for (var f = 0; f < filters; f++)
            _filters.Add(reader.String());
        reader.Int32(); // filter cause

        responses.Add(Process(requestId, actions, mode, drop, out var ok));
        if (!ok)
            responses.AddRange(ContentPackets());
        return true;
    }

    // Action variant (+ legacy byte); variants from 7 on skip the two never-sent container actions.
    private static Op ReadAction(ref BedrockReader r)
    {
        var variant = r.VarUInt32();
        r.Byte();
        var id = variant >= 7 ? variant + 2 : variant;
        switch (id)
        {
            case 0 or 1: // take, place
                return new Transfer(r.Byte(), ReadSlot(ref r), ReadSlot(ref r));
            case 2:
                return new Swap(ReadSlot(ref r), ReadSlot(ref r));
            case 3:
            {
                var count = r.Byte();
                var from = ReadSlot(ref r);
                r.Bool(); // randomly
                return new Drop(count, from);
            }
            case 4:
                return new Destroy(r.Byte(), ReadSlot(ref r));
            case 5: // consume: informative, the craft action already consumed the grid
                r.Byte();
                ReadSlot(ref r);
                return new Ignored();
            case 6: // create (picks one result of a multi-result craft)
                return new Create(r.Byte());
            case 9:
                return new Unsupported("lab table");
            case 10:
                r.VarInt32();
                r.VarInt32();
                return new Unsupported("beacon");
            case 11: // mine block: only verifies the held stack
            {
                var hotbar = r.VarInt32();
                r.VarInt32(); // predicted durability
                var stackId = r.Int32();
                return new Verify(new SlotRef(ContainerInventory, (byte)hotbar, stackId));
            }
            case 12: // craft recipe
                return new Craft(r.VarUInt32(), r.Byte(), false);
            case 13: // auto craft (recipe book): ingredient descriptors are informative
            {
                var craft = new Craft(r.VarUInt32(), r.Byte(), true);
                var ingredients = r.VarUInt32();
                for (var i = 0; i < ingredients; i++)
                {
                    var descriptor = r.VarUInt32();
                    r.Byte();
                    switch (descriptor)
                    {
                        case 1:
                            r.String();
                            r.VarInt32();
                            break;
                        case 2:
                            r.String();
                            r.Bytes(2);
                            break;
                        case 3:
                            r.String();
                            break;
                        case > 3:
                            return new Unsupported("unparsable");
                    }
                    r.Bytes(2); // count
                }
                return craft;
            }
            case 14:
            {
                var creativeId = r.VarUInt32();
                r.Byte(); // number of crafts
                return new CreativeCraft(creativeId);
            }
            case 15: // craft recipe optional: the anvil (the filter string is the new name)
                r.VarUInt32();
                return new AnvilCraft(r.Int32());
            case 16:
                r.Int32();
                r.Byte();
                r.VarInt32();
                return new Unsupported("grindstone");
            case 17:
                r.String();
                r.Byte();
                return new Unsupported("loom");
            case 18: // craft reserved (1.26.60)
                r.String();
                r.Byte();
                return new Unsupported("reserved craft");
            case 19:
                return new Ignored();
            case 20: // craft results (deprecated, informative only)
            {
                var items = r.VarUInt32();
                for (var i = 0; i < items; i++)
                {
                    var descriptor = r.VarUInt32();
                    r.Byte();
                    if (descriptor == 1)
                    {
                        r.String();
                        r.VarInt32();
                    }
                    r.Bytes(2); // count
                    r.VarUInt32(); // block runtime id
                    r.ByteArray(); // user data
                }
                r.Byte();
                return new Ignored();
            }
            default: // anything newer
                return new Unsupported("unparsable");
        }
    }

    // Full container name (id + optional dynamic id), slot, stack network id.
    private static SlotRef ReadSlot(ref BedrockReader r)
    {
        var container = r.Byte();
        if (r.Bool())
            r.Int32();
        return new SlotRef(container, r.Byte(), r.Int32());
    }

    private (Inventory Inventory, int Slot)? Resolve(SlotRef slot) => slot.Container switch
    {
        ContainerHotBar or ContainerInventory or ContainerCombined => (Main, slot.Slot),
        ContainerArmor => (Armour, slot.Slot),
        ContainerOffhand => (Offhand, 0),
        ContainerCursor or ContainerCreatedOutput or ContainerCraftingInput or ContainerEnchantingInput or ContainerEnchantingMaterial => (Ui, slot.Slot),
        ContainerAnvilInput or ContainerAnvilMaterial when AnvilUsed is not null => (Ui, slot.Slot),
        // The open chest, barrel or ender chest; furnaces: ingredient 0, fuel 1, result 2.
        ContainerLevelEntity or ContainerBarrel when OpenContainer is { } open => (open.Items, slot.Slot),
        ContainerFurnaceIngredient or ContainerBlastFurnaceIngredient or ContainerSmokerIngredient when OpenContainer is { } open => (open.Items, 0),
        ContainerFurnaceFuel when OpenContainer is { } open => (open.Items, 1),
        ContainerFurnaceResult when OpenContainer is { } open => (open.Items, 2),
        _ => null,
    };

    private byte[] Process(int requestId, List<Op> actions, GameMode mode, Action<ItemStack> drop, out bool ok)
    {
        var before = new Dictionary<(Inventory, int), (ItemStack? Stack, int Count)>();
        var changes = new Dictionary<(byte, byte), (Inventory Inventory, int Slot)>();
        var dropped = new List<ItemStack>();
        // Later actions of this same request may point at slots it changed (e.g. taking the crafted item).
        var current = new Dictionary<(Inventory, int), int>();
        _results[requestId] = current;

        (Inventory Inventory, int Slot) Slot(SlotRef slot)
        {
            var resolved = Resolve(slot) ?? throw new InvalidOperationException($"unknown container {slot.Container}");
            var actual = resolved.Inventory[resolved.Slot]?.NetworkId ?? 0;
            var expected = slot.ClientStackId >= 0
                ? slot.ClientStackId
                : _results.TryGetValue(slot.ClientStackId, out var result) && result.TryGetValue(resolved, out var id) ? id : -1;
            if (expected != actual)
                throw new InvalidOperationException($"slot {slot.Container}:{slot.Slot} out of sync ({expected} vs {actual})");
            return resolved;
        }
        void Set(SlotRef slot, (Inventory Inventory, int Slot) at, ItemStack? stack)
        {
            Remember(at);
            at.Inventory[at.Slot] = stack;
            changes[(slot.Container, slot.Slot)] = at;
            current[at] = stack?.NetworkId ?? 0;
        }
        void Remember((Inventory Inventory, int Slot) at)
        {
            var current = at.Inventory[at.Slot];
            before.TryAdd(at, (current, current?.Count ?? 0));
        }
        // A menu's listener sees each of its slots that changes; a refusal cancels the whole request.
        void CheckMenu((Inventory Inventory, int Slot) at, ItemStack? with)
        {
            if (MenuListener is { } listener && OpenContainer is { Kind: "Menu" } menu && at.Inventory == menu.Items
                && !listener(at.Slot, at.Inventory[at.Slot], with))
                throw new InvalidOperationException("menu click refused");
        }
        List<ItemStack?> pending = [];
        void CreateResult(ItemStack? result)
        {
            if (result is null)
                throw new InvalidOperationException("craft result already taken");
            Set(new SlotRef(ContainerCreatedOutput, CreatedOutputSlot, 0), (Ui, CreatedOutputSlot), result);
        }
        void Take(byte container, Inventory inventory, int slot, int count)
        {
            var stack = inventory[slot]!;
            Remember((inventory, slot));
            stack.SetCount(stack.Count - count);
            Set(new SlotRef(container, (byte)slot, 0), (inventory, slot), stack.Count > 0 ? stack : null);
        }
        // Each input must sit in its own grid cell with enough items (shape itself is not checked, like Dragonfly).
        void ConsumeGrid(Recipe recipe, int times, int offset, int size)
        {
            var used = new bool[size];
            foreach (var input in recipe.Inputs.Where(i => !i.Empty))
            {
                var cell = Enumerable.Range(0, size).FirstOrDefault(i => !used[i] && Ui[offset + i] is { } has
                    && has.Count >= input.Count * times && input.Matches(has), -1);
                if (cell < 0)
                    throw new InvalidOperationException($"missing ingredient {input.Name}{input.Tag}");
                used[cell] = true;
                Take(ContainerCraftingInput, Ui, offset + cell, input.Count * times);
            }
        }
        // Recipe book: ingredients may come from the grid or anywhere in the inventory.
        void ConsumeAuto(Recipe recipe, int times, int offset, int size)
        {
            foreach (var input in recipe.Inputs.Where(i => !i.Empty))
            {
                var remaining = input.Count * times;
                var sources = Enumerable.Range(offset, size).Select(s => (ContainerCraftingInput, Ui, s))
                    .Concat(Enumerable.Range(0, Main.Size).Select(s => (ContainerCombined, Main, s)));
                foreach (var (container, inventory, slot) in sources)
                {
                    if (remaining == 0)
                        break;
                    if (inventory[slot] is not { } has || !input.Matches(has))
                        continue;
                    var taken = Math.Min(remaining, has.Count);
                    Take(container, inventory, slot, taken);
                    remaining -= taken;
                }
                if (remaining > 0)
                    throw new InvalidOperationException($"missing ingredient {input.Name}{input.Tag}");
            }
        }

        try
        {
            foreach (var action in actions)
            {
                switch (action)
                {
                    case Transfer t:
                    {
                        var from = Slot(t.From);
                        var to = Slot(t.To);
                        var source = from.Inventory[from.Slot] ?? throw new InvalidOperationException("empty source");
                        var dest = to.Inventory[to.Slot];
                        if (t.Count <= 0 || t.Count > source.Count)
                            throw new InvalidOperationException("bad count");
                        if (dest is not null && (!dest.StacksWith(source) || dest.Count + t.Count > dest.MaxCount))
                            throw new InvalidOperationException("cannot merge stacks");
                        CheckMenu(from, null);
                        CheckMenu(to, source);
                        if (Player is not null)
                        {
                            var tx = new InventoryTransactionEvent(Player, InventoryTransactionType.Transfer, source, t.Count, (t.From.Container, t.From.Slot), (t.To.Container, t.To.Slot));
                            if (!Player.Server.Events.OnInventoryTransaction(tx))
                                throw new InvalidOperationException("inventory transaction cancelled");
                        }
                        Remember(from);
                        Remember(to);
                        if (dest is null && t.Count == source.Count)
                        {
                            Set(t.To, to, source);
                            Set(t.From, from, null);
                            break;
                        }
                        if (dest is null)
                            Set(t.To, to, source.Split(t.Count));
                        else
                        {
                            dest.SetCount(dest.Count + t.Count);
                            Set(t.To, to, dest);
                        }
                        source.SetCount(source.Count - t.Count);
                        Set(t.From, from, source.Count > 0 ? source : null);
                        break;
                    }
                    case Swap s:
                    {
                        var a = Slot(s.A);
                        var b = Slot(s.B);
                        var stackA = a.Inventory[a.Slot];
                        var stackB = b.Inventory[b.Slot];
                        CheckMenu(a, stackB);
                        CheckMenu(b, stackA);
                        if (Player is not null)
                        {
                            var tx = new InventoryTransactionEvent(Player, InventoryTransactionType.Swap, stackA ?? stackB, (stackA ?? stackB)?.Count ?? 0, (s.A.Container, s.A.Slot), (s.B.Container, s.B.Slot));
                            if (!Player.Server.Events.OnInventoryTransaction(tx))
                                throw new InvalidOperationException("inventory swap cancelled");
                        }
                        Set(s.A, a, stackB);
                        Set(s.B, b, stackA);
                        break;
                    }
                    case Drop d:
                    {
                        var from = Slot(d.From);
                        var source = from.Inventory[from.Slot] ?? throw new InvalidOperationException("empty source");
                        if (d.Count <= 0 || d.Count > source.Count)
                            throw new InvalidOperationException("bad count");
                        CheckMenu(from, null);
                        var droppedStack = source.Split(d.Count);
                        if (Player is not null)
                        {
                            var dropEvent = new PlayerDropItemEvent(Player, droppedStack);
                            if (!Player.Server.Events.OnPlayerDropItem(dropEvent))
                                throw new InvalidOperationException("item drop cancelled");
                            var tx = new InventoryTransactionEvent(Player, InventoryTransactionType.Drop, droppedStack, d.Count, (d.From.Container, d.From.Slot));
                            if (!Player.Server.Events.OnInventoryTransaction(tx))
                                throw new InvalidOperationException("inventory drop cancelled");
                        }
                        Remember(from);
                        dropped.Add(droppedStack);
                        source.SetCount(source.Count - d.Count);
                        Set(d.From, from, source.Count > 0 ? source : null);
                        break;
                    }
                    case Destroy x:
                    {
                        if (mode != GameMode.Creative)
                            throw new InvalidOperationException("destroy outside creative");
                        var from = Slot(x.From);
                        var source = from.Inventory[from.Slot] ?? throw new InvalidOperationException("empty source");
                        CheckMenu(from, null);
                        if (Player is not null)
                        {
                            var tx = new InventoryTransactionEvent(Player, InventoryTransactionType.Destroy, source, x.Count, (x.From.Container, x.From.Slot));
                            if (!Player.Server.Events.OnInventoryTransaction(tx))
                                throw new InvalidOperationException("inventory destroy cancelled");
                        }
                        Remember(from);
                        source.SetCount(source.Count - Math.Min(x.Count, source.Count));
                        Set(x.From, from, source.Count > 0 ? source : null);
                        break;
                    }
                    case Verify v:
                    {
                        var at = Slot(v.Slot);
                        Set(v.Slot, at, at.Inventory[at.Slot]);
                        break;
                    }
                    case CreativeCraft c:
                    {
                        if (mode != GameMode.Creative)
                            throw new InvalidOperationException("creative item outside creative");
                        var templates = Items.CreativeTemplates;
                        if (c.CreativeId == 0 || c.CreativeId > templates.Count)
                            throw new InvalidOperationException("unknown creative item");
                        var template = templates[(int)c.CreativeId - 1];
                        if (Player is not null)
                        {
                            var tx = new InventoryTransactionEvent(Player, InventoryTransactionType.CreativeCraft, template, template.Count, (0, 0));
                            if (!Player.Server.Events.OnInventoryTransaction(tx))
                                throw new InvalidOperationException("creative craft cancelled");
                        }
                        CreateResult(template.Split(template.MaxCount));
                        break;
                    }
                    case AnvilCraft a when AnvilUsed is not null:
                    {
                        var input = Ui[AnvilInputSlot] ?? throw new InvalidOperationException("nothing in the anvil");
                        var material = Ui[AnvilMaterialSlot];
                        var rename = a.FilterIndex >= 0 && a.FilterIndex < _filters.Count ? _filters[a.FilterIndex] : null;
                        var (repaired, cost, used) = Anvil.Combine(input, material, rename) ?? throw new InvalidOperationException("nothing to do on the anvil");
                        if (Player is not null && Player.Server.Events.HasAnvilUse)
                        {
                            var anvilEv = new AnvilUseEvent(Player, repaired, rename ?? "", cost);
                            if (!Player.Server.Events.OnAnvilUse(anvilEv))
                                throw new InvalidOperationException("anvil use cancelled");
                            cost = anvilEv.CostLevels;
                        }
                        if (mode != GameMode.Creative && (cost >= 40 || Player is null || !Player.SpendLevels(cost, cost)))
                            throw new InvalidOperationException("too expensive or not enough experience levels");
                        Remember((Ui, AnvilInputSlot));
                        Set(new SlotRef(ContainerAnvilInput, AnvilInputSlot, 0), (Ui, AnvilInputSlot), null);
                        if (material is not null)
                        {
                            Remember((Ui, AnvilMaterialSlot));
                            material.SetCount(material.Count - used);
                            Set(new SlotRef(ContainerAnvilMaterial, AnvilMaterialSlot, 0), (Ui, AnvilMaterialSlot), material.Count > 0 ? material : null);
                        }
                        CreateResult(repaired);
                        AnvilUsed();
                        break;
                    }
                    case Craft c when Enchanter is not null && c.RecipeId <= 2:
                    {
                        // Ported from Dragonfly session/handler_enchanting.go: the "recipe" is the chosen option 0..2.
                        var input = Ui[EnchantInputSlot] ?? throw new InvalidOperationException("nothing to enchant");
                        var (costs, options) = Enchanter.Options(input) ?? throw new InvalidOperationException("item cannot be enchanted");
                        var levels = (int)c.RecipeId + 1;
                        if (Player is not null && Player.Server.Events.HasPlayerEnchantItem)
                        {
                            var enchantEv = new PlayerEnchantItemEvent(Player, input, costs[c.RecipeId], levels);
                            if (!Player.Server.Events.OnPlayerEnchantItem(enchantEv))
                                throw new InvalidOperationException("enchant cancelled");
                        }
                        if (mode != GameMode.Creative)
                        {
                            if (Ui[EnchantLapisSlot] is not { } lapis || lapis.Type.Name != "minecraft:lapis_lazuli" || lapis.Count < levels)
                                throw new InvalidOperationException("not enough lapis lazuli");
                            if (!Enchanter.SpendLevels(costs[c.RecipeId], levels))
                                throw new InvalidOperationException("not enough experience levels");
                            Take(ContainerEnchantingMaterial, Ui, EnchantLapisSlot, levels);
                        }
                        Enchanter.Reseed();
                        Remember((Ui, EnchantInputSlot));
                        Set(new SlotRef(ContainerEnchantingInput, EnchantInputSlot, 0), (Ui, EnchantInputSlot), null);
                        CreateResult(Enchanting.With(input, options[c.RecipeId]));
                        break;
                    }
                    case Craft c:
                    {
                        // Special recipes (fireworks, map copies): the result comes from what is in the grid.
                        if (Recipes.ByNetworkId(c.RecipeId) is null && Recipes.IsMulti(c.RecipeId))
                        {
                            var (gridOffset, gridSize) = LargeGrid ? (LargeGridOffset, LargeGridSize) : (SmallGridOffset, SmallGridSize);
                            for (var time = 0; time < Math.Max(1, (int)c.Times); time++)
                            {
                                var cells = Enumerable.Range(gridOffset, gridSize).Where(s => Ui[s] is not null).ToList();
                                if (SpecialRecipes.Craft([.. cells.Select(s => Ui[s]!)]) is not { } special)
                                {
                                    if (time == 0)
                                        throw new InvalidOperationException($"nothing to craft with recipe {c.RecipeId}");
                                    break;
                                }
                                if (Player is not null && !Player.Server.Events.OnInventoryTransaction(
                                        new InventoryTransactionEvent(Player, InventoryTransactionType.Craft, special, special.Count, (0, 0))))
                                    throw new InvalidOperationException("craft cancelled");
                                foreach (var cell in cells)
                                    Take(ContainerCraftingInput, Ui, cell, 1);
                                pending.Add(special);
                            }
                            if (pending.Count == 1)
                            {
                                CreateResult(pending[0]);
                                pending.Clear();
                            }
                            break;
                        }
                        // Crafting table and inventory crafting
                        // The recipe must belong to the open table (a custom table's own recipes only there).
                        if (Recipes.ByNetworkId(c.RecipeId) is not { } recipe || !(LargeGrid ? CraftingTags : ["crafting_table"]).Contains(recipe.Block) || c.Times < 1)
                            throw new InvalidOperationException($"unknown crafting recipe {c.RecipeId}");
                        if (Player is not null)
                        {
                            var tx = new InventoryTransactionEvent(Player, InventoryTransactionType.Craft, recipe.Outputs.FirstOrDefault(), c.Times, (0, 0));
                            if (!Player.Server.Events.OnInventoryTransaction(tx))
                                throw new InvalidOperationException("craft cancelled");

                            if (Player.Server.Events.HasPlayerCraftItem)
                            {
                                var craftEv = new PlayerCraftItemEvent(Player, recipe.Outputs.FirstOrDefault(), recipe.Inputs);
                                if (!Player.Server.Events.OnPlayerCraftItem(craftEv))
                                    throw new InvalidOperationException("craft cancelled");
                            }
                        }
                        var (offset, size) = LargeGrid ? (LargeGridOffset, LargeGridSize) : (SmallGridOffset, SmallGridSize);
                        if (c.Auto)
                            ConsumeAuto(recipe, c.Times, offset, size);
                        else
                            ConsumeGrid(recipe, c.Times, offset, size);
                        pending = [.. Enumerable.Repeat(recipe.Outputs, c.Times).SelectMany(o => o).Select(o => o.Split(o.Count))];
                        if (pending.Count == 1)
                        {
                            CreateResult(pending[0]);
                            pending.Clear();
                        }
                        break;
                    }
                    case Create cr:
                    {
                        if (cr.ResultSlot >= pending.Count)
                            throw new InvalidOperationException("invalid craft result slot");
                        var created = pending[cr.ResultSlot];
                        pending[cr.ResultSlot] = null;
                        CreateResult(created);
                        break;
                    }
                    case Ignored:
                        break;
                    case Unsupported u:
                        throw new InvalidOperationException($"{u.Name} is not supported yet");
                }
            }
        }
        catch (InvalidOperationException e)
        {
            Rejected?.Invoke($"inventory request {requestId} ({string.Join(", ", actions.Select(a => a.GetType().Name))}) rejected: {e.Message}");
            // Revert everything this request changed; the caller resyncs the client.
            foreach (var ((inventory, slot), (stack, count)) in before)
            {
                stack?.SetCount(count);
                inventory[slot] = stack;
            }
            _results.Remove(requestId);
            ok = false;
            return InventoryPackets.ItemStackResponse(requestId, false, []);
        }

        foreach (var stack in dropped)
            drop(stack);
        var result = changes.ToDictionary(c => (c.Value.Inventory, c.Value.Slot), c => c.Value.Inventory[c.Value.Slot]?.NetworkId ?? 0);
        _results[requestId] = result;
        _resultOrder.Enqueue(requestId);
        while (_resultOrder.Count > RememberedRequests)
            _results.Remove(_resultOrder.Dequeue());
        ok = true;
        Accepted?.Invoke($"inventory request {requestId} ({string.Join(", ", actions.Select(a => a.ToString()))}) accepted");
        return InventoryPackets.ItemStackResponse(requestId, true,
            changes.Select(c => new InventoryPackets.SlotChange(c.Key.Item1, c.Key.Item2, c.Value.Inventory[c.Value.Slot])).ToList());
    }

    /// <summary>Adds picked-up items; returns InventorySlot packets for the changed slots and what did not fit.</summary>
    public (List<byte[]> Packets, int Remaining) Add(ItemStack stack)
    {
        var (changed, remaining) = Main.Add(stack);
        return (changed.Select(slot => InventoryPackets.InventorySlot(InventoryPackets.WindowInventory, slot, Main[slot])).ToList(), remaining);
    }

    /// <summary>
    /// Inventory closed: moves the cursor and crafting grid back into the inventory (vanilla behaviour).
    /// Returns what did not fit, to drop in the world; the caller resyncs with <see cref="ContentPackets"/>.
    /// </summary>
    public List<ItemStack> ReturnCraftingItems()
    {
        var leftover = new List<ItemStack>();
        CraftingTableOpen = false; // the tags stay: those of the last table opened (each table sets them)
        Enchanter = null;
        AnvilUsed = null;
        Ui[CreatedOutputSlot] = null;
        foreach (var slot in Enumerable.Range(SmallGridOffset, LargeGridOffset + LargeGridSize - SmallGridOffset).Prepend(0)
                     .Append(EnchantInputSlot).Append(EnchantLapisSlot).Append(AnvilInputSlot).Append(AnvilMaterialSlot))
        {
            if (Ui[slot] is not { } stack)
                continue;
            Ui[slot] = null;
            var (_, remaining) = Main.Add(stack);
            if (remaining > 0)
                leftover.Add(stack.Split(remaining));
        }
        return leftover;
    }

    /// <summary>Removes one item from the held stack (block placed in survival); returns the slot update.</summary>
    public byte[] ConsumeHeld()
    {
        var held = Held;
        if (held is not null)
        {
            held.SetCount(held.Count - 1);
            if (held.Count == 0)
                Main[SelectedSlot] = null;
        }
        return InventoryPackets.InventorySlot(InventoryPackets.WindowInventory, SelectedSlot, Main[SelectedSlot]);
    }
}
