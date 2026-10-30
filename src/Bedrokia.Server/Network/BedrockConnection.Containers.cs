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
    internal bool TryOpenContainer(BlockType block, int x, int y, int z)
    {
        var custom = BlockEntity.FurnaceSpeed(block.Name) is not null;
        if (!custom && !ContainerBlocks.Contains(block.Name))
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

        if (_server.Events.HasContainerOpen)
        {
            var openEv = new ContainerOpenEvent(this, WindowContainer, type, new BlockPos(x, y, z), entity.Id, contents);
            if (!_server.Events.OnContainerOpen(openEv))
            {
                CloseWindowFromServer(WindowContainer, 0);
                return true;
            }
        }

        if (custom)
        {
            OpenCustomFurnace(entity, x, y, z);
            return true;
        }
        Inventory.OpenContainer = (contents, entity.Id);
        OpenContainerAt = (x, y, z);
        Send(InventoryPackets.ContainerOpen(WindowContainer, type, x, y, z),
            InventoryPackets.InventoryContent(WindowContainer, contents));
        if (entity.Id is "Furnace" or "BlastFurnace" or "Smoker")
            _server.WatchFurnace((x, y, z), World);
        return true;
    }

    // Where the window of a custom furnace or crafting table is anchored for the client (a vanilla block only it sees).
    private (int X, int Y, int Z)? _fakeBlockAt;

    // The client opens a furnace window only on a furnace: one is shown under the player, as the menus do with chests;
    // the window works on the custom block's own furnace (OpenContainerAt), which cooks in TickFurnaces.
    private void OpenCustomFurnace(BlockEntity entity, int x, int y, int z)
    {
        CloseContainer(notifyClient: true);
        var fake = ((int)Math.Floor(Position.X), (int)Math.Floor(Position.Y - EyeHeight) - 2, (int)Math.Floor(Position.Z));
        _fakeBlockAt = fake;
        Send(Packets.UpdateBlock(fake.Item1, fake.Item2, fake.Item3, Blocks.ByName("furnace")!.NetworkId),
            BlockEntity.DataPacket(fake.Item1, fake.Item2, fake.Item3, entity));
        var world = World;
        _server.Schedule(3, () => // the client needs the furnace before it accepts the window
        {
            if (_fakeBlockAt != fake || !IsInGame || World != world || world.GetBlockEntity(x, y, z) != entity)
                return;
            Inventory.OpenContainer = (entity.Items!, entity.Id);
            OpenContainerAt = (x, y, z);
            Send(InventoryPackets.ContainerOpen(WindowContainer, 2, fake.Item1, fake.Item2, fake.Item3),
                InventoryPackets.InventoryContent(WindowContainer, entity.Items!));
            _server.WatchFurnace((x, y, z), world);
        });
    }

    // A custom crafting table: the client computes no result in its own screen for a custom table (tested with 1.26.60),
    // so it gets the vanilla crafting screen on a vanilla table only it sees, under the player; the recipes go out as
    // "crafting_table" ones and the server checks the custom table's tags (PlayerInventory Craft).
    private void OpenCustomCraftingTable(IReadOnlyList<string> craftingTags)
    {
        CloseContainer(notifyClient: true);
        var fake = ((int)Math.Floor(Position.X), (int)Math.Floor(Position.Y - EyeHeight) - 2, (int)Math.Floor(Position.Z));
        _fakeBlockAt = fake;
        Send(Packets.UpdateBlock(fake.Item1, fake.Item2, fake.Item3, Blocks.ByName("crafting_table")!.NetworkId));
        var world = World;
        _server.Schedule(3, () => // the client needs the table before it accepts the window
        {
            if (_fakeBlockAt != fake || !IsInGame || World != world)
                return;
            Inventory.CraftingTableOpen = true;
            Inventory.CraftingTags = craftingTags;
            Send(InventoryPackets.ContainerOpen(InventoryPackets.WindowCraftingTable, InventoryPackets.ContainerTypeWorkbench, fake.Item1, fake.Item2, fake.Item3));
        });
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

    private int _closingWindow = -1;
    private long _closingTick;

    // The server closes a window: the client answers with a close of its own, which must not be answered again (it
    // rejects that: "should have sent the server initiated packet").
    private void CloseWindowFromServer(byte window, byte type)
    {
        _closingWindow = window;
        _closingTick = _server.CurrentTick;
        Send(InventoryPackets.ContainerClose(window, type, serverInitiated: true));
    }

    /// <summary>The container was broken or the window was closed.</summary>
    internal void CloseContainer(bool notifyClient)
    {
        if (OpenContainerAt is not null)
        {
            if (_server.Events.HasContainerClose)
                _server.Events.OnContainerClose(new ContainerCloseEvent(this, WindowContainer));
        }
        if (notifyClient && OpenContainerAt is not null)
            CloseWindowFromServer(WindowContainer, 0);
        OpenContainerAt = null;
        Inventory.OpenContainer = null;
        if (_fakeBlockAt is var (fx, fy, fz))
        {
            _fakeBlockAt = null;
            if (World.GetBlock(fx, fy, fz) is { } real) // the real block comes back where the fake furnace was
                Send(Packets.UpdateBlock(fx, fy, fz, real.NetworkId));
        }
        CloseMenu();
    }

    /// <summary>ContainerSetData (id 51): furnace progress bars for the players looking into it.</summary>
    internal static byte[] ContainerData(int key, int value) =>
        new BedrockWriter().VarUInt32(51).Byte(WindowContainer).VarInt32(key).VarInt32(value).ToArray();
}
