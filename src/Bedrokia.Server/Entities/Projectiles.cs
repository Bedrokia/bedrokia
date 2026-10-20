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

using System.Numerics;
using Bedrokia.Server.Events;
using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Entities;

/// <summary>A thrown snowball, egg, ender pearl or splash potion, or an arrow or fireball shot by a mob.</summary>
public sealed class Projectile
{
    internal Projectile(long entityId, string identifier, World world, BedrockConnection? owner, Vector3 position, Vector3 velocity, short potion)
    {
        EntityId = entityId;
        Identifier = identifier;
        World = world;
        Owner = owner;
        Position = position;
        Velocity = velocity;
        Potion = potion;
    }

    public long EntityId { get; }
    public string Identifier { get; }
    public World World { get; }
    public BedrockConnection? Owner { get; }
    public Vector3 Position { get; internal set; }
    public Vector3 Velocity { get; internal set; }

    /// <summary>The potion id (item meta) of a splash potion.</summary>
    public short Potion { get; }

    /// <summary>The mob that shot it (skeleton arrow, ghast or blaze fireball), or null.</summary>
    public Mob? Shooter { get; internal init; }

    /// <summary>Damage done to what it hits (arrows, small fireballs).</summary>
    public float Damage { get; internal init; }

    internal int Ticks;
}

/// <summary>
/// Projectiles (<see cref="BedrokiaServer.Projectiles"/>): players throw snowballs, eggs, ender pearls and splash
/// potions with a right click, plugins with <see cref="Launch"/>; skeletons shoot arrows, ghasts fireballs (they explode)
/// and blazes small fireballs (<see cref="Shoot"/>). Gravity and drag as vanilla: throwables 0.03, arrows 0.05,
/// fireballs none; drag 0.01.
/// ponytail: no chickens from eggs, no potion effects on mobs, arrows do not stay stuck in blocks.
/// </summary>
public sealed class ProjectileManager(BedrokiaServer server)
{
    public const string Snowball = "minecraft:snowball", Egg = "minecraft:egg", EnderPearl = "minecraft:ender_pearl",
        SplashPotion = "minecraft:splash_potion", Arrow = "minecraft:arrow", Fireball = "minecraft:fireball",
        SmallFireball = "minecraft:small_fireball";

    private const float Gravity = 0.03f, ArrowGravity = 0.05f, Drag = 0.99f, SplashRadius = 4;
    private const int MaxTicks = 1200;

    private readonly List<Projectile> _projectiles = [];

    public IReadOnlyList<Projectile> All => _projectiles;

    /// <summary>True for the items a right click throws.</summary>
    public static bool IsThrowable(ItemStack? item) => item?.Type.Name is Snowball or Egg or EnderPearl or SplashPotion;

    /// <summary>
    /// Throws a projectile from the player's eyes where they look; <paramref name="speed"/> 1.5 is a hand throw.
    /// Returns null when a plugin cancelled <see cref="ServerEvents.ProjectileLaunch"/>.
    /// </summary>
    public Projectile? Launch(BedrockConnection player, string identifier, float speed = 1.5f, short potion = 0)
    {
        if (server.Events.HasProjectileLaunch)
        {
            var launch = new ProjectileLaunchEvent(player, identifier, speed);
            if (!server.Events.OnProjectileLaunch(launch))
                return null;
            speed = launch.Force;
        }
        var yaw = player.Yaw * MathF.PI / 180;
        var pitch = player.Pitch * MathF.PI / 180;
        var direction = new Vector3(-MathF.Sin(yaw) * MathF.Cos(pitch), -MathF.Sin(pitch), MathF.Cos(yaw) * MathF.Cos(pitch));
        var projectile = Spawn(identifier, player.World, new Vector3(player.Position.X, player.Position.Y - 0.1f, player.Position.Z),
            direction * speed, player, potion);
        server.PlaySound("random.bow", player.Position.X, player.Position.Y, player.Position.Z, player.World, 0.5f, 0.4f);
        return projectile;
    }

    /// <summary>A projectile from anywhere (a dispenser, a trap...), with its velocity in blocks per tick.</summary>
    public Projectile Spawn(string identifier, World world, Vector3 position, Vector3 velocity, BedrockConnection? owner = null, short potion = 0) =>
        Add(new Projectile(server.NextEntityId(), identifier, world, owner, position, velocity, potion));

    /// <summary>A mob shoots: an <see cref="Arrow"/>, a <see cref="Fireball"/> (explodes) or a <see cref="SmallFireball"/>.</summary>
    public Projectile Shoot(Mob shooter, string identifier, Vector3 position, Vector3 velocity, float damage) =>
        Add(new Projectile(server.NextEntityId(), identifier, shooter.World!, null, position, velocity, 0) { Shooter = shooter, Damage = damage });

    private Projectile Add(Projectile projectile)
    {
        _projectiles.Add(projectile);
        var (position, velocity, identifier) = (projectile.Position, projectile.Velocity, projectile.Identifier);
        var size = identifier == Fireball ? 1f : 0.25f;
        var yaw = MathF.Atan2(-velocity.X, velocity.Z) * 180 / MathF.PI;
        server.BroadcastIn(projectile.World,
            EntityPackets.AddActor(projectile.EntityId, identifier, position.X, position.Y, position.Z, yaw,
                1, 1, 0, size, size, GravityOf(identifier) > 0 ? EntityPackets.FlagGravity : 0,
                variant: identifier == SplashPotion ? projectile.Potion : null));
        server.BroadcastIn(projectile.World, EntityPackets.Motion(projectile.EntityId, velocity.X, velocity.Y, velocity.Z));
        return projectile;
    }

    private static float GravityOf(string identifier) => identifier switch
    {
        Arrow => ArrowGravity,
        Fireball or SmallFireball => 0,
        _ => Gravity,
    };

    internal void Tick()
    {
        for (var i = _projectiles.Count - 1; i >= 0; i--)
        {
            var p = _projectiles[i];
            if (++p.Ticks > MaxTicks || p.Position.Y < Chunk.MinY - 64
                || p.World.LoadedChunk((int)MathF.Floor(p.Position.X) >> 4, (int)MathF.Floor(p.Position.Z) >> 4) is null)
            {
                Remove(i);
                continue;
            }
            var from = p.Position;
            var to = from + p.Velocity;
            if (FindHit(p, from, to) is { } hit)
            {
                Remove(i);
                OnHit(p, hit.At, hit.Player, hit.Mob);
                continue;
            }
            p.Position = to;
            p.Velocity = p.Identifier is Fireball or SmallFireball ? p.Velocity : p.Velocity * Drag - new Vector3(0, GravityOf(p.Identifier), 0);
            if (p.Ticks % 2 == 0)
            {
                var v = p.Velocity;
                var pitch = -MathF.Atan2(v.Y, MathF.Sqrt(v.X * v.X + v.Z * v.Z)) * 180 / MathF.PI;
                server.BroadcastIn(p.World, EntityPackets.MoveAbsolute(p.EntityId, to.X, to.Y, to.Z, pitch, MathF.Atan2(-v.X, v.Z) * 180 / MathF.PI, false));
            }
        }
    }

    // Walks the segment in quarter blocks: the first solid block, player or mob touched.
    private (Vector3 At, BedrockConnection? Player, Mob? Mob)? FindHit(Projectile p, Vector3 from, Vector3 to)
    {
        var steps = Math.Max(1, (int)MathF.Ceiling(Vector3.Distance(from, to) / 0.25f));
        for (var s = 1; s <= steps; s++)
        {
            var at = Vector3.Lerp(from, to, (float)s / steps);
            foreach (var player in server.Players)
            {
                if (!player.IsInGame || player.World != p.World || player.Dead || player == p.Owner && p.Ticks < 5)
                    continue;
                var feet = player.Position.Y - BedrockConnection.EyeHeight;
                if (MathF.Abs(at.X - player.Position.X) < 0.45f && MathF.Abs(at.Z - player.Position.Z) < 0.45f && at.Y >= feet && at.Y <= feet + 1.9f)
                    return (at, player, null);
            }
            foreach (var mob in server.Mobs.All)
                if (!mob.Dead && mob.World == p.World && (mob != p.Shooter || p.Ticks > 20) && Math.Abs(at.X - mob.X) < mob.Width / 2 + 0.15 && Math.Abs(at.Z - mob.Z) < mob.Width / 2 + 0.15
                    && at.Y >= mob.Y && at.Y <= mob.Y + mob.Height)
                    return (at, null, mob);
            if (BlockPhysics.IsSolid(p.World.GetBlock((int)MathF.Floor(at.X), (int)MathF.Floor(at.Y), (int)MathF.Floor(at.Z))))
                return (at, null, null);
        }
        return null;
    }

    private void OnHit(Projectile p, Vector3 at, BedrockConnection? player, Mob? mob)
    {
        var target = player?.EntityId ?? mob?.EntityId ?? 0;
        if (server.Events.HasProjectileHit
            && !server.Events.OnProjectileHit(new ProjectileHitEvent((ulong)p.EntityId, p.Identifier, (ulong)(p.Owner?.EntityId ?? p.Shooter?.EntityId ?? 0), (ulong)target, at, 0)))
            return;
        var source = p.Shooter is { } shooter ? DamageSource.EntityAttack(shooter) : DamageSource.Custom(p.Identifier);
        switch (p.Identifier)
        {
            case Arrow or SmallFireball:
                player?.Damage(p.Damage, p.Position.X, p.Position.Z, source: source);
                if (mob is not null)
                    server.Mobs.Damage(mob, p.Damage, (p.Position.X, p.Position.Z), source: source);
                if (p.Identifier == SmallFireball && mob is not null && mob.Kind != MobKind.Blaze)
                    mob.FireTicks = Math.Max(mob.FireTicks, 100);
                if (p.Identifier == Arrow)
                    server.PlaySound("random.bowhit", at.X, at.Y, at.Z, p.World);
                break;
            case Fireball:
                player?.Damage(6, p.Position.X, p.Position.Z, source: source);
                server.Explode(at.X, at.Y, at.Z, 1, p.World);
                break;
            case Snowball or Egg:
                // No damage, only the knockback (vanilla); a snowball does 3 to blazes.
                player?.Damage(0, p.Position.X, p.Position.Z, source: p.Owner is { } thrower ? DamageSource.PlayerAttack(thrower) : null);
                if (mob is not null)
                    server.Mobs.Damage(mob, p.Identifier == Snowball && mob.Kind.Id == "minecraft:blaze" ? 3 : 0, (p.Position.X, p.Position.Z), p.Owner);
                break;
            case EnderPearl:
                if (p.Owner is { IsInGame: true, Dead: false } owner && owner.World == p.World)
                {
                    var back = at - Vector3.Normalize(p.Velocity) * 0.5f; // not inside the block it hit
                    owner.Teleport(back.X, back.Y, back.Z);
                    owner.Damage(5, back.X, back.Z, armour: false, source: DamageSource.Fall());
                }
                break;
            case SplashPotion:
                Splash(p, at);
                break;
        }
    }

    private void Splash(Projectile p, Vector3 at)
    {
        var radius = SplashRadius;
        if (server.Events.HasPotionSplash)
        {
            var splash = new PotionSplashEvent((ulong)p.EntityId, p.Potion, at, false, radius);
            if (!server.Events.OnPotionSplash(splash))
                return;
            radius = splash.Radius;
        }
        server.PlaySound("random.glass", at.X, at.Y, at.Z, p.World);
        if (StatusEffects.FromPotionMeta(p.Potion) is not { } effect)
            return;
        foreach (var player in server.Players.Where(pl => pl.IsInGame && pl.World == p.World && !pl.Dead).ToList())
        {
            var distance = Vector3.Distance(at, new Vector3(player.Position.X, player.Position.Y - BedrockConnection.EyeHeight + 0.9f, player.Position.Z));
            if (distance > radius)
                continue;
            // Closer is stronger, as vanilla: full at the centre, nothing at the edge (instant effects keep their level).
            var factor = 1 - distance / radius;
            var applied = effect.Clone();
            if (!applied.IsInstant)
                applied.DurationTicks = Math.Max(1, (int)(applied.DurationTicks * factor));
            player.AddEffect(applied, EffectChangeCause.Potion);
        }
    }

    private void Remove(int index)
    {
        var p = _projectiles[index];
        _projectiles.RemoveAt(index);
        server.BroadcastIn(p.World, Bedrokia.Protocol.Packets.RemoveActor(p.EntityId));
    }
}
