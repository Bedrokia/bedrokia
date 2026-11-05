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

/// <summary>Inventory and item entity packets. Layouts from gophertunnel (MIT) at protocol 2193.</summary>
internal static class InventoryPackets
{
    public const uint InventoryContentId = 49;
    public const uint InventorySlotId = 50;
    public const uint ItemStackRequestId = 147;
    public const uint ItemStackResponseId = 148;
    public const uint MobEquipmentId = 31;
    public const uint AddItemActorId = 15;
    public const uint TakeItemActorId = 17;
    public const uint InteractId = 33;
    public const uint ContainerOpenId = 46;
    public const uint ContainerCloseId = 47;

    public const byte InteractOpenInventory = 6;
    public const byte ContainerTypeInventory = 0xFF, ContainerTypeWorkbench = 1, ContainerTypeAnvil = 5, ContainerTypeGrindstone = 26, ContainerTypeStonecutter = 29;
    public const byte WindowCraftingTable = 1, WindowAnvil = 4, WindowGrindstone = 5, WindowStonecutter = 6;

    public static byte[] ContainerOpen(byte windowId, byte type, int x, int y, int z, long entityId = -1) =>
        new BedrockWriter().VarUInt32(ContainerOpenId).Byte(windowId).Byte(type)
            .VarInt32(x).VarInt32(y).VarInt32(z).VarInt64(entityId).ToArray();

    /// <param name="serverInitiated">True when the server closes the window itself; false when it answers the client closing it
    /// (the client rejects a close sent with the wrong one).</param>
    public static byte[] ContainerClose(byte windowId, byte type, bool serverInitiated) =>
        new BedrockWriter().VarUInt32(ContainerCloseId).Byte(windowId).Byte(type).Bool(serverInitiated).ToArray();

    public const uint WindowInventory = 0;
    public const uint WindowOffhand = 119;
    public const uint WindowArmour = 120;
    public const uint WindowUi = 124;

    public static byte[] InventoryContent(uint windowId, Inventory inventory)
    {
        var w = new BedrockWriter().VarUInt32(InventoryContentId).VarUInt32(windowId).VarUInt32((uint)inventory.Size);
        foreach (var stack in inventory.Slots)
            ItemStack.WriteInstance(w, stack);
        w.Byte(0).Bool(false); // container name (unused here)
        ItemStack.WriteInstance(w, null); // storage item
        return w.ToArray();
    }

    public static byte[] InventorySlot(uint windowId, int slot, ItemStack? stack)
    {
        var w = new BedrockWriter().VarUInt32(InventorySlotId).VarUInt32(windowId).VarUInt32((uint)slot)
            .Bool(false) // container name
            .Bool(false); // storage item
        ItemStack.WriteInstance(w, stack);
        return w.ToArray();
    }

    public sealed record SlotChange(byte Container, byte Slot, ItemStack? Stack);

    /// <summary>One ItemStackResponse: status 0 = ok (with the changed slots), 1 = error (client reverts).</summary>
    public static byte[] ItemStackResponse(int requestId, bool ok, IReadOnlyCollection<SlotChange> changes)
    {
        var w = new BedrockWriter().VarUInt32(ItemStackResponseId).VarUInt32(1)
            .Byte(ok ? (byte)0 : (byte)1).VarInt32(requestId);
        if (!ok || changes.Count == 0)
        {
            w.Bool(false);
            return w.ToArray();
        }
        var byContainer = changes.GroupBy(c => c.Container).ToList();
        w.Bool(true).VarUInt32((uint)byContainer.Count);
        foreach (var container in byContainer)
        {
            w.Byte(container.Key).Bool(false).VarUInt32((uint)container.Count());
            foreach (var change in container)
            {
                var stack = change.Stack is { Count: > 0 } ? change.Stack : null;
                w.Byte(change.Slot).Byte(change.Slot).Byte((byte)(stack?.Count ?? 0));
                if (stack is null)
                    w.Bool(false);
                else
                    w.Bool(true).VarInt32(stack.NetworkId);
                w.String("") // custom name
                    .Bool(false) // filtered custom name
                    .VarInt32(0); // durability correction
            }
        }
        return w.ToArray();
    }

    public static byte[] AddItemActor(long entityId, ItemStack stack, float x, float y, float z, float vx, float vy, float vz, bool still = false)
    {
        var w = new BedrockWriter().VarUInt32(AddItemActorId).VarInt64(entityId).VarUInt64((ulong)entityId);
        ItemStack.WriteInstance(w, stack);
        w.Vec3(x, y, z).Vec3(vx, vy, vz);
        if (still) // flags (key 0, int64): no AI (16) and no gravity flag (49), so the client leaves it where it is
            w.VarUInt32(1).VarUInt32(0).VarUInt32(7).Byte(7).VarInt64(1L << 16);
        else
            w.VarUInt32(0); // metadata
        return w
            .Bool(false) // from fishing
            .ToArray();
    }

    public static byte[] TakeItemActor(long itemEntityId, long takerEntityId) =>
        new BedrockWriter().VarUInt32(TakeItemActorId).VarUInt64((ulong)itemEntityId).VarUInt64((ulong)takerEntityId).ToArray();

    public static byte[] MobEquipment(long entityId, ItemStack? item, int inventorySlot, int hotbarSlot, uint windowId = 0)
    {
        var w = new BedrockWriter().VarUInt32(MobEquipmentId).VarUInt64((ulong)entityId);
        ItemStack.WriteInstance(w, item);
        return w.Byte((byte)inventorySlot).Byte((byte)hotbarSlot).Byte((byte)windowId).ToArray();
    }

    /// <summary>MobArmourEquipment: the armour the others see (helmet, chestplate, leggings, boots, body).</summary>
    public static byte[] MobArmourEquipment(long entityId, ItemStack? helmet, ItemStack? chestplate, ItemStack? leggings, ItemStack? boots)
    {
        var w = new BedrockWriter().VarUInt32(MobArmourEquipmentId).VarUInt64((ulong)entityId);
        foreach (var item in (ItemStack?[])[helmet, chestplate, leggings, boots, null])
            ItemStack.WriteInstance(w, item);
        return w.ToArray();
    }

    private const uint MobArmourEquipmentId = 32;

    public static byte[] PlayerHotbar(int selectedSlot, uint windowId = 0) =>
        new BedrockWriter().VarUInt32(48).VarUInt32((uint)selectedSlot).Byte((byte)windowId).Bool(true).ToArray();

    /// <summary>
    /// Encodes a MobArmourEquipment packet (packet ID 32) sending armor equipment to viewers.
    /// Source: gophertunnel (MIT) minecraft/protocol/packet/mob_armour_equipment.go.
    /// </summary>
    public static byte[] MobArmorEquipment(long entityId, ItemStack? helmet = null, ItemStack? chestplate = null, ItemStack? leggings = null, ItemStack? boots = null, ItemStack? body = null)
    {
        var w = new BedrockWriter().VarUInt32(PacketId.MobArmourEquipment).VarUInt64((ulong)entityId);
        ItemStack.WriteInstance(w, helmet);
        ItemStack.WriteInstance(w, chestplate);
        ItemStack.WriteInstance(w, leggings);
        ItemStack.WriteInstance(w, boots);
        ItemStack.WriteInstance(w, body);
        return w.ToArray();
    }
}

