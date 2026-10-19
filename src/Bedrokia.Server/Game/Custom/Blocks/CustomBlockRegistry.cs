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
using Bedrokia.Server.Game.Custom.Items;

namespace Bedrokia.Server.Game.Custom.Blocks;

public static class CustomBlockRegistry
{
    private static int _nextRuntimeId = 10000;
    private static readonly Dictionary<string, CustomBlock> ByIdentifier = [];
    private static readonly List<Packets.CustomBlockDefinition> Definitions = [];

    public static int Count => ByIdentifier.Count;

    public static IReadOnlyCollection<CustomBlock> All => ByIdentifier.Values;

    public static IReadOnlyList<Packets.CustomBlockDefinition> GetDefinitions() => Definitions;

    public static void Register(CustomBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        if (ByIdentifier.ContainsKey(block.Identifier))
            throw new InvalidOperationException($"Custom block '{block.Identifier}' is already registered.");

        ByIdentifier[block.Identifier] = block;

        var runtimeId = Interlocked.Increment(ref _nextRuntimeId);
        var nbt = block.ToNbt(runtimeId);
        Definitions.Add(new Packets.CustomBlockDefinition(block.Identifier, nbt));

        var states = block.GetStateCombinations();
        foreach (var state in states)
            Bedrokia.Server.Game.Blocks.RegisterCustom(block.Identifier, state);

        var displayName = block.Components.TryGetValue(BlockComponentIds.DisplayName, out var comp)
                          && comp is DisplayNameBlockComponent nameComp
            ? nameComp.Value
            : block.Identifier;

        var texture = block.Identifier.Contains(':')
            ? block.Identifier[(block.Identifier.IndexOf(':') + 1)..]
            : block.Identifier;

        if (block.Components.TryGetValue(BlockComponentIds.MaterialInstances, out var matComp)
            && matComp is MaterialInstancesBlockComponent mats
            && mats.Materials.Count > 0)
        {
            texture = mats.Materials[0].Texture;
        }

        if (CustomItemRegistry.Get(block.Identifier) is null)
        {
            CustomItemBuilder.Create(block.Identifier)
                .WithDisplayName(displayName)
                .WithIcon(texture)
                .WithBlockPlacer(block.Identifier)
                .WithCreative(block.Creative.Category, block.Creative.Group, block.Creative.IsHidden)
                .Register();
        }
    }

    public static CustomBlock? Get(string identifier)
    {
        var key = identifier.Contains(':') ? identifier : "custom:" + identifier;
        return ByIdentifier.GetValueOrDefault(key);
    }
}
