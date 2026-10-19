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
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Entities;

/// <summary>
/// A non-player character: a player entity with its own skin that stands still (a shop keeper, a crate...).
/// Players see it within <see cref="NpcManager.ViewDistance"/> blocks; hitting or using it fires
/// <see cref="Events.NpcInteractEvent"/>. It is not saved: a plugin spawns its NPCs again when it starts.
/// </summary>
public sealed class Npc
{
    internal Npc(long entityId, World world, double x, double y, double z, float yaw, string nameTag, byte[] skin)
    {
        EntityId = entityId;
        World = world;
        (X, Y, Z, Yaw) = (x, y, z, yaw);
        NameTag = nameTag;
        Skin = skin;
    }

    public long EntityId { get; }
    public Guid Uuid { get; } = Guid.NewGuid();
    public World World { get; }
    public double X { get; }
    public double Y { get; }
    public double Z { get; }
    public float Yaw { get; }
    public string NameTag { get; }

    /// <summary>The encoded skin (<see cref="PlayerPackets.CustomSkin"/>).</summary>
    public byte[] Skin { get; }

    /// <summary>Free data for the plugin that spawned it (e.g. the crate type).</summary>
    public string Tag { get; set; } = "";

    internal HashSet<BedrockConnection> Viewers { get; } = [];
}

/// <summary>The server's NPCs (<see cref="BedrokiaServer.Npcs"/>).</summary>
public sealed class NpcManager(BedrokiaServer server)
{
    public const int ViewDistance = 64;

    private readonly List<Npc> _npcs = [];

    public IReadOnlyList<Npc> All => _npcs;

    /// <summary>Spawns an NPC; <paramref name="y"/> is where its feet are. Main thread only.</summary>
    /// <param name="skin">From <see cref="PlayerPackets.CustomSkin"/>, or see <see cref="SkinFromPng"/>.</param>
    public Npc Spawn(World world, double x, double y, double z, float yaw, string nameTag, byte[] skin)
    {
        var npc = new Npc(server.NextEntityId(), world, x, y, z, yaw, nameTag, skin);
        _npcs.Add(npc);
        Sync(npc);
        return npc;
    }

    public void Remove(Npc npc)
    {
        if (!_npcs.Remove(npc))
            return;
        foreach (var viewer in npc.Viewers)
            viewer.Send(Packets.RemoveActor(npc.EntityId));
        npc.Viewers.Clear();
    }

    public Npc? Find(long entityId) => _npcs.FirstOrDefault(n => n.EntityId == entityId);

    /// <summary>
    /// A skin from a PNG file and an optional .geo.json model (its first geometry identifier is used). Images larger
    /// than 128×128 are scaled down to 128×128.
    /// </summary>
    /// <exception cref="InvalidDataException">The PNG cannot be read.</exception>
    public static byte[] SkinFromPng(string skinId, byte[] png, string geometryJson = "")
    {
        var (width, height, rgba) = Png.Decode(png);
        if (width > 128 || height > 128)
        {
            rgba = Png.Resize(rgba, width, height, 128, 128);
            (width, height) = (128, 128);
        }
        var geometryName = "geometry.humanoid.custom";
        if (geometryJson != "")
        {
            using var doc = System.Text.Json.JsonDocument.Parse(geometryJson);
            if (doc.RootElement.TryGetProperty("minecraft:geometry", out var list) && list.GetArrayLength() > 0
                && list[0].TryGetProperty("description", out var description) && description.TryGetProperty("identifier", out var id))
                geometryName = id.GetString() ?? geometryName;
        }
        return PlayerPackets.CustomSkin(skinId, width, height, rgba, geometryName, geometryJson);
    }

    internal void Tick(long tick)
    {
        if (tick % 20 == 0)
            foreach (var npc in _npcs)
                Sync(npc);
    }

    internal void Forget(BedrockConnection player)
    {
        foreach (var npc in _npcs)
            npc.Viewers.Remove(player);
    }

    // Shows the NPC to the players near it and hides it from those who left.
    private void Sync(Npc npc)
    {
        foreach (var viewer in npc.Viewers.Where(v => !InRange(npc, v)).ToList())
        {
            npc.Viewers.Remove(viewer);
            if (viewer.IsInGame)
                viewer.Send(Packets.RemoveActor(npc.EntityId));
        }
        foreach (var player in server.Players)
            if (InRange(npc, player) && npc.Viewers.Add(player))
            {
                // The skin travels in the player list; the entry is removed again so the NPC is not in the tab list.
                var info = new PlayerInfo(npc.Uuid, npc.EntityId, npc.NameTag, "", 0, npc.Skin);
                player.Send(PlayerPackets.PlayerListAdd([info]),
                    PlayerPackets.AddPlayer(info, (float)npc.X, (float)npc.Y + BedrockConnection.EyeHeight, (float)npc.Z, 0, npc.Yaw, 0,
                        showName: npc.NameTag != "", nameTag: npc.NameTag),
                    PlayerPackets.PlayerListRemove(npc.Uuid));
            }
    }

    private static bool InRange(Npc npc, BedrockConnection player)
    {
        if (!player.IsInGame || player.World != npc.World)
            return false;
        var dx = player.Position.X - npc.X;
        var dz = player.Position.Z - npc.Z;
        return dx * dx + dz * dz <= ViewDistance * ViewDistance;
    }
}
