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
/// Supported data types for Bedrock custom entity properties.
/// Source: Minecraft Bedrock Entity Properties Documentation &amp; Dragonfly (MIT).
/// </summary>
public enum EntityPropertyType
{
    Int,
    Float,
    Bool,
    Enum
}

/// <summary>
/// Represents a synchronized dynamic property on a custom entity (e.g. integer counter, float cooldown, boolean state, or enum value).
/// Supports client-side rendering synchronization via <see cref="Bedrokia.Protocol.PacketId.SyncActorProperty"/>.
/// Source: gophertunnel (MIT) minecraft/protocol/packet/sync_actor_property.go.
/// </summary>
public sealed class EntityProperty
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityProperty"/> class.
    /// </summary>
    /// <param name="name">The property name (namespaced as <c>custom:name</c> if not qualified).</param>
    /// <param name="type">The property data type.</param>
    /// <param name="defaultValue">The default initial value.</param>
    /// <param name="clientSync">Whether this property is synchronized to Bedrock clients.</param>
    /// <param name="minInt">Minimum integer range bound.</param>
    /// <param name="maxInt">Maximum integer range bound.</param>
    /// <param name="minFloat">Minimum floating-point range bound.</param>
    /// <param name="maxFloat">Maximum floating-point range bound.</param>
    /// <param name="enumValues">List of allowed enum string values if type is <see cref="EntityPropertyType.Enum"/>.</param>
    public EntityProperty(
        string name,
        EntityPropertyType type,
        object defaultValue,
        bool clientSync = true,
        int minInt = 0,
        int maxInt = 100,
        float minFloat = 0f,
        float maxFloat = 1f,
        IReadOnlyList<string>? enumValues = null)
    {
        Name = name.Contains(':') ? name : "custom:" + name;
        Type = type;
        DefaultValue = defaultValue;
        ClientSync = clientSync;
        MinInt = minInt;
        MaxInt = maxInt;
        MinFloat = minFloat;
        MaxFloat = maxFloat;
        EnumValues = enumValues ?? [];
    }

    /// <summary>Gets the namespaced identifier of this property.</summary>
    public string Name { get; }
    /// <summary>Gets the data type of this property.</summary>
    public EntityPropertyType Type { get; }
    /// <summary>Gets the default value assigned to this property upon spawning.</summary>
    public object DefaultValue { get; }
    /// <summary>Gets whether this property's changes are sent to client renderers.</summary>
    public bool ClientSync { get; }
    /// <summary>Gets the minimum allowed integer value.</summary>
    public int MinInt { get; }
    /// <summary>Gets the maximum allowed integer value.</summary>
    public int MaxInt { get; }
    /// <summary>Gets the minimum allowed float value.</summary>
    public float MinFloat { get; }
    /// <summary>Gets the maximum allowed float value.</summary>
    public float MaxFloat { get; }
    /// <summary>Gets the list of allowed enum states.</summary>
    public IReadOnlyList<string> EnumValues { get; }

    /// <summary>Creates an integer entity property with range clamping.</summary>
    public static EntityProperty CreateInt(string name, int defaultValue = 0, int min = 0, int max = 100, bool clientSync = true) =>
        new(name, EntityPropertyType.Int, defaultValue, clientSync, minInt: min, maxInt: max);

    /// <summary>Creates a floating-point entity property with range clamping.</summary>
    public static EntityProperty CreateFloat(string name, float defaultValue = 0f, float min = 0f, float max = 1f, bool clientSync = true) =>
        new(name, EntityPropertyType.Float, defaultValue, clientSync, minFloat: min, maxFloat: max);

    /// <summary>Creates a boolean entity property.</summary>
    public static EntityProperty CreateBool(string name, bool defaultValue = false, bool clientSync = true) =>
        new(name, EntityPropertyType.Bool, defaultValue, clientSync);

    /// <summary>Creates an enum entity property with defined string variants.</summary>
    public static EntityProperty CreateEnum(string name, IReadOnlyList<string> values, string defaultValue, bool clientSync = true) =>
        new(name, EntityPropertyType.Enum, defaultValue, clientSync, enumValues: values);

    /// <summary>
    /// Serializes this property definition into Bedrock NBT format for client sync registration.
    /// </summary>
    public OrderedDictionary<string, object> BuildNbt()
    {
        var dict = new OrderedDictionary<string, object>
        {
            ["client_sync"] = (byte)(ClientSync ? 1 : 0)
        };

        switch (Type)
        {
            case EntityPropertyType.Int:
                dict["type"] = "int";
                dict["default"] = Convert.ToInt32(DefaultValue);
                dict["range"] = new NbtList(Nbt.Int, [MinInt, MaxInt]);
                break;
            case EntityPropertyType.Float:
                dict["type"] = "float";
                dict["default"] = Convert.ToSingle(DefaultValue);
                dict["range"] = new NbtList(Nbt.Float, [MinFloat, MaxFloat]);
                break;
            case EntityPropertyType.Bool:
                dict["type"] = "bool";
                dict["default"] = Convert.ToBoolean(DefaultValue);
                break;
            case EntityPropertyType.Enum:
                dict["type"] = "enum";
                dict["default"] = DefaultValue.ToString() ?? "";
                dict["values"] = new NbtList(Nbt.String, [.. EnumValues.Cast<object>()]);
                break;
        }

        return dict;
    }
}
