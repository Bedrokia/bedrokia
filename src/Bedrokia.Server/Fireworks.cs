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
using Bedrokia.Server.Network;

namespace Bedrokia.Server;

/// <summary>
/// Firework rockets as PocketMine (entity/object/FireworkRocket.php, item/FireworkRocket.php): a real entity that
/// speeds up every tick (+0.04 up, sideways x1.15) for (flight + 1) x 10 + 0..12 ticks, then bursts where it really
/// is. The client draws the stars from the entity's firework NBT; each star adds a sound and 2 damage to the living
/// within 5 blocks that the burst reaches (feet or mid-body not behind a block).
/// </summary>
public sealed partial class BedrokiaServer
{
    private sealed class Rocket(long id, World world, double x, double y, double z, int lifetime, NbtList? explosions)
    {
        public readonly long Id = id;
        public readonly World World = world;
        public readonly int Lifetime = lifetime;
        public readonly NbtList? Explosions = explosions;
        public double X = x, Y = y, Z = z, Vx, Vy = 0.05, Vz;
        public int Ticks;
    }

    private readonly List<Rocket> _rockets = [];

    /// <summary>Launches the firework <paramref name="rocket"/> item from (x, y, z), straight up.</summary>
    public void LaunchFirework(double x, double y, double z, ItemStack rocket, World? world = null)
    {
        world ??= World;
        var fireworks = rocket.Nbt?.GetValueOrDefault("Fireworks") as OrderedDictionary<string, object>;
        var flight = fireworks?.GetValueOrDefault("Flight") is byte f ? f : 1;
        var r = Random.Shared;
        var launched = new Rocket(NextEntityId(), world, x, y, z, (flight + 1) * 10 + r.Next(13), fireworks?.GetValueOrDefault("Explosions") as NbtList)
        {
            Vx = (r.NextDouble() - r.NextDouble()) * 0.0023,
            Vz = (r.NextDouble() - r.NextDouble()) * 0.0023,
        };
        _rockets.Add(launched);
        // Only the stars go in the metadata, as PocketMine: the client needs nothing else to draw the burst.
        var display = new OrderedDictionary<string, object>
        {
            ["Fireworks"] = new OrderedDictionary<string, object> { ["Explosions"] = launched.Explosions ?? new NbtList(10, []) },
        };
        BroadcastIn(world, EntityPackets.AddActor(launched.Id, "minecraft:fireworks_rocket", x, y, z, r.NextSingle() * 360, 1, 1, 0,
            0.25f, 0.25f, 0, displayNbt: display, pitch: 90));
        BroadcastIn(world, EntityPackets.Motion(launched.Id, launched.Vx, launched.Vy, launched.Vz));
        PlaySound("firework.launch", x, y, z, world);
    }

    private void TickFireworks()
    {
        for (var i = _rockets.Count - 1; i >= 0; i--)
        {
            var rocket = _rockets[i];
            rocket.Ticks++;
            if (rocket.Ticks < 60) // PocketMine: plugin rockets living longer stop speeding up
            {
                rocket.Vx *= 1.15;
                rocket.Vy += 0.04;
                rocket.Vz *= 1.15;
            }
            var (nx, ny, nz) = (rocket.X + rocket.Vx, rocket.Y + rocket.Vy, rocket.Z + rocket.Vz);
            if (BlockPhysics.IsSolid(rocket.World.GetBlock((int)Math.Floor(nx), (int)Math.Floor(ny), (int)Math.Floor(nz))))
                (rocket.Vx, rocket.Vy, rocket.Vz) = (0, 0, 0); // stopped by a ceiling, bursts there
            else
                (rocket.X, rocket.Y, rocket.Z) = (nx, ny, nz);
            BroadcastInChunk(rocket.World, (int)Math.Floor(rocket.X) >> 4, (int)Math.Floor(rocket.Z) >> 4,
                EntityPackets.MoveAbsolute(rocket.Id, rocket.X, rocket.Y, rocket.Z, 90, 0, false));
            if (rocket.Ticks < rocket.Lifetime)
                continue;
            _rockets.RemoveAt(i);
            Burst(rocket);
        }
    }

    private void Burst(Rocket rocket)
    {
        var (world, x, y, z) = (rocket.World, rocket.X, rocket.Y, rocket.Z);
        if (Events.HasFireworkExplode)
        {
            var explodeEv = new FireworkExplodeEvent(world, x, y, z, rocket.Explosions);
            if (!Events.OnFireworkExplode(explodeEv)) return;
        }
        var stars = rocket.Explosions?.Values.OfType<OrderedDictionary<string, object>>().ToList() ?? [];
        if (stars.Count > 0)
        {
            BroadcastIn(world, EntityPackets.Event(rocket.Id, 25)); // firework particles
            foreach (var star in stars)
            {
                PlaySound(star.GetValueOrDefault("FireworkType") is (byte)1 ? "firework.large_blast" : "firework.blast", x, y, z, world);
                if (star.GetValueOrDefault("FireworkFlicker") is (byte)1)
                    PlaySound("firework.twinkle", x, y, z, world);
            }
        }
        BroadcastIn(world, Packets.RemoveActor(rocket.Id));
        if (stars.Count == 0)
            return;
        var force = stars.Count * 2 + 5;
        foreach (var player in _players.Where(p => p.IsInGame && p.World == world && !p.Dead).ToList())
        {
            var feet = player.Position.Y - BedrockConnection.EyeHeight;
            if (BurstDamage(world, x, y, z, force, player.Position.X, feet, player.Position.Z, 1.8) is { } damage)
                player.Damage(damage, x, z, source: DamageSource.Explosion());
        }
        foreach (var mob in Mobs.All.ToList())
            if (mob.World == world && !mob.Dead && BurstDamage(world, x, y, z, force, mob.X, mob.Y, mob.Z, mob.Height) is { } damage)
                Mobs.Damage(mob, damage, (x, z), source: DamageSource.Explosion());
    }

    // PocketMine: within 5 blocks, a ray to the feet or to mid-body must be clear; force x sqrt((5 - distance) / 5).
    private static float? BurstDamage(World world, double x, double y, double z, int force, double ex, double ey, double ez, double height)
    {
        var distance = Math.Sqrt((ex - x) * (ex - x) + (ey - y) * (ey - y) + (ez - z) * (ez - z));
        if (distance > 5)
            return null;
        if (RayBlocked(world, x, y, z, ex, ey, ez) && RayBlocked(world, x, y, z, ex, ey + height / 2, ez))
            return null;
        return (float)(force * Math.Sqrt((5 - distance) / 5));
    }
}

