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
using Bedrokia.Server.Game;

namespace Bedrokia.Server.Network;

/// <summary>Chests, trapped chests, barrels, ender chests and furnaces (window 3), ported from Dragonfly (MIT) session/world.go.</summary>
public sealed partial class BedrockConnection
{
    public const byte WindowContainer = 3;

    /// <summary>Position of the container block this player has open, or null.</summary>
    public (int X, int Y, int Z)? OpenContainerAt { get; private set; }

    private static readonly HashSet<string> ContainerBlocks =
    [
        "minecraft:chest", "minecraft:trapped_chest", "minecraft:barrel", "minecraft:ender_chest",
        "minecraft:furnace", "minecraft:lit_furnace", "minecraft:blast_furnace", "minecraft:lit_blast_furnace",
        "minecraft:smoker", "minecraft:lit_smoker",
    ];

    /// <summary>Opens the container at (x, y, z) if the block is one; returns false otherwise.</summary>
    private bool TryOpenContainer(BlockType block, int x, int y, int z)
    {
        if (!ContainerBlocks.Contains(block.Name))
            return false;
        var world = World;
        var entity = world.GetBlockEntity(x, y, z);
        if (entity is null && BlockEntity.For(block) is { } created) // e.g. imported worlds or older saves
        {
            world.SetBlockEntity(x, y, z, created);
            entity = created;
        }
        if (entity is null)
            return false;
        var contents = entity.Id == "EnderChest" ? Inventory.EnderChest : entity.Items!;
        var type = entity.Id switch { "Furnace" => (byte)2, "BlastFurnace" => (byte)27, "Smoker" => (byte)28, _ => (byte)0 };
        Inventory.OpenContainer = (contents, entity.Id);
        OpenContainerAt = (x, y, z);
        Send(InventoryPackets.ContainerOpen(WindowContainer, type, x, y, z),
            InventoryPackets.InventoryContent(WindowContainer, contents));
        if (entity.Id is "Furnace" or "BlastFurnace" or "Smoker")
            _server.WatchFurnace((x, y, z), World);
        return true;
    }

    /// <summary>After an inventory request: other players looking into the same container see the change.</summary>
    private void SyncOpenContainer()
    {
        if (OpenContainerAt is not { } at || Inventory.OpenContainer is not { } open || open.Kind == "EnderChest")
            return;
        var packet = InventoryPackets.InventoryContent(WindowContainer, open.Items);
        foreach (var viewer in _server.Players.Where(p => p != this && p.OpenContainerAt == at && p.World == World))
            viewer.Send(packet);
        if (open.Kind is "Furnace" or "BlastFurnace" or "Smoker")
            _server.WatchFurnace(at, World);
    }

    /// <summary>The container was broken or the window was closed.</summary>
    internal void CloseContainer(bool notifyClient)
    {
        if (notifyClient && OpenContainerAt is not null)
            Send(InventoryPackets.ContainerClose(WindowContainer, 0));
        OpenContainerAt = null;
        Inventory.OpenContainer = null;
    }

    /// <summary>ContainerSetData (id 51): furnace progress bars for the players looking into it.</summary>
    internal static byte[] ContainerData(int key, int value) =>
        new BedrockWriter().VarUInt32(51).Byte(WindowContainer).VarInt32(key).VarInt32(value).ToArray();
}
