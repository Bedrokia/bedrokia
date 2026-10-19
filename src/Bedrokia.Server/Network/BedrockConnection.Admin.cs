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
                _server.BroadcastInExcept(World, this, PlayerPackets.SetPlayerData(EntityId, NameTag, _nameTagVisible));
        }
    }

    /// <summary>AddPlayer for another client: this player as it stands, name tag included.</summary>
    internal byte[] AddPlayerPacket() =>
        PlayerPackets.AddPlayer(Info!, Position.X, Position.Y, Position.Z, Pitch, Yaw, (int)GameMode, _nameTagVisible, NameTag);

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
                _server.BroadcastInExcept(World, this, PlayerPackets.SetPlayerData(EntityId, NameTag, value));
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
