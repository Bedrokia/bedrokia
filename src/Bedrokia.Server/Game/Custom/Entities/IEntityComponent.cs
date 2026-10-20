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

namespace Bedrokia.Server.Game.Custom.Entities;

/// <summary>
/// Represents a Bedrock entity component capable of serializing into an NBT compound.
/// Source: Dragonfly (MIT) / Minecraft Bedrock Entity Component Architecture.
/// </summary>
public interface IEntityComponent
{
    /// <summary>
    /// Gets the unique namespaced component identifier (e.g. <c>minecraft:health</c>).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Serializes this component's data into a Bedrock NBT compound structure.
    /// </summary>
    /// <returns>An ordered dictionary containing component parameters.</returns>
    OrderedDictionary<string, object> BuildNbt();
}
