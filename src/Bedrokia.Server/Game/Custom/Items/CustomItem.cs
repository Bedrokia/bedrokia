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
using Bedrokia.Server.Plugins;

namespace Bedrokia.Server.Game.Custom.Items;

public sealed class CustomItem
{
    public CustomItem(
        string identifier,
        short numericId,
        int version = 2,
        CreativeInfo? creative = null,
        IReadOnlyDictionary<string, IItemProperty>? properties = null,
        IReadOnlyDictionary<string, IItemComponent>? components = null,
        Action<ItemUseEvent>? onUse = null)
    {
        Identifier = identifier.Contains(':') ? identifier : "custom:" + identifier;
        NumericId = numericId;
        Version = version;
        Creative = creative ?? new CreativeInfo();
        Properties = properties ?? new Dictionary<string, IItemProperty>();
        Components = components ?? new Dictionary<string, IItemComponent>();
        OnUse = onUse;
    }

    public string Identifier { get; }
    public short NumericId { get; }
    public int RuntimeId => NumericId;
    public int Version { get; }
    public CreativeInfo Creative { get; }
    public IReadOnlyDictionary<string, IItemProperty> Properties { get; }
    public IReadOnlyDictionary<string, IItemComponent> Components { get; }
    public Action<ItemUseEvent>? OnUse { get; }

    public string DisplayName => GetComponent<DisplayNameItemComponent>()?.Value ?? Identifier;

    public bool HasProperty(string name) => Properties.ContainsKey(name);
    public bool HasComponent(string name) => Components.ContainsKey(name);

    public T? GetProperty<T>() where T : class, IItemProperty => Properties.Values.OfType<T>().FirstOrDefault();
    public T? GetComponent<T>() where T : class, IItemComponent => Components.Values.OfType<T>().FirstOrDefault();

    public OrderedDictionary<string, object> ToNbt()
    {
        var componentsDict = new OrderedDictionary<string, object>();
        var propertiesDict = new OrderedDictionary<string, object>();

        foreach (var (_, prop) in Properties)
            propertiesDict[prop.Name] = prop.BuildValue();

        foreach (var (_, comp) in Components)
            componentsDict[comp.Name] = comp.BuildNbt();

        // Dragonfly (MIT) server/internal/iteminternal/builder.go: every custom item tells its creative tab.
        propertiesDict["creative_category"] = (int)Creative.Category;
        propertiesDict["creative_group"] = Creative.Group;
        componentsDict["item_properties"] = propertiesDict;

        return new OrderedDictionary<string, object>
        {
            ["components"] = componentsDict
        };
    }

    /// <summary>The item of a custom block (it places the block of the same name).</summary>
    public bool IsBlockItem => GetComponent<BlockPlacerItemComponent>()?.Block == Identifier;

    // Dragonfly server.go makeItemComponents: custom items are data-driven (version 1, components); the item of a
    // custom block is a plain block item (version 2, not component based). Two definitions for one name crashed the client.
    public ItemType ToItemType() => IsBlockItem
        ? new ItemType(Identifier, NumericId, ComponentBased: false, Version: 2, Data: [])
        : new ItemType(Identifier, NumericId, ComponentBased: true, Version: 1, Data: ToNbt());
}
