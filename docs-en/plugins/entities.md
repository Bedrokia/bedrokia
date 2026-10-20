---
title: Custom Entities
---

# Custom Entities

Bedrokia features a native Custom Entities engine based on the official Minecraft: Bedrock Edition component model and network protocol (v1.26.x+).

This system allows you to declare unique creatures, monsters, bosses, mounts, and NPCs directly in C# within your plugins. All entity definitions are automatically registered and sent across the network protocol during player login (`AvailableActorIdentifiersPacket`, `SyncEntityPropertyPacket`, etc.).

---

## 1. Identifiers and Namespaces

Like items and blocks, every custom entity has a unique identifier formatted as `namespace:entity_name`.

Namespaces can be freely defined:
- `my_server:crystal_golem`
- `rpg:infernal_dragon`
- `bedrokia:crystal_fox`

> **Note:** If no namespace is specified (e.g., `CustomEntityBuilder.Create("goblin")`), Bedrokia automatically prefixes `custom:` (`custom:goblin`).

---

## 2. Quick Example in a Plugin

Entities are registered in your plugin's `OnEnable()` method using the fluent `CustomEntityBuilder`:

```csharp
using Bedrokia.Server.Game.Custom.Entities;
using Bedrokia.Server.Plugins;

namespace MyPlugin;

public sealed class MyPlugin : Plugin
{
    public override void OnEnable()
    {
        // 1. Hostile Mob with loot and attack
        CustomEntityBuilder.CreateHostileMob("my_server:shadow_golem", "Shadow Golem", health: 80, damage: 10f, speed: 0.22f)
            .WithScale(1.5f)
            .WithSpawnEgg(summonable: true, hasSpawnEgg: true)
            .Register();

        // 2. Boss with Boss Bar and Phase Component Group
        CustomEntityBuilder.CreateBoss("my_server:fire_lord", "Fire Lord", health: 300, damage: 15f, scale: 2.0f)
            .WithSpawnEgg(summonable: true, hasSpawnEgg: true)
            .WithIntProperty("custom:phase", min: 1, max: 2, defaultValue: 1, clientSync: true)
            .WithComponentGroup("phase2",
                new MovementEntityComponent(0.40f),
                new AttackEntityComponent(22f),
                new ScaleEntityComponent(2.5f))
            .OnHurt((entity, source) =>
            {
                if (entity.Health <= 150 && entity.GetProperty<int>("custom:phase") == 1)
                {
                    entity.SetProperty("custom:phase", 2);
                    entity.AddComponentGroup("phase2");
                }
            })
            .Register();

        // 3. Interactive NPC
        CustomEntityBuilder.CreateNpc("my_server:merchant", "Wandering Merchant")
            .WithInteract("Talk", "minecraft:emerald", (player, entity) =>
            {
                player.SendMessage("§a[Merchant] Greetings traveller!");
            })
            .Register();
    }
}
```

---

## 3. Factory Presets

For ease of use, `CustomEntityBuilder` provides preset factory methods:

- `CustomEntityBuilder.CreatePassiveMob(identifier, displayName, health = 10, speed = 0.25f)` (alias `CreateSimpleMob`): Sets up water floating, panic upon receiving damage, and random strolling.
- `CustomEntityBuilder.CreateHostileMob(identifier, displayName, health = 20, damage = 3f, speed = 0.28f)`: Sets up target acquisition (`NearestAttackableTarget`) and melee attacks (`MeleeAttack`).
- `CustomEntityBuilder.CreateNpc(identifier, displayName)`: Sets up a persistent, gravity-free NPC looking at nearby players.
- `CustomEntityBuilder.CreateBoss(identifier, bossBarTitle, health = 200, damage = 8f, scale = 1.5f)`: Sets up a synchronized HUD boss bar, high health, and scaled appearance.

---

## 4. Spawning in Game

Once registered, custom entities can be spawned through several methods:

### Via Chat or Server Command
```
/summon custom:shadow_golem
```

### Via Spawn Egg
When configured with `.WithSpawnEgg(hasSpawnEgg: true)`, a spawn egg item `custom:shadow_golem_spawn_egg` is automatically registered and ready for right-clicking on blocks.

### Directly via C# API
```csharp
// Spawning in world
var instance = world.SpawnCustomEntity("my_server:shadow_golem", x, y, z);

// Dynamic property updates
instance.SetProperty("custom:phase", 2);

// Dynamic component groups
instance.AddComponentGroup("phase2");
instance.RemoveComponentGroup("phase2");

// Combat & damage
instance.Damage(20, (x, z));
```
