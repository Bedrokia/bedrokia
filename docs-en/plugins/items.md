---
title: Custom items
---

# Custom items

Bedrokia has a native custom item engine, based on the component (NBT) model of the official Minecraft Bedrock
protocol.

It registers unique items (weapons, tools, armour, food, items with effects) straight from a C# plugin, with no
complex setup. The server builds the required NBT and sends the item catalogue to Bedrock clients when they join
(`ItemRegistryPacket`).

---

## 1. Identifier and namespace

Each item has an identifier of the form `namespace:item_name`.

The namespace is up to you:

- `my_server:legendary_sword`
- `pvp:war_hammer`
- `kitmap:knockback_stick`
- `bedrokia:ruby_sword`

Notes:

- Without a namespace (for example `CustomItemBuilder.Create("ruby_sword")`), Bedrokia uses the default `custom:`
  namespace (`custom:ruby_sword`).
- The identifier must be unique on the server.

---

## 2. Quick example in a plugin

Items are registered in your plugin's `OnEnable()` with the fluent `CustomItemBuilder` class.

```csharp
using Bedrokia.Server.Game.Custom.Items;
using Bedrokia.Server.Plugins;

namespace MyPlugin;

public sealed class MyPlugin : Plugin
{
    public override void OnEnable()
    {
        // A custom sword
        CustomItemBuilder.Create("my_server:ruby_sword")
            .WithDisplayName("§cRuby Sword")
            .WithIcon("ruby_sword")
            .WithDamage(9)
            .WithDurability(1561)
            .WithMaxStackSize(1)
            .WithHandEquipped(true)
            .Register();

        // A magic apple
        CustomItemBuilder.Create("my_server:magic_apple")
            .WithDisplayName("§6Magic Apple")
            .WithIcon("golden_apple")
            .WithFood(nutrition: 8, saturationModifier: 1.2f, canAlwaysEat: true)
            .WithMaxStackSize(64)
            .WithFoil(true)
            .Register();
    }
}
```

---

## 3. CustomItemBuilder reference

### General

- `Create(string identifier)`: starts a builder for this identifier.
- `WithDisplayName(string displayName)`: name shown to players. Supports Minecraft colour codes (`§a`, `§c`, `§l`...).
- `WithIcon(string textureName)`: short name of the texture in the resource pack's `item_texture.json`.
- `WithMaxStackSize(int maxStackSize)`: items per inventory slot (usually 1 or 64).
- `WithHandEquipped(bool handEquipped = true)`: held like a tool or weapon (upright) instead of flat.
- `WithFoil(bool foil = true)`: permanent enchantment glint.
- `WithFireResistant(bool fireResistant = true)`: the dropped item survives lava and fire (like netherite).
- `WithAllowOffHand(bool allow = true)`: can be held in the off hand.
- `WithCanDestroyInCreative(bool canDestroy = true)`: can break blocks in Creative mode.

### Combat and durability

- `WithDamage(int damage)`: damage per hit when used as a weapon.
- `WithDurability(int maxDurability, int minDamageChance = 1, int maxDamageChance = 1)`: durability bar and the
  chance of losing durability per use.

### Tools and mining

- `WithDigger(IReadOnlyList<DestroySpeed> speeds, bool useEfficiency = true)`: mines some blocks or block categories
  faster. A name starting with `query.` is a tag query, e.g. `"query.any_tag('stone', 'metal')"`.
- `WithTool(string kind, int level, double speed)`: the item mines like a vanilla tool on the server: `kind` =
  `pickaxe`, `axe`, `shovel`, `hoe` or `sword`; `level` = 0 wood/gold, 1 stone, 2 iron, 3 diamond, 4 netherite (which
  ores drop); `speed` (diamond 8).

```csharp
CustomItemBuilder.Create("my_server:super_pickaxe")
    .WithDisplayName("Super Pickaxe")
    .WithIcon("super_pickaxe")
    .WithDurability(2000)
    .WithDigger([
        new DestroySpeed("minecraft:stone", speed: 12),
        new DestroySpeed("minecraft:is_pickaxe_item_destructible", speed: 10)
    ])
    .Register();
```

### Armour

- `WithArmor(int protection, string textureType = "iron")`: defence points.
- `WithWearable(string slot, int protection = 0)`: armour slot.
    - `"slot.armor.head"`: helmet
    - `"slot.armor.chest"`: chestplate
    - `"slot.armor.legs"`: leggings
    - `"slot.armor.feet"`: boots

### Food

- `WithFood(int nutrition, float saturationModifier, bool canAlwaysEat = false, string usingConvertsTo = "minecraft:air")`:
  makes the item edible, with eating time and animation.
    - `nutrition`: hunger points restored (half drumsticks).
    - `saturationModifier`: saturation multiplier.
    - `canAlwaysEat`: `true` lets the player eat with a full hunger bar (like golden apples).
    - `usingConvertsTo`: item left after eating (a bowl, a bottle...).

### Cooldowns and use

- `WithCooldown(string category, float durationSeconds)`: cooldown on every item of this category after use.
- `WithFuel(float durationSeconds)`: burns in a furnace for this many seconds.
- `OnUse(Action<ItemUseEvent> handler)`: runs your C# code when the player right clicks with the item.

### Build and register

- `Build()`: builds the `CustomItem` without registering it.
- `Register()`: builds it and registers it in `CustomItemRegistry`.

---

## 4. Shortcuts

Static helpers build common items without setting every component:

```csharp
// Weapon
CustomItemBuilder.CreateWeapon(
    identifier: "my_server:dagger",
    displayName: "Dark Dagger",
    texture: "dark_dagger",
    damage: 7,
    durability: 850
).Register();

// Tool
CustomItemBuilder.CreateTool(
    identifier: "my_server:ruby_pickaxe",
    displayName: "Ruby Pickaxe",
    texture: "ruby_pickaxe",
    speedMultiplier: 8,
    durability: 1561
).Register();

// Armour piece
CustomItemBuilder.CreateArmor(
    identifier: "my_server:ruby_chestplate",
    displayName: "Ruby Chestplate",
    texture: "ruby_chestplate",
    slot: "slot.armor.chest",
    protection: 8,
    durability: 528
).Register();

// Food
CustomItemBuilder.CreateFood(
    identifier: "my_server:gingerbread",
    displayName: "Gingerbread",
    texture: "gingerbread",
    nutrition: 5,
    saturation: 0.8f,
    canAlwaysEat: false
).Register();
```

---

## 5. Resource pack side

For the Bedrock client to show your item's texture, map it in a Bedrock resource pack.

### Pack layout

```text
my_pack/
├── manifest.json
├── textures/
│   ├── items/
│   │   ├── ruby_sword.png
│   │   └── ruby_apple.png
│   └── item_texture.json
```

### textures/item_texture.json

```json
{
  "resource_pack_name": "my_server_pack",
  "texture_name": "atlas.items",
  "texture_data": {
    "ruby_sword": {
      "textures": "textures/items/ruby_sword"
    },
    "ruby_apple": {
      "textures": "textures/items/ruby_apple"
    }
  }
}
```

The value given to `.WithIcon("ruby_sword")` is the key declared in `texture_data`. Without the pack, the item shows
the missing-texture pattern.

---

## 6. Full example plugin

A complete working example is in the Bedrokia sources:

- Folder: `examples/plugins/CustomContent/`
- Source: `examples/plugins/CustomContent/src/CustomContentPlugin.cs`
- Description: `examples/plugins/CustomContent/plugin.yml`
