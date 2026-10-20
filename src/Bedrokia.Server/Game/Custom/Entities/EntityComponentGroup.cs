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

namespace Bedrokia.Server.Game.Custom.Entities;

/// <summary>
/// Represents a named group of Bedrock entity components that can be dynamically added or removed at runtime
/// (e.g. enraged state, saddle equip, baby variant).
/// Source: Minecraft Bedrock Entity Definition Documentation (component_groups) &amp; Dragonfly (MIT).
/// </summary>
public sealed class EntityComponentGroup
{
    /// <summary>
    /// Initializes a new component group with a dictionary of components.
    /// </summary>
    /// <param name="name">The group name (e.g. <c>minecraft:is_baby</c> or <c>custom:enraged</c>).</param>
    /// <param name="components">Map of component names to components.</param>
    public EntityComponentGroup(string name, IReadOnlyDictionary<string, IEntityComponent>? components = null)
    {
        Name = name;
        Components = components ?? new Dictionary<string, IEntityComponent>();
    }

    /// <summary>
    /// Initializes a new component group with an array of components.
    /// </summary>
    /// <param name="name">The group name.</param>
    /// <param name="components">Array of entity components belonging to this group.</param>
    public EntityComponentGroup(string name, params IEntityComponent[] components)
    {
        Name = name;
        var dict = new Dictionary<string, IEntityComponent>();
        foreach (var c in components)
            dict[c.Name] = c;
        Components = dict;
    }

    /// <summary>Gets the unique name of this component group.</summary>
    public string Name { get; }

    /// <summary>Gets the dictionary of components contained in this group.</summary>
    public IReadOnlyDictionary<string, IEntityComponent> Components { get; }

    /// <summary>
    /// Serializes all components in this group into an NBT compound.
    /// </summary>
    public OrderedDictionary<string, object> BuildNbt()
    {
        var dict = new OrderedDictionary<string, object>();
        foreach (var (_, comp) in Components)
            dict[comp.Name] = comp.BuildNbt();
        return dict;
    }
}
