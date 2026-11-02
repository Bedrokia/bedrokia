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

namespace Bedrokia.Server.Network;

/// <summary>
/// Items with a use of their own: bow, crossbow, trident, fishing rod, goat horn, firework rocket, glass bottle,
/// fish buckets, totem of undying, shield, mace; and the wear of tools, weapons and armour (vanilla durability).
/// </summary>
/// <remarks>
/// ponytail: no Riptide, Channeling, Piercing or Impaling.
/// </remarks>
public sealed partial class BedrockConnection
{
    private const byte EventConsumeTotem = 65;
    private long? _chargeSince; // bow, crossbow or trident drawn since this tick
    private long _hornReadyAt;

    /// <summary>What the player rides: a <see cref="Vehicle"/> (boat, minecart) or a saddled <see cref="Mob"/>; null on foot.</summary>
    public object? Riding { get; internal set; }

    private const uint MapInfoRequestId = 68;

    // The client asks for a map it shows (held, in an item frame...).
    private void OnMapInfoRequest(ref BedrockReader reader)
    {
        if (_server.Maps.Get(reader.VarInt64()) is { } map)
            Send(MapManager.Packet(map, this));
    }

    /// <summary>Shows everyone (the player included) that the player sits at <paramref name="seatY"/> on its vehicle, or stands.</summary>
    internal void SendRidingData(float? seatY)
    {
        _seatY = seatY;
        _server.BroadcastIn(World, DataPacket());
    }

    // --- Right click in the air ---

    /// <summary>Right click in the air with an item that has a use of its own; false for the others (food, armour...).</summary>
    private bool UseHeldInAir()
    {
        if (Inventory.Held is not { } held)
            return false;
        var tick = _server.CurrentTick;
        switch (held.Type.Name)
        {
            case "minecraft:crossbow" when held.Nbt?.ContainsKey("chargedItem") == true:
                FireCrossbow(held); // loaded: a click shoots
                return true;
            case "minecraft:bow" or "minecraft:crossbow":
                if (GameMode == GameMode.Creative || Enchanting.Level(held, "infinity") > 0 || ArrowSlot() is not null)
                    _chargeSince = tick;
                return true;
            case "minecraft:trident":
                _chargeSince = tick;
                return true;
            case "minecraft:fishing_rod":
                if (_server.Projectiles.HookOf(this) is { } hook)
                {
                    if (_server.Projectiles.Reel(hook))
                        WearHeld(1);
                }
                else
                {
                    _server.Projectiles.Fire(this, ProjectileManager.FishingHook, 1.1f, 0);
                    _server.PlaySound("random.bow", Position.X, Position.Y, Position.Z, World, 0.5f, 0.4f);
                }
                return true;
            case "minecraft:goat_horn":
                if (tick >= _hornReadyAt)
                {
                    _hornReadyAt = tick + 140; // 7 s, as vanilla
                    _server.PlaySound($"horn.call.{Math.Clamp((int)held.Meta, 0, 7)}", Position.X, Position.Y, Position.Z, World, 16);
                }
                return true;
            case "minecraft:firework_rocket":
                // Elytra boost: only while flying (elytra worn, nothing under the feet).
                if (Inventory.Armour[1]?.Type.Name != "minecraft:elytra"
                    || World.GetBlock((int)Math.Floor(Position.X), (int)Math.Floor(Position.Y - EyeHeight - 0.5f), (int)Math.Floor(Position.Z)) is not { IsAir: true })
                    return true;
                var yaw = Yaw * MathF.PI / 180;
                var pitch = Pitch * MathF.PI / 180;
                SetMotion(-MathF.Sin(yaw) * MathF.Cos(pitch) * 1.7, -MathF.Sin(pitch) * 1.7, MathF.Cos(yaw) * MathF.Cos(pitch) * 1.7);
                _server.PlaySound("firework.launch", Position.X, Position.Y, Position.Z, World);
                if (GameMode != GameMode.Creative)
                    Send(Inventory.ConsumeHeld());
                return true;
            case "minecraft:glass_bottle":
                return FillBottle();
            case "minecraft:empty_map":
                if (_server.Maps.Create(this) is { } map)
                {
                    if (GameMode != GameMode.Creative)
                        Send(Inventory.ConsumeHeld());
                    GiveItem(map);
                    _server.PlaySound("ui.cartography_table.take_result", Position.X, Position.Y, Position.Z, World);
                }
                return true;
        }
        return PlaceBoatOnWater(held);
    }

    /// <summary>The use key was released: a drawn bow or crossbow shoots, a trident is thrown.</summary>
    internal void OnReleaseItem()
    {
        var since = _chargeSince;
        _chargeSince = null;
        if (since is null || Inventory.Held is not { } held || Dead)
            return;
        var ticks = (int)(_server.CurrentTick - since.Value);
        switch (held.Type.Name)
        {
            case "minecraft:bow":
                ShootBow(held, ticks);
                break;
            case "minecraft:crossbow":
                LoadCrossbow(held, ticks);
                break;
            case "minecraft:trident" when ticks >= 10:
                ThrowTrident(held);
                break;
        }
    }

    // Vanilla bow: power from the draw time (full at 1 s), damage 2 x speed (Power adds), a critical hit when full.
    private void ShootBow(ItemStack bow, int ticks)
    {
        var draw = ticks / 20f;
        draw = Math.Min(1, (draw * draw + draw * 2) / 3);
        if (draw < 0.1f)
            return;
        var infinite = GameMode == GameMode.Creative || Enchanting.Level(bow, "infinity") > 0;
        if (!infinite && !TakeArrow())
            return;
        var power = Enchanting.Level(bow, "power");
        var speed = draw * 3;
        var damage = MathF.Ceiling((2 + (power > 0 ? power * 0.5f + 0.5f : 0)) * speed);
        if (draw >= 1)
            damage += Random.Shared.Next((int)damage / 2 + 2);
        _server.Projectiles.Fire(this, ProjectileManager.Arrow, speed, damage, punch: Enchanting.Level(bow, "punch"),
            flame: Enchanting.Level(bow, "flame") > 0, pickup: !infinite);
        _server.PlaySound("random.bow", Position.X, Position.Y, Position.Z, World, 1, 1.2f - draw * 0.5f);
        WearHeld(1);
    }

    // Crossbow: charged in 1.25 s (Quick Charge: 0.25 s less per level), it stays loaded (the arrow is in the item's
    // "chargedItem", which the client shows) until the next click fires it.
    private void LoadCrossbow(ItemStack crossbow, int ticks)
    {
        if (ticks < 25 - 5 * Enchanting.Level(crossbow, "quick_charge"))
            return;
        if (GameMode != GameMode.Creative && !TakeArrow())
            return;
        var nbt = crossbow.Nbt is null ? new OrderedDictionary<string, object>() : new OrderedDictionary<string, object>(crossbow.Nbt);
        nbt["chargedItem"] = new OrderedDictionary<string, object>
        {
            ["Count"] = (byte)1, ["Damage"] = (short)0, ["Name"] = "minecraft:arrow", ["WasPickedUp"] = (byte)0,
        };
        SetHeld(new ItemStack(crossbow.Type, crossbow.Count, crossbow.Meta, crossbow.BlockRuntimeId, nbt));
        _server.PlaySound("crossbow.loading.end", Position.X, Position.Y, Position.Z, World);
    }

    // A loaded crossbow shoots hard; Multishot fires three arrows.
    private void FireCrossbow(ItemStack crossbow)
    {
        var multishot = Enchanting.Level(crossbow, "multishot") > 0;
        foreach (var offset in multishot ? new[] { -10f, 0f, 10f } : [0f])
            _server.Projectiles.Fire(this, ProjectileManager.Arrow, 3.15f, 6 + Random.Shared.Next(6), yawOffset: offset,
                pickup: GameMode != GameMode.Creative && offset == 0);
        _server.PlaySound("crossbow.shoot", Position.X, Position.Y, Position.Z, World);
        var nbt = new OrderedDictionary<string, object>(crossbow.Nbt!);
        nbt.Remove("chargedItem");
        SetHeld(new ItemStack(crossbow.Type, crossbow.Count, crossbow.Meta, crossbow.BlockRuntimeId, nbt.Count == 0 ? null : nbt));
        WearHeld(multishot ? 3 : 1);
    }

    private void SetHeld(ItemStack? stack)
    {
        Inventory.Main[Inventory.SelectedSlot] = stack;
        Send(InventoryPackets.InventorySlot(InventoryPackets.WindowInventory, Inventory.SelectedSlot, stack));
    }

    // Trident: thrown from the hand (kept in creative), 8 damage; it lands on the ground or comes back with Loyalty.
    private void ThrowTrident(ItemStack trident)
    {
        ItemStack? thrown = null;
        if (GameMode != GameMode.Creative)
        {
            thrown = Durability.Wear(trident, 1, Random.Shared);
            Inventory.Main[Inventory.SelectedSlot] = null;
            Send(InventoryPackets.InventorySlot(InventoryPackets.WindowInventory, Inventory.SelectedSlot, null));
            if (thrown is null)
            {
                _server.PlaySound("random.break", Position.X, Position.Y, Position.Z, World);
                return;
            }
        }
        _server.Projectiles.Fire(this, ProjectileManager.Trident, 2.5f, 8, item: thrown);
        _server.PlaySound("item.trident.throw", Position.X, Position.Y, Position.Z, World);
    }

    // The arrow slot used first: off hand, then the inventory in order. Null when there is none.
    private (Inventory Container, int Slot, uint Window)? ArrowSlot()
    {
        if (Inventory.Offhand[0]?.Type.Name == "minecraft:arrow")
            return (Inventory.Offhand, 0, InventoryPackets.WindowOffhand);
        for (var i = 0; i < Inventory.Main.Size; i++)
            if (Inventory.Main[i]?.Type.Name == "minecraft:arrow")
                return (Inventory.Main, i, InventoryPackets.WindowInventory);
        return null;
    }

    private bool TakeArrow()
    {
        if (ArrowSlot() is not { } at || at.Container[at.Slot] is not { } arrows)
            return false;
        arrows.SetCount(arrows.Count - 1);
        if (arrows.Count == 0)
            at.Container[at.Slot] = null;
        Send(InventoryPackets.InventorySlot(at.Window, at.Slot, at.Container[at.Slot]));
        return true;
    }

    // The water block the player looks at (5 blocks at most, nothing solid before it).
    private (int X, int Y, int Z)? WaterInSight()
    {
        var yaw = Yaw * MathF.PI / 180;
        var pitch = Pitch * MathF.PI / 180;
        var (dx, dy, dz) = (-MathF.Sin(yaw) * MathF.Cos(pitch), -MathF.Sin(pitch), MathF.Cos(yaw) * MathF.Cos(pitch));
        for (var d = 0.5f; d <= 5; d += 0.25f)
        {
            int bx = (int)MathF.Floor(Position.X + dx * d), by = (int)MathF.Floor(Position.Y + dy * d), bz = (int)MathF.Floor(Position.Z + dz * d);
            var block = World.GetBlock(bx, by, bz);
            if (block?.Name is "minecraft:water" or "minecraft:flowing_water")
                return (bx, by, bz);
            if (block is { IsAir: false })
                return null;
        }
        return null;
    }

    // A glass bottle aimed at water becomes a water bottle.
    private bool FillBottle()
    {
        if (WaterInSight() is null)
            return false;
        GiveBucket("potion"); // meta 0: water bottle
        _server.PlaySound("bucket.fill_water", Position.X, Position.Y, Position.Z, World);
        return true;
    }

    // A boat item aimed at water: the boat floats on it.
    private bool PlaceBoatOnWater(ItemStack held)
    {
        if (VehicleManager.ForItem(held.Type.Name) is not { } kind || kind.Identifier.Contains("minecart") || WaterInSight() is not var (x, y, z))
            return false;
        _server.Vehicles.Place(held.Type.Name, World, x + 0.5, y + 0.9, z + 0.5, Yaw);
        if (GameMode != GameMode.Creative)
            Send(Inventory.ConsumeHeld());
        return true;
    }

    // --- Used on a block ---

    /// <summary>Items used on a block that are not in <see cref="UseItemOn"/>'s first list; true when used.</summary>
    private bool UseSpecialItemOn(string item, BlockType clicked, int x, int y, int z, int face)
    {
        var (fx, fy, fz) = FaceOffset(face);
        var survival = GameMode != GameMode.Creative;

        // Axe: strips logs, wood, stems and bamboo blocks.
        if (item.EndsWith("_axe") && !clicked.Name.Contains("stripped_")
            && (clicked.Name.EndsWith("_log") || clicked.Name.EndsWith("_wood") || clicked.Name.EndsWith("_stem")
                || clicked.Name.EndsWith("_hyphae") || clicked.Name == "minecraft:bamboo_block")
            && Blocks.ByName("stripped_" + clicked.Name["minecraft:".Length..]) is not null)
        {
            var stripped = Blocks.Get("stripped_" + clicked.Name["minecraft:".Length..], [.. clicked.States.Select(s => (s.Key, s.Value))]);
            _server.SetBlockAndUpdate(x, y, z, stripped, World);
            _server.PlaySound("use.wood", x + 0.5, y + 0.5, z + 0.5, World);
            WearHeld(1);
            return true;
        }
        // Shears carve a pumpkin (and give its seeds).
        if (item == "shears" && clicked.Name == "minecraft:pumpkin" && Blocks.ByName("carved_pumpkin") is not null)
        {
            var carved = Blocks.Get("carved_pumpkin", [.. clicked.States.Select(s => (s.Key, s.Value))]);
            _server.SetBlockAndUpdate(x, y, z, carved, World);
            if (ItemStack.Of("pumpkin_seeds", 4) is { } seeds)
                _server.DropItem(seeds, x + 0.5f + fx * 0.6f, y + 0.5f + fy * 0.6f, z + 0.5f + fz * 0.6f, world: World);
            WearHeld(1);
            return true;
        }
        // Fire charge: like flint and steel, used up instead of worn.
        if (item == "fire_charge")
        {
            if (clicked.Name == "minecraft:tnt")
            {
                _server.SetBlockAndUpdate(x, y, z, Blocks.Air, World);
                _server.PrimeTnt(x, y, z, world: World);
            }
            else if (!(clicked.Name == "minecraft:obsidian" && TryLightPortal(x + fx, y + fy, z + fz)))
            {
                if (World.GetBlock(x + fx, y + fy, z + fz) is not { IsAir: true } || Blocks.ByName("fire") is not { } fire)
                    return false;
                _server.SetBlockAndUpdate(x + fx, y + fy, z + fz, fire, World);
            }
            _server.PlaySound("mob.ghast.fireball", x + 0.5, y + 0.5, z + 0.5, World);
            if (survival)
                Send(Inventory.ConsumeHeld());
            return true;
        }
        if (item == "glass_bottle")
            return FillBottle();
        // Snow on snow: one more layer (8 at most), as vanilla.
        if (item == "snow_layer")
        {
            foreach (var (sx, sy, sz) in new[] { (x, y, z), (x + fx, y + fy, z + fz) })
            {
                if (World.GetBlock(sx, sy, sz) is not { Name: "minecraft:snow_layer" } layer || SpawnProtected(sx, sz))
                    continue;
                var height = Convert.ToInt32(layer.States.GetValueOrDefault("height", 0));
                if (height >= 7)
                    continue;
                _server.SetBlockAndUpdate(sx, sy, sz, Blocks.Get("snow_layer",
                    ("covered_bit", layer.States.GetValueOrDefault("covered_bit", (byte)0)), ("height", height + 1)), World);
                _server.PlaySound("use.snow", sx + 0.5, sy + 0.5, sz + 0.5, World);
                if (survival)
                    Send(Inventory.ConsumeHeld());
                return true;
            }
            return false;
        }
        // Boats on water or the ground, minecarts on a rail.
        if (VehicleManager.ForItem(item) is { } vehicle)
        {
            double vx, vy, vz;
            if (vehicle.Identifier.Contains("minecart"))
            {
                if (!Rails.IsRail(clicked))
                    return false;
                (vx, vy, vz) = (x + 0.5, y, z + 0.5);
            }
            else if (clicked.Name is "minecraft:water" or "minecraft:flowing_water")
                (vx, vy, vz) = (x + 0.5, y + 0.9, z + 0.5);
            else if (World.GetBlock(x + fx, y + fy, z + fz) is { } ground && !BlockPhysics(ground))
                (vx, vy, vz) = (x + fx + 0.5, y + fy, z + fz + 0.5);
            else
                return false;
            _server.Vehicles.Place("minecraft:" + item, World, vx, vy, vz, Yaw);
            if (survival)
                Send(Inventory.ConsumeHeld());
            return true;
        }
        // Fish buckets: water and the fish in it.
        if (FishInBucket(item) is { } fish)
        {
            if (World.GetBlock(x + fx, y + fy, z + fz) is not { } target || BlockPhysics(target) || SpawnProtected(x + fx, z + fz))
                return false;
            if (World.Dimension != 1)
                _server.SetBlockAndUpdate(x + fx, y + fy, z + fz, Fluids.Source("water"), World);
            _server.Mobs.Spawn(fish, x + fx + 0.5, y + fy, z + fz + 0.5, World);
            if (survival)
                GiveBucket("bucket");
            return true;
        }
        if (item == "powder_snow_bucket" && Blocks.ByName("powder_snow") is { } snow
            && World.GetBlock(x + fx, y + fy, z + fz) is { } free && !BlockPhysics(free) && !SpawnProtected(x + fx, z + fz))
        {
            _server.SetBlockAndUpdate(x + fx, y + fy, z + fz, snow, World);
            if (survival)
                GiveBucket("bucket");
            return true;
        }
        if (item == "firework_rocket")
        {
            _server.LaunchFirework(x + fx + 0.5f, y + fy + 0.2f, z + fz + 0.5f, Inventory.Held!, World);
            if (survival)
                Send(Inventory.ConsumeHeld());
            return true;
        }
        return false;
    }

    internal static MobKind? FishInBucket(string item) => item switch
    {
        "cod_bucket" => MobKind.Cod,
        "salmon_bucket" => MobKind.Salmon,
        "tropical_fish_bucket" => MobKind.TropicalFish,
        "pufferfish_bucket" => MobKind.Pufferfish,
        "axolotl_bucket" => MobKind.Axolotl,
        "tadpole_bucket" => MobKind.Tadpole,
        _ => null,
    };

    // --- Wear ---

    /// <summary>Wears the held item by <paramref name="amount"/> (survival); it breaks with a sound when used up.</summary>
    internal void WearHeld(int amount)
    {
        if (GameMode == GameMode.Creative || Inventory.Held is not { } held)
            return;
        var worn = Durability.Wear(held, amount, Random.Shared);
        if (ReferenceEquals(worn, held))
            return;
        Inventory.Main[Inventory.SelectedSlot] = worn;
        Send(InventoryPackets.InventorySlot(InventoryPackets.WindowInventory, Inventory.SelectedSlot, worn));
        if (worn is null)
            _server.PlaySound("random.break", Position.X, Position.Y, Position.Z, World);
    }

    // Each worn piece loses a point per 4 damage (at least one), as vanilla.
    private void WearArmour(float damage)
    {
        var amount = Math.Max(1, (int)(damage / 4));
        for (var slot = 0; slot < 4; slot++)
        {
            if (Inventory.Armour[slot] is not { } piece || Durability.Max(piece) == 0)
                continue;
            var worn = Durability.Wear(piece, amount, Random.Shared, armour: true);
            if (ReferenceEquals(worn, piece))
                continue;
            Inventory.Armour[slot] = worn;
            Send(InventoryPackets.InventorySlot(InventoryPackets.WindowArmour, slot, worn));
            if (worn is null)
                _server.PlaySound("random.break", Position.X, Position.Y, Position.Z, World);
        }
    }

    // --- Shield, totem, mace ---

    /// <summary>A raised shield (sneaking with it in a hand) blocks attacks from the front and wears instead.</summary>
    private bool BlockedByShield(DamageSource source, float amount, double fromX, double fromZ)
    {
        if (!Sneaking || source.Cause is not (DamageCause.EntityAttack or DamageCause.PlayerAttack or DamageCause.Explosion))
            return false;
        var inOffhand = Inventory.Offhand[0]?.Type.Name == "minecraft:shield";
        if (!inOffhand && Inventory.Held?.Type.Name != "minecraft:shield")
            return false;
        // In front: the attacker within 90° of where the player looks.
        var yaw = Yaw * Math.PI / 180;
        double lookX = -Math.Sin(yaw), lookZ = Math.Cos(yaw), toX = fromX - Position.X, toZ = fromZ - Position.Z;
        if (lookX * toX + lookZ * toZ <= 0)
            return false;
        _server.PlaySound("item.shield.block", Position.X, Position.Y, Position.Z, World);
        if (amount >= 3 && GameMode != GameMode.Creative)
        {
            var uses = 1 + (int)amount;
            if (inOffhand)
            {
                var worn = Durability.Wear(Inventory.Offhand[0]!, uses, Random.Shared);
                Inventory.Offhand[0] = worn;
                Send(InventoryPackets.InventorySlot(InventoryPackets.WindowOffhand, 0, worn));
                if (worn is null)
                    _server.PlaySound("random.break", Position.X, Position.Y, Position.Z, World);
            }
            else
                WearHeld(uses);
        }
        return true;
    }

    /// <summary>A totem of undying in a hand saves the player from death (not from the void).</summary>
    private bool UseTotem(DamageSource source)
    {
        if (source.Cause == DamageCause.Void)
            return false;
        if (Inventory.Offhand[0]?.Type.Name == "minecraft:totem_of_undying")
        {
            Inventory.Offhand[0] = null;
            Send(InventoryPackets.InventorySlot(InventoryPackets.WindowOffhand, 0, null));
        }
        else if (Inventory.Held?.Type.Name == "minecraft:totem_of_undying")
        {
            Inventory.Main[Inventory.SelectedSlot] = null;
            Send(InventoryPackets.InventorySlot(InventoryPackets.WindowInventory, Inventory.SelectedSlot, null));
        }
        else
            return false;
        Health = 1;
        ClearEffects();
        AddEffect(new EffectInstance(EffectType.Regeneration, 1, 900));
        AddEffect(new EffectInstance(EffectType.Absorption, 1, 100));
        AddEffect(new EffectInstance(EffectType.FireResistance, 0, 800));
        SendVitals();
        _server.BroadcastIn(World, EntityPackets.Event(EntityId, EventConsumeTotem));
        _server.PlaySound("random.totem", Position.X, Position.Y, Position.Z, World);
        return true;
    }

    /// <summary>
    /// Mace smash attack: hitting while falling adds 4 damage per block for the first 3 blocks, 2 per block for the next
    /// 5, 1 per block beyond, and cancels the fall damage.
    /// </summary>
    private float MaceBonus(ItemStack? held)
    {
        if (held?.Type.Name != "minecraft:mace" || float.IsNaN(_fallPeak))
            return 0;
        var fallen = _fallPeak - Position.Y;
        if (fallen <= 1.5f)
            return 0;
        _fallPeak = float.NaN;
        _server.PlaySound("mace.smash_ground", Position.X, Position.Y, Position.Z, World);
        return 4 * Math.Min(fallen, 3) + 2 * Math.Clamp(fallen - 3, 0, 5) + Math.Max(0, fallen - 8);
    }

    // Wear of the weapon after a hit: swords, axes, tridents and maces 1, other tools 2.
    private void WearWeapon()
    {
        var name = Inventory.Held?.Type.Name ?? "";
        WearHeld(name.EndsWith("_sword") || name.EndsWith("_axe") || name is "minecraft:trident" or "minecraft:mace" ? 1 : 2);
    }
}
