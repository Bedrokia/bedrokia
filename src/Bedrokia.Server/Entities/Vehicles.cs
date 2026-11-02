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

using System.Text.Json;
using Bedrokia.Protocol;
using Bedrokia.Server.Core;
using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Entities;

/// <summary>A boat or a minecart. Positions are the bottom centre; game thread only.</summary>
public sealed class Vehicle(long entityId, string identifier, string item, World world, double x, double y, double z)
{
    public long EntityId { get; } = entityId;

    /// <summary>Entity type: minecraft:boat, minecraft:chest_boat, minecraft:minecart, minecraft:chest_minecart...</summary>
    public string Identifier { get; } = identifier;

    /// <summary>The item it drops when broken (oak_boat, minecart...).</summary>
    public string Item { get; } = item;

    public World World { get; internal set; } = world;
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Z { get; set; } = z;
    public float Yaw { get; set; }
    public double Vx, Vy, Vz;

    /// <summary>Boat wood (0 oak, 1 spruce, 2 birch, 3 jungle, 4 acacia, 5 dark oak, 6 mangrove, 7 bamboo, 8 cherry, 9 pale oak).</summary>
    public int Variant { get; init; }

    public BedrockConnection? Rider { get; internal set; }

    public bool IsBoat => Identifier.Contains("boat");
    public bool IsMinecart => Identifier.Contains("minecart");

    /// <summary>Only plain boats, chest boats and minecarts carry a player.</summary>
    public bool Rideable => Identifier is "minecraft:boat" or "minecraft:chest_boat" or "minecraft:minecart";

    internal int Hits, HurtTicks;
    internal bool OnGround;
    internal double Speed; // minecart: blocks per tick along the rail
    internal (int X, int Z) Heading = (0, 1);
    internal bool PastCentre; // minecart on a curve: already turned
    internal (double X, double Y, double Z, float Yaw) LastSent = (double.NaN, 0, 0, 0);
    internal readonly HashSet<BedrockConnection> Viewers = [];

    public float Width => IsBoat ? 1.4f : 0.98f;
    public float Height => IsBoat ? 0.455f : 0.7f;

    public byte[] AddPacket() =>
        EntityPackets.AddActor(EntityId, Identifier, X, Y, Z, Yaw, 1, 1, 0, Width, Height, EntityPackets.FlagHasCollision | EntityPackets.FlagGravity,
            variant: IsBoat ? Variant : null);
}

/// <summary>
/// Boats and minecarts: placed from their item, ridden (one player), broken in a few hits. Boats float and are steered
/// by their rider's client; minecarts follow rails (curves, slopes, powered rails) pushed by their rider. Players also
/// ride saddled pigs, striders, horses, donkeys, mules and camels (<see cref="MobManager"/>). Saved with the world
/// (vehicles.json).
/// </summary>
/// <remarks>ponytail: one passenger; chest boats and chest, hopper, TNT and command block minecarts have no inventory or
/// behaviour of their own; minecarts do not collide with each other or with mobs.</remarks>
public sealed class VehicleManager(BedrokiaServer server)
{
    private const uint SetActorLinkId = 41;
    private const double MaxCartSpeed = 0.4, SlopeAcceleration = 0.0078125;
    public const float BoatSeat = 1.02f, CartSeat = 1.02f;

    private readonly List<Vehicle> _vehicles = [];
    private readonly Random _random = new();

    public IReadOnlyList<Vehicle> All => _vehicles;

    public Vehicle? Find(long entityId) => _vehicles.FirstOrDefault(v => v.EntityId == entityId);

    /// <summary>The entity a vehicle item places, and the boat wood; null for other items.</summary>
    public static (string Identifier, int Variant)? ForItem(string item)
    {
        var name = item.StartsWith("minecraft:") ? item["minecraft:".Length..] : item;
        switch (name)
        {
            case "minecart" or "chest_minecart" or "hopper_minecart" or "tnt_minecart" or "command_block_minecart":
                return ("minecraft:" + name, 0);
            case "bamboo_raft":
                return ("minecraft:boat", 7);
            case "bamboo_chest_raft":
                return ("minecraft:chest_boat", 7);
        }
        var chest = name.EndsWith("_chest_boat");
        if (!chest && !name.EndsWith("_boat"))
            return null;
        var wood = name[..name.IndexOf(chest ? "_chest_boat" : "_boat", StringComparison.Ordinal)];
        var variant = Array.IndexOf<string>(["oak", "spruce", "birch", "jungle", "acacia", "dark_oak", "mangrove", "bamboo", "cherry", "pale_oak"], wood);
        return variant < 0 ? null : (chest ? "minecraft:chest_boat" : "minecraft:boat", variant);
    }

    /// <summary>Places the vehicle of <paramref name="item"/> at (x, y, z); null if it is not a vehicle item.</summary>
    public Vehicle? Place(string item, World world, double x, double y, double z, float yaw)
    {
        if (ForItem(item) is not var (identifier, variant))
            return null;
        var vehicle = new Vehicle(server.NextEntityId(), identifier, item.StartsWith("minecraft:") ? item["minecraft:".Length..] : item, world, x, y, z)
        {
            Yaw = yaw, Variant = variant,
        };
        if (vehicle.IsMinecart)
        {
            // Heads where the player looks, along the rail.
            var rad = yaw * Math.PI / 180;
            vehicle.Heading = Math.Abs(Math.Sin(rad)) > Math.Abs(Math.Cos(rad)) ? (Math.Sin(rad) > 0 ? -1 : 1, 0) : (0, Math.Cos(rad) > 0 ? 1 : -1);
        }
        _vehicles.Add(vehicle);
        return vehicle;
    }

    // --- Riding ---

    internal static byte[] Link(long vehicle, long rider, bool mount) => new BedrockWriter().VarUInt32(SetActorLinkId)
        .VarInt64(vehicle).VarInt64(rider).Byte(mount ? (byte)1 : (byte)0) // 1 = rider, 0 = remove
        .Bool(true).Bool(true).Float32(0) // immediate, rider initiated, angular velocity
        .ToArray();

    /// <summary>Seats <paramref name="player"/> on <paramref name="vehicle"/> (a <see cref="Vehicle"/> or a <see cref="Mob"/>).</summary>
    public bool Mount(BedrockConnection player, object vehicle)
    {
        if (player.Riding is not null || player.Dead)
            return false;
        long id;
        float seat;
        switch (vehicle)
        {
            case Vehicle v when v.Rideable && v.Rider is null && v.World == player.World:
                v.Rider = player;
                id = v.EntityId;
                seat = v.IsBoat ? BoatSeat : CartSeat;
                break;
            case Mob m when m.Rider is null && m.World == player.World:
                m.Rider = player;
                id = m.EntityId;
                seat = MobManager.SeatHeight(m.Kind);
                break;
            default:
                return false;
        }
        player.Riding = vehicle;
        server.BroadcastIn(player.World, Link(id, player.EntityId, mount: true));
        player.SendRidingData(seat);
        return true;
    }

    /// <summary>Gets <paramref name="player"/> off what they ride, next to it (or where they are: death, leaving).</summary>
    public void Dismount(BedrockConnection player, bool teleport = true)
    {
        if (player.Riding is not { } vehicle)
            return;
        player.Riding = null;
        var (id, x, y, z) = vehicle switch
        {
            Vehicle v => (v.EntityId, v.X, v.Y, v.Z),
            Mob m => (m.EntityId, m.X, m.Y, m.Z),
            _ => (0L, 0.0, 0.0, 0.0),
        };
        if (vehicle is Vehicle vh)
            vh.Rider = null;
        else if (vehicle is Mob mob)
            mob.Rider = null;
        server.BroadcastIn(player.World, Link(id, player.EntityId, mount: false));
        player.SendRidingData(null);
        if (teleport && player.IsInGame && !player.Dead)
            player.Teleport((float)x, (float)y + 1, (float)z);
    }

    /// <summary>
    /// The rider's input each tick: boats and client-steered mounts (horses, donkeys, mules, camels) follow the
    /// position its client predicts; a minecart is pushed by the forward key; a pig or strider goes where its rider
    /// looks while they hold the stick with its bait.
    /// </summary>
    internal void RiderInput(BedrockConnection player, float x, float y, float z, float yaw, float moveX, float moveZ)
    {
        switch (player.Riding)
        {
            case Vehicle { IsBoat: true } boat:
                (boat.X, boat.Y, boat.Z, boat.Yaw) = (x, y - BoatSeat, z, yaw);
                boat.Vx = boat.Vy = boat.Vz = 0;
                break;
            case Vehicle { IsMinecart: true } cart when Math.Abs(moveZ) > 0.1:
                var rad = yaw * Math.PI / 180;
                double lookX = -Math.Sin(rad), lookZ = Math.Cos(rad);
                var along = (lookX * cart.Heading.X + lookZ * cart.Heading.Z) * Math.Sign(moveZ);
                if (cart.Speed < 0.01 && along < 0)
                {
                    cart.Heading = (-cart.Heading.X, -cart.Heading.Z); // starts the other way
                    along = -along;
                }
                cart.Speed = Math.Min(MaxCartSpeed, cart.Speed + 0.01 * Math.Sign(along));
                break;
            case Mob mob:
                server.Mobs.RiderInput(mob, player, x, y, z, yaw);
                break;
        }
    }

    // --- Breaking ---

    /// <summary>A player hits a vehicle: it breaks after a few hits (at once in creative), dropping its item.</summary>
    public void Hit(BedrockConnection player, Vehicle vehicle)
    {
        if (vehicle.HurtTicks > 0)
            return;
        vehicle.HurtTicks = 10;
        vehicle.Hits++;
        Broadcast(vehicle, EntityPackets.Event(vehicle.EntityId, EntityPackets.EventHurt));
        if (player.GameMode != GameMode.Creative && vehicle.Hits < 3)
            return;
        if (vehicle.Rider is { } rider)
            Dismount(rider);
        Remove(vehicle);
        if (player.GameMode != GameMode.Creative && ItemStack.Of(vehicle.Item) is { } drop)
            server.DropItem(drop, (float)vehicle.X, (float)vehicle.Y + 0.5f, (float)vehicle.Z, world: vehicle.World);
    }

    private void Remove(Vehicle vehicle)
    {
        _vehicles.Remove(vehicle);
        Broadcast(vehicle, Packets.RemoveActor(vehicle.EntityId));
        vehicle.Viewers.Clear();
    }

    // --- Simulation ---

    public void Tick(long tick)
    {
        foreach (var vehicle in _vehicles.ToList())
        {
            if (vehicle.World.LoadedChunk((int)Math.Floor(vehicle.X) >> 4, (int)Math.Floor(vehicle.Z) >> 4) is null)
                continue; // waits for its chunk
            if (vehicle.HurtTicks > 0)
                vehicle.HurtTicks--;
            if (vehicle.Rider is { } rider && (!rider.IsInGame || rider.Dead || rider.World != vehicle.World))
            {
                vehicle.Rider = null;
                rider.Riding = null;
            }
            if (vehicle.IsMinecart)
                MoveCart(vehicle);
            else if (vehicle.Rider is null)
                Float(vehicle);
            if (vehicle.Y < Chunk.MinY - 32)
                Remove(vehicle);
        }
        if (tick % 2 == 0)
            foreach (var vehicle in _vehicles)
                Sync(vehicle);
    }

    // A boat without its rider floats on water, drifts to a stop and falls when there is no water under it.
    private void Float(Vehicle boat)
    {
        var world = boat.World;
        var inWater = Water(world.GetBlock((int)Math.Floor(boat.X), (int)Math.Floor(boat.Y + 0.1), (int)Math.Floor(boat.Z)));
        boat.Vy = inWater ? Math.Min(boat.Vy + 0.04, 0.08) : boat.Vy - 0.04;
        if (!inWater && Water(world.GetBlock((int)Math.Floor(boat.X), (int)Math.Floor(boat.Y - 0.05), (int)Math.Floor(boat.Z))))
            boat.Vy = Math.Max(boat.Vy, 0); // floating at the surface
        var ny = boat.Y + boat.Vy;
        if (Solid(world, boat.X, ny, boat.Z))
        {
            boat.Y = Math.Floor(ny) + 1;
            boat.Vy = 0;
            boat.OnGround = true;
        }
        else
        {
            boat.Y = ny;
            boat.OnGround = false;
        }
        if (!Solid(world, boat.X + boat.Vx, boat.Y + 0.1, boat.Z))
            boat.X += boat.Vx;
        if (!Solid(world, boat.X, boat.Y + 0.1, boat.Z + boat.Vz))
            boat.Z += boat.Vz;
        var drag = boat.OnGround ? 0.5 : 0.9;
        boat.Vx *= drag;
        boat.Vz *= drag;
    }

    /// <summary>
    /// A minecart on a rail runs along it: straight on, turning at the centre of curves, up and down slopes (gravity
    /// along them), boosted by on powered rails and stopped by off ones; off rails it falls and slides to a stop.
    /// </summary>
    internal void MoveCart(Vehicle cart)
    {
        var world = cart.World;
        int bx = (int)Math.Floor(cart.X), by = (int)Math.Floor(cart.Y + 0.01), bz = (int)Math.Floor(cart.Z);
        var rail = world.GetBlock(bx, by, bz);
        if (!Rails.IsRail(rail) && Rails.IsRail(world.GetBlock(bx, by - 1, bz)))
            rail = world.GetBlock(bx, --by, bz);
        if (!Rails.IsRail(rail))
        {
            // Off the rails: gravity and friction.
            cart.Vy -= 0.04;
            var ny = cart.Y + cart.Vy;
            if (Solid(world, cart.X, ny, cart.Z))
            {
                cart.Y = Math.Floor(ny) + 1;
                cart.Vy = 0;
                cart.Speed *= 0.5;
            }
            else
                cart.Y = ny;
            var nx = cart.X + cart.Heading.X * cart.Speed;
            var nz = cart.Z + cart.Heading.Z * cart.Speed;
            if (!Solid(world, nx, cart.Y + 0.1, nz))
                (cart.X, cart.Z) = (nx, nz);
            else
                cart.Speed = 0;
            cart.Speed *= 0.95;
            return;
        }

        var shape = Rails.Shape(rail!);
        var exits = Rails.Exits(shape);
        cart.Vy = 0;

        // Which way along this rail: the exit the cart heads to (not the one it came from).
        (int X, int Y, int Z) Towards((int X, int Z) heading) =>
            exits[0].X * heading.X + exits[0].Z * heading.Z >= exits[1].X * heading.X + exits[1].Z * heading.Z ? exits[0] : exits[1];

        if (Rails.IsCurve(shape))
        {
            // Run to the centre first, then turn.
            double cx = bx + 0.5, cz = bz + 0.5;
            var toCentre = Math.Sqrt((cx - cart.X) * (cx - cart.X) + (cz - cart.Z) * (cz - cart.Z));
            if (!cart.PastCentre && toCentre > cart.Speed && cart.Speed > 0)
            {
                cart.X += (cx - cart.X) / toCentre * cart.Speed;
                cart.Z += (cz - cart.Z) / toCentre * cart.Speed;
                cart.Y = by;
                Friction(cart);
                return;
            }
            if (!cart.PastCentre)
            {
                // At the centre: leave by the side the cart did not come in from.
                var cameFrom = (-cart.Heading.X, -cart.Heading.Z);
                var exit = exits[0].X == cameFrom.Item1 && exits[0].Z == cameFrom.Item2 ? exits[1] : exits[0];
                cart.Heading = (exit.X, exit.Z);
                cart.PastCentre = true;
                (cart.X, cart.Z) = (cx, cz);
            }
        }
        else
        {
            cart.PastCentre = false;
            var exit = Towards(cart.Heading);
            cart.Heading = (exit.X, exit.Z);
            // Stay on the rail's centre line.
            if (cart.Heading.X == 0)
                cart.X = bx + 0.5;
            else
                cart.Z = bz + 0.5;
        }

        // Gravity along slopes.
        if (Rails.IsSlope(shape))
        {
            var up = exits[0].Y == 1 ? exits[0] : exits[1];
            var climbing = up.X == cart.Heading.X && up.Z == cart.Heading.Z;
            cart.Speed += climbing ? -SlopeAcceleration : SlopeAcceleration;
            if (cart.Speed < 0)
            {
                cart.Speed = -cart.Speed; // rolls back down
                cart.Heading = (-cart.Heading.X, -cart.Heading.Z);
            }
        }
        if (rail!.Name == "minecraft:golden_rail")
        {
            if (Rails.Powered(world, bx, by, bz, rail))
                cart.Speed = cart.Speed < 0.02 ? 0.1 : cart.Speed + 0.06;
            else
                cart.Speed = cart.Speed < 0.03 ? 0 : cart.Speed * 0.5;
        }
        cart.Speed = Math.Min(MaxCartSpeed, cart.Speed);

        var nextX = cart.X + cart.Heading.X * cart.Speed;
        var nextZ = cart.Z + cart.Heading.Z * cart.Speed;
        // A wall ends the run (on a slope the block ahead is the one the next rail stands on).
        if (!Rails.IsSlope(shape) && Solid(world, nextX, by + 0.1, nextZ))
        {
            cart.Speed = 0;
            return;
        }
        (cart.X, cart.Z) = (nextX, nextZ);
        cart.Y = by + SlopeHeight(shape, cart.X - bx, cart.Z - bz);
        if ((int)Math.Floor(cart.X) != bx || (int)Math.Floor(cart.Z) != bz)
            cart.PastCentre = false;
        cart.Yaw = (float)(Math.Atan2(-cart.Heading.X, cart.Heading.Z) * 180 / Math.PI);
        Friction(cart);
    }

    // Height above the rail block along a slope (0 on flat rails), from the position inside the block.
    internal static double SlopeHeight(int shape, double fx, double fz) => shape switch
    {
        2 => Math.Clamp(fx, 0, 1), // ascending east
        3 => Math.Clamp(1 - fx, 0, 1), // west
        4 => Math.Clamp(1 - fz, 0, 1), // north
        5 => Math.Clamp(fz, 0, 1), // south
        _ => 0,
    };

    private static void Friction(Vehicle cart) => cart.Speed *= cart.Rider is null ? 0.96 : 0.997;

    private static bool Water(BlockType? block) => block?.Name is "minecraft:water" or "minecraft:flowing_water";

    private static bool Solid(World world, double x, double y, double z) =>
        BlockPhysics.IsSolid(world.GetBlock((int)Math.Floor(x), (int)Math.Floor(y), (int)Math.Floor(z)));

    // --- Visibility ---

    private void Sync(Vehicle vehicle)
    {
        foreach (var player in server.Players)
        {
            var visible = player.IsInGame && player.World == vehicle.World
                          && Math.Abs(player.Position.X - vehicle.X) < MobManager.ViewDistance && Math.Abs(player.Position.Z - vehicle.Z) < MobManager.ViewDistance;
            if (visible && vehicle.Viewers.Add(player))
            {
                player.Send(vehicle.AddPacket());
                if (vehicle.Rider is { } rider)
                    player.Send(Link(vehicle.EntityId, rider.EntityId, mount: true));
            }
            else if (!visible && vehicle.Viewers.Remove(player))
                player.Send(Packets.RemoveActor(vehicle.EntityId));
        }
        var (lx, ly, lz, lyaw) = vehicle.LastSent;
        if (Math.Abs(vehicle.X - lx) + Math.Abs(vehicle.Y - ly) + Math.Abs(vehicle.Z - lz) < 0.001 && Math.Abs(vehicle.Yaw - lyaw) < 0.5)
            return;
        vehicle.LastSent = (vehicle.X, vehicle.Y, vehicle.Z, vehicle.Yaw);
        var move = EntityPackets.MoveAbsolute(vehicle.EntityId, vehicle.X, vehicle.Y, vehicle.Z, 0, vehicle.Yaw, vehicle.OnGround);
        foreach (var viewer in vehicle.Viewers)
            if (viewer != vehicle.Rider || vehicle.IsMinecart) // a boat's rider already sees where it steers
                viewer.Send(move);
    }

    private static void Broadcast(Vehicle vehicle, byte[] packet)
    {
        foreach (var viewer in vehicle.Viewers)
            viewer.Send(packet);
    }

    /// <summary>A player left: off their vehicle, forgotten as a viewer.</summary>
    public void Forget(BedrockConnection player)
    {
        foreach (var vehicle in _vehicles)
        {
            vehicle.Viewers.Remove(player);
            if (vehicle.Rider == player)
                vehicle.Rider = null;
        }
        player.Riding = null;
    }

    // --- Saving ---

    private sealed record SavedVehicle(string Identifier, string Item, double X, double Y, double Z, float Yaw, int Variant);

    public void Load(World world)
    {
        var path = Path.Combine(world.Folder, "vehicles.json");
        if (!File.Exists(path))
            return;
        try
        {
            foreach (var s in JsonSerializer.Deserialize<List<SavedVehicle>>(File.ReadAllBytes(path)) ?? [])
                _vehicles.Add(new Vehicle(server.NextEntityId(), s.Identifier, s.Item, world, s.X, s.Y, s.Z) { Yaw = s.Yaw, Variant = s.Variant });
        }
        catch (Exception e) when (e is JsonException or IOException)
        {
            server.Logger.Warn($"Could not read {path} ({e.Message}): its boats and minecarts are lost.");
        }
    }

    public void Save(World world)
    {
        var path = Path.Combine(world.Folder, "vehicles.json");
        var saved = _vehicles.Where(v => v.World == world).Select(v => new SavedVehicle(v.Identifier, v.Item, v.X, v.Y, v.Z, v.Yaw, v.Variant)).ToList();
        if (saved.Count == 0 && !File.Exists(path))
            return;
        File.WriteAllBytes(path + ".tmp", JsonSerializer.SerializeToUtf8Bytes(saved));
        File.Move(path + ".tmp", path, overwrite: true);
    }
}
