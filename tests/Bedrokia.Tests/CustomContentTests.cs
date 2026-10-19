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
using Bedrokia.Server.Game;
using Bedrokia.Server.Game.Custom;
using Bedrokia.Server.Game.Custom.Blocks;
using Bedrokia.Server.Game.Custom.Items;

namespace Bedrokia.Tests;

public class CustomContentTests
{
    [Fact]
    public void Custom_item_builder_creates_weapon_with_correct_components()
    {
        var sword = CustomItemBuilder.CreateWeapon("custom:ruby_blade", "Ruby Blade", "ruby_blade", 9, 1200)
            .SetFoil(true)
            .Build();

        Assert.Equal("custom:ruby_blade", sword.Identifier);
        Assert.Equal("Ruby Blade", sword.DisplayName);
        Assert.True(sword.HasProperty(PropertyComponentIds.Damage));
        Assert.True(sword.HasProperty(PropertyComponentIds.HandEquipped));
        Assert.True(sword.HasProperty(PropertyComponentIds.Foil));
        Assert.True(sword.HasComponent(ItemComponentIds.Durability));

        var damageProp = sword.GetProperty<DamageItemProperty>();
        Assert.NotNull(damageProp);
        Assert.Equal(9, damageProp.Damage);

        var duraComp = sword.GetComponent<DurabilityItemComponent>();
        Assert.NotNull(duraComp);
        Assert.Equal(1200, duraComp.MaxDurability);
    }

    [Fact]
    public void Custom_item_builder_creates_armor_with_correct_components()
    {
        var chestplate = CustomItemBuilder.CreateArmor("custom:ruby_chestplate", "Ruby Chestplate", "ruby_chestplate", "slot.armor.chest", 7, 750)
            .Build();

        Assert.Equal("custom:ruby_chestplate", chestplate.Identifier);
        Assert.True(chestplate.HasComponent(ItemComponentIds.Armor));
        Assert.True(chestplate.HasComponent(ItemComponentIds.Wearable));
        Assert.True(chestplate.HasComponent(ItemComponentIds.Durability));

        var armorComp = chestplate.GetComponent<ArmorItemComponent>();
        Assert.NotNull(armorComp);
        Assert.Equal(7, armorComp.Protection);

        var wearableComp = chestplate.GetComponent<WearableItemComponent>();
        Assert.NotNull(wearableComp);
        Assert.Equal("slot.armor.chest", wearableComp.Slot);
    }

    [Fact]
    public void Custom_item_builder_creates_food_with_correct_components()
    {
        var food = CustomItemBuilder.CreateFood("custom:ruby_apple", "Ruby Apple", "ruby_apple", 8, 14.4f, canAlwaysEat: true)
            .SetUseAnimation(UseAnimation.Eat)
            .Build();

        Assert.Equal("custom:ruby_apple", food.Identifier);
        Assert.True(food.HasComponent(ItemComponentIds.Food));
        Assert.True(food.HasProperty(PropertyComponentIds.UseAnimation));

        var foodComp = food.GetComponent<FoodItemComponent>();
        Assert.NotNull(foodComp);
        Assert.Equal(8, foodComp.Nutrition);
        Assert.Equal(14.4f, foodComp.SaturationModifier);
        Assert.True(foodComp.CanAlwaysEat);
    }

    [Fact]
    public void Custom_item_nbt_generation_matches_bedrock_format()
    {
        var item = CustomItemBuilder.CreateTool("custom:ruby_pickaxe", "Ruby Pickaxe", "ruby_pickaxe", 4, 1500)
            .SetCreativeInfo(CreativeCategory.Items, "itemGroup.name.pickaxe")
            .Build();

        var nbt = item.ToNbt();
        Assert.True(nbt.ContainsKey("components"));

        var components = (OrderedDictionary<string, object>)nbt["components"];
        Assert.True(components.ContainsKey("item_properties"));

        var itemProps = (OrderedDictionary<string, object>)components["item_properties"];
        Assert.True(itemProps.ContainsKey("minecraft:icon"));
        Assert.True(itemProps.ContainsKey("max_stack_size"));
        Assert.True(itemProps.ContainsKey("hand_equipped"));
        Assert.True(components.ContainsKey("minecraft:display_name"));
        Assert.True(components.ContainsKey("minecraft:durability"));
        Assert.True(components.ContainsKey("minecraft:digger"));
    }

    [Fact]
    public void Custom_item_registration_updates_runtime_id_and_packets()
    {
        var initialItemCount = Items.Count;
        var initialCreativeCount = Items.CreativeItemCount;

        var customItem = CustomItemBuilder.Create("custom:ruby_gem")
            .SetDisplayName("Ruby Gem")
            .SetIcon("ruby_gem")
            .SetMaxStackSize(64)
            .SetCreativeInfo(CreativeCategory.Items)
            .Build();

        var registered = CustomItemRegistry.Register(customItem);

        Assert.Same(customItem, registered);
        Assert.True(customItem.RuntimeId >= 10000);
        Assert.Same(customItem, CustomItemRegistry.Get("custom:ruby_gem"));
        Assert.Same(customItem, CustomItemRegistry.GetByRuntimeId(customItem.RuntimeId));

        Assert.Equal(initialItemCount + 1, Items.Count);
        Assert.Equal(initialCreativeCount + 1, Items.CreativeItemCount);

        var itemType = Items.ByName("custom:ruby_gem");
        Assert.NotNull(itemType);
        Assert.Equal(customItem.RuntimeId, itemType.RuntimeId);
        Assert.Equal("custom:ruby_gem", itemType.Name);

        var stack = ItemStack.Of("custom:ruby_gem", 5);
        Assert.NotNull(stack);
        Assert.Equal(5, stack.Count);
        Assert.Equal(customItem.RuntimeId, stack.Type.RuntimeId);
    }

    [Fact]
    public void Custom_block_builder_creates_block_and_permutations()
    {
        var block = CustomBlockBuilder.Create("custom:ruby_lamp")
            .SetDisplayName("Ruby Lamp")
            .SetLightEmission(15)
            .SetDestructibleByMining(1.5f)
            .AddProperty(new BlockProperty("custom:lit", [false, true]))
            .AddPermutation(new BlockPermutation("q.block_property('custom:lit') == true",
                [new LightEmissionBlockComponent(15)]))
            .AddPermutation(new BlockPermutation("q.block_property('custom:lit') == false",
                [new LightEmissionBlockComponent(0)]))
            .Build();

        Assert.Equal("custom:ruby_lamp", block.Identifier);
        Assert.Equal("Ruby Lamp", block.DisplayName);
        Assert.Single(block.Properties);
        Assert.Equal(2, block.Permutations.Count);

        var states = block.GetStateCombinations();
        Assert.Equal(2, states.Count);
        Assert.Contains(states, s => (bool)s["custom:lit"] == false);
        Assert.Contains(states, s => (bool)s["custom:lit"] == true);
    }

    [Fact]
    public void Custom_block_nbt_generation_contains_correct_structure()
    {
        var block = CustomBlockBuilder.Create("custom:ruby_ore")
            .SetDisplayName("Ruby Ore")
            .SetDestructibleByMining(3.0f)
            .SetGeometry("geometry.cube")
            .SetLightEmission(3)
            .Build();

        var nbt = block.ToNbt(1001);

        // Dragonfly's layout: the name is sent next to the NBT, no empty lists, no unknown keys (they crashed the client).
        Assert.Equal(["components", "molangVersion", "menu_category", "vanilla_block_data"], nbt.Keys);
        Assert.Equal(1001, ((OrderedDictionary<string, object>)nbt["vanilla_block_data"])["block_id"]);

        var comps = (OrderedDictionary<string, object>)nbt["components"];
        Assert.True(comps.ContainsKey("minecraft:destructible_by_mining"));
        Assert.True(comps.ContainsKey("minecraft:geometry"));
        Assert.Equal(3, ((OrderedDictionary<string, object>)comps["minecraft:light_emission"])["emission"]);
        Assert.False(comps.ContainsKey("minecraft:breathability"));
        Assert.True(((OrderedDictionary<string, object>)comps["minecraft:material_instances"]).ContainsKey("mappings"));
    }

    [Fact]
    public void Custom_items_are_data_driven_and_block_items_are_plain()
    {
        var gem = CustomItemBuilder.Create("custom:entry_gem").WithIcon("entry_gem").Register().ToItemType();
        Assert.True(gem.ComponentBased);
        Assert.Equal(1, gem.Version);
        var properties = (OrderedDictionary<string, object>)((OrderedDictionary<string, object>)gem.Data["components"])["item_properties"];
        Assert.Equal("entry_gem", ((OrderedDictionary<string, object>)((OrderedDictionary<string, object>)properties["minecraft:icon"])["textures"])["default"]);

        CustomBlockBuilder.Create("custom:entry_block").Register();
        var blockItem = CustomItemRegistry.Get("custom:entry_block")!.ToItemType();
        Assert.False(blockItem.ComponentBased);
        Assert.Equal(2, blockItem.Version);
    }

    [Fact]
    public void Custom_block_registration_registers_in_blocks_and_item_registry()
    {
        var block = CustomBlockBuilder.Create("custom:ruby_block")
            .SetDisplayName("Block of Ruby")
            .SetDestructibleByMining(5.0f)
            .SetGeometry("geometry.cube")
            .Build();

        CustomBlockRegistry.Register(block);

        Assert.NotNull(CustomBlockRegistry.Get("custom:ruby_block"));
        var definitions = CustomBlockRegistry.GetDefinitions();
        Assert.Contains(definitions, d => d.Name == "custom:ruby_block");

        var blockType = Blocks.ByName("custom:ruby_block");
        Assert.NotNull(blockType);
        Assert.Equal("custom:ruby_block", blockType.Name);

        var byNetworkId = Blocks.ByNetworkId(blockType.NetworkId);
        Assert.NotNull(byNetworkId);
        Assert.Equal("custom:ruby_block", byNetworkId.Name);

        var blockItem = CustomItemRegistry.Get("custom:ruby_block");
        Assert.NotNull(blockItem);
        Assert.True(blockItem.HasComponent(ItemComponentIds.BlockPlacer));

        var placer = blockItem.GetComponent<BlockPlacerItemComponent>();
        Assert.NotNull(placer);
        Assert.Equal("custom:ruby_block", placer.Block);
    }

    [Fact]
    public void Start_game_packet_serializes_custom_blocks_correctly()
    {
        var data = new Packets.StartGameData(
            EntityId: 1,
            GameMode: 0,
            X: 0,
            Y: 64,
            Z: 0,
            SpawnX: 0,
            SpawnY: 64,
            SpawnZ: 0,
            WorldName: "Test",
            Time: 1000,
            CustomBlocks: CustomBlockRegistry.GetDefinitions()
        );

        var packet = Packets.StartGame(data);
        Assert.NotNull(packet);
        Assert.True(packet.Length > 100);

        var reader = new BedrockReader(packet);
        var packetId = reader.VarUInt32();
        Assert.Equal((uint)PacketId.StartGame, packetId);
    }
}
