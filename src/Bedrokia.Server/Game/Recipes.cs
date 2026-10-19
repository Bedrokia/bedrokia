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
using System.Text.Json;
using Bedrokia.Protocol;

namespace Bedrokia.Server.Game;

/// <summary>One recipe input: an item (meta 32767 = any variant), an item tag, or nothing (empty grid cell).</summary>
public sealed record Ingredient(string Name, int Meta, string Tag, int Count)
{
    public const int AnyMeta = short.MaxValue;

    // The data marks the holes of shaped recipes as "minecraft:air" x0: they must go out as empty descriptors,
    // otherwise the client never matches recipes with holes (armour, buckets, bowls...).
    public bool Empty => Tag.Length == 0 && (Name.Length == 0 || Name == "minecraft:air" || Count == 0);

    public bool Matches(ItemStack stack) => Tag.Length > 0
        ? Recipes.TagContains(Tag, stack.Type.Name)
        : stack.Type.Name == Name && (Meta == AnyMeta || stack.Meta == Meta);
}

/// <summary>A crafting recipe; <see cref="Block"/> is where it is crafted ("crafting_table", "furnace"...).</summary>
public sealed record Recipe(uint NetworkId, IReadOnlyList<Ingredient> Inputs, IReadOnlyList<ItemStack> Outputs, string Block);

/// <summary>
/// Vanilla recipes from Dragonfly's (MIT) crafting_data.nbt and item_tags.json, and the CraftingData packet
/// (gophertunnel packet/crafting_data.go, protocol 2193). Recipes using unknown items are skipped, like Dragonfly.
/// </summary>
public static class Recipes
{
    public const uint CraftingDataId = 52;

    private static readonly FrozenDictionary<uint, Recipe> ById;
    private static readonly FrozenDictionary<string, Recipe[]> ByResultItem;
    private static readonly FrozenDictionary<string, FrozenSet<string>> Tags = LoadTags();

    static Recipes()
    {
        var byIdTemp = new Dictionary<uint, Recipe>();
        var byResultTemp = new Dictionary<string, List<Recipe>>();
        var root = Nbt.ReadAll(Resource("crafting_data.nbt"), network: true)[0];
        var shaped = new BedrockWriter();
        var shapeless = new BedrockWriter();
        var userData = new BedrockWriter();
        var multi = new BedrockWriter();
        uint shapedCount = 0, shapelessCount = 0, userDataCount = 0, multiCount = 0, next = 1;

        void IndexRecipe(Recipe r)
        {
            byIdTemp[r.NetworkId] = r;
            foreach (var outStack in r.Outputs)
            {
                if (!byResultTemp.TryGetValue(outStack.Type.Name, out var list))
                    byResultTemp[outStack.Type.Name] = list = [];
                list.Add(r);
            }
        }

        foreach (OrderedDictionary<string, object> entry in List(root, "shaped"))
        {
            if (Parse(entry, next) is not { } recipe)
                continue;
            IndexRecipe(recipe);
            next++;
            shapedCount++;
            shaped.String(RecipeId(next - 1)).VarInt32(Int(entry, "width")).VarInt32(Int(entry, "height"));
            WriteRecipeBody(shaped, recipe, Int(entry, "priority"), shapedSymmetry: true);
        }
        foreach (var (key, writer) in new[] { ("shapeless", shapeless), ("shulker_box", userData) })
        foreach (OrderedDictionary<string, object> entry in List(root, key))
        {
            if (Parse(entry, next) is not { } recipe)
                continue;
            IndexRecipe(recipe);
            next++;
            if (key == "shapeless") shapelessCount++; else userDataCount++;
            writer.String(RecipeId(next - 1));
            WriteRecipeBody(writer, recipe, Int(entry, "priority"), shapedSymmetry: false);
        }
        foreach (var id in List(root, "multi"))
        {
            if (id is not string text || !Guid.TryParse(text, out var uuid))
                continue;
            multi.Uuid(uuid).VarUInt32(next++);
            multiCount++;
        }

        ById = byIdTemp.ToFrozenDictionary();
        ByResultItem = byResultTemp.ToFrozenDictionary(k => k.Key, k => k.Value.ToArray());

        var packet = new BedrockWriter().VarUInt32(CraftingDataId)
            .VarUInt32(shapedCount).Bytes(shaped.ToArray())
            .VarUInt32(shapelessCount).Bytes(shapeless.ToArray())
            .VarUInt32(multiCount).Bytes(multi.ToArray())
            .VarUInt32(userDataCount).Bytes(userData.ToArray());
        for (var i = 0; i < 7; i++) // chemistry x2, smithing transform/trim, potions, potion containers, material reducers
            packet.VarUInt32(0);
        CraftingDataPacket = packet.Bool(true).ToArray(); // clear recipes
        Count = ById.Count;
    }

    public static ReadOnlySpan<Recipe> ByResult(string itemName) => ByResultItem.GetValueOrDefault(itemName) ?? [];

    /// <summary>CraftingData packet with every recipe; send once at spawn.</summary>
    public static byte[] CraftingDataPacket { get; }

    private static string RecipeId(uint networkId) => $"bedrokia:recipe_{networkId}";

    public const uint TrimDataId = 302;

    /// <summary>
    /// Armour trim patterns and materials (Dragonfly session/player.go sendArmourTrimData, MIT). Without it the
    /// client treats every armour piece as unknown and refuses to craft armour (red result slot).
    /// </summary>
    public static byte[] TrimDataPacket { get; } = BuildTrimData();

    private static byte[] BuildTrimData()
    {
        string[] patterns = ["sentry", "vex", "wild", "coast", "dune", "wayfinder", "raiser", "shaper", "host", "ward",
            "silence", "tide", "snout", "rib", "eye", "spire", "flow", "bolt"];
        (string Material, string Colour, string Item)[] materials =
        [
            ("amethyst", "§u", "amethyst_shard"), ("copper", "§n", "copper_ingot"), ("diamond", "§s", "diamond"),
            ("emerald", "§q", "emerald"), ("gold", "§p", "gold_ingot"), ("iron", "§i", "iron_ingot"),
            ("lapis", "§t", "lapis_lazuli"), ("netherite", "§j", "netherite_ingot"), ("quartz", "§h", "quartz"),
            ("resin", "§v", "resin_brick"), ("redstone", "§m", "redstone"),
        ];
        var w = new BedrockWriter().VarUInt32(TrimDataId).VarUInt32((uint)patterns.Length);
        foreach (var pattern in patterns)
            w.String($"minecraft:{pattern}_armor_trim_smithing_template").String(pattern);
        w.VarUInt32((uint)materials.Length);
        foreach (var (material, colour, item) in materials)
            w.String(material).String(colour).String("minecraft:" + item);
        return w.ToArray();
    }

    public static int Count { get; }

    public static Recipe? ByNetworkId(uint id) => ById.GetValueOrDefault(id);

    public static IEnumerable<Recipe> All => ById.Values;

    internal static bool TagContains(string tag, string item) => Tags.TryGetValue(tag, out var items) && items.Contains(item);

    // Inputs, outputs, uuid, block, priority, [assume symmetry], unlock requirement, network id.
    private static void WriteRecipeBody(BedrockWriter w, Recipe recipe, int priority, bool shapedSymmetry)
    {
        w.VarUInt32((uint)recipe.Inputs.Count);
        foreach (var input in recipe.Inputs)
            WriteDescriptor(w, input);
        w.VarUInt32((uint)recipe.Outputs.Count);
        foreach (var output in recipe.Outputs)
        {
            w.VarInt32(output.Type.RuntimeId).UInt16((ushort)output.Count).VarUInt32((ushort)output.Meta)
                .VarInt32(unchecked((int)output.BlockRuntimeId));
            Items.WriteUserData(w, output.Nbt, output.Type.RuntimeId == Items.ShieldRuntimeId);
        }
        w.Uuid(Guid.Empty).String(recipe.Block).VarInt32(priority);
        if (shapedSymmetry)
            w.Bool(true);
        // Unlock requirement present, context "always unlocked" (1), so no ingredient list.
        w.Bool(true).VarInt32(1).Bool(false).VarUInt32(recipe.NetworkId);
    }

    // gophertunnel Writer.ItemDescriptorCount: variant (capped at 1), type name, descriptor, [aux], count.
    private static void WriteDescriptor(BedrockWriter w, Ingredient input)
    {
        if (input.Empty)
            w.VarUInt32(0).VarInt32(short.MaxValue);
        else if (input.Tag.Length > 0)
            w.VarUInt32(1).String("item_tag").String(input.Tag).VarInt32(short.MaxValue);
        else
            w.VarUInt32(1).String("name").String(input.Name).VarInt32(input.Meta);
        w.VarInt32(input.Count);
    }

    private static Recipe? Parse(OrderedDictionary<string, object> entry, uint networkId)
    {
        var inputs = new List<Ingredient>();
        foreach (var raw in List(entry, "input"))
        {
            var input = (OrderedDictionary<string, object>)raw;
            var name = Str(input, "name");
            var tag = Str(input, "tag");
            if (name.Length > 0 && Items.ByName(name) is null)
                return null;
            inputs.Add(new Ingredient(name, Int(input, "meta"), tag, Int(input, "count")));
        }
        var outputs = new List<ItemStack>();
        foreach (var raw in List(entry, "output"))
        {
            var output = (OrderedDictionary<string, object>)raw;
            if (Items.ByName(Str(output, "name")) is not { } type)
                return null;
            var block = Blocks.ByName(type.Name);
            if (output.TryGetValue("block", out var state) && state is OrderedDictionary<string, object> { Count: > 0 } s
                && s.TryGetValue("states", out var states) && states is OrderedDictionary<string, object> values)
                block = Blocks.ByKey(new BlockType(0, Str(s, "name"), values).Key) ?? block;
            var nbt = output.TryGetValue("data", out var data) ? data as OrderedDictionary<string, object> : null;
            outputs.Add(new ItemStack(type, Math.Max(1, Int(output, "count")), (short)Int(output, "meta"), block?.NetworkId ?? 0,
                nbt is { Count: > 0 } ? nbt : null));
        }
        return outputs.Count == 0 ? null : new Recipe(networkId, inputs, outputs, Str(entry, "block"));
    }

    private static IEnumerable<object> List(OrderedDictionary<string, object> c, string key) =>
        c.TryGetValue(key, out var v) && v is NbtList list ? list.Values : [];

    private static string Str(OrderedDictionary<string, object> c, string key) =>
        c.TryGetValue(key, out var v) && v is string s ? s : "";

    private static int Int(OrderedDictionary<string, object> c, string key) =>
        c.TryGetValue(key, out var v) && v is IConvertible ? Convert.ToInt32(v) : 0;

    private static FrozenDictionary<string, FrozenSet<string>> LoadTags()
    {
        var tags = JsonSerializer.Deserialize<Dictionary<string, string[]>>(Resource("item_tags.json")) ?? [];
        return tags.ToFrozenDictionary(t => t.Key, t => t.Value.ToFrozenSet());
    }

    private static byte[] Resource(string name)
    {
        using var stream = typeof(Recipes).Assembly.GetManifestResourceStream(name)
                           ?? throw new InvalidOperationException($"{name} resource is missing.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
