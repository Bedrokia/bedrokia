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
using Bedrokia.Server.Entities;
using Bedrokia.Server.Events;
using Bedrokia.Server.Game;
using Bedrokia.Server.Game.Custom.Items;
using Bedrokia.Server.Game.Ui.Forms;

namespace Bedrokia.Server.Network;

/// <summary>
/// Survival health and hunger: damage, fall damage, food, saturation and exhaustion (rules from Dragonfly,
/// MIT, server/player/hunger.go), regeneration, starvation, eating, melee attacks, death and respawn.
/// </summary>
/// <remarks>ponytail: no armour protection, no drowning or fire damage for players yet.</remarks>
public sealed partial class BedrockConnection
{
    public const float MaxHealth = 20;
    public const int MaxFood = 20;

    public float Health { get; private set; } = MaxHealth;
    public int Food { get; private set; } = MaxFood;
    public float Saturation { get; private set; } = 5;
    public bool Dead { get; private set; }

    private readonly Dictionary<EffectType, EffectInstance> _activeEffects = [];
    private readonly Dictionary<EffectType, int> _effectElapsed = [];

    public IReadOnlyCollection<EffectInstance> GetActiveEffects() => _activeEffects.Values.ToList();
    public EffectInstance? GetEffect(EffectType type) => _activeEffects.GetValueOrDefault(type);

    public float CurrentSpeed
    {
        get
        {
            float speed = 0.1f;
            if (_activeEffects.TryGetValue(EffectType.Speed, out var s))
                speed *= 1f + 0.2f * (s.Amplifier + 1);
            if (_activeEffects.TryGetValue(EffectType.Slowness, out var sl))
                speed *= Math.Max(0f, 1f - 0.15f * (sl.Amplifier + 1));
            return speed;
        }
    }

    public float CurrentMaxHealth => 20f + (_activeEffects.TryGetValue(EffectType.HealthBoost, out var hb) ? 4f * (hb.Amplifier + 1) : 0f);

    public void SetMaxHealth(float maxHealth) => Send(new BedrockWriter().VarUInt32(PacketId.UpdateAttributes).VarUInt64((ulong)EntityId).VarUInt32(1)
        .Float32(0).Float32(maxHealth).Float32(Health).Float32(0).Float32(maxHealth).Float32(20f).String("minecraft:health").VarUInt32(0)
        .VarUInt64(0).ToArray());

    public bool AddEffect(EffectInstance effect, EffectChangeCause cause = EffectChangeCause.Plugin)
    {
        if (Dead)
            return false;

        if (effect.Type is EffectType.InstantHealth or EffectType.InstantDamage)
        {
            ApplyInstantEffect(effect.Type, effect.Amplifier, cause);
            return true;
        }

        MobEffectAction action = MobEffectAction.Add;

        if (_activeEffects.TryGetValue(effect.Type, out var existing))
        {
            if (effect.Amplifier > existing.Amplifier)
                action = MobEffectAction.Modify;
            else if (effect.Amplifier == existing.Amplifier && effect.DurationTicks > existing.DurationTicks)
                action = MobEffectAction.Modify;
            else
                return false;
        }

        if (_server.Events.HasPlayerEffectChange)
        {
            var ev = new PlayerEffectChangeEvent(this, existing, effect, cause);
            if (!_server.Events.OnPlayerEffectChange(ev))
                return false;
            if (ev.NewEffect is null)
                return false;
            effect = ev.NewEffect;
        }

        _activeEffects[effect.Type] = effect;
        _effectElapsed[effect.Type] = 0;

        Send(Packets.MobEffect((ulong)EntityId, action, (int)effect.Type, effect.Amplifier, effect.ShowParticles, effect.DurationTicks));
        ApplyEffectAttributes(effect.Type);
        return true;
    }

    public bool RemoveEffect(EffectType type, EffectChangeCause cause = EffectChangeCause.Plugin)
    {
        if (!_activeEffects.TryGetValue(type, out var existing))
            return false;

        if (_server.Events.HasPlayerEffectChange)
        {
            var ev = new PlayerEffectChangeEvent(this, existing, null, cause);
            if (!_server.Events.OnPlayerEffectChange(ev))
                return false;
        }

        _activeEffects.Remove(type);
        _effectElapsed.Remove(type);
        Send(Packets.MobEffect((ulong)EntityId, MobEffectAction.Remove, (int)type, 0, false, 0));
        ResetEffectAttributes(type);
        return true;
    }

    public void ClearEffects(EffectChangeCause cause = EffectChangeCause.Plugin)
    {
        foreach (var (type, effect) in _activeEffects.ToList())
        {
            if (_server.Events.HasPlayerEffectChange)
            {
                var ev = new PlayerEffectChangeEvent(this, effect, null, cause);
                if (!_server.Events.OnPlayerEffectChange(ev))
                    continue;
            }

            _activeEffects.Remove(type);
            _effectElapsed.Remove(type);
            Send(Packets.MobEffect((ulong)EntityId, MobEffectAction.Remove, (int)type, 0, false, 0));
            ResetEffectAttributes(type);
        }
    }

    private void TickEffects()
    {
        if (Dead || _activeEffects.Count == 0)
            return;

        foreach (var (type, effect) in _activeEffects.ToList())
        {
            var elapsed = _effectElapsed.GetValueOrDefault(type, 0) + 1;
            _effectElapsed[type] = elapsed;

            switch (type)
            {
                case EffectType.Regeneration:
                {
                    var interval = Math.Max(1, 50 >> effect.Amplifier);
                    if (elapsed % interval == 0)
                        Heal(1, HealCause.Regeneration);
                    break;
                }
                case EffectType.Poison:
                {
                    var interval = Math.Max(1, 25 >> effect.Amplifier);
                    if (elapsed % interval == 0 && Health > 1f)
                    {
                        var dmg = Math.Min(1f, Health - 1f);
                        if (dmg > 0)
                        {
                            _hurtTicks = 0;
                            Damage(dmg, Position.X, Position.Z, armour: false, source: DamageSource.Poison());
                        }
                    }
                    break;
                }
                case EffectType.Wither:
                {
                    var interval = Math.Max(1, 40 >> effect.Amplifier);
                    if (elapsed % interval == 0)
                    {
                        _hurtTicks = 0;
                        Damage(1f, Position.X, Position.Z, armour: false, source: DamageSource.Wither());
                    }
                    break;
                }
                case EffectType.Hunger:
                {
                    AddExhaustion(0.005f * (effect.Amplifier + 1));
                    break;
                }
                case EffectType.Saturation:
                {
                    AddFood(1, 2f);
                    break;
                }
            }

            effect.DurationTicks--;
            if (effect.DurationTicks <= 0)
            {
                RemoveEffect(type, EffectChangeCause.Expiration);
            }
        }
    }

    private void ApplyEffectAttributes(EffectType type)
    {
        switch (type)
        {
            case EffectType.Speed or EffectType.Slowness:
                SendVitals();
                break;
            case EffectType.HealthBoost:
                SetMaxHealth(CurrentMaxHealth);
                SendVitals();
                break;
        }
    }

    private void ResetEffectAttributes(EffectType type)
    {
        switch (type)
        {
            case EffectType.Speed or EffectType.Slowness:
                SendVitals();
                break;
            case EffectType.HealthBoost:
                SetMaxHealth(CurrentMaxHealth);
                if (Health > CurrentMaxHealth)
                    Health = CurrentMaxHealth;
                SendVitals();
                break;
        }
    }

    private void ApplyInstantEffect(EffectType type, int amplifier, EffectChangeCause cause)
    {
        if (type == EffectType.InstantHealth)
        {
            var heal = 4f * (1 << amplifier);
            Heal(heal, HealCause.Effect);
        }
        else if (type == EffectType.InstantDamage)
        {
            var damage = 6f * (1 << amplifier);
            _hurtTicks = 0;
            Damage(damage, Position.X, Position.Z, armour: false, source: DamageSource.Magic());
        }
    }

    private float _exhaustion;
    private int _hurtTicks, _foodTicks;
    private float _fallPeak = float.NaN;
    private long _eatingSince = -1;
    private (float X, float Z) _lastWalk = (float.NaN, 0);

    private DamageSource? _lastDamageSource;

    /// <summary>
    /// Hurts the player (survival/adventure only) with knockback away from (fromX, fromZ). Attacks are reduced by
    /// armour points (4 % each, up to 80 %) and Protection enchantments (4 % per level, up to 80 %).
    /// </summary>
    public void Damage(float amount, double fromX, double fromZ, bool armour = true, DamageSource? source = null)
    {
        if (Dead || GameMode is GameMode.Creative or GameMode.Spectator || _god || _hurtTicks > 0 || amount <= 0)
            return;

        source ??= DamageSource.Custom();

        // FireResistance cancels fire and lava damage
        if ((source.Cause is DamageCause.Fire or DamageCause.Lava) && _activeEffects.ContainsKey(EffectType.FireResistance))
            return;

        var ev = new PlayerDamageEvent(this, source, amount, (fromX, fromZ));
        if (!_server.Events.OnPlayerDamage(ev))
            return;

        amount = ev.Damage;
        if (amount <= 0)
            return;

        // Resistance reduces damage by 20% * (amplifier + 1) (except void/starvation)
        if (source.Cause is not (DamageCause.Void or DamageCause.Starvation) && _activeEffects.TryGetValue(EffectType.Resistance, out var resistance))
        {
            var reduction = Math.Min(1.0f, 0.20f * (resistance.Amplifier + 1));
            amount *= Math.Max(0f, 1f - reduction);
        }

        _lastDamageSource = source;

        if (armour)
        {
            amount *= 1 - Math.Min(ArmourPoints(), 20) / 25f;
            amount *= 1 - Math.Min(ProtectionPoints(), 20) * 0.04f;
        }
        _hurtTicks = 10;
        AddExhaustion(0.1f);
        Health = Math.Max(0, Health - amount);
        SendVitals();
        _server.BroadcastIn(World, EntityPackets.Event(EntityId, EntityPackets.EventHurt));
        if (ev.ApplyKnockback)
        {
            var dx = Position.X - ev.KnockbackOrigin.X;
            var dz = Position.Z - ev.KnockbackOrigin.Z;
            var length = Math.Max(0.01, Math.Sqrt(dx * dx + dz * dz));
            Send(EntityPackets.Motion(EntityId, dx / length * 0.4, 0.36, dz / length * 0.4));
        }
        if (Health <= 0)
            Die();
    }

    // Vanilla defence points of the worn armour (helmet, chestplate, leggings, boots).
    private int ArmourPoints() => Inventory.Armour.Slots.Sum(piece =>
    {
        if (piece is null) return 0;
        if (CustomItemRegistry.Get(piece.Type.Name)?.GetComponent<ArmorItemComponent>() is { } customArmor)
            return customArmor.Protection;
        if (!piece.Type.Name.StartsWith("minecraft:"))
            return 0;
        return piece.Type.Name["minecraft:".Length..].Split('_') switch
        {
            [var material, var part] => (material, part) switch
            {
                ("leather", "helmet") => 1, ("leather", "chestplate") => 3, ("leather", "leggings") => 2, ("leather", "boots") => 1,
                ("chainmail", "helmet") => 2, ("chainmail", "chestplate") => 5, ("chainmail", "leggings") => 4, ("chainmail", "boots") => 1,
                ("iron", "helmet") => 2, ("iron", "chestplate") => 6, ("iron", "leggings") => 5, ("iron", "boots") => 2,
                ("golden", "helmet") => 2, ("golden", "chestplate") => 5, ("golden", "leggings") => 3, ("golden", "boots") => 1,
                ("diamond" or "netherite", "helmet") => 3, ("diamond" or "netherite", "chestplate") => 8,
                ("diamond" or "netherite", "leggings") => 6, ("diamond" or "netherite", "boots") => 3,
                ("turtle", "helmet") => 2,
                _ => 0,
            },
            _ => 0,
        };
    });

    private int ProtectionPoints() => Inventory.Armour.Slots.Sum(piece => Enchanting.Level(piece, "protection"));

    public void Heal(float amount, HealCause cause = HealCause.Custom)
    {
        if (amount <= 0 || Health >= CurrentMaxHealth)
            return;

        if (_server.Events.HasPlayerHeal)
        {
            var healEvent = new PlayerHealEvent(this, amount, cause);
            if (!_server.Events.OnPlayerHeal(healEvent))
                return;
            amount = healEvent.Amount;
            if (amount <= 0)
                return;
        }

        Health = Math.Min(CurrentMaxHealth, Health + amount);
        SendVitals();
    }

    private void SendVitals() =>
        Send(PlayerPackets.PlayerAttributes(EntityId, (float)Math.Ceiling(Health), Food, Saturation, ExperienceLevel, ExperienceProgress, CurrentSpeed, CurrentMaxHealth));

    public void AddFood(int foodAmount, float saturationAmount)
    {
        var targetFood = Math.Clamp(Food + foodAmount, 0, MaxFood);
        var targetSat = Math.Clamp(Saturation + saturationAmount, 0, targetFood);
        if (targetFood == Food && Math.Abs(targetSat - Saturation) < 0.001f)
            return;

        if (_server.Events.HasPlayerHungerChange)
        {
            var ev = new PlayerHungerChangeEvent(this, Food, targetFood, targetSat);
            if (!_server.Events.OnPlayerHungerChange(ev))
            {
                SendVitals();
                return;
            }
            targetFood = ev.NewFood;
            targetSat = ev.NewSaturation;
        }

        if (_server.Events.HasPlayerFoodChange)
        {
            var foodEv = new PlayerFoodChangeEvent(this, Food, targetFood, targetSat);
            if (!_server.Events.OnPlayerFoodChange(foodEv))
            {
                SendVitals();
                return;
            }
            targetFood = foodEv.Food;
            targetSat = foodEv.Saturation;
        }

        Food = targetFood;
        Saturation = targetSat;
        SendVitals();
    }

    /// <summary>Hunger cost of an action (Bedrock values: sprinting 0.1/m, jumping 0.05, attacking 0.1, mining 0.005).</summary>
    public void AddExhaustion(float amount)
    {
        if (Dead || GameMode == GameMode.Creative || _god)
            return;
        _exhaustion += amount;
        if (_exhaustion < 4)
            return;
        _exhaustion -= 4;

        var targetFood = Food;
        var targetSat = Saturation;
        if (targetSat > 0)
            targetSat = Math.Max(0, targetSat - 1);
        else
            targetFood = Math.Max(0, targetFood - 1);

        if (_server.Events.HasPlayerHungerChange)
        {
            var ev = new PlayerHungerChangeEvent(this, Food, targetFood, targetSat);
            if (!_server.Events.OnPlayerHungerChange(ev))
            {
                SendVitals();
                return;
            }
            targetFood = ev.NewFood;
            targetSat = ev.NewSaturation;
        }

        if (_server.Events.HasPlayerFoodChange)
        {
            var foodEv = new PlayerFoodChangeEvent(this, Food, targetFood, targetSat);
            if (!_server.Events.OnPlayerFoodChange(foodEv))
            {
                SendVitals();
                return;
            }
            targetFood = foodEv.Food;
            targetSat = foodEv.Saturation;
        }

        Food = targetFood;
        Saturation = targetSat;
        SendVitals();
    }

    private void Die()
    {
        Dead = true;
        ClearEffects(EffectChangeCause.Death);
        CloseAllForms(FormCancelReason.UserClosed);
        DduiSession.CloseAll();
        _fallPeak = float.NaN;
        _eatingSince = -1;
        _server.BroadcastIn(World, EntityPackets.Event(EntityId, EntityPackets.EventDeath));

        // The crafting grid and cursor go back to the inventory first, so they are listed (and kept or dropped) with
        // the rest; what does not fit falls on the ground even with keep-inventory, instead of vanishing.
        var overflow = Inventory.ReturnCraftingItems();
        var drops = new List<ItemStack>();
        foreach (var inventory in new[] { Inventory.Main, Inventory.Armour, Inventory.Offhand })
            for (var slot = 0; slot < inventory.Size; slot++)
                if (inventory[slot] is { } stack)
                    drops.Add(stack);

        var deathEvent = new PlayerDeathEvent(this, _lastDamageSource, $"{Name} died", _server.Config.KeepInventory, drops);
        _server.Events.OnPlayerDeath(deathEvent);

        if (deathEvent.DeathMessage is { Length: > 0 } msg)
            _server.BroadcastMessage(msg);

        var (x, y, z) = (Position.X, Position.Y - EyeHeight + 0.5f, Position.Z);
        if (!deathEvent.KeepInventory)
        {
            foreach (var inventory in new[] { Inventory.Main, Inventory.Armour, Inventory.Offhand })
                for (var slot = 0; slot < inventory.Size; slot++)
                    inventory[slot] = null;
            foreach (var stack in deathEvent.Drops)
                _server.DropItem(stack, x, y, z, world: World);
        }
        foreach (var stack in overflow)
            _server.DropItem(stack, x, y, z, world: World);

        Send([.. Inventory.ContentPackets()]);
    }

    /// <summary>Respawn packet (state 2 = client ready): back to the world spawn with full health and food.</summary>
    internal void OnRespawnRequest(ref BedrockReader reader)
    {
        reader.Bytes(12); // position
        var state = reader.Byte();
        if (!Dead || state != 2)
            return;
        var (sx, sy, sz) = BedSpawn is { } bed && _server.World?.GetBlock(bed.X, bed.Y - 1, bed.Z)?.Name == "minecraft:bed" ? bed : (_server.World?.Spawn ?? (0, 64, 0));
        var respawnEvent = new PlayerRespawnEvent(this, (sx + 0.5f, sy + EyeHeight, sz + 0.5f));
        _server.Events.OnPlayerRespawn(respawnEvent);

        var (rx, ry, rz) = respawnEvent.RespawnPosition;
        Send(EntityPackets.Respawn(EntityId, rx, ry, rz));
        Dead = false;
        Health = MaxHealth;
        Food = MaxFood;
        Saturation = 5;
        _exhaustion = 0;
        SendVitals();
        ChangeDimension(_server.World!, rx, ry - EyeHeight, rz, respawn: true); // dying in the Nether brings the player back to the overworld
        // Others still see the corpse: re-add the player.
        _server.BroadcastInExcept(World, this, Packets.RemoveActor(EntityId));
        if (Info is not null && !Vanished)
            _server.BroadcastInExcept(World, this, PlayerPackets.AddPlayer(Info, Position.X, Position.Y, Position.Z, Pitch, Yaw, (int)GameMode, NameTagVisible));
    }

    /// <summary>Called every tick by the server.</summary>
    internal void TickHealth()
    {
        if (_hurtTicks > 0)
            _hurtTicks--;
        if (_server.CurrentTick % 100 == 0)
            PruneExpiredForms(_server.CurrentTick);
        TickPortal();
        TickEffects();

        if (Dead || GameMode == GameMode.Creative)
            return;

        // Walking costs nothing, sprinting 0.1 per metre (a sprinting player covers more than 0.25 blocks per tick).
        if (!float.IsNaN(_lastWalk.X))
        {
            var dx = Position.X - _lastWalk.X;
            var dz = Position.Z - _lastWalk.Z;
            var distance = MathF.Sqrt(dx * dx + dz * dz);
            if (distance is > 0.25f and < 2f)
                AddExhaustion(0.1f * distance);
        }
        _lastWalk = (Position.X, Position.Z);

        _foodTicks++;
        if (Food == MaxFood && Saturation > 0 && Health < MaxHealth && _foodTicks >= 10)
        {
            // Saturation boost: fast regeneration.
            _foodTicks = 0;
            var spent = Math.Min(Saturation, 6);
            Heal(spent / 6, HealCause.Food);
            AddExhaustion(spent);
        }
        else if (Food >= 18 && Health < MaxHealth && _foodTicks >= 80)
        {
            _foodTicks = 0;
            Heal(1, HealCause.Food);
            AddExhaustion(6);
        }
        else if (Food == 0 && _foodTicks >= 80)
        {
            _foodTicks = 0;
            var floor = _server.Config.Difficulty switch { Difficulty.Easy => 10, Difficulty.Normal => 1, Difficulty.Hard => 0, _ => 20 };
            if (Health > floor) // starvation stops at half health in easy, at half a heart in normal, kills in hard
            {
                _hurtTicks = 0;
                Damage(1, Position.X, Position.Z, armour: false, source: DamageSource.Starvation());
            }
        }

        if (_eatingSince >= 0 && _server.CurrentTick - _eatingSince >= 30) // 1.6 s, minus some latency margin
            FinishEating();
        if (Position.Y < Chunk.MinY - 64)
            Damage(4, Position.X, Position.Z, armour: false, source: DamageSource.Void());
    }

    // --- Eating ---

    /// <summary>Food points and saturation of each food (vanilla values).</summary>
    private static readonly Dictionary<string, (int Food, float Saturation)> Foods = new()
    {
        ["apple"] = (4, 2.4f), ["baked_potato"] = (5, 6), ["beef"] = (3, 1.8f), ["cooked_beef"] = (8, 12.8f), ["bread"] = (5, 6),
        ["carrot"] = (3, 3.6f), ["chicken"] = (2, 1.2f), ["cooked_chicken"] = (6, 7.2f), ["cod"] = (2, 0.4f), ["cooked_cod"] = (5, 6),
        ["cookie"] = (2, 0.4f), ["golden_apple"] = (4, 9.6f), ["enchanted_golden_apple"] = (4, 9.6f), ["golden_carrot"] = (6, 14.4f),
        ["melon_slice"] = (2, 1.2f), ["mutton"] = (2, 1.2f), ["cooked_mutton"] = (6, 9.6f), ["porkchop"] = (3, 1.8f),
        ["cooked_porkchop"] = (8, 12.8f), ["potato"] = (1, 0.6f), ["pumpkin_pie"] = (8, 4.8f), ["rabbit"] = (3, 1.8f),
        ["cooked_rabbit"] = (5, 6), ["rotten_flesh"] = (4, 0.8f), ["salmon"] = (2, 0.4f), ["cooked_salmon"] = (6, 9.6f),
        ["sweet_berries"] = (2, 0.4f), ["glow_berries"] = (2, 0.4f), ["beetroot"] = (1, 1.2f), ["mushroom_stew"] = (6, 7.2f),
        ["beetroot_soup"] = (6, 7.2f), ["rabbit_stew"] = (10, 12), ["dried_kelp"] = (1, 0.6f), ["honey_bottle"] = (6, 1.2f),
        ["tropical_fish"] = (1, 0.2f), ["pufferfish"] = (1, 0.2f), ["spider_eye"] = (2, 3.2f), ["poisonous_potato"] = (2, 1.2f),
    };

    private static (int Food, float Saturation)? FoodValue(ItemStack? stack)
    {
        if (stack is null) return null;
        if (CustomItemRegistry.Get(stack.Type.Name)?.GetComponent<FoodItemComponent>() is { } customFood)
            return (customFood.Nutrition, customFood.SaturationModifier);
        if (stack.Type.Name.StartsWith("minecraft:") && Foods.TryGetValue(stack.Type.Name["minecraft:".Length..], out var value))
            return value;
        return null;
    }

    /// <summary>Right click in the air: starts eating/drinking if the held item is consumable.</summary>
    private void StartEating()
    {
        if (Inventory.Held is not { } held)
            return;
        var name = held.Type.Name;
        var isPotion = name == "minecraft:potion";
        var isMilk = name == "minecraft:milk_bucket";
        var always = isPotion || isMilk || name is "minecraft:golden_apple" or "minecraft:enchanted_golden_apple"
            || (CustomItemRegistry.Get(name)?.GetComponent<FoodItemComponent>()?.CanAlwaysEat == true);
        if ((FoodValue(held) is not null || isPotion || isMilk) && (Food < MaxFood || always || GameMode == GameMode.Creative) && _eatingSince < 0)
            _eatingSince = _server.CurrentTick;
    }

    private void FinishEating()
    {
        _eatingSince = -1;
        if (Inventory.Held is not { } held)
            return;

        var name = held.Type.Name;
        var isPotion = name == "minecraft:potion";
        var isMilk = name == "minecraft:milk_bucket";
        var food = FoodValue(held);

        if (!isPotion && !isMilk && food is null)
            return;

        if (_server.Events.HasPlayerConsumeItem)
        {
            var consumeEv = new PlayerConsumeItemEvent(this, held);
            if (!_server.Events.OnPlayerConsumeItem(consumeEv))
            {
                Send(InventoryPackets.InventorySlot(InventoryPackets.WindowInventory, Inventory.SelectedSlot, held));
                SendVitals();
                return;
            }
        }

        if (isPotion)
        {
            if (StatusEffects.FromPotionMeta(held.Meta) is { } effect)
                AddEffect(effect, EffectChangeCause.Potion);

            if (GameMode != GameMode.Creative)
            {
                Send(Inventory.ConsumeHeld());
                if (ItemStack.Of("glass_bottle") is { } bottle)
                    Send([.. Inventory.Add(bottle).Packets]);
            }
        }
        else if (isMilk)
        {
            ClearEffects(EffectChangeCause.Milk);

            if (GameMode != GameMode.Creative)
            {
                Send(Inventory.ConsumeHeld());
                if (ItemStack.Of("bucket") is { } bucket)
                    Send([.. Inventory.Add(bucket).Packets]);
            }
        }
        else if (food is { } f)
        {
            AddFood(f.Food, f.Saturation);

            if (held.Type.Name is "minecraft:golden_apple")
            {
                Heal(4, HealCause.Food);
                AddEffect(new EffectInstance(EffectType.Absorption, 0, 2400), EffectChangeCause.Potion);
                AddEffect(new EffectInstance(EffectType.Regeneration, 1, 100), EffectChangeCause.Potion);
            }
            else if (held.Type.Name is "minecraft:enchanted_golden_apple")
            {
                Heal(4, HealCause.Food);
                AddEffect(new EffectInstance(EffectType.Absorption, 3, 2400), EffectChangeCause.Potion);
                AddEffect(new EffectInstance(EffectType.Regeneration, 4, 600), EffectChangeCause.Potion);
                AddEffect(new EffectInstance(EffectType.Resistance, 0, 6000), EffectChangeCause.Potion);
                AddEffect(new EffectInstance(EffectType.FireResistance, 0, 6000), EffectChangeCause.Potion);
            }

            var bowl = held.Type.Name.EndsWith("_stew") || held.Type.Name.EndsWith("_soup");
            if (GameMode != GameMode.Creative)
            {
                Send(Inventory.ConsumeHeld());
                if (bowl && ItemStack.Of("bowl") is { } empty)
                    Send([.. Inventory.Add(empty).Packets]);
            }
        }
        SendVitals();
    }

    /// <summary>Fall damage from the vertical movement reported by the client: 1 per block beyond 3.</summary>
    private void TrackFall(float previousY, float y)
    {
        if (GameMode == GameMode.Creative || Dead)
        {
            _fallPeak = float.NaN;
            return;
        }
        if (y < previousY - 0.001f)
        {
            if (float.IsNaN(_fallPeak))
                _fallPeak = previousY;
            return;
        }
        if (y > previousY + 0.4f && float.IsNaN(_fallPeak))
            AddExhaustion(0.05f); // a jump
        if (float.IsNaN(_fallPeak))
            return;
        var fallen = _fallPeak - y;
        _fallPeak = float.NaN;
        var feet = World.GetBlock((int)Math.Floor(Position.X), (int)Math.Floor(y - EyeHeight), (int)Math.Floor(Position.Z));
        if (fallen > 3.5f && feet != Blocks.Water)
        {
            _hurtTicks = 0;
            Damage((float)Math.Floor(fallen - 3) * (1 - Math.Min(Enchanting.Level(Inventory.Armour[3], "feather_falling") * 3 + ProtectionPoints(), 20) * 0.04f), Position.X, Position.Z, armour: false, source: DamageSource.Fall(fallen));
        }
    }

    /// <summary>Spawn egg used on a block: the mob appears on the clicked face (egg consumed in survival).</summary>
    private void UseSpawnEgg(ItemStack egg, int x, int y, int z, int face)
    {
        if (!egg.Type.Name.StartsWith("minecraft:") || !egg.Type.Name.EndsWith("_spawn_egg"))
            return;
        var mobName = egg.Type.Name["minecraft:".Length..^"_spawn_egg".Length];
        if (!MobKind.ById.TryGetValue(mobName, out var kind))
        {
            SendMessage($"§cThis mob ({mobName}) is not available yet.");
            return;
        }
        var (dx, dy, dz) = face switch { 0 => (0, -2, 0), 1 => (0, 1, 0), 2 => (0, 0, -1), 3 => (0, 0, 1), 4 => (-1, 0, 0), _ => (1, 0, 0) };
        // A plugin can cancel the spawn (EntitySpawn): then the egg is not used up.
        if (_server.Mobs.Spawn(kind, x + dx + 0.5, y + dy, z + dz + 0.5) is not null && GameMode != GameMode.Creative)
            Send(Inventory.ConsumeHeld());
    }

    // --- Combat ---

    // InventoryTransaction type 3 (UseItemOnEntity): target runtime id, action (1 = attack), hotbar slot, held item, ...
    internal void AttackEntity(long targetEntityId)
    {
        var writer = new BedrockWriter();
        writer.VarUInt64((ulong)targetEntityId);
        writer.VarInt32(1);
        var reader = new BedrockReader(writer.ToArray());
        OnUseItemOnEntity(ref reader);
    }

    internal void OnUseItemOnEntity(ref BedrockReader reader)
    {
        var target = (long)reader.VarUInt64();
        var action = reader.VarInt32();
        if (action == 0)
        {
            if (_server.Events.HasPlayerInteractEntity)
                _server.Events.OnPlayerInteractEntity(new PlayerInteractEntityEvent(this, target));
            return;
        }
        if (action != 1 || Dead)
            return;
        var damage = AttackDamage(Inventory.Held);
        if (_activeEffects.TryGetValue(EffectType.Strength, out var str))
            damage += 3f * (str.Amplifier + 1);
        if (_activeEffects.TryGetValue(EffectType.Weakness, out var weak))
            damage = Math.Max(0f, damage - 4f * (weak.Amplifier + 1));
        AddExhaustion(0.1f);
        _server.BroadcastInExcept(World, this, EntityPackets.Swing(EntityId));
        if (_server.Mobs.Find(target) is { } mob)
        {
            var dx = mob.X - Position.X;
            var dz = mob.Z - Position.Z;
            if (dx * dx + dz * dz > 6 * 6)
                return;
            var held = Inventory.Held;
            damage += 1.25f * Enchanting.Level(held, "sharpness");
            if (mob.Kind == MobKind.Zombie || mob.Kind == MobKind.Skeleton)
                damage += 2.5f * Enchanting.Level(held, "smite");
            if (mob.Kind == MobKind.Spider)
                damage += 2.5f * Enchanting.Level(held, "bane_of_arthropods");
            if (_server.Mobs.Damage(mob, damage, (Position.X, Position.Z), this, Enchanting.Level(held, "knockback"), Enchanting.Level(held, "looting"), source: DamageSource.PlayerAttack(this)))
                mob.FireTicks = Math.Max(mob.FireTicks, 80 * Enchanting.Level(held, "fire_aspect"));
            return;
        }
        if (_server.Config.Pvp && _server.Players.FirstOrDefault(p => p.EntityId == target && p.IsInGame) is { } victim && victim != this)
        {
            var dx = victim.Position.X - Position.X;
            var dz = victim.Position.Z - Position.Z;
            if (dx * dx + dz * dz <= 6 * 6)
                victim.Damage(damage + 1.25f * Enchanting.Level(Inventory.Held, "sharpness"), Position.X, Position.Z, source: DamageSource.PlayerAttack(this));
        }
    }

    // Bedrock melee damage per tool (hand = 1).
    private static float AttackDamage(ItemStack? held)
    {
        var name = held?.Type.Name ?? "";
        if (CustomItemRegistry.Get(name)?.GetProperty<DamageItemProperty>() is { } customDamage)
            return customDamage.Damage;
        var tier = name.Split(':')[^1].Split('_')[0] switch
        {
            "wooden" or "golden" => 0,
            "stone" => 1,
            "iron" => 2,
            "diamond" => 3,
            "netherite" => 4,
            _ => -1,
        };
        if (tier < 0)
            return 1;
        return name.EndsWith("_sword") ? 5 + tier
            : name.EndsWith("_axe") ? 4 + tier
            : name.EndsWith("_pickaxe") ? 3 + tier
            : name.EndsWith("_shovel") ? 2 + tier
            : 1;
    }
}

public sealed partial class BedrockConnection
{
    /// <summary>spawn-protection: only operators may build within that radius of the world spawn.</summary>
    private bool SpawnProtected(int x, int z)
    {
        var radius = _server.Config.SpawnProtection;
        if (radius <= 0 || IsOp || World != _server.World)
            return false;
        var (sx, _, sz) = _server.World.Spawn;
        return Math.Abs(x - sx) <= radius && Math.Abs(z - sz) <= radius;
    }
}

public sealed partial class BedrockConnection
{
    // Normal InventoryTransaction: actions are (source, [window], [flags], slot, old item, new item). Dropping with
    // the drop key is "world interaction, flags 0" (new item = what is thrown) plus the inventory slot it left.
    private void OnDropTransaction(ref BedrockReader reader)
    {
        const uint sourceContainer = 0, sourceWorld = 2;
        (int Id, int Count)? thrown = null;
        (int Slot, int OldId, int OldCount, int NewCount)? taken = null;
        var count = reader.VarUInt32();
        for (var i = 0; i < count; i++)
        {
            var source = reader.VarUInt32();
            sbyte? window = reader.Bool() ? (sbyte)reader.Byte() : null;
            uint? flags = reader.Bool() ? reader.VarUInt32() : null;
            var slot = (int)reader.VarUInt32();
            var (oldId, oldCount) = ReadItemIdAndCount(ref reader);
            var (newId, newCount) = ReadItemIdAndCount(ref reader);
            if (source == sourceWorld && flags == 0)
                thrown = (newId, newCount);
            else if (source == sourceContainer && window == 0)
                taken = (slot, oldId, oldCount, newCount);
        }
        if (thrown is not { } t || taken is not { } s || Dead)
            return;
        var stack = Inventory.Main[s.Slot];
        var amount = s.OldCount - s.NewCount;
        if (stack is null || stack.Type.RuntimeId != t.Id || s.OldId != t.Id || amount != t.Count || amount <= 0 || stack.Count < amount)
        {
            Send([.. Inventory.ContentPackets()]); // client and server disagree: resync, nothing is dropped
            return;
        }
        var dropped = stack.Split(amount);
        var dropEvent = new PlayerDropItemEvent(this, dropped);
        if (!_server.Events.OnPlayerDropItem(dropEvent))
        {
            Send(InventoryPackets.InventorySlot(InventoryPackets.WindowInventory, s.Slot, Inventory.Main[s.Slot]));
            return;
        }
        stack.SetCount(stack.Count - amount);
        if (stack.Count == 0)
            Inventory.Main[s.Slot] = null;
        Send(InventoryPackets.InventorySlot(InventoryPackets.WindowInventory, s.Slot, Inventory.Main[s.Slot]));
        DropFromHand(dropped);
    }

    // ItemInstance: id (int16), count (uint16), meta, [stack network id], block runtime id, user data.
    private static (int Id, int Count) ReadItemIdAndCount(ref BedrockReader reader)
    {
        var bytes = reader.Bytes(4);
        int id = System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(bytes);
        int itemCount = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(bytes[2..]);
        reader.VarUInt32();
        if (reader.Bool())
            reader.VarInt32();
        reader.VarUInt32();
        reader.ByteArray();
        return (id, itemCount);
    }
}

public sealed partial class BedrockConnection
{
    /// <summary>
    /// Middle click on a block: the server decides what ends up in the hand (the client must not create items
    /// itself). Survival: brings the block's item from the inventory into the hand. Creative: a full stack of it.
    /// </summary>
    private void OnBlockPick(int x, int y, int z)
    {
        if (Dead || World.GetBlock(x, y, z) is not { IsAir: false } block || ItemStack.Of(block.Name) is not { } item)
            return;
        var main = Inventory.Main;
        var found = Enumerable.Range(0, main.Size).FirstOrDefault(i => main[i]?.Type == item.Type, -1);
        if (found is >= 0 and < 9)
        {
            Inventory.SelectedSlot = found;
            Send(new Bedrokia.Protocol.BedrockWriter().VarUInt32(48).VarUInt32((uint)found).Byte(0).Bool(true).ToArray()); // PlayerHotbar: select it
        }
        else if (found >= 9)
            (main[found], main[Inventory.SelectedSlot]) = (main[Inventory.SelectedSlot], main[found]);
        else if (GameMode == GameMode.Creative)
            main[Inventory.SelectedSlot] = item.Split(item.MaxCount);
        Send([.. Inventory.ContentPackets()]);
    }
}
