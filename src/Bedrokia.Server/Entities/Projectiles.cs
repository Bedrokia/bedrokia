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

/// <summary>A thrown snowball, egg, ender pearl or splash potion.</summary>
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

    internal int Ticks;
}

/// <summary>
/// Thrown projectiles (<see cref="BedrokiaServer.Projectiles"/>): players throw snowballs, eggs, ender pearls and splash
/// potions with a right click, plugins with <see cref="Launch"/>. Gravity and drag as vanilla throwables (0.03, 0.01).
/// ponytail: no chickens from eggs and no potion effects on mobs.
/// </summary>
public sealed class ProjectileManager(BedrokiaServer server)
{
    public const string Snowball = "minecraft:snowball", Egg = "minecraft:egg", EnderPearl = "minecraft:ender_pearl",
        SplashPotion = "minecraft:splash_potion";

    private const float Gravity = 0.03f, Drag = 0.99f, SplashRadius = 4;
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
    public Projectile Spawn(string identifier, World world, Vector3 position, Vector3 velocity, BedrockConnection? owner = null, short potion = 0)
    {
        var projectile = new Projectile(server.NextEntityId(), identifier, world, owner, position, velocity, potion);
        _projectiles.Add(projectile);
        server.BroadcastIn(world,
            EntityPackets.AddActor(projectile.EntityId, identifier, position.X, position.Y, position.Z, 0,
                1, 1, 0, 0.25f, 0.25f, EntityPackets.FlagGravity, variant: identifier == SplashPotion ? potion : null));
        server.BroadcastIn(world, EntityPackets.Motion(projectile.EntityId, velocity.X, velocity.Y, velocity.Z));
        return projectile;
    }

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
            p.Velocity = p.Velocity * Drag - new Vector3(0, Gravity, 0);
            if (p.Ticks % 2 == 0)
                server.BroadcastIn(p.World, EntityPackets.MoveAbsolute(p.EntityId, to.X, to.Y, to.Z, 0, 0, false));
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
                if (!mob.Dead && mob.World == p.World && Math.Abs(at.X - mob.X) < mob.Width / 2 + 0.15 && Math.Abs(at.Z - mob.Z) < mob.Width / 2 + 0.15
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
            && !server.Events.OnProjectileHit(new ProjectileHitEvent((ulong)p.EntityId, p.Identifier, (ulong)(p.Owner?.EntityId ?? 0), (ulong)target, at, 0)))
            return;
        switch (p.Identifier)
        {
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
