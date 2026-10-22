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
using Bedrokia.Server.Core;

namespace Bedrokia.Server.Network;

/// <summary>
/// What staff commands need: heal, feed, fly, god mode, freeze and vanish.
/// UpdateAbilities layout and ability bits from gophertunnel (MIT) packet/update_abilities.go and
/// protocol/ability.go; how they are filled from Dragonfly (MIT) session/player.go SendAbilities.
/// </summary>
public sealed partial class BedrockConnection
{
    private const uint UpdateAbilitiesId = 187;
    private const uint AbilityBuild = 1 << 0, AbilityMine = 1 << 1, AbilityDoorsAndSwitches = 1 << 2, AbilityOpenContainers = 1 << 3,
        AbilityAttackPlayers = 1 << 4, AbilityAttackMobs = 1 << 5, AbilityOperatorCommands = 1 << 6, AbilityTeleport = 1 << 7,
        AbilityInvulnerable = 1 << 8, AbilityFlying = 1 << 9, AbilityMayFly = 1 << 10, AbilityInstantBuild = 1 << 11, AbilityNoClip = 1 << 17,
        AbilityAll = (1 << 20) - 1;

    private bool _canFly, _god, _vanished;

    /// <summary>The player may fly in survival and adventure (always true in creative).</summary>
    public bool CanFly
    {
        get => _canFly;
        set
        {
            _canFly = value;
            SendAbilities();
        }
    }

    /// <summary>God mode: takes no damage and loses no food.</summary>
    public bool God
    {
        get => _god;
        set
        {
            _god = value;
            SendAbilities();
        }
    }

    private bool _nameTagVisible = true;

    private string? _nameTag;

    /// <summary>The text above the head (PocketMine setNameTag), the player's name by default.</summary>
    public string NameTag
    {
        get => _nameTag ?? Name;
        set
        {
            if (NameTag == value)
                return;
            _nameTag = value;
            if (IsInGame)
                _server.BroadcastInExcept(World, this, DataPacket());
        }
    }

    /// <summary>AddPlayer for another client: this player as it stands, name tag included.</summary>
    internal byte[] AddPlayerPacket()
    {
        _equipmentShown = false; // whoever gets this player now also needs its armour and held item (next tick)
        return PlayerPackets.AddPlayer(Info!, Position.X, Position.Y, Position.Z, Pitch, Yaw, (int)GameMode, _nameTagVisible, NameTag, ActionFlags);
    }

    // What the others see of this player: name, riding seat and actions (sneaking, sprinting, swimming, gliding).
    internal byte[] DataPacket() => PlayerPackets.SetPlayerData(EntityId, NameTag, _nameTagVisible, _seatY, ActionFlags);

    private float? _seatY;
    private bool _sprinting, _swimming, _gliding;
    private long _shownActionFlags;

    // Entity flags (gophertunnel EntityDataFlag): sneaking 1, sprinting 3, gliding 32, swimming 57.
    private long ActionFlags => (Sneaking ? 1L << 1 : 0) | (_sprinting ? 1L << 3 : 0) | (_gliding ? 1L << 32 : 0) | (_swimming ? 1L << 57 : 0);

    /// <summary>Whether the player is flying with an elytra.</summary>
    public bool Gliding => _gliding;

    // Held item and armour as last shown to the others: sent again when one of them is another item.
    private bool _equipmentShown;
    private readonly Game.ItemStack?[] _shownEquipment = new Game.ItemStack?[6];

    private void SyncEquipment()
    {
        var held = Inventory.Held;
        var armour = Inventory.Armour;
        var offhand = Inventory.Offhand[0];
        if (_equipmentShown && ReferenceEquals(_shownEquipment[0], held) && ReferenceEquals(_shownEquipment[1], armour[0])
            && ReferenceEquals(_shownEquipment[2], armour[1]) && ReferenceEquals(_shownEquipment[3], armour[2]) && ReferenceEquals(_shownEquipment[4], armour[3]) && ReferenceEquals(_shownEquipment[5], offhand))
            return;
        _equipmentShown = true;
        (_shownEquipment[0], _shownEquipment[1], _shownEquipment[2], _shownEquipment[3], _shownEquipment[4], _shownEquipment[5]) = (held, armour[0], armour[1], armour[2], armour[3], offhand);
        if (Vanished)
            return;
        _server.BroadcastInExcept(World, this, InventoryPackets.MobEquipment(EntityId, held, Inventory.SelectedSlot, Inventory.SelectedSlot));
        _server.BroadcastInExcept(World, this, InventoryPackets.MobArmourEquipment(EntityId, armour[0], armour[1], armour[2], armour[3]));
        // The second hand (totem, shield...): MobEquipment on the offhand window, as PocketMine.
        _server.BroadcastInExcept(World, this, InventoryPackets.MobEquipment(EntityId, offhand, 0, 0, InventoryPackets.WindowOffhand));
    }

    private void SyncActionFlags()
    {
        if (ActionFlags == _shownActionFlags)
            return;
        var glidingChanged = ((ActionFlags ^ _shownActionFlags) & 1L << 32) != 0;
        _shownActionFlags = ActionFlags;
        _server.BroadcastInExcept(World, this, DataPacket());
        if (glidingChanged)
            Send(DataPacket()); // as PocketMine: the glider is told too, or its client keeps flying on its own
    }

    // PocketMine Player::toggleGlide: only with a working elytra (one durability point left is a broken one).
    private bool CanGlide() => Inventory.Armour[1] is { Type.Name: "minecraft:elytra" } elytra
                               && Game.Durability.Damage(elytra) < Game.Durability.Max(elytra) - 1 && GameMode != GameMode.Spectator;

    private void StartGliding()
    {
        _gliding = CanGlide();
        if (!_gliding)
            Send(DataPacket()); // refused: the client stops gliding
    }

    private int _glideTicks;

    // Every tick while gliding: the elytra wears one point a second (vanilla) and stops working at its last point;
    // without one (taken off, broken) or back on the ground, the glide ends.
    private void TickGliding()
    {
        if (!_gliding)
            return;
        if (!CanGlide() || _onGround && _glideTicks > 10)
        {
            _gliding = false;
            _glideTicks = 0;
            return;
        }
        if (++_glideTicks % 20 != 0 || GameMode == GameMode.Creative || Inventory.Armour[1] is not { } elytra)
            return;
        var nbt = elytra.Nbt is null ? new OrderedDictionary<string, object>() : new OrderedDictionary<string, object>(elytra.Nbt);
        nbt["Damage"] = Math.Min(Game.Durability.Damage(elytra) + 1, Game.Durability.Max(elytra) - 1);
        var worn = new Game.ItemStack(elytra.Type, elytra.Count, elytra.Meta, elytra.BlockRuntimeId, nbt);
        Inventory.Armour[1] = worn;
        Send(InventoryPackets.InventorySlot(InventoryPackets.WindowArmour, 1, worn));
    }

    /// <summary>Whether the other players see this player's name above the head (PocketMine setNameTagVisible).</summary>
    public bool NameTagVisible
    {
        get => _nameTagVisible;
        set
        {
            if (_nameTagVisible == value)
                return;
            _nameTagVisible = value;
            if (IsInGame)
                _server.BroadcastInExcept(World, this, DataPacket());
        }
    }

    /// <summary>Frozen players cannot move: every move is sent back to where they are.</summary>
    public bool Frozen { get; set; }

    /// <summary>Vanished players are invisible to the other players (they stay in the player list).</summary>
    public bool Vanished
    {
        get => _vanished;
        set
        {
            if (_vanished == value)
                return;
            _vanished = value;
            if (!IsInGame)
                return;
            if (value)
                _server.BroadcastInExcept(World, this, Packets.RemoveActor(EntityId));
            else
                _server.BroadcastInExcept(World, this, AddPlayerPacket());
        }
    }

    /// <summary>Sets the health (full by default).</summary>
    public void SetHealth(float health = MaxHealth)
    {
        if (Dead)
            return;
        Health = Math.Clamp(health, 0.5f, MaxHealth);
        SendVitals();
    }

    /// <summary>Sets the food bar (0..20) and saturation.</summary>
    public void SetFood(int food = MaxFood, float saturation = 5)
    {
        Food = Math.Clamp(food, 0, MaxFood);
        Saturation = Math.Clamp(saturation, 0, Food);
        _exhaustion = 0;
        SendVitals();
    }

    /// <summary>Empties the inventory (and the armour unless <paramref name="armour"/> is false).</summary>
    public void ClearInventory(bool armour = true)
    {
        Inventory.Main.Clear();
        Inventory.Offhand.Clear();
        if (armour)
            Inventory.Armour.Clear();
        SyncInventory();
    }

    /// <summary>Moves the player to another server; plugins save what must follow them before calling this.</summary>
    public void Transfer(string address, ushort port = 19132) => Send(Packets.Transfer(address, port));

    /// <summary>Sends the whole inventory again, after a plugin changed slots directly.</summary>
    public void SyncInventory() => Send([.. Inventory.ContentPackets()]);

    private void SendAbilities()
    {
        if (!IsInGame)
            return;
        var creative = GameMode == GameMode.Creative;
        var spectator = GameMode == GameMode.Spectator;
        var values = spectator ? AbilityMayFly | AbilityFlying | AbilityNoClip | AbilityInvulnerable
            : AbilityBuild | AbilityMine | AbilityDoorsAndSwitches | AbilityOpenContainers | AbilityAttackPlayers | AbilityAttackMobs;
        if (IsOp)
            values |= AbilityOperatorCommands | AbilityTeleport;
        if (creative || _canFly)
            values |= AbilityMayFly;
        if (creative || _god)
            values |= AbilityInvulnerable;
        if (creative)
            values |= AbilityInstantBuild;
        Send(new BedrockWriter().VarUInt32(UpdateAbilitiesId)
            .Int64(EntityId).Byte((byte)(IsOp ? 2 : 1)).Byte((byte)(IsOp ? 1 : 0)) // unique id, player and command permissions
            .VarUInt32(1).UInt16(1) // one layer: base
            .UInt32(AbilityAll).UInt32(values)
            .Float32(0.05f).Float32(1f).Float32(0.1f) // fly, vertical fly and walk speed
            .ToArray());
    }
}
