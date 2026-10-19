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
using Bedrokia.Server.Entities;
using Bedrokia.Server.Game;

namespace Bedrokia.Server.Network;

/// <summary>
/// What plugins can do with a player: titles, action bar, sidebar, effects, speed, motion, items and sounds.
/// Packet layouts from gophertunnel (MIT) packet/set_title.go, set_display_objective.go, set_score.go, mob_effect.go.
/// </summary>
public sealed partial class BedrockConnection
{
    private const uint SetTitleId = 88, RemoveObjectiveId = 106, SetDisplayObjectiveId = 107, SetScoreId = 108, MobEffectId = 28;
    private const string SidebarObjective = "bedrokia.sidebar";

    /// <summary>Effect ids (Bedrock): speed 1, slowness 2, haste 3, strength 5, jump boost 8, regeneration 10,
    /// resistance 11, fire resistance 12, water breathing 13, invisibility 14, night vision 16, levitation 24, slow falling 27.</summary>
    public static class Effects
    {
        public const int Speed = 1, Slowness = 2, Haste = 3, Strength = 5, JumpBoost = 8, Regeneration = 10, Resistance = 11,
            FireResistance = 12, WaterBreathing = 13, Invisibility = 14, NightVision = 16, Levitation = 24, SlowFalling = 27;
    }

    /// <summary>Big title in the middle of the screen, with an optional subtitle. Durations in ticks.</summary>
    public void SendTitle(string title, string subtitle = "", int fadeIn = 10, int stay = 60, int fadeOut = 20)
    {
        Send(Title(5, "", fadeIn, stay, fadeOut));
        if (subtitle.Length > 0)
            Send(Title(3, subtitle, fadeIn, stay, fadeOut));
        Send(Title(2, title, fadeIn, stay, fadeOut));
    }

    /// <summary>Text above the hotbar.</summary>
    public void SendActionBar(string text) => Send(Title(4, text, 0, 40, 10));

    private static byte[] Title(int action, string text, int fadeIn, int stay, int fadeOut) =>
        new BedrockWriter().VarUInt32(SetTitleId).VarInt32(action).String(text).VarInt32(fadeIn).VarInt32(stay).VarInt32(fadeOut)
            .String("").String("").String("").ToArray();

    /// <summary>Sidebar scoreboard on the right of the screen: a title and up to 15 lines, top to bottom.</summary>
    public void SetSidebar(string title, IReadOnlyList<string> lines)
    {
        ClearSidebar();
        Send(new BedrockWriter().VarUInt32(SetDisplayObjectiveId).String("sidebar").String(SidebarObjective).String(title)
            .String("dummy").VarInt32(0).ToArray()); // ascending: score 0 on top
        var w = new BedrockWriter().VarUInt32(SetScoreId).VarUInt32((uint)Math.Min(lines.Count, 15));
        for (var i = 0; i < Math.Min(lines.Count, 15); i++)
            w.VarUInt32(3).String("changefakeplayer").VarInt64(i + 1).String(SidebarObjective).Int32(i)
                .String(lines[i] + string.Concat(Enumerable.Repeat("§r", i))); // equal lines must differ
        Send(w.ToArray());
    }

    public void ClearSidebar() => Send(new BedrockWriter().VarUInt32(RemoveObjectiveId).String(SidebarObjective).ToArray());

    /// <summary>Gives a potion effect (see <see cref="Effects"/>); level 0 is "I". Speed also changes the walk speed.</summary>
    public void AddEffect(int effect, int amplifier, int seconds, bool particles = true) =>
        AddEffect(new EffectInstance((EffectType)effect, amplifier, seconds * 20, particles));

    public void RemoveEffect(int effect) => RemoveEffect((EffectType)effect);

    /// <summary>Walk speed (vanilla 0.1).</summary>
    public void SetSpeed(float speed) => Send(new BedrockWriter().VarUInt32(PacketId.UpdateAttributes).VarUInt64((ulong)EntityId).VarUInt32(1)
        .Float32(0).Float32(float.MaxValue).Float32(speed).Float32(0).Float32(float.MaxValue).Float32(0.1f).String("minecraft:movement").VarUInt32(0)
        .VarUInt64(0).ToArray());

    /// <summary>Pushes the player (blocks per tick; 1.0 up is a big jump).</summary>
    public void SetMotion(double vx, double vy, double vz) => Send(EntityPackets.Motion(EntityId, vx, vy, vz));

    /// <summary>Adds an item to the inventory; what does not fit is dropped at the player's feet.</summary>
    public void GiveItem(ItemStack stack)
    {
        var (packets, remaining) = Inventory.Add(stack);
        Send([.. packets]);
        if (remaining > 0)
            _server.DropItem(stack.Split(remaining), Position.X, Position.Y - EyeHeight, Position.Z, world: World);
    }

    /// <summary>Item in the main hand, null if empty.</summary>
    public ItemStack? HeldItem => Inventory.Held;

    /// <summary>Armour slot: 0 helmet, 1 chestplate, 2 leggings, 3 boots.</summary>
    public ItemStack? ArmourItem(int slot) => Inventory.Armour[slot];
}
