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

namespace Bedrokia.Server.Game.Custom.Items;

public sealed record DisplayNameItemComponent(string Value) : IItemComponent
{
    public string Name => ItemComponentIds.DisplayName;
    public OrderedDictionary<string, object> BuildNbt() => new() { ["value"] = Value };
}

public sealed record DurabilityItemComponent(int MaxDurability, int MinDamageChance = 1, int MaxDamageChance = 1) : IItemComponent
{
    public string Name => ItemComponentIds.Durability;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["max_durability"] = MaxDurability,
        ["damage_chance"] = new OrderedDictionary<string, object>
        {
            ["min"] = MinDamageChance,
            ["max"] = MaxDamageChance
        }
    };
}

public sealed record DurabilitySensorItemComponent(int DurabilityThreshold) : IItemComponent
{
    public string Name => ItemComponentIds.DurabilitySensor;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["durability_thresholds"] = new NbtList(Nbt.Compound, [
            new OrderedDictionary<string, object> { ["durability"] = DurabilityThreshold }
        ])
    };
}

public sealed record FoodItemComponent(
    int Nutrition,
    float SaturationModifier,
    bool CanAlwaysEat = false,
    string UsingConvertsTo = "minecraft:air",
    int CooldownTime = 0,
    string CooldownType = "none") : IItemComponent
{
    public string Name => ItemComponentIds.Food;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["nutrition"] = Nutrition,
        ["saturation_modifier"] = SaturationModifier,
        ["can_always_eat"] = (byte)(CanAlwaysEat ? 1 : 0),
        ["using_converts_to"] = new OrderedDictionary<string, object> { ["name"] = UsingConvertsTo },
        ["cooldown_time"] = CooldownTime,
        ["cooldown_type"] = CooldownType
    };
}

public sealed record ArmorItemComponent(int Protection, string TextureType = "iron") : IItemComponent
{
    public string Name => ItemComponentIds.Armor;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["protection"] = Protection,
        ["texture_type"] = TextureType
    };
}

public sealed record WearableItemComponent(string Slot, int Protection = 0) : IItemComponent
{
    public string Name => ItemComponentIds.Wearable;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["slot"] = Slot,
        ["protection"] = Protection
    };
}

public sealed record DestroySpeed(string BlockTagOrName, float Speed);

public sealed record DiggerItemComponent(IReadOnlyList<DestroySpeed> DestroySpeeds, bool UseEfficiency = true) : IItemComponent
{
    public string Name => ItemComponentIds.Digger;
    public OrderedDictionary<string, object> BuildNbt()
    {
        var list = new List<object>();
        foreach (var ds in DestroySpeeds)
        {
            list.Add(new OrderedDictionary<string, object>
            {
                // "query.any_tag('stone', ...)" selects blocks by tag, anything else is a block name.
                ["block"] = ds.BlockTagOrName.StartsWith("query.", StringComparison.Ordinal)
                    ? new OrderedDictionary<string, object> { ["tags"] = ds.BlockTagOrName }
                    : new OrderedDictionary<string, object> { ["name"] = ds.BlockTagOrName },
                ["speed"] = ds.Speed
            });
        }
        return new OrderedDictionary<string, object>
        {
            ["use_efficiency"] = (byte)(UseEfficiency ? 1 : 0),
            ["destroy_speeds"] = new NbtList(Nbt.Compound, list)
        };
    }
}

public sealed record EnchantableItemComponent(string Slot, int Value) : IItemComponent
{
    public string Name => ItemComponentIds.Enchantable;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["slot"] = Slot,
        ["value"] = Value
    };
}

public sealed record CooldownItemComponent(string Category, float DurationSeconds) : IItemComponent
{
    public string Name => ItemComponentIds.Cooldown;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["category"] = Category,
        ["duration"] = DurationSeconds
    };
}

public sealed record FuelItemComponent(float DurationSeconds) : IItemComponent
{
    public string Name => ItemComponentIds.Fuel;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["duration"] = DurationSeconds
    };
}

public sealed record FireResistantItemComponent(bool FireResistant = true) : IItemComponent
{
    public string Name => ItemComponentIds.FireResistant;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["value"] = (byte)(FireResistant ? 1 : 0)
    };
}

public sealed record TagsItemComponent(IReadOnlyList<string> Tags) : IItemComponent
{
    public string Name => ItemComponentIds.Tags;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["tags"] = new NbtList(Nbt.String, Tags.Cast<object>().ToList())
    };
}

public sealed record BlockPlacerItemComponent(
    string Block,
    bool CanUseBlockAsIcon = false,
    bool ReplaceBlockItem = false,
    bool AlignedPlacement = false,
    IReadOnlyList<string>? UseOn = null) : IItemComponent
{
    public string Name => ItemComponentIds.BlockPlacer;
    public OrderedDictionary<string, object> BuildNbt()
    {
        var dict = new OrderedDictionary<string, object>
        {
            ["block"] = Block,
            ["canUseBlockAsIcon"] = (byte)(CanUseBlockAsIcon ? 1 : 0),
            ["replace_block_item"] = (byte)(ReplaceBlockItem ? 1 : 0),
            ["aligned_placement"] = (byte)(AlignedPlacement ? 1 : 0)
        };
        if (UseOn is { Count: > 0 } useOnList)
        {
            var blocks = useOnList.Select(u => (object)new OrderedDictionary<string, object> { ["name"] = u }).ToList();
            dict["use_on"] = new NbtList(Nbt.Compound, blocks);
        }
        return dict;
    }
}

public sealed record ThrowableItemComponent(bool DoSwingAnimation = true, float LaunchPowerScale = 1.0f, float MaxDrawDuration = 0.0f) : IItemComponent
{
    public string Name => ItemComponentIds.Throwable;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["do_swing_animation"] = (byte)(DoSwingAnimation ? 1 : 0),
        ["launch_power_scale"] = LaunchPowerScale,
        ["max_draw_duration"] = MaxDrawDuration
    };
}

public sealed record ProjectileItemComponent(string ProjectileEntity, float MinimumCriticalDamage = 0.0f) : IItemComponent
{
    public string Name => ItemComponentIds.Projectile;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["projectile_entity"] = ProjectileEntity,
        ["minimum_critical_damage"] = MinimumCriticalDamage
    };
}

public sealed record RecordItemComponent(string SoundEvent, int Duration = 0) : IItemComponent
{
    public string Name => ItemComponentIds.Record;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["sound_event"] = SoundEvent,
        ["duration"] = Duration
    };
}

public sealed record RepairItemEntry(IReadOnlyList<string> Items, int RepairAmount);

public sealed record RepairableItemComponent(IReadOnlyList<RepairItemEntry> RepairEntries) : IItemComponent
{
    public string Name => ItemComponentIds.Repairable;
    public OrderedDictionary<string, object> BuildNbt()
    {
        var list = new List<object>();
        foreach (var entry in RepairEntries)
        {
            list.Add(new OrderedDictionary<string, object>
            {
                ["items"] = new NbtList(Nbt.String, entry.Items.Cast<object>().ToList()),
                ["repair_amount"] = entry.RepairAmount
            });
        }
        return new OrderedDictionary<string, object>
        {
            ["repair_items"] = new NbtList(Nbt.Compound, list)
        };
    }
}

public sealed record EntityPlacerItemComponent(string Entity, IReadOnlyList<string>? DispenseOn = null) : IItemComponent
{
    public string Name => ItemComponentIds.EntityPlacer;
    public OrderedDictionary<string, object> BuildNbt()
    {
        var dict = new OrderedDictionary<string, object> { ["entity"] = Entity };
        if (DispenseOn is { Count: > 0 } list)
            dict["dispense_on"] = new NbtList(Nbt.String, list.Cast<object>().ToList());
        return dict;
    }
}

public sealed record DyeableItemComponent(string DefaultColor) : IItemComponent
{
    public string Name => ItemComponentIds.Dyeable;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["default_color"] = DefaultColor
    };
}

public sealed record UseModifiersItemComponent(float UseDuration = 0.0f, float MovementModifier = 1.0f) : IItemComponent
{
    public string Name => ItemComponentIds.UseModifiers;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["use_duration"] = UseDuration,
        ["movement_modifier"] = MovementModifier
    };
}

public sealed record StorageItemComponent(int MaxWeightLimit, int Weight = 1) : IItemComponent
{
    public string Name => ItemComponentIds.StorageItem;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["max_weight_limit"] = MaxWeightLimit,
        ["weight"] = Weight
    };
}

public sealed record SwingDurationItemComponent(float Duration) : IItemComponent
{
    public string Name => ItemComponentIds.SwingDuration;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["duration"] = Duration
    };
}

public sealed record CustomComponentsItemComponent(IReadOnlyList<string> Components) : IItemComponent
{
    public string Name => ItemComponentIds.CustomComponents;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["components"] = new NbtList(Nbt.String, Components.Cast<object>().ToList())
    };
}
