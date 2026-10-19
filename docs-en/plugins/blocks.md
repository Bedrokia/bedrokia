---
title: Custom blocks
---

# Custom blocks

Bedrokia has a native custom block engine based on the official Minecraft Bedrock component (NBT) system.

It declares unique blocks with custom textures, physical properties, light emission, collision boxes and state
permutations, in C#, from your plugins. The definitions are sent to the player while the game loads
(`StartGamePacket.CustomBlocks`).

---

## 1. Identifier and namespace

Like items, each block has a unique identifier of the form `namespace:block_name`.

The namespace is up to you:

- `my_server:ruby_ore`
- `pvp:reinforced_obsidian`
- `kitmap:custom_spawner`
- `bedrokia:ruby_block`

Notes:

- Without a namespace (for example `CustomBlockBuilder.Create("ruby_block")`), Bedrokia uses `custom:`
  (`custom:ruby_block`).
- The identifier must be unique on the server.

---

## 2. Quick example in a plugin

Blocks are registered in your plugin's `OnEnable()` with `CustomBlockBuilder`.

```csharp
using Bedrokia.Server.Game.Custom.Blocks;
using Bedrokia.Server.Plugins;

namespace MyPlugin;

public sealed class MyPlugin : Plugin
{
    public override void OnEnable()
    {
        // A simple solid block with friction and hardness
        CustomBlockBuilder.Create("my_server:ruby_block")
            .WithDisplayName("§cRuby Block")
            .WithTexture("ruby_block")
            .WithHardness(2.0f)
            .WithBlastResistance(9.0f)
            .WithFriction(0.6f)
            .Register();

        // A lamp
        CustomBlockBuilder.Create("my_server:ruby_lamp")
            .WithDisplayName("§6Ruby Lamp")
            .WithTexture("ruby_lamp")
            .WithLightEmission(15)
            .WithHardness(0.5f)
            .Register();
    }
}
```

---

## 3. CustomBlockBuilder reference

### Look and textures

- `Create(string identifier)`: starts a builder for this identifier.
- `WithDisplayName(string name)`: name shown to players (supports `§a`, `§c`... codes).
- `WithTexture(string texture, string renderMethod = "opaque")`: one texture for every face.
    - `renderMethod`: `"opaque"` (normal full block), `"alpha_test"` (cut-out transparency like leaves or glass),
      `"blend"` (smooth transparency with alpha).
- `WithMaterials(params BlockMaterial[] materials)`: different textures per face (`"up"`, `"down"`, `"north"`,
  `"south"`, `"east"`, `"west"`, or `"*"` for all).
- `WithGeometry(string geometryIdentifier)`: 3D model (default `"minecraft:geometry.full_block"`).
- `WithTransformation(rx, ry, rz, sx, sy, sz)`: rotates or scales the block's model.

### Physics and resistance

- `WithHardness(float secondsToDestroy)`: base time to break the block (seconds).
- `WithBlastResistance(float resistance)`: resistance to explosions (TNT, creepers).
- `WithFriction(float friction)`: grip for entities walking on it (`0.6f` by default, `0.98f` for an icy block).
- `WithBreathability(bool solid)`: kept for compatibility but **ignored**: this component no longer exists in Bedrock
  and crashed the client.
- `WithReplaceable()`: can be replaced by another block without breaking it first (like tall grass).
- `WithFlammable(int catchOdds = 5, int burnOdds = 20)`: makes the block flammable and sets how fast it burns.

### Light

- `WithLightEmission(byte lightLevel)`: light level emitted (0 to 15).
- `WithLightDampening(byte lightFilter)`: light absorbed (0 lets all light through like glass, 15 blocks it all).

### Collision and selection boxes

- `WithCollision(bool enabled = true, float[]? origin = null, float[]? size = null)`: the physical box that stops
  entities.
    - `origin`: minimum corner `[x, y, z]` in pixels, x and z from the block centre (e.g. `[-8.0f, 0.0f, -8.0f]`).
    - `size`: `[width, height, depth]` in pixels (e.g. `[16.0f, 16.0f, 16.0f]` for a full block).
- `WithSelectionBox(bool enabled = true, float[]? origin = null, float[]? size = null)`: the outline the player
  targets with the cursor.

### Redstone and crafting

- `WithCraftingTable(params string[] craftingTags)`: turns the block into a crafting table with these crafting tags
  (`"crafting_table"`).
- `WithRedstoneConductivity(bool redstoneConductor = true)`: conducts redstone like an opaque full block.
- `WithRedstoneProducer(int power = 15)`: a redstone power source of this strength (1 to 15).

### States, properties and permutations

One block can have several visual or physical variants depending on its properties:

- `WithProperty(string name, params object[] values)`: declares a state property and its allowed values (booleans,
  integers or strings).
- `WithPermutation(string condition, params IBlockComponent[] components)`: changes components when the MoLang
  condition is true.

```csharp
CustomBlockBuilder.Create("my_server:magic_switch")
    .WithDisplayName("Magic Switch")
    .WithProperty("my_server:on", false, true)
    .WithPermutation("q.block_property('my_server:on') == true",
        new LightEmissionBlockComponent(15),
        new MaterialInstancesBlockComponent([new BlockMaterial("switch_on")])
    )
    .WithPermutation("q.block_property('my_server:on') == false",
        new LightEmissionBlockComponent(0),
        new MaterialInstancesBlockComponent([new BlockMaterial("switch_off")])
    )
    .Register();
```

### Creative inventory and tags

- `WithTags(params string[] tags)`: gameplay tags (for example `"minecraft:is_pickaxe_item_destructible"`).
- `WithCreative(CreativeCategory category, string group = "", bool isHidden = false)`: where the block appears in the
  Creative inventory.

### Build and register

- `Build()`: builds the configured `CustomBlock`.
- `Register()`: registers the block in the server's `CustomBlockRegistry`.

---

## 4. Shortcuts

Templates for the most common blocks:

```csharp
// Standard solid block
CustomBlockBuilder.CreateSimpleBlock(
    identifier: "my_server:amethyst_brick",
    displayName: "Amethyst Brick",
    texture: "amethyst_brick",
    hardness: 1.5f,
    resistance: 6.0f
).Register();

// Light block (like glowstone)
CustomBlockBuilder.CreateLightBlock(
    identifier: "my_server:infernal_lantern",
    displayName: "Infernal Lantern",
    texture: "infernal_lamp",
    lightLevel: 15
).Register();

// Transparent block (like glass)
CustomBlockBuilder.CreateTransparentBlock(
    identifier: "my_server:reinforced_glass",
    displayName: "Reinforced Glass",
    texture: "reinforced_glass",
    hardness: 3.0f
).Register();
```

---

## 5. Resource pack side

To show the custom textures on the Bedrock client:

### Pack layout

```text
my_pack/
├── manifest.json
├── blocks.json
├── textures/
│   ├── blocks/
│   │   ├── ruby_block.png
│   │   └── ruby_lamp.png
│   └── terrain_texture.json
```

### textures/terrain_texture.json

```json
{
  "resource_pack_name": "my_server_pack",
  "texture_name": "atlas.terrain",
  "texture_data": {
    "ruby_block": {
      "textures": "textures/blocks/ruby_block"
    },
    "ruby_lamp": {
      "textures": "textures/blocks/ruby_lamp"
    }
  }
}
```

### blocks.json

```json
{
  "format_version": "1.21.0",
  "my_server:ruby_block": {
    "textures": "ruby_block",
    "sound": "stone"
  },
  "my_server:ruby_lamp": {
    "textures": "ruby_lamp",
    "sound": "glass"
  }
}
```

---

## 6. Full example in the sources

A complete example registering several blocks (decorative block, ore and lamp) is in the repository:

- Folder: `examples/plugins/CustomContent/`
- Source: `examples/plugins/CustomContent/src/CustomContentPlugin.cs`
- Description: `examples/plugins/CustomContent/plugin.yml`
