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

    /// <summary>Damage done to what it hits (arrows, tridents, small fireballs).</summary>
    public float Damage { get; internal init; }

    /// <summary>The item it is (a thrown trident), dropped or brought back where it lands.</summary>
    public ItemStack? Item { get; internal init; }

    /// <summary>Punch level (extra knockback) and Flame (sets on fire) of the bow that shot it.</summary>
    public int Punch { get; internal init; }
    public bool Flame { get; internal init; }

    /// <summary>An arrow that can be picked up where it lands (not from Infinity or creative).</summary>
    public bool Pickup { get; internal init; }

    // Fishing hook: ticks until a fish bites, then ticks the bite lasts.
    internal int HookWait = -1, BiteTicks;

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
        SmallFireball = "minecraft:small_fireball", Trident = "minecraft:thrown_trident", FishingHook = "minecraft:fishing_hook",
        XpBottle = "minecraft:xp_bottle", WindCharge = "minecraft:wind_charge_projectile", LingeringPotion = "minecraft:lingering_potion";

    private const float Gravity = 0.03f, ArrowGravity = 0.05f, Drag = 0.99f, SplashRadius = 4;
    private const int MaxTicks = 1200;

    private readonly List<Projectile> _projectiles = [];

    public IReadOnlyList<Projectile> All => _projectiles;

    /// <summary>True for the items a right click throws.</summary>
    public static bool IsThrowable(ItemStack? item) => item?.Type.Name is Snowball or Egg or EnderPearl or SplashPotion or LingeringPotion
        or "minecraft:experience_bottle" or "minecraft:wind_charge";

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
        identifier = identifier switch { "minecraft:experience_bottle" => XpBottle, "minecraft:wind_charge" => WindCharge, _ => identifier };
        var projectile = Spawn(identifier, player.World, new Vector3(player.Position.X, player.Position.Y - 0.1f, player.Position.Z),
            Look(player) * speed, player, potion);
        server.PlaySound("random.bow", player.Position.X, player.Position.Y, player.Position.Z, player.World, 0.5f, 0.4f);
        return projectile;
    }

    /// <summary>A projectile from anywhere (a dispenser, a trap...), with its velocity in blocks per tick.</summary>
    public Projectile Spawn(string identifier, World world, Vector3 position, Vector3 velocity, BedrockConnection? owner = null, short potion = 0) =>
        Add(new Projectile(server.NextEntityId(), identifier, world, owner, position, velocity, potion));

    private static Vector3 Look(BedrockConnection player, float yawOffset = 0)
    {
        var yaw = (player.Yaw + yawOffset) * MathF.PI / 180;
        var pitch = player.Pitch * MathF.PI / 180;
        return new Vector3(-MathF.Sin(yaw) * MathF.Cos(pitch), -MathF.Sin(pitch), MathF.Cos(yaw) * MathF.Cos(pitch));
    }

    /// <summary>
    /// A player fires where they look: an arrow (bow, crossbow), a trident or a fishing hook, at <paramref name="speed"/>
    /// blocks per tick, <paramref name="yawOffset"/> degrees aside (crossbow Multishot).
    /// </summary>
    public Projectile Fire(BedrockConnection player, string identifier, float speed, float damage, ItemStack? item = null,
        int punch = 0, bool flame = false, bool pickup = false, float yawOffset = 0) =>
        Add(new Projectile(server.NextEntityId(), identifier, player.World,
            player, new Vector3(player.Position.X, player.Position.Y - 0.1f, player.Position.Z), Look(player, yawOffset) * speed, 0)
        {
            Damage = damage, Item = item, Punch = punch, Flame = flame, Pickup = pickup,
        });

    /// <summary>The fishing hook a player has out, or null.</summary>
    public Projectile? HookOf(BedrockConnection player) => _projectiles.FirstOrDefault(p => p.Identifier == FishingHook && p.Owner == player);

    /// <summary>
    /// Reels the hook in: a catch when a fish is biting (fish 85 %, junk 10 %, treasure 5 %, Luck of the Sea helps),
    /// returned with 1-6 experience. Returns true when something was caught.
    /// </summary>
    public bool Reel(Projectile hook)
    {
        var index = _projectiles.IndexOf(hook);
        if (index >= 0)
            Remove(index);
        if (hook.BiteTicks <= 0 || hook.Owner is not { IsInGame: true } angler)
            return false;
        var luck = Enchanting.Level(angler.HeldItem, "luck_of_the_sea");
        var roll = _random.NextDouble();
        var treasure = 0.05 + 0.021 * luck;
        var junk = Math.Max(0, 0.10 - 0.025 * luck);
        string[] table = roll < treasure ? ["name_tag", "saddle", "nautilus_shell", "bow", "fishing_rod", "book"]
            : roll < treasure + junk ? ["bowl", "leather", "leather_boots", "rotten_flesh", "stick", "string", "bone", "ink_sac", "tripwire_hook", "waterlily"]
            : null!;
        var name = table is null
            ? _random.Next(100) switch { < 60 => "cod", < 85 => "salmon", < 87 => "tropical_fish", _ => "pufferfish" }
            : table[_random.Next(table.Length)];
        if (ItemStack.Of(name) is { } caught)
            angler.GiveItem(caught);
        angler.AddExperience(1 + _random.Next(6));
        server.PlaySound("random.orb", angler.Position.X, angler.Position.Y, angler.Position.Z, angler.World, 0.5f);
        return true;
    }

    private readonly Random _random = new();

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
            if (p.Identifier == FishingHook && Fish(p))
            {
                Remove(i);
                continue;
            }
            var from = p.Position;
            var to = from + p.Velocity;
            if (FindHit(p, from, to) is { } hit)
            {
                if (p.Identifier == FishingHook)
                {
                    p.Velocity = Vector3.Zero; // the hook rests where it landed
                    continue;
                }
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

    // A fishing hook: in water it floats, waits for a bite, then the fish nibbles for a second. True: remove it.
    private bool Fish(Projectile p)
    {
        if (p.Owner is not { IsInGame: true, Dead: false } owner || owner.World != p.World
            || owner.HeldItem?.Type.Name != "minecraft:fishing_rod"
            || Vector3.Distance(p.Position, new Vector3(owner.Position.X, owner.Position.Y, owner.Position.Z)) > 32)
            return true;
        var block = p.World.GetBlock((int)MathF.Floor(p.Position.X), (int)MathF.Floor(p.Position.Y), (int)MathF.Floor(p.Position.Z));
        if (block?.Name is not ("minecraft:water" or "minecraft:flowing_water"))
            return false;
        p.Velocity = new Vector3(p.Velocity.X * 0.5f, 0.02f, p.Velocity.Z * 0.5f); // floats up to the surface
        if (p.World.GetBlock((int)MathF.Floor(p.Position.X), (int)MathF.Floor(p.Position.Y + 0.3f), (int)MathF.Floor(p.Position.Z)) is { IsAir: true })
            p.Velocity = new Vector3(0, 0, 0);
        if (p.BiteTicks > 0)
        {
            if (--p.BiteTicks == 0)
                p.HookWait = -1; // got away
            return false;
        }
        if (p.HookWait < 0)
            p.HookWait = Math.Max(20, 100 + _random.Next(500) - 100 * Enchanting.Level(owner.HeldItem, "lure"));
        else if (--p.HookWait == 0)
        {
            p.BiteTicks = 20 + _random.Next(20);
            server.BroadcastIn(p.World, EntityPackets.Event(p.EntityId, 14)); // the bobber dips
            server.PlaySound("random.splash", p.Position.X, p.Position.Y, p.Position.Z, p.World, 0.4f);
        }
        return false;
    }

    // Walks the segment in quarter blocks: the first solid block, player or mob touched.
    private (Vector3 At, BedrockConnection? Player, Mob? Mob)? FindHit(Projectile p, Vector3 from, Vector3 to)
    {
        var steps = Math.Max(1, (int)MathF.Ceiling(Vector3.Distance(from, to) / 0.25f));
        for (var s = 1; s <= steps; s++)
        {
            var at = Vector3.Lerp(from, to, (float)s / steps);
            foreach (var player in p.Identifier == FishingHook ? [] : server.Players)
            {
                if (!player.IsInGame || player.World != p.World || player.Dead || player == p.Owner && p.Ticks < 5)
                    continue;
                var feet = player.Position.Y - BedrockConnection.EyeHeight;
                if (MathF.Abs(at.X - player.Position.X) < 0.45f && MathF.Abs(at.Z - player.Position.Z) < 0.45f && at.Y >= feet && at.Y <= feet + 1.9f)
                    return (at, player, null);
            }
            foreach (var mob in p.Identifier == FishingHook ? [] : server.Mobs.All)
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
        var source = p.Shooter is { } shooter ? DamageSource.EntityAttack(shooter)
            : p.Owner is { } archer ? DamageSource.PlayerAttack(archer) : DamageSource.Custom(p.Identifier);
        switch (p.Identifier)
        {
            case Arrow or SmallFireball or Trident:
                // Players shoot players only with pvp on.
                if (player is not null && (p.Owner is null || server.Config.Pvp))
                    player.Damage(p.Damage, p.Position.X, p.Position.Z, source: source);
                if (mob is not null && server.Mobs.Damage(mob, p.Damage, (p.Position.X, p.Position.Z), p.Owner, knockback: p.Punch, source: source)
                    && (p.Flame || p.Identifier == SmallFireball && mob.Kind != MobKind.Blaze))
                    mob.FireTicks = Math.Max(mob.FireTicks, 100);
                server.PlaySound(p.Identifier == Trident ? "item.trident.hit" : "random.bowhit", at.X, at.Y, at.Z, p.World);
                var landed = at - Vector3.Normalize(p.Velocity) * 0.3f;
                if (p.Identifier == Arrow && p.Pickup && player is null && mob is null && ItemStack.Of("arrow") is { } arrow)
                    server.DropItem(arrow, landed.X, landed.Y, landed.Z, world: p.World);
                if (p.Identifier == Trident && p.Item is { } trident)
                {
                    // Loyalty brings it back to its thrower; otherwise it lies where it landed.
                    if (Enchanting.Level(trident, "loyalty") > 0 && p.Owner is { IsInGame: true } loyal)
                        loyal.GiveItem(trident);
                    else
                        server.DropItem(trident, landed.X, landed.Y, landed.Z, world: p.World);
                }
                break;
            case XpBottle:
                server.PlaySound("random.glass", at.X, at.Y, at.Z, p.World);
                if (p.Owner is { IsInGame: true } drinker && drinker.World == p.World)
                    drinker.AddExperience(3 + _random.Next(9));
                break;
            case WindCharge:
                WindBurst(p, at);
                break;
            case LingeringPotion:
                Splash(p, at, durationFactor: 0.25f);
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
                // An egg hatches a chick one time in eight, as vanilla.
                if (p.Identifier == Egg && _random.Next(8) == 0 && server.Mobs.Spawn(MobKind.Chicken, at.X, at.Y, at.Z, p.World) is { } chick)
                {
                    chick.Baby = true;
                    chick.AgeTicks = -24000;
                }
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

    // Wind charge: pushes everything around away, players included (a jump boost under one's feet).
    private void WindBurst(Projectile p, Vector3 at)
    {
        server.PlaySound("wind_charge.burst", at.X, at.Y, at.Z, p.World);
        foreach (var player in server.Players.Where(pl => pl.IsInGame && pl.World == p.World && !pl.Dead).ToList())
        {
            var offset = new Vector3(player.Position.X, player.Position.Y - BedrockConnection.EyeHeight + 0.9f, player.Position.Z) - at;
            if (offset.Length() > 2.5f)
                continue;
            var push = offset.LengthSquared() < 0.01f ? Vector3.UnitY : Vector3.Normalize(offset);
            player.SetMotion(push.X * 1.1, Math.Max(0.6, push.Y * 1.1), push.Z * 1.1);
        }
        foreach (var mob in server.Mobs.All.Where(m => m.World == p.World && !m.Dead))
        {
            var offset = new Vector3((float)mob.X, (float)mob.Y + mob.Height / 2, (float)mob.Z) - at;
            if (offset.Length() > 2.5f)
                continue;
            var push = offset.LengthSquared() < 0.01f ? Vector3.UnitY : Vector3.Normalize(offset);
            (mob.Vx, mob.Vy, mob.Vz) = (push.X * 1.1, Math.Max(0.6, push.Y * 1.1), push.Z * 1.1);
            if (mob.Kind.Movement == MobMovement.Ground)
                mob.OnGround = false;
        }
    }

    private void Splash(Projectile p, Vector3 at, float durationFactor = 1)
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
                applied.DurationTicks = Math.Max(1, (int)(applied.DurationTicks * factor * durationFactor));
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
