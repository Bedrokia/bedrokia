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

/// <summary>A click in a menu: the slot, the item in it and the item put there instead (null when taking).</summary>
public sealed record MenuClick(BedrockConnection Player, int Slot, ItemStack? ItemClicked, ItemStack? ItemClickedWith);

/// <summary>
/// A chest window that is not a block of the world (shops, menus, another player's inventory...): 27 slots for a
/// chest, 54 for a double chest. Like muqsit/InvMenu (MIT): a fake chest is shown under the player and opened.
/// </summary>
public sealed class InventoryMenu
{
    public InventoryMenu(string name, bool doubleChest = false)
    {
        Name = name;
        DoubleChest = doubleChest;
        Inventory = new Inventory(doubleChest ? 54 : 27);
    }

    /// <summary>
    /// A window of any size (up to 255 slots) drawn by the resource pack, as ModSmith does: it is opened on an invisible
    /// entity whose name is <paramref name="layout"/>, the hidden code the pack's ui/chest_screen.json looks for in the
    /// title to show its own grid (colours, slots, title). Without such a pack the client shows a plain chest window.
    /// </summary>
    public InventoryMenu(string name, int slots, string layout)
    {
        Name = name;
        Inventory = new Inventory(Math.Clamp(slots, 1, 255));
        Layout = layout;
    }

    public string Name { get; set; }
    public bool DoubleChest { get; }
    public Inventory Inventory { get; }

    /// <summary>The pack's layout code of a window of any size (see the constructor), or null for a chest window.</summary>
    public string? Layout { get; }

    /// <summary>Called for every slot a player changes; return false to refuse it (the items go back).</summary>
    public Func<MenuClick, bool>? Listener { get; set; }

    /// <summary>Called when the player closes the menu (or leaves).</summary>
    public Action<BedrockConnection>? OnClose { get; set; }

    /// <summary>The player can look but not take or put anything.</summary>
    public InventoryMenu ReadOnly()
    {
        Listener = _ => false;
        return this;
    }
}

public sealed partial class BedrockConnection
{
    private InventoryMenu? _menu;
    private (int X, int Y, int Z) _menuAt;

    /// <summary>The menu this player has open, or null.</summary>
    public InventoryMenu? OpenMenu => _menu;

    /// <summary>Opens <paramref name="menu"/> (closing what was open). The window opens a few ticks later.</summary>
    public void SendMenu(InventoryMenu menu)
    {
        CloseContainer(notifyClient: true);
        if (menu.Layout is { } layout)
        {
            SendLayoutMenu(menu, layout);
            return;
        }
        var feet = ((int)Math.Floor(Position.X), (int)Math.Floor(Position.Y - EyeHeight) - 2, (int)Math.Floor(Position.Z));
        _menu = menu;
        _menuAt = feet;
        var chest = Blocks.ByName("chest")!;
        var (x, y, z) = feet;
        Send(Packets.UpdateBlock(x, y, z, chest.NetworkId), MenuChestData(x, y, z, menu, pairX: menu.DoubleChest ? x + 1 : null));
        if (menu.DoubleChest)
            Send(Packets.UpdateBlock(x + 1, y, z, chest.NetworkId), MenuChestData(x + 1, y, z, menu, pairX: x));
        // The client needs the chest and its data before it accepts the window.
        _server.Schedule(3, () =>
        {
            if (_menu != menu || !IsInGame)
                return;
            Inventory.OpenContainer = (menu.Inventory, "Menu");
            Inventory.MenuListener = (slot, clicked, with) => menu.Listener?.Invoke(new MenuClick(this, slot, clicked, with)) ?? true;
            OpenContainerAt = feet;
            Send(InventoryPackets.ContainerOpen(WindowContainer, 0, x, y, z), InventoryPackets.InventoryContent(WindowContainer, menu.Inventory));
        });
    }

    private long _menuEntity = -1;

    // ModSmith's way: an invisible entity named with the layout code, holding a container of the menu's size, riding
    // the player (it stays in reach), and the window opened on it.
    private void SendLayoutMenu(InventoryMenu menu, string layout)
    {
        var feet = ((int)Math.Floor(Position.X), (int)Math.Floor(Position.Y - EyeHeight), (int)Math.Floor(Position.Z));
        var id = _server.NextEntityId();
        _menu = menu;
        _menuAt = feet;
        _menuEntity = id;
        const long invisible = 1L << 5;
        const sbyte containerInventory = -1; // WindowTypes::INVENTORY, as ModSmith
        Send(Entities.EntityPackets.AddActor(id, Entities.ActorIdentifiers.InventoryHolder, Position.X, Position.Y - EyeHeight, Position.Z, 0, 1, 1, 0, 0, 0,
                invisible, nameTag: layout, showName: false),
            Entities.EntityPackets.ContainerData(id, containerInventory, menu.Inventory.Size),
            Entities.VehicleManager.Link(EntityId, id, mount: true));
        _server.Schedule(2, () =>
        {
            if (_menu != menu || !IsInGame)
                return;
            Inventory.OpenContainer = (menu.Inventory, "Menu");
            Inventory.MenuListener = (slot, clicked, with) => menu.Listener?.Invoke(new MenuClick(this, slot, clicked, with)) ?? true;
            OpenContainerAt = feet;
            Send(InventoryPackets.ContainerOpen(WindowContainer, 0, feet.Item1, feet.Item2, feet.Item3, id),
                InventoryPackets.InventoryContent(WindowContainer, menu.Inventory));
        });
    }

    /// <summary>Closes the open window (menu, chest, furnace...), like PocketMine removeCurrentWindow.</summary>
    public void CloseWindow() => CloseContainer(notifyClient: true);

    /// <summary>Sends the menu's items again after changing them while it is open.</summary>
    public void RefreshMenu()
    {
        if (_menu is { } menu && Inventory.OpenContainer?.Items == menu.Inventory)
            Send(InventoryPackets.InventoryContent(WindowContainer, menu.Inventory));
    }

    private static byte[] MenuChestData(int x, int y, int z, InventoryMenu menu, int? pairX)
    {
        var nbt = new OrderedDictionary<string, object>
        {
            ["id"] = "Chest", ["x"] = x, ["y"] = y, ["z"] = z, ["isMovable"] = (byte)0, ["CustomName"] = menu.Name,
        };
        if (pairX is { } px)
        {
            nbt["pairx"] = px;
            nbt["pairz"] = z;
            nbt["pairlead"] = (byte)(px > x ? 1 : 0);
        }
        return new BedrockWriter().VarUInt32(56).VarInt32(x).VarInt32(y).VarInt32(z).Bytes(Nbt.Write(nbt, network: true)).ToArray();
    }

    // The menu closed: the real blocks come back and the plugin is told.
    private void CloseMenu()
    {
        if (_menu is not { } menu)
            return;
        _menu = null;
        Inventory.MenuListener = null;
        if (_menuEntity >= 0)
        {
            Send(Packets.RemoveActor(_menuEntity));
            _menuEntity = -1;
            menu.OnClose?.Invoke(this);
            return;
        }
        var (x, y, z) = _menuAt;
        foreach (var bx in menu.DoubleChest ? new[] { x, x + 1 } : [x])
        {
            if (World.GetBlock(bx, y, z) is { } real)
                Send(Packets.UpdateBlock(bx, y, z, real.NetworkId));
            if (World.GetBlockEntity(bx, y, z) is { } entity)
                Send(BlockEntity.DataPacket(bx, y, z, entity));
        }
        menu.OnClose?.Invoke(this);
    }
}
