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
using Bedrokia.Server.Game.Custom.Entities;
using Bedrokia.Server.Game.Custom.Items;
using Bedrokia.Server.Plugins;

namespace CustomContent;

/// <summary>
/// Example plugin demonstrating custom items, custom blocks, and custom entities.
/// Source: Bedrokia custom content system inspired by Dragonfly (MIT).
/// </summary>
public sealed class CustomContentPlugin : Plugin
{
    public override void OnEnable()
    {
        RegisterCustomItems();
        RegisterCustomBlocks();
        RegisterCustomEntities();
        Log("Plugin CustomContent active.");
    }

    /// <summary>
    /// Registers custom entities (passive mob, hostile mob, NPC, boss) for the Bedrokia server.
    /// </summary>
    private static void RegisterCustomEntities()
    {
        // 1. Passive Mob: Crystal Fox
        CustomEntityBuilder.CreatePassiveMob("bedrokia:crystal_fox", "Crystal Fox", health: 15, speed: 0.28f)
            .WithSpawnEgg(summonable: true, hasSpawnEgg: true)
            .Register();

        // 2. Hostile Mob: Ruby Golem with Enraged Component Group
        CustomEntityBuilder.CreateHostileMob("bedrokia:ruby_golem", "Ruby Golem", health: 60, damage: 8f, speed: 0.22f)
            .WithSpawnEgg(summonable: true, hasSpawnEgg: true)
            .WithComponentGroup("enraged",
                new MovementEntityComponent(0.38f),
                new AttackEntityComponent(14f),
                new ScaleEntityComponent(1.4f))
            .OnHurt((entity, source) =>
            {
                if (entity.Health <= 30 && !entity.ActiveComponentGroups.Contains("enraged"))
                {
                    entity.AddComponentGroup("enraged");
                }
            })
            .Register();

        // 3. NPC: Ruby Merchant
        CustomEntityBuilder.CreateNpc("bedrokia:ruby_merchant", "Ruby Merchant")
            .WithSpawnEgg(summonable: true, hasSpawnEgg: true)
            .WithInteract("Trade", "minecraft:emerald", (player, entity) =>
            {
                player.SendMessage("§a[Ruby Merchant] Welcome! I offer the finest rubies in the realm.");
            })
            .Register();

        // 4. Boss: Infernal Behemoth with Boss Bar and Properties
        CustomEntityBuilder.CreateBoss("bedrokia:infernal_behemoth", "Infernal Behemoth", health: 250, damage: 10f, scale: 2.0f)
            .WithSpawnEgg(summonable: true, hasSpawnEgg: true)
            .WithIntProperty("custom:phase", min: 1, max: 3, defaultValue: 1, clientSync: true)
            .WithFloatProperty("custom:rage", min: 0f, max: 100f, defaultValue: 0f, clientSync: true)
            .WithRideable(seatCount: 1, inputGroundControlled: false)
            .OnHurt((entity, source) =>
            {
                var rage = entity.GetProperty<float>("custom:rage") + 10f;
                entity.SetProperty("custom:rage", Math.Min(100f, rage));

                if (entity.Health <= 100 && entity.GetProperty<int>("custom:phase") == 1)
                {
                    entity.SetProperty("custom:phase", 2);
                }
            })
            .Register();
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
