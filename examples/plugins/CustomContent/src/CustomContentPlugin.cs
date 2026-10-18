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

using Bedrokia.Server.Game.Custom.Blocks;
using Bedrokia.Server.Game.Custom.Items;
using Bedrokia.Server.Plugins;

namespace CustomContent;

public sealed class CustomContentPlugin : Plugin
{
    public override void OnEnable()
    {
        RegisterCustomItems();
        RegisterCustomBlocks();
        Log("Plugin CustomContent active.");
    }

    private static void RegisterCustomItems()
    {
        CustomItemBuilder.Create("bedrokia:ruby_sword")
            .WithDisplayName("Ruby Sword")
            .WithIcon("ruby_sword")
            .WithDamage(9)
            .WithMaxStackSize(1)
            .WithHandEquipped(true)
            .Register();

        CustomItemBuilder.Create("bedrokia:ruby_pickaxe")
            .WithDisplayName("Ruby Pickaxe")
            .WithIcon("ruby_pickaxe")
            .WithDamage(5)
            .WithDigger([new DestroySpeed("minecraft:is_pickaxe_item_destructible", 8)])
            .WithMaxStackSize(1)
            .WithHandEquipped(true)
            .Register();

        CustomItemBuilder.Create("bedrokia:ruby_helmet")
            .WithDisplayName("Ruby Helmet")
            .WithIcon("ruby_helmet")
            .WithArmor(4)
            .WithWearable("slot.armor.head", 4)
            .WithMaxStackSize(1)
            .Register();

        CustomItemBuilder.Create("bedrokia:ruby_chestplate")
            .WithDisplayName("Ruby Chestplate")
            .WithIcon("ruby_chestplate")
            .WithArmor(9)
            .WithWearable("slot.armor.chest", 9)
            .WithMaxStackSize(1)
            .Register();

        CustomItemBuilder.Create("bedrokia:ruby_apple")
            .WithDisplayName("Ruby Apple")
            .WithIcon("ruby_apple")
            .WithFood(8, 1.2f, canAlwaysEat: true)
            .WithMaxStackSize(64)
            .Register();
    }

    private static void RegisterCustomBlocks()
    {
        CustomBlockBuilder.Create("bedrokia:ruby_block")
            .WithDisplayName("Ruby Block")
            .WithTexture("ruby_block")
            .WithHardness(1.5f)
            .WithFriction(0.6f)
            .WithLightEmission(5)
            .Register();

        CustomBlockBuilder.Create("bedrokia:ruby_ore")
            .WithDisplayName("Ruby Ore")
            .WithTexture("ruby_ore")
            .WithHardness(3.0f)
            .WithLightEmission(2)
            .Register();

        CustomBlockBuilder.Create("bedrokia:ruby_lamp")
            .WithDisplayName("Ruby Lamp")
            .WithTexture("ruby_lamp")
            .WithLightEmission(15)
            .WithHardness(0.5f)
            .Register();
    }
}
