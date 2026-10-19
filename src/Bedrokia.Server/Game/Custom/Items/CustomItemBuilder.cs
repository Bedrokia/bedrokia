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

using Bedrokia.Server.Plugins;

namespace Bedrokia.Server.Game.Custom.Items;

public sealed class CustomItemBuilder
{
    private readonly string _identifier;
    private short _numericId;
    private int _version = 2;
    private CreativeInfo _creative = new(CreativeCategory.Items);
    private readonly Dictionary<string, IItemProperty> _properties = [];
    private readonly Dictionary<string, IItemComponent> _components = [];
    private Action<ItemUseEvent>? _onUse;

    public CustomItemBuilder(string identifier)
    {
        _identifier = identifier.Contains(':') ? identifier : "custom:" + identifier;
        var icon = _identifier.Contains(':') ? _identifier[(_identifier.IndexOf(':') + 1)..] : _identifier;
        AddProperty(new IconItemProperty(icon));
        AddProperty(new MaxStackSizeItemProperty(64));
        AddProperty(new HandEquippedItemProperty(false));
        AddProperty(new CanDestroyInCreativeItemProperty(true));
        AddComponent(new DisplayNameItemComponent(_identifier));
    }

    public static CustomItemBuilder Create(string identifier) => new(identifier);

    public CustomItemBuilder WithNumericId(short id)
    {
        _numericId = id;
        return this;
    }

    public CustomItemBuilder WithVersion(int version)
    {
        _version = version;
        return this;
    }

    public CustomItemBuilder WithDisplayName(string displayName)
    {
        AddComponent(new DisplayNameItemComponent(displayName));
        return this;
    }

    public CustomItemBuilder WithIcon(string texture)
    {
        AddProperty(new IconItemProperty(texture));
        return this;
    }

    public CustomItemBuilder WithDamage(int damage)
    {
        AddProperty(new DamageItemProperty(damage));
        return this;
    }

    public CustomItemBuilder WithMaxStackSize(int maxStackSize)
    {
        AddProperty(new MaxStackSizeItemProperty(maxStackSize));
        return this;
    }

    public CustomItemBuilder WithHandEquipped(bool handEquipped = true)
    {
        AddProperty(new HandEquippedItemProperty(handEquipped));
        return this;
    }

    public CustomItemBuilder WithFoil(bool foil = true)
    {
        AddProperty(new FoilItemProperty(foil));
        return this;
    }

    public CustomItemBuilder WithCanDestroyInCreative(bool canDestroy = true)
    {
        AddProperty(new CanDestroyInCreativeItemProperty(canDestroy));
        return this;
    }

    public CustomItemBuilder WithAllowOffHand(bool allow = true)
    {
        AddProperty(new AllowOffHandItemProperty(allow));
        return this;
    }

    public CustomItemBuilder WithDurability(int maxDurability, int minDamageChance = 1, int maxDamageChance = 1)
    {
        AddComponent(new DurabilityItemComponent(maxDurability, minDamageChance, maxDamageChance));
        return this;
    }

    public CustomItemBuilder WithFood(int nutrition, float saturationModifier, bool canAlwaysEat = false, string usingConvertsTo = "minecraft:air")
    {
        AddComponent(new FoodItemComponent(nutrition, saturationModifier, canAlwaysEat, usingConvertsTo));
        AddProperty(new UseDurationItemProperty(32));
        AddProperty(new UseAnimationItemProperty(1));
        return this;
    }

    public CustomItemBuilder WithArmor(int protection, string textureType = "iron")
    {
        AddComponent(new ArmorItemComponent(protection, textureType));
        return this;
    }

    public CustomItemBuilder WithWearable(string slot, int protection = 0)
    {
        AddComponent(new WearableItemComponent(slot, protection));
        return this;
    }

    public CustomItemBuilder WithDigger(IReadOnlyList<DestroySpeed> speeds, bool useEfficiency = true)
    {
        AddComponent(new DiggerItemComponent(speeds, useEfficiency));
        WithHandEquipped(true);
        return this;
    }

    public CustomItemBuilder WithEnchantable(string slot, int value)
    {
        AddComponent(new EnchantableItemComponent(slot, value));
        AddProperty(new EnchantableSlotItemProperty(slot));
        AddProperty(new EnchantableValueItemProperty(value));
        return this;
    }

    public CustomItemBuilder WithCooldown(string category, float durationSeconds)
    {
        AddComponent(new CooldownItemComponent(category, durationSeconds));
        return this;
    }

    public CustomItemBuilder WithFuel(float durationSeconds)
    {
        AddComponent(new FuelItemComponent(durationSeconds));
        return this;
    }

    public CustomItemBuilder WithFireResistant(bool fireResistant = true)
    {
        AddComponent(new FireResistantItemComponent(fireResistant));
        return this;
    }

    public CustomItemBuilder WithTags(params string[] tags)
    {
        AddComponent(new TagsItemComponent(tags));
        return this;
    }

    public CustomItemBuilder WithBlockPlacer(string block, bool canUseBlockAsIcon = false, bool replaceBlockItem = false, bool alignedPlacement = false)
    {
        AddComponent(new BlockPlacerItemComponent(block, canUseBlockAsIcon, replaceBlockItem, alignedPlacement));
        AddProperty(new BlockItemProperty(block));
        return this;
    }

    public CustomItemBuilder WithThrowable(bool swing = true, float power = 1.0f, float maxDraw = 0.0f)
    {
        AddComponent(new ThrowableItemComponent(swing, power, maxDraw));
        return this;
    }

    public CustomItemBuilder WithProjectile(string projectileEntity, float minCriticalDamage = 0.0f)
    {
        AddComponent(new ProjectileItemComponent(projectileEntity, minCriticalDamage));
        return this;
    }

    public CustomItemBuilder WithUseModifiers(float duration, float movement = 1.0f)
    {
        AddComponent(new UseModifiersItemComponent(duration, movement));
        return this;
    }

    public CustomItemBuilder WithCreative(CreativeCategory category, string group = "", bool isHidden = false)
    {
        _creative = new CreativeInfo(category, group, isHidden);
        return this;
    }

    public CustomItemBuilder OnUse(Action<ItemUseEvent> handler)
    {
        _onUse = handler;
        return this;
    }

    public CustomItemBuilder SetDisplayName(string displayName) => WithDisplayName(displayName);
    public CustomItemBuilder SetIcon(string texture) => WithIcon(texture);
    public CustomItemBuilder SetDamage(int damage) => WithDamage(damage);
    public CustomItemBuilder SetMaxStackSize(int maxStackSize) => WithMaxStackSize(maxStackSize);
    public CustomItemBuilder SetHandEquipped(bool handEquipped = true) => WithHandEquipped(handEquipped);
    public CustomItemBuilder SetFoil(bool foil = true) => WithFoil(foil);
    public CustomItemBuilder SetCanDestroyInCreative(bool canDestroy = true) => WithCanDestroyInCreative(canDestroy);
    public CustomItemBuilder SetAllowOffHand(bool allow = true) => WithAllowOffHand(allow);
    public CustomItemBuilder SetDurability(int maxDurability, int minDamageChance = 1, int maxDamageChance = 1) => WithDurability(maxDurability, minDamageChance, maxDamageChance);
    public CustomItemBuilder SetUseAnimation(UseAnimation animation)
    {
        AddProperty(new UseAnimationItemProperty((int)animation));
        return this;
    }
    public CustomItemBuilder SetCreativeInfo(CreativeCategory category, string group = "", bool isHidden = false) => WithCreative(category, group, isHidden);

    public CustomItemBuilder AddProperty(IItemProperty property)
    {
        _properties[property.Name] = property;
        return this;
    }

    public CustomItemBuilder AddComponent(IItemComponent component)
    {
        _components[component.Name] = component;
        return this;
    }

    public static CustomItemBuilder CreateWeapon(string identifier, string displayName, string texture, int damage, int durability)
    {
        return Create(identifier)
            .WithDisplayName(displayName)
            .WithIcon(texture)
            .WithDamage(damage)
            .WithDurability(durability)
            .WithMaxStackSize(1)
            .WithHandEquipped(true)
            .WithCanDestroyInCreative(false)
            .WithCreative(CreativeCategory.Equipment, "itemGroup.name.sword");
    }

    public static CustomItemBuilder CreateTool(string identifier, string displayName, string texture, int speedMultiplier, int durability)
    {
        return Create(identifier)
            .WithDisplayName(displayName)
            .WithIcon(texture)
            .WithDurability(durability)
            .WithMaxStackSize(1)
            .WithHandEquipped(true)
            .WithDigger([new DestroySpeed("minecraft:stone", speedMultiplier)])
            .WithCreative(CreativeCategory.Equipment, "itemGroup.name.pickaxe");
    }

    public static CustomItemBuilder CreateTool(string identifier, string displayName, string texture, int durability, params DestroySpeed[] speeds)
    {
        return Create(identifier)
            .WithDisplayName(displayName)
            .WithIcon(texture)
            .WithDurability(durability)
            .WithMaxStackSize(1)
            .WithHandEquipped(true)
            .WithDigger(speeds)
            .WithCreative(CreativeCategory.Equipment, "itemGroup.name.pickaxe");
    }

    public static CustomItemBuilder CreateArmor(string identifier, string displayName, string texture, string slot, int protection, int durability)
    {
        return Create(identifier)
            .WithDisplayName(displayName)
            .WithIcon(texture)
            .WithDurability(durability)
            .WithMaxStackSize(1)
            .WithArmor(protection)
            .WithWearable(slot, protection)
            .WithCreative(CreativeCategory.Equipment, "itemGroup.name.helmet");
    }

    public static CustomItemBuilder CreateFood(string identifier, string displayName, string texture, int nutrition, float saturation, bool canAlwaysEat = false)
    {
        return Create(identifier)
            .WithDisplayName(displayName)
            .WithIcon(texture)
            .WithMaxStackSize(64)
            .WithFood(nutrition, saturation, canAlwaysEat)
            .WithCreative(CreativeCategory.Items, "itemGroup.name.cookedFood");
    }

    public CustomItem Build()
    {
        if (_numericId == 0)
            _numericId = CustomItemRegistry.AllocateNextRuntimeId();

        return new CustomItem(
            _identifier,
            _numericId,
            _version,
            _creative,
            _properties,
            _components,
            _onUse);
    }

    public CustomItem Register()
    {
        var item = Build();
        CustomItemRegistry.Register(item);
        return item;
    }
}
