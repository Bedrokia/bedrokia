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

namespace Bedrokia.Server.Game.Custom.Items;

public sealed record IconItemProperty(string Texture) : IItemProperty
{
    public string Name => PropertyComponentIds.Icon;
    public object BuildValue() => new OrderedDictionary<string, object> { ["texture"] = Texture };
}

public sealed record DamageItemProperty(int Damage) : IItemProperty
{
    public string Name => PropertyComponentIds.Damage;
    public object BuildValue() => Damage;
}

public sealed record MaxStackSizeItemProperty(int MaxStackSize) : IItemProperty
{
    public string Name => PropertyComponentIds.MaxStackSize;
    public object BuildValue() => MaxStackSize;
}

public sealed record HandEquippedItemProperty(bool HandEquipped = true) : IItemProperty
{
    public string Name => PropertyComponentIds.HandEquipped;
    public object BuildValue() => HandEquipped;
}

public sealed record CanDestroyInCreativeItemProperty(bool CanDestroy = true) : IItemProperty
{
    public string Name => PropertyComponentIds.CanDestroyInCreative;
    public object BuildValue() => CanDestroy;
}

public sealed record FoilItemProperty(bool Foil = true) : IItemProperty
{
    public string Name => PropertyComponentIds.Foil;
    public object BuildValue() => Foil;
}

public sealed record AllowOffHandItemProperty(bool Allow = true) : IItemProperty
{
    public string Name => PropertyComponentIds.AllowOffHand;
    public object BuildValue() => Allow;
}

public enum UseAnimation
{
    None = 0,
    Eat = 1,
    Drink = 2,
    Block = 3,
    Bow = 4,
    Camera = 5,
    Spear = 6,
    Crossbow = 7,
    Spyglass = 8,
    GoatHorn = 9,
    Brush = 10
}

public sealed record UseAnimationItemProperty(int Animation) : IItemProperty
{
    public string Name => PropertyComponentIds.UseAnimation;
    public object BuildValue() => Animation;
}

public sealed record UseDurationItemProperty(int Duration) : IItemProperty
{
    public string Name => PropertyComponentIds.UseDuration;
    public object BuildValue() => Duration;
}

public sealed record MiningSpeedItemProperty(float Speed) : IItemProperty
{
    public string Name => PropertyComponentIds.MiningSpeed;
    public object BuildValue() => Speed;
}

public sealed record ShouldDespawnItemProperty(bool ShouldDespawn = true) : IItemProperty
{
    public string Name => PropertyComponentIds.ShouldDespawn;
    public object BuildValue() => ShouldDespawn;
}

public sealed record StackedByDataItemProperty(bool StackedByData = true) : IItemProperty
{
    public string Name => PropertyComponentIds.StackedByData;
    public object BuildValue() => StackedByData;
}

public sealed record LiquidClippedItemProperty(bool LiquidClipped = true) : IItemProperty
{
    public string Name => PropertyComponentIds.LiquidClipped;
    public object BuildValue() => LiquidClipped;
}

public sealed record HoverTextColorItemProperty(string Color) : IItemProperty
{
    public string Name => PropertyComponentIds.HoverTextColor;
    public object BuildValue() => Color;
}

public sealed record RarityItemProperty(string Rarity) : IItemProperty
{
    public string Name => PropertyComponentIds.Rarity;
    public object BuildValue() => Rarity;
}

public sealed record BlockItemProperty(string Block) : IItemProperty
{
    public string Name => PropertyComponentIds.Block;
    public object BuildValue() => Block;
}

public sealed record EnchantableSlotItemProperty(string Slot) : IItemProperty
{
    public string Name => PropertyComponentIds.EnchantableSlot;
    public object BuildValue() => Slot;
}

public sealed record EnchantableValueItemProperty(int Value) : IItemProperty
{
    public string Name => PropertyComponentIds.EnchantableValue;
    public object BuildValue() => Value;
}
