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
        var compDict = new OrderedDictionary<string, object>();
        foreach (var (_, comp) in Components)
            compDict[comp.Name] = comp.BuildNbt();

        var propList = new List<object>();
        foreach (var prop in Properties)
            propList.Add(prop.BuildNbt());

        var permList = new List<object>();
        foreach (var perm in Permutations)
            permList.Add(perm.BuildNbt());

        var tagList = Tags.Select(t => (object)t).ToList();

        var menuCategory = new OrderedDictionary<string, object>
        {
            ["category"] = Creative.Category.ToString().ToLowerInvariant(),
            ["group"] = Creative.Group.ToLowerInvariant(),
            ["is_hidden_in_commands"] = (byte)(Creative.IsHidden ? 1 : 0)
        };

        var vanillaBlockData = new OrderedDictionary<string, object>
        {
            ["block_id"] = runtimeId,
            ["material"] = Material
        };

        return new OrderedDictionary<string, object>
        {
            ["name"] = Identifier,
            ["components"] = compDict,
            ["properties"] = new NbtList(Nbt.Compound, propList),
            ["permutations"] = new NbtList(Nbt.Compound, permList),
            ["menu_category"] = menuCategory,
            ["blockTags"] = new NbtList(Nbt.String, tagList),
            ["vanilla_block_data"] = vanillaBlockData,
            ["molangVersion"] = 13
        };
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
