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
using Bedrokia.Protocol;
using Bedrokia.Server.Core;
using Bedrokia.Server.Entities;
using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Game.Custom.Entities;

/// <summary>
/// Live, ticked custom entity instance in a Bedrokia world.
/// Handles physics simulation, gravity, collision detection, movement AI behaviors,
/// combat/damage/death, equipment, mounts/riders, and client viewer packet synchronization.
/// Source: Dragonfly (MIT) server/entity/living.go &amp; Bedrock Protocol actor sync.
/// </summary>
public sealed class CustomEntityInstance
{
    private readonly BedrokiaServer _server;
    private readonly Random _random = new();

    public long EntityId { get; }
    public CustomEntity Definition { get; }
    public World World { get; }

    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double Vx { get; set; }
    public double Vy { get; set; }
    public double Vz { get; set; }
    public float Pitch { get; set; }
    public float Yaw { get; set; }
    public float HeadYaw { get; set; }

    public float Health { get; set; }
    public float MaxHealth { get; set; }
    public float Speed { get; set; }
    public float UnderwaterSpeed { get; set; } = 0.02f;
    public float FlyingSpeed { get; set; } = 0.4f;
    public float KnockbackResistance { get; set; }
    public float AttackDamage { get; set; } = 3.0f;
    public string? AttackEffect { get; set; }
    public int AttackEffectDuration { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public float Scale { get; set; } = 1.0f;
    public string? NameTag { get; set; }
    public bool AlwaysShowName { get; set; }
    public int Variant { get; set; }
    public int MarkVariant { get; set; }
    public int SkinId { get; set; }

    public bool OnGround { get; set; }
    public bool Burning { get; set; }
    public int FireTicks { get; set; }
    public int HurtTicks { get; set; }
    public int DeathTicks { get; set; }
    public int AttackCooldown { get; set; }
    public int PanicTicks { get; set; }
    public int WanderTicks { get; set; }
    public (double X, double Z)? WanderTarget { get; set; }

    public bool Dead => Health <= 0;

    public BedrockConnection? Target { get; set; }
    public BedrockConnection? Rider { get; private set; }

    public ItemStack? MainHand { get; set; }
    public ItemStack? OffHand { get; set; }
    public ItemStack? Helmet { get; set; }
    public ItemStack? Chestplate { get; set; }
    public ItemStack? Leggings { get; set; }
    public ItemStack? Boots { get; set; }

    public HashSet<string> ActiveComponentGroups { get; } = [];
    public Dictionary<string, object> PropertyValues { get; } = [];
    public HashSet<BedrockConnection> Viewers { get; } = [];
    public HashSet<BedrockConnection> BossViewers { get; } = [];

    private (double X, double Y, double Z, float Yaw) _lastSent = (double.NaN, 0, 0, 0);

    /// <summary>
    /// Initializes a new live custom entity instance in the specified world and position.
    /// </summary>
    /// <param name="server">The Bedrokia server instance.</param>
    /// <param name="entityId">Unique runtime entity ID allocated by the server.</param>
    /// <param name="definition">Custom entity definition.</param>
    /// <param name="world">World in which the entity lives.</param>
    /// <param name="x">Initial X position.</param>
    /// <param name="y">Initial Y position.</param>
    /// <param name="z">Initial Z position.</param>
    public CustomEntityInstance(BedrokiaServer server, long entityId, CustomEntity definition, World world, double x, double y, double z)
    {
        _server = server;
        EntityId = entityId;
        Definition = definition;
        World = world;
        X = x;
        Y = y;
        Z = z;

        ApplyBaseComponents();
        InitializeProperties();
        InitializeEquipment();

        Definition.OnSpawnAction?.Invoke(this);
    }

    private void ApplyBaseComponents()
    {
        Health = Definition.Health;
        MaxHealth = Definition.MaxHealth;
        Speed = Definition.Speed;
        Width = Definition.Width;
        Height = Definition.Height;
        Scale = Definition.Scale;

        if (Definition.GetComponent<UnderwaterMovementEntityComponent>() is { } um)
            UnderwaterSpeed = um.Value;
        if (Definition.GetComponent<FlyingSpeedEntityComponent>() is { } fs)
            FlyingSpeed = fs.Value;
        if (Definition.GetComponent<KnockbackResistanceEntityComponent>() is { } kr)
            KnockbackResistance = kr.Value;
        if (Definition.GetComponent<AttackEntityComponent>() is { } att)
        {
            AttackDamage = att.Damage;
            AttackEffect = att.EffectName;
            AttackEffectDuration = att.EffectDuration;
        }
        if (Definition.GetComponent<NameableEntityComponent>() is { } nameable)
        {
            AlwaysShowName = nameable.AlwaysShow;
        }
        if (Definition.GetComponent<VariantEntityComponent>() is { } v)
            Variant = v.Value;
        if (Definition.GetComponent<MarkVariantEntityComponent>() is { } mv)
            MarkVariant = mv.Value;
        if (Definition.GetComponent<SkinIdEntityComponent>() is { } si)
            SkinId = si.Value;
    }

    private void InitializeProperties()
    {
        foreach (var (name, prop) in Definition.Properties)
            PropertyValues[name] = prop.DefaultValue;
    }

    private void InitializeEquipment()
    {
        if (Definition.GetComponent<EquipmentEntityComponent>() is { } eq)
        {
            if (eq.MainHand is not null) MainHand = ItemStack.Of(eq.MainHand, 1);
            if (eq.OffHand is not null) OffHand = ItemStack.Of(eq.OffHand, 1);
            if (eq.Helmet is not null) Helmet = ItemStack.Of(eq.Helmet, 1);
            if (eq.Chestplate is not null) Chestplate = ItemStack.Of(eq.Chestplate, 1);
            if (eq.Leggings is not null) Leggings = ItemStack.Of(eq.Leggings, 1);
            if (eq.Boots is not null) Boots = ItemStack.Of(eq.Boots, 1);
        }
    }

    /// <summary>
    /// Gets the Bedrock actor bitwise flags for this entity instance.
    /// Source: gophertunnel (MIT) minecraft/protocol/packet/set_actor_data.go.
    /// </summary>
    public long Flags
    {
        get
        {
            long flags = EntityPackets.FlagBreathing;
            var hasPhys = Definition.GetComponent<PhysicsEntityComponent>()?.HasGravity ?? true;
            if (hasPhys) flags |= EntityPackets.FlagGravity;
            var hasCol = Definition.GetComponent<PhysicsEntityComponent>()?.HasCollision ?? true;
            if (hasCol) flags |= EntityPackets.FlagHasCollision;
            if (Burning) flags |= EntityPackets.FlagOnFire;
            if (Definition.HasComponent(EntityComponentIds.FireImmune)) flags |= EntityPackets.FlagFireImmune;
            if (Definition.HasComponent(EntityComponentIds.IsBaby)) flags |= EntityPackets.FlagBaby;
            if (Definition.HasComponent(EntityComponentIds.IsTamed)) flags |= EntityPackets.FlagTamed;
            if (Definition.HasComponent(EntityComponentIds.IsSaddled)) flags |= EntityPackets.FlagSaddled;
            if (Definition.HasComponent(EntityComponentIds.IsCharged)) flags |= EntityPackets.FlagCharged;
            if (Definition.HasComponent(EntityComponentIds.CanClimb)) flags |= EntityPackets.FlagCanClimb;
            if (!string.IsNullOrEmpty(NameTag))
            {
                flags |= EntityPackets.FlagShowName;
                if (AlwaysShowName) flags |= EntityPackets.FlagAlwaysShowName;
            }
            return flags;
        }
    }

    // --- Entity Properties ---

    /// <summary>
    /// Sets a dynamic property value and broadcasts update to viewers if clientSync is true.
    /// </summary>
    /// <param name="name">Property name.</param>
    /// <param name="value">New value.</param>
    public void SetProperty(string name, object value)
    {
        var key = name.Contains(':') ? name : "custom:" + name;
        PropertyValues[key] = value;

        if (Definition.Properties.TryGetValue(key, out var prop) && prop.ClientSync)
        {
            BroadcastToViewers(SetDataPacket());
        }
    }

    /// <summary>
    /// Retrieves the current value of a dynamic entity property.
    /// </summary>
    /// <typeparam name="T">Target return type.</typeparam>
    /// <param name="name">Property name.</param>
    public T? GetProperty<T>(string name)
    {
        var key = name.Contains(':') ? name : "custom:" + name;
        if (PropertyValues.TryGetValue(key, out var val))
        {
            if (val is T typed) return typed;
            return (T)Convert.ChangeType(val, typeof(T));
        }
        return default;
    }

    // --- Component Groups ---

    /// <summary>
    /// Activates a component group on this entity instance, applying its attributes and broadcasting changes.
    /// </summary>
    /// <param name="groupName">Name of the component group to activate.</param>
    public void AddComponentGroup(string groupName)
    {
        if (!ActiveComponentGroups.Add(groupName))
            return;

        if (Definition.ComponentGroups.TryGetValue(groupName, out var group))
        {
            ApplyGroupComponents(group);
            BroadcastToViewers(EntityPackets.EntityAttributes(EntityId, Health, MaxHealth, Speed, UnderwaterSpeed, FlyingSpeed, KnockbackResistance));
            BroadcastToViewers(SetDataPacket());
        }
    }

    /// <summary>
    /// Deactivates a component group from this entity instance, resetting attributes and broadcasting changes.
    /// </summary>
    /// <param name="groupName">Name of the component group to deactivate.</param>
    public void RemoveComponentGroup(string groupName)
    {
        if (!ActiveComponentGroups.Remove(groupName))
            return;

        // Réapplique la base, puis les groupes restants
        ApplyBaseComponents();
        foreach (var grp in ActiveComponentGroups)
        {
            if (Definition.ComponentGroups.TryGetValue(grp, out var g))
                ApplyGroupComponents(g);
        }

        BroadcastToViewers(EntityPackets.EntityAttributes(EntityId, Health, MaxHealth, Speed, UnderwaterSpeed, FlyingSpeed, KnockbackResistance));
        BroadcastToViewers(SetDataPacket());
    }

    private void ApplyGroupComponents(EntityComponentGroup group)
    {
        foreach (var (_, comp) in group.Components)
        {
            switch (comp)
            {
                case HealthEntityComponent h:
                    MaxHealth = h.Max;
                    Health = Math.Min(Health, MaxHealth);
                    break;
                case MovementEntityComponent m:
                    Speed = m.Value;
                    break;
                case FlyingSpeedEntityComponent fs:
                    FlyingSpeed = fs.Value;
                    break;
                case UnderwaterMovementEntityComponent um:
                    UnderwaterSpeed = um.Value;
                    break;
                case KnockbackResistanceEntityComponent kr:
                    KnockbackResistance = kr.Value;
                    break;
                case ScaleEntityComponent sc:
                    Scale = sc.Value;
                    break;
                case CollisionBoxEntityComponent cb:
                    Width = cb.Width;
                    Height = cb.Height;
                    break;
                case AttackEntityComponent att:
                    AttackDamage = att.Damage;
                    AttackEffect = att.EffectName;
                    AttackEffectDuration = att.EffectDuration;
                    break;
                case VariantEntityComponent v:
                    Variant = v.Value;
                    break;
                case MarkVariantEntityComponent mv:
                    MarkVariant = mv.Value;
                    break;
                case SkinIdEntityComponent si:
                    SkinId = si.Value;
                    break;
            }
        }
    }

    // --- Monture & Cavalier ---

    /// <summary>
    /// Mounts a player onto this entity instance and sends link packets to all viewers.
    /// Source: gophertunnel (MIT) minecraft/protocol/packet/set_actor_link.go.
    /// </summary>
    /// <param name="rider">The player mounting the entity.</param>
    /// <returns>True if mount succeeded; false if a rider is already present.</returns>
    public bool Mount(BedrockConnection rider)
    {
        if (Rider is not null)
            return false;

        Rider = rider;
        var linkPacket = Packets.SetActorLink(EntityId, rider.EntityId, linkType: 1, immediate: true, riderInitiated: true);
        BroadcastToViewers(linkPacket);
        rider.Send(linkPacket);
        return true;
    }

    /// <summary>
    /// Dismounts the current rider from this entity and synchronizes link removal to viewers.
    /// </summary>
    public void Dismount()
    {
        if (Rider is { } r)
        {
            var linkPacket = Packets.SetActorLink(EntityId, r.EntityId, linkType: 0, immediate: true, riderInitiated: true);
            BroadcastToViewers(linkPacket);
            r.Send(linkPacket);
            Rider = null;
        }
    }

    /// <summary>
    /// Handles interaction from a player (e.g. right click), invoking definition callbacks,
    /// component callbacks, or mounting if rideable.
    /// </summary>
    /// <param name="player">The player initiating interaction.</param>
    /// <returns>True if interaction was handled.</returns>
    public bool Interact(BedrockConnection player)
    {
        if (Dead) return false;

        Definition.OnInteractAction?.Invoke(player, this);

        if (Definition.GetComponent<InteractEntityComponent>() is { } interact)
            interact.OnInteract?.Invoke(player, this);

        if (Definition.GetComponent<RideableEntityComponent>() is not null && Rider is null)
            return Mount(player);

        return true;
    }

    // --- Dégâts & Combat ---

    /// <summary>
    /// Applies damage to this custom entity instance, triggering knockback, hurt animations,
    /// invulnerability ticks, callbacks, boss bar updates, or death.
    /// Source: Dragonfly (MIT) server/entity/living.go.
    /// </summary>
    /// <param name="amount">Amount of damage to inflict.</param>
    /// <param name="from">Optional origin coordinates of the damage for knockback calculation.</param>
    /// <param name="damager">Optional attacking player.</param>
    /// <param name="source">Damage source categorization.</param>
    /// <returns>True if damage was successfully dealt; false if invulnerable or dead.</returns>
    public bool Damage(float amount, (double X, double Z)? from = null, BedrockConnection? damager = null, DamageSource? source = null)
    {
        if (Dead || HurtTicks > 0 && from is not null)
            return false;

        source ??= damager is not null ? DamageSource.PlayerAttack(damager) : DamageSource.Custom();
        if (amount <= 0)
            return false;

        Health = Math.Max(0, Health - amount);
        HurtTicks = 10;
        Definition.OnHurtAction?.Invoke(this, source);

        BroadcastToViewers(EntityPackets.Event(EntityId, EntityPackets.EventHurt));
        UpdateBossBar();

        if (from is { } fromPos)
        {
            var dx = X - fromPos.X;
            var dz = Z - fromPos.Z;
            var length = Math.Max(0.01, Math.Sqrt(dx * dx + dz * dz));
            var resist = Math.Clamp(1.0 - KnockbackResistance, 0.0, 1.0);
            Vx = dx / length * (0.4 + 0.5 * 0) * resist;
            Vz = dz / length * (0.4 + 0.5 * 0) * resist;
            Vy = 0.36 * resist;
            PanicTicks = 80;
        }

        if (Dead)
        {
            DeathTicks = 0;
            BroadcastToViewers(EntityPackets.Event(EntityId, EntityPackets.EventDeath));
            Dismount();
            HideBossBar();

            var drops = new List<ItemStack>();
            if (Definition.GetComponent<LootEntityComponent>() is { } loot)
            {
                // Loot table logic
            }
            if (MainHand is not null) drops.Add(MainHand);
            if (Helmet is not null) drops.Add(Helmet);
            if (Chestplate is not null) drops.Add(Chestplate);
            if (Leggings is not null) drops.Add(Leggings);
            if (Boots is not null) drops.Add(Boots);

            Definition.OnDeathAction?.Invoke(this);

            foreach (var stack in drops)
                _server.DropItem(stack, (float)X, (float)Y + 0.5f, (float)Z, world: World);

            damager?.AddExperience(5);
        }

        return true;
    }

    /// <summary>
    /// Restores health up to <see cref="MaxHealth"/> and broadcasts attribute update to viewers.
    /// </summary>
    /// <param name="amount">Health points to restore.</param>
    public void Heal(float amount)
    {
        if (Dead || amount <= 0) return;
        Health = Math.Min(Health + amount, MaxHealth);
        UpdateBossBar();
        BroadcastToViewers(EntityPackets.EntityAttributes(EntityId, Health, MaxHealth, Speed, UnderwaterSpeed, FlyingSpeed, KnockbackResistance));
    }

    // --- Tick de Simulation & IA ---

    /// <summary>
    /// Advances physics simulation, movement AI, targeting, boss bars, and viewer tracking for this entity.
    /// </summary>
    /// <param name="tick">Current server game tick.</param>
    /// <param name="players">List of active connected players in the server.</param>
    public void Tick(long tick, List<BedrockConnection> players)
    {
        if (Dead)
        {
            DeathTicks++;
            return;
        }

        Definition.OnTickAction?.Invoke(this);

        if (HurtTicks > 0) HurtTicks--;
        if (AttackCooldown > 0) AttackCooldown--;
        if (PanicTicks > 0) PanicTicks--;

        // Cavalier avec contrôle au sol
        if (Rider is { } rider && rider.IsInGame)
        {
            var isGroundControlled = Definition.GetComponent<RideableEntityComponent>()?.InputGroundControlled ?? false;
            if (isGroundControlled)
            {
                Yaw = rider.Yaw;
                HeadYaw = rider.Yaw;
                // Le déplacement suit la rotation du cavalier
            }
        }
        else
        {
            // IA autonome
            Think(players, tick);
        }

        Physics();
        SyncBossBar(players);

        if (tick % 2 == 0)
            SyncViewers(players);
    }

    private void Think(List<BedrockConnection> players, long tick)
    {
        if (Rider is not null)
        {
            Yaw = Rider.Yaw;
            if (Definition.GetComponent<RideableEntityComponent>()?.InputGroundControlled == true)
            {
                X = Rider.Position.X;
                Y = Rider.Position.Y - BedrockConnection.EyeHeight;
                Z = Rider.Position.Z;
                Vx = Vz = 0;
            }
            return;
        }

        BedrockConnection? nearestPlayer = null;
        double nearestDist2 = double.MaxValue;
        for (var i = 0; i < players.Count; i++)
        {
            var p = players[i];
            if (p.Dead || p.World != World) continue;
            var dx = p.Position.X - X;
            var dz = p.Position.Z - Z;
            var d2 = dx * dx + dz * dz;
            if (d2 < nearestDist2)
            {
                nearestDist2 = d2;
                nearestPlayer = p;
            }
        }

        var dist = Math.Sqrt(nearestDist2);

        // Flottaison
        if (Definition.HasComponent(EntityComponentIds.BehaviorFloat))
        {
            var feet = World.GetBlock((int)Math.Floor(X), (int)Math.Floor(Y), (int)Math.Floor(Z));
            if (feet == Bedrokia.Server.Game.Blocks.Water)
                Vy = Math.Min(Vy + 0.08, 0.15);
        }

        // Cible d'attaque
        if (Definition.HasComponent(EntityComponentIds.BehaviorNearestAttackableTarget) && Target is null)
        {
            if (nearestPlayer is not null && nearestPlayer.GameMode != GameMode.Creative && dist < 16)
                Target = nearestPlayer;
        }

        if (Target is not null && (Target.Dead || Target.World != World || Target.GameMode == GameMode.Creative))
            Target = null;

        // Attaque de mêlée
        if (Definition.HasComponent(EntityComponentIds.BehaviorMeleeAttack) && Target is not null)
        {
            var dx = Target.Position.X - X;
            var dz = Target.Position.Z - Z;
            var d = Math.Sqrt(dx * dx + dz * dz);
            Yaw = (float)(Math.Atan2(dz, dx) * 180 / Math.PI) - 90;

            if (d > 1.5)
            {
                var step = Speed * 0.45;
                Vx = dx / d * step;
                Vz = dz / d * step;
            }
            else if (AttackCooldown <= 0)
            {
                Target.Damage((int)AttackDamage, X, Z, source: DamageSource.Custom());
                AttackCooldown = 20;
            }
            return;
        }

        // Regarder le joueur
        if (Definition.HasComponent(EntityComponentIds.BehaviorLookAtPlayer) && nearestPlayer is not null && dist < 8)
        {
            var dx = nearestPlayer.Position.X - X;
            var dz = nearestPlayer.Position.Z - Z;
            Yaw = (float)(Math.Atan2(dz, dx) * 180 / Math.PI) - 90;
        }

        // Déambulation aléatoire (RandomStroll)
        if (Definition.HasComponent(EntityComponentIds.BehaviorRandomStroll))
        {
            if (WanderTicks-- <= 0 || WanderTarget is null)
            {
                var angle = _random.NextDouble() * Math.PI * 2;
                var range = 4 + _random.Next(8);
                WanderTarget = (X + Math.Cos(angle) * range, Z + Math.Sin(angle) * range);
                WanderTicks = 60 + _random.Next(60);
            }

            if (WanderTarget is { } target)
            {
                var dx = target.X - X;
                var dz = target.Z - Z;
                var d = Math.Sqrt(dx * dx + dz * dz);
                if (d < 1)
                {
                    WanderTarget = null;
                    Vx = Vz = 0;
                }
                else
                {
                    Yaw = (float)(Math.Atan2(dz, dx) * 180 / Math.PI) - 90;
                    var step = Speed * 0.25;
                    Vx = dx / d * step;
                    Vz = dz / d * step;
                }
            }
        }
    }

    private void Physics()
    {
        var hasPhysics = Definition.GetComponent<PhysicsEntityComponent>()?.HasGravity ?? true;
        if (hasPhysics)
        {
            var feet = World.GetBlock((int)Math.Floor(X), (int)Math.Floor(Y), (int)Math.Floor(Z));
            if (feet == Bedrokia.Server.Game.Blocks.Water)
                Vy = Math.Min(Vy + 0.04, 0.1);
            else
                Vy = (Vy - 0.08) * 0.98;

            if (OnGround && (Vx != 0 || Vz != 0)
                && Collides(X + Vx, Y, Z + Vz) && !Collides(X + Vx, Y + 1.1, Z + Vz))
                Vy = 0.42;

            var ny = Y + Vy;
            if (Collides(X, ny, Z))
            {
                if (Vy < 0)
                {
                    OnGround = true;
                    Y = Math.Floor(ny) + 1;
                    if (Collides(X, Y, Z))
                        Y = Math.Floor(Y);
                }
                Vy = 0;
            }
            else
            {
                Y = ny;
                OnGround = false;
            }

            if (!Collides(X + Vx, Y, Z)) X += Vx;
            if (!Collides(X, Y, Z + Vz)) Z += Vz;

            var friction = OnGround ? 0.6 : 0.91;
            Vx *= friction;
            Vz *= friction;
        }
        else
        {
            X += Vx;
            Y += Vy;
            Z += Vz;
        }
    }

    private bool Collides(double x, double y, double z)
    {
        var half = Width / 2 - 0.01;
        for (var bx = (int)Math.Floor(x - half); bx <= (int)Math.Floor(x + half); bx++)
        for (var bz = (int)Math.Floor(z - half); bz <= (int)Math.Floor(z + half); bz++)
        for (var by = (int)Math.Floor(y); by <= (int)Math.Floor(y + Height - 0.01); by++)
            if (BlockPhysics.IsSolid(World.GetBlock(bx, by, bz)))
                return true;
        return false;
    }

    // --- Boss Bar Management ---

    private void SyncBossBar(List<BedrockConnection> players)
    {
        if (Definition.GetComponent<BossEntityComponent>() is not { } boss)
            return;

        var hudRange2 = boss.HudRange * boss.HudRange;
        foreach (var p in players)
        {
            if (p.World != World || p.Dead)
            {
                if (BossViewers.Remove(p))
                    p.Send(Packets.BossEventHide(EntityId));
                continue;
            }

            var dx = p.Position.X - X;
            var dz = p.Position.Z - Z;
            var inRange = dx * dx + dz * dz <= hudRange2;

            if (inRange && BossViewers.Add(p))
            {
                p.Send(Packets.BossEventShow(EntityId, boss.Name, Health / MaxHealth, boss.ShouldDarkenSky));
            }
            else if (!inRange && BossViewers.Remove(p))
            {
                p.Send(Packets.BossEventHide(EntityId));
            }
        }
    }

    private void UpdateBossBar()
    {
        if (Definition.GetComponent<BossEntityComponent>() is null)
            return;

        var healthPercent = Math.Clamp(Health / MaxHealth, 0f, 1f);
        var packet = Packets.BossEventHealth(EntityId, healthPercent);
        foreach (var viewer in BossViewers)
            viewer.Send(packet);
    }

    private void HideBossBar()
    {
        var hidePacket = Packets.BossEventHide(EntityId);
        foreach (var viewer in BossViewers)
            viewer.Send(hidePacket);
        BossViewers.Clear();
    }

    // --- Synchronisation Réseau des Viewers ---

    private void SyncViewers(List<BedrockConnection> players)
    {
        const int viewDistance2 = 64 * 64;
        foreach (var p in players)
        {
            if (p.World != World) continue;
            var dx = p.Position.X - X;
            var dz = p.Position.Z - Z;
            var visible = dx * dx + dz * dz <= viewDistance2;

            if (visible && Viewers.Add(p))
            {
                p.Send(AddPacket());
                SendEquipment(p);
            }
            else if (!visible && Viewers.Remove(p))
            {
                p.Send(Packets.RemoveActor(EntityId));
            }
        }

        Viewers.RemoveWhere(v => !v.IsInGame || v.World != World);

        var (lx, ly, lz, lyaw) = _lastSent;
        if (Math.Abs(X - lx) + Math.Abs(Y - ly) + Math.Abs(Z - lz) > 0.001 || Math.Abs(Yaw - lyaw) > 0.5)
        {
            _lastSent = (X, Y, Z, Yaw);
            var movePacket = EntityPackets.MoveAbsolute(EntityId, X, Y, Z, Pitch, Yaw, OnGround);
            foreach (var v in Viewers)
                v.Send(movePacket);
        }
    }

    /// <summary>
    /// Sends current held items and equipped armor to the specified viewer.
    /// Source: gophertunnel (MIT) MobEquipment &amp; MobArmourEquipment packets.
    /// </summary>
    /// <param name="player">The player connection to receive equipment updates.</param>
    public void SendEquipment(BedrockConnection player)
    {
        if (MainHand is not null)
            player.Send(InventoryPackets.MobEquipment(EntityId, MainHand, 0, 0));
        if (Helmet is not null || Chestplate is not null || Leggings is not null || Boots is not null)
            player.Send(InventoryPackets.MobArmorEquipment(EntityId, Helmet, Chestplate, Leggings, Boots));
    }

    /// <summary>
    /// Sends the entity spawn packet and equipment to a player who just entered rendering range.
    /// </summary>
    /// <param name="player">The player to spawn the entity for.</param>
    public void SendTo(BedrockConnection player)
    {
        if (player.World != World || Dead)
            return;

        if (Viewers.Add(player))
        {
            player.Send(AddPacket());
            SendEquipment(player);
        }
    }

    private void BroadcastToViewers(byte[] packet)
    {
        foreach (var viewer in Viewers)
            viewer.Send(packet);
    }

    /// <summary>
    /// Constructs the Bedrock <see cref="Bedrokia.Protocol.PacketId.AddActor"/> spawn packet for this instance,
    /// embedding position, attributes, metadata flags, variant, scale, and PropertySyncData.
    /// </summary>
    public byte[] AddPacket()
    {
        var (intProps, floatProps) = BuildPropertySyncData();
        var links = Rider is not null
            ? new List<(long, long, byte, bool, bool)> { (EntityId, Rider.EntityId, 1, true, true) }
            : null;

        return EntityPackets.AddActor(
            EntityId,
            Definition.Identifier,
            X, Y, Z,
            Yaw,
            Health,
            MaxHealth,
            Speed,
            Width,
            Height,
            Flags,
            scale: Scale,
            nameTag: NameTag,
            alwaysShowName: AlwaysShowName,
            variant: Variant,
            markVariant: MarkVariant,
            skinId: SkinId,
            intProperties: intProps,
            floatProperties: floatProps,
            links: links);
    }

    /// <summary>
    /// Constructs the Bedrock <see cref="Bedrokia.Protocol.PacketId.SetActorData"/> packet for synchronizing
    /// metadata flags, collision box, scale, variant, and updated entity properties.
    /// </summary>
    public byte[] SetDataPacket()
    {
        var (intProps, floatProps) = BuildPropertySyncData();
        return EntityPackets.SetData(
            EntityId,
            Flags,
            Width,
            Height,
            scale: Scale,
            nameTag: NameTag,
            alwaysShowName: AlwaysShowName,
            variant: Variant,
            markVariant: MarkVariant,
            skinId: SkinId,
            intProperties: intProps,
            floatProperties: floatProps);
    }

    private (List<(uint Index, int Value)> Ints, List<(uint Index, float Value)> Floats) BuildPropertySyncData()
    {
        var ints = new List<(uint Index, int Value)>();
        var floats = new List<(uint Index, float Value)>();

        uint index = 0;
        foreach (var (propName, propDef) in Definition.Properties)
        {
            if (propDef.ClientSync && PropertyValues.TryGetValue(propName, out var val))
            {
                switch (propDef.Type)
                {
                    case EntityPropertyType.Int:
                        ints.Add((index, Convert.ToInt32(val)));
                        break;
                    case EntityPropertyType.Bool:
                        ints.Add((index, Convert.ToBoolean(val) ? 1 : 0));
                        break;
                    case EntityPropertyType.Enum:
                        var str = val.ToString() ?? "";
                        var idx = propDef.EnumValues.ToList().IndexOf(str);
                        ints.Add((index, Math.Max(0, idx)));
                        break;
                    case EntityPropertyType.Float:
                        floats.Add((index, Convert.ToSingle(val)));
                        break;
                }
            }
            index++;
        }

        return (ints, floats);
    }
}
