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

/// <summary>
/// The world a player is in (overworld or Nether), dimension changes and nether portals.
/// Dimension change sequence from Dragonfly (MIT) session/session.go changeDimension; portal frames, the 1:8 scale
/// and the 4 s wait as in vanilla.
/// </summary>
public sealed partial class BedrockConnection
{
    private const uint ChangeDimensionId = 61, StopSoundId = 87, PlayerActionId = 36;
    private const int PlayerActionDimensionChangeDone = 14;
    private const int PortalWaitTicks = 80; // survival: 4 s standing in a portal
    // After a dimension change the client keeps sending positions from the old dimension for a while: portals are
    // ignored meanwhile, otherwise a stale position "leaves" the portal and the player bounces back and forth.
    private const int PortalCooldownTicks = 100; // 5 s, then the portal works again even if the player stayed inside

    private World? _world;
    private int _portalTicks;
    private int _portalCooldown;

    /// <summary>The world (dimension) this player is in.</summary>
    public World World => _world ??= _server.World;

    /// <summary>
    /// Moves the player to (x, y, z) (feet) in <paramref name="target"/>: the client shows the dimension change
    /// screen, the players and items of the old world disappear and those of the new one appear.
    /// </summary>
    public void ChangeDimension(World target, float x, float y, float z, bool respawn = false)
    {
        if (target == World)
        {
            Teleport(x, y, z);
            return;
        }
        CloseContainer(notifyClient: true);
        var old = World;
        _server.BroadcastInExcept(old, this, Packets.RemoveActor(EntityId));
        foreach (var other in _server.Players.Where(p => p.IsInGame && p != this && p.World == old))
            Send(Packets.RemoveActor(other.EntityId));
        foreach (var item in old.DroppedItems.All)
            Send(Packets.RemoveActor(item.EntityId));

        _world = target;
        _portalCooldown = PortalCooldownTicks;
        _portalTicks = 0;
        Position = (x, y + EyeHeight, z);
        foreach (var key in _sentChunks)
        {
            var (cx, cz) = Game.World.UnpackChunkKey(key);
            old.LoadedChunk(cx, cz)?.Viewers.Remove(this);
        }
        _sentChunks.Clear();
        _requestedChunks.Clear();
        _centerChunk = (int.MinValue, int.MinValue);
        Send(new BedrockWriter().VarUInt32(ChangeDimensionId).VarInt32(target.Dimension).Vec3(x, y + EyeHeight, z)
                .Bool(respawn).Bool(false).ToArray(), // no loading screen id
            new BedrockWriter().VarUInt32(StopSoundId).String("").Bool(true).Bool(false).ToArray(),
            Packets.PlayStatus(PlayStatus.PlayerSpawn),
            // Since 1.19.50 the server sends the "dimension change done" acknowledgement itself.
            new BedrockWriter().VarUInt32(PlayerActionId).VarUInt64((ulong)EntityId).VarInt32(PlayerActionDimensionChangeDone)
                .BlockPos(0, 0, 0).BlockPos(0, 0, 0).VarInt32(0).ToArray());
        UpdateChunks(ChunkPosition.X, ChunkPosition.Z);

        if (!Vanished)
            _server.BroadcastInExcept(target, this, AddPlayerPacket());
        foreach (var other in _server.Players.Where(p => p.IsInGame && p != this && p.World == target && !p.Vanished))
            Send(other.AddPlayerPacket());
        foreach (var item in target.DroppedItems.All)
            Send(InventoryPackets.AddItemActor(item.EntityId, item.Stack, item.X, item.Y + DroppedItems.NetworkOffset, item.Z, item.Vx, item.Vy, item.Vz));
    }

    // --- Nether portals ---

    private static bool IsPortal(BlockType? block) => block?.Name == "minecraft:portal";

    /// <summary>Called every tick: standing in a portal for 4 s (at once in creative) takes the player through it.</summary>
    private void TickPortal()
    {
        if (_server.World is null)
            return; // no world loaded (tests)
        if (_portalCooldown > 0)
        {
            _portalCooldown--;
            return;
        }
        int x = (int)Math.Floor(Position.X), feet = (int)Math.Floor(Position.Y - EyeHeight), z = (int)Math.Floor(Position.Z);
        var inPortal = IsPortal(World.GetBlock(x, feet, z)) || IsPortal(World.GetBlock(x, feet + 1, z));
        if (!inPortal)
        {
            _portalTicks = 0;
            return;
        }
        if (Dead)
            return;
        if (++_portalTicks < (GameMode == Core.GameMode.Creative ? 1 : PortalWaitTicks))
            return;
        _portalTicks = 0;
        _portalCooldown = PortalCooldownTicks; // no second trip while the destination chunks load
        TravelThroughPortal(x, feet, z);
    }

    // 1 nether block = 8 overworld blocks. The chunks around the destination are loaded first, then a portal is found
    // within 16 blocks or built.
    private void TravelThroughPortal(int x, int y, int z)
    {
        var toNether = World.Dimension == 0;
        var target = toNether ? _server.Nether : _server.World;
        if (target is null || target == World)
            return;
        int tx = toNether ? (int)Math.Floor(x / 8.0) : x * 8, tz = toNether ? (int)Math.Floor(z / 8.0) : z * 8;
        var ty = Math.Clamp(y, toNether ? 34 : 5, toNether ? 118 : 250);
        var chunks = new List<Task<Chunk>>();
        for (var cx = (tx - 16) >> 4; cx <= (tx + 16) >> 4; cx++)
        for (var cz = (tz - 16) >> 4; cz <= (tz + 16) >> 4; cz++)
            chunks.Add(target.GetChunkAsync(cx, cz));
        Task.WhenAll(chunks).ContinueWith(_ => _server.RunOnMainThread(() =>
        {
            if (!IsInGame || World == target)
                return;
            var (px, py, pz) = FindPortal(target, tx, ty, tz) ?? BuildPortal(target, tx, ty, tz);
            ChangeDimension(target, px + 0.5f, py, pz + 0.5f);
        }));
    }

    private static (int X, int Y, int Z)? FindPortal(World world, int x, int y, int z)
    {
        (int, int, int)? best = null;
        var bestDistance = double.MaxValue;
        var (minY, maxY) = world.Dimension == 1 ? (1, 126) : (Chunk.MinY + 1, Chunk.MaxY - 1);
        for (var px = x - 16; px <= x + 16; px++)
        for (var pz = z - 16; pz <= z + 16; pz++)
        for (var py = minY; py <= maxY; py++)
        {
            if (!IsPortal(world.GetBlock(px, py, pz)) || IsPortal(world.GetBlock(px, py - 1, pz)))
                continue; // the bottom block of a portal
            double distance = (px - x) * (px - x) + (py - y) * (py - y) + (pz - z) * (pz - z);
            if (distance < bestDistance)
                (best, bestDistance) = ((px, py, pz), distance);
        }
        return best;
    }

    // A 4x5 obsidian frame along x with a 2x3 portal, an obsidian floor on both sides and air around it.
    private (int X, int Y, int Z) BuildPortal(World world, int x, int y, int z)
    {
        var obsidian = Blocks.ByName("obsidian")!;
        var portal = Blocks.Get("portal", ("portal_axis", "x"));
        for (var dx = -1; dx <= 2; dx++)
        for (var dz = -1; dz <= 1; dz++)
        {
            world.SetBlock(x + dx, y - 1, z + dz, obsidian);
            for (var dy = 0; dy <= 3; dy++)
                world.SetBlock(x + dx, y + dy, z + dz, Blocks.Air);
        }
        for (var dx = -1; dx <= 2; dx++)
        for (var dy = -1; dy <= 3; dy++)
        {
            var frame = dx is -1 or 2 || dy is -1 or 3;
            world.SetBlock(x + dx, y + dy, z, frame ? obsidian : portal);
        }
        return (x, y, z);
    }

    /// <summary>Flint and steel on an obsidian frame: fills it with portal blocks; false if there is no frame here.</summary>
    internal bool TryLightPortal(int x, int y, int z)
    {
        foreach (var axis in new[] { "x", "z" })
        {
            if (PortalInside(x, y, z, axis) is not { } inside)
                continue;
            var portal = Blocks.Get("portal", ("portal_axis", axis));
            foreach (var (px, py, pz) in inside)
                _server.SetBlockAndUpdate(px, py, pz, portal, World);
            return true;
        }
        return false;
    }

    // The air inside an obsidian frame (inside 2x3 to 21x21) around (x, y, z), along one axis.
    private List<(int X, int Y, int Z)>? PortalInside(int x, int y, int z, string axis)
    {
        int ax = axis == "x" ? 1 : 0, az = axis == "z" ? 1 : 0;
        bool Obsidian(int bx, int by, int bz) => World.GetBlock(bx, by, bz)?.Name == "minecraft:obsidian";
        bool Open(int bx, int by, int bz) => World.GetBlock(bx, by, bz) is { } b && (b.IsAir || b.Name == "minecraft:fire");
        var bottom = y;
        while (bottom > y - 21 && Open(x, bottom - 1, z))
            bottom--;
        if (!Obsidian(x, bottom - 1, z))
            return null;
        int left = 0, right = 0;
        while (left < 21 && Open(x - ax * (left + 1), bottom, z - az * (left + 1)))
            left++;
        while (right < 21 && Open(x + ax * (right + 1), bottom, z + az * (right + 1)))
            right++;
        var width = left + right + 1;
        if (width is < 2 or > 21 || !Obsidian(x - ax * (left + 1), bottom, z - az * (left + 1))
            || !Obsidian(x + ax * (right + 1), bottom, z + az * (right + 1)))
            return null;
        int startX = x - ax * left, startZ = z - az * left;
        var height = 0;
        while (height < 22 && Open(startX, bottom + height, startZ))
            height++;
        if (height is < 3 or > 21)
            return null;
        var inside = new List<(int, int, int)>();
        for (var w = 0; w < width; w++)
        {
            int bx = startX + ax * w, bz = startZ + az * w;
            if (!Obsidian(bx, bottom - 1, bz) || !Obsidian(bx, bottom + height, bz))
                return null;
            for (var h = 0; h < height; h++)
            {
                if (!Open(bx, bottom + h, bz))
                    return null;
                inside.Add((bx, bottom + h, bz));
            }
        }
        for (var h = 0; h < height; h++)
            if (!Obsidian(startX - ax, bottom + h, startZ - az) || !Obsidian(startX + ax * width, bottom + h, startZ + az * width))
                return null;
        return inside;
    }

    /// <summary>A portal block or its frame was broken: the whole portal goes out.</summary>
    internal void BreakPortal(int x, int y, int z)
    {
        var queue = new Queue<(int, int, int)>();
        foreach (var (dx, dy, dz) in new[] { (0, 0, 0), (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1) })
            queue.Enqueue((x + dx, y + dy, z + dz));
        var seen = new HashSet<(int, int, int)>();
        while (queue.Count > 0 && seen.Count < 1000)
        {
            var (bx, by, bz) = queue.Dequeue();
            if (!seen.Add((bx, by, bz)) || !IsPortal(World.GetBlock(bx, by, bz)))
                continue;
            _server.SetBlockAndUpdate(bx, by, bz, Blocks.Air, World);
            foreach (var (dx, dy, dz) in new[] { (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1) })
                queue.Enqueue((bx + dx, by + dy, bz + dz));
        }
    }
}
