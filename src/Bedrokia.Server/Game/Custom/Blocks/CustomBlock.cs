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

namespace Bedrokia.Server.Game.Custom.Blocks;

public sealed class CustomBlock
{
    public CustomBlock(
        string identifier,
        string material = "dirt",
        CreativeInfo? creative = null,
        IReadOnlyDictionary<string, IBlockComponent>? components = null,
        IReadOnlyList<BlockProperty>? properties = null,
        IReadOnlyList<BlockPermutation>? permutations = null,
        IReadOnlyList<string>? tags = null)
    {
        Identifier = identifier.Contains(':') ? identifier : "custom:" + identifier;
        Material = material;
        Creative = creative ?? new CreativeInfo(CreativeCategory.Construction);
        Components = components ?? new Dictionary<string, IBlockComponent>();
        Properties = properties ?? [];
        Permutations = permutations ?? [];
        Tags = tags ?? [];
    }

    public string Identifier { get; }
    public string Material { get; }
    public CreativeInfo Creative { get; }
    public IReadOnlyDictionary<string, IBlockComponent> Components { get; }
    public IReadOnlyList<BlockProperty> Properties { get; }
    public IReadOnlyList<BlockPermutation> Permutations { get; }
    public IReadOnlyList<string> Tags { get; }

    public string DisplayName =>
        Components.TryGetValue(BlockComponentIds.DisplayName, out var comp) && comp is DisplayNameBlockComponent nameComp
            ? nameComp.Value
            : Identifier;

    public OrderedDictionary<string, object> ToNbt(int runtimeId)
    {
        // Same layout as Dragonfly (MIT) server/internal/blockinternal/builder.go Construct: the client crashed on
        // the extra keys and empty lists this used to send.
        var compDict = new OrderedDictionary<string, object>();
        foreach (var (_, comp) in Components)
            if (comp.Name != BlockComponentIds.Breathability) // removed from Bedrock
                compDict[comp.Name] = comp.BuildNbt();
        foreach (var tag in Tags)
            compDict[tag.Contains(':') ? tag : "tag:" + tag] = new OrderedDictionary<string, object>();

        var nbt = new OrderedDictionary<string, object>
        {
            ["components"] = compDict,
            ["molangVersion"] = 10,
            ["menu_category"] = new OrderedDictionary<string, object>
            {
                ["category"] = Creative.Category.ToString().ToLowerInvariant(),
                ["group"] = Creative.Group,
            },
            ["vanilla_block_data"] = new OrderedDictionary<string, object> { ["block_id"] = runtimeId },
        };
        if (Properties.Count > 0)
            nbt["properties"] = new NbtList(Nbt.Compound, [.. Properties.Select(p => (object)p.BuildNbt())]);
        if (Permutations.Count > 0)
            nbt["permutations"] = new NbtList(Nbt.Compound, [.. Permutations.Select(p => (object)p.BuildNbt())]);
        return nbt;
    }

    public List<OrderedDictionary<string, object>> GetStateCombinations()
    {
        var result = new List<OrderedDictionary<string, object>>();
        if (Properties.Count == 0)
        {
            result.Add([]);
            return result;
        }

        GenerateCombinations(Properties, 0, [], result);
        return result;
    }

    private static void GenerateCombinations(
        IReadOnlyList<BlockProperty> properties,
        int index,
        OrderedDictionary<string, object> current,
        List<OrderedDictionary<string, object>> results)
    {
        if (index == properties.Count)
        {
            results.Add(new OrderedDictionary<string, object>(current));
            return;
        }

        var prop = properties[index];
        foreach (var val in prop.Values)
        {
            current[prop.Name] = val;
            GenerateCombinations(properties, index + 1, current, results);
            current.Remove(prop.Name);
        }
    }
}
