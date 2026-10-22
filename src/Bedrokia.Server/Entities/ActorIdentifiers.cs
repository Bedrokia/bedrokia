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

namespace Bedrokia.Server.Entities;

/// <summary>
/// AvailableActorIdentifiers: the entity types the client knows. The vanilla list (pmmp/BedrockData
/// entity_identifiers.nbt, CC0) plus Bedrokia's invisible inventory holder, which carries the menus of any size
/// drawn by a resource pack (ModSmith's InventoryHolder, MIT), and the entities plugins register (<see cref="Register"/>).
/// </summary>
public static class ActorIdentifiers
{
    private const uint PacketIdValue = 119;

    /// <summary>The invisible entity a big menu is opened on (its container type and size in its metadata).</summary>
    public const string InventoryHolder = "bedrokia:inventory_holder";

    private static readonly List<string> Custom = [InventoryHolder];
    private static byte[]? _packet;

    public static byte[] Packet => _packet ??= Build();

    /// <summary>
    /// Makes the client know a custom entity (e.g. "antafac:grenade"); its look (geometry, texture, animations) comes
    /// from the resource pack (entity/&lt;name&gt;.entity.json). Register before players join (when the plugin loads).
    /// </summary>
    public static void Register(string identifier)
    {
        if (Custom.Contains(identifier))
            return;
        Custom.Add(identifier);
        _packet = null;
    }

    private static byte[] Build()
    {
        using var stream = typeof(ActorIdentifiers).Assembly.GetManifestResourceStream("entity_identifiers.nbt")
                           ?? throw new InvalidOperationException("entity_identifiers.nbt resource is missing.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var root = Nbt.ReadAll(buffer.ToArray(), network: true)[0];
        var list = (NbtList)root["idlist"];
        foreach (var id in Custom)
            list.Values.Add(new OrderedDictionary<string, object>
            {
                ["bid"] = "", ["hasspawnegg"] = (byte)0, ["id"] = id, ["summonable"] = (byte)0,
            });
        return new BedrockWriter().VarUInt32(PacketIdValue).Bytes(Nbt.Write(root, network: true)).ToArray();
    }
}
