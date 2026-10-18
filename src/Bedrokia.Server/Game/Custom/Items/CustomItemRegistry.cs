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

namespace Bedrokia.Server.Game.Custom.Items;

public static class CustomItemRegistry
{
    private static int _nextRuntimeId = 10000;
    private static readonly Dictionary<string, CustomItem> ByIdentifier = [];
    private static readonly Dictionary<short, CustomItem> ById = [];

    public static short AllocateNextRuntimeId() => (short)Interlocked.Increment(ref _nextRuntimeId);

    public static int Count => ByIdentifier.Count;

    public static IReadOnlyCollection<CustomItem> All => ByIdentifier.Values;

    public static CustomItem Register(CustomItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (ByIdentifier.ContainsKey(item.Identifier))
            throw new InvalidOperationException($"Custom item '{item.Identifier}' is already registered.");

        ByIdentifier[item.Identifier] = item;
        ById[item.NumericId] = item;

        Bedrokia.Server.Game.Items.RegisterCustomItem(
            item.ToItemType(),
            isCreative: !item.Creative.IsHidden,
            creativeGroupIndex: (int)item.Creative.Category);

        return item;
    }

    public static CustomItem? Get(string identifier)
    {
        var key = identifier.Contains(':') ? identifier : "custom:" + identifier;
        return ByIdentifier.GetValueOrDefault(key);
    }

    public static CustomItem? ByRuntimeId(short runtimeId) => ById.GetValueOrDefault(runtimeId);
    public static CustomItem? GetByRuntimeId(int runtimeId) => ById.GetValueOrDefault((short)runtimeId);
}
