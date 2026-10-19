---
title: Worlds and packs
---

# Worlds and resource packs

## Worlds

The loaded world is `worlds/<level-name>/`. Each world has its `world.properties` (see
[Configuration](configuration.md)): vanilla world (`normal`), flat (`flat`), empty (`void`) or Nether (`nether`).

### The Nether

The Nether is the world `worlds/<level-name>/nether/`, created on first start with `generator=nether`.

- An obsidian frame (inside 2×3 to 21×21) lit with flint and steel becomes a portal.
- Standing in the portal for 4 seconds (at once in creative) changes dimension.
- Distances are divided by 8 in the Nether. An existing portal within 16 blocks is reused, otherwise one is built
  on arrival.
- As in vanilla:
    - beds explode;
    - water evaporates;
    - lava flows faster and further;
    - dying in the Nether brings you back to the overworld.
- Players reconnect in the dimension they left from.

!!! note
    Nether mobs, fortresses and bastions are not there yet.

### Import a Bedrock world

Drop into `worlds/`:

- a **`.mcworld`** file (exported from Minecraft), or
- a Bedrock world folder (with `db/` and `level.dat`), for example from a Realm or BDS.

At startup, Bedrokia converts it to its own format (blocks and biomes), keeps the spawn point and writes a
`world.properties` in `void` mode so nothing is generated around it. Then set `level-name` to this world.

!!! note
    Entities, chests and other block data of the imported world are not carried over yet.

## Resource packs

Drop `.mcpack` files, `.zip` files or pack folders (with `manifest.json`) into `resource_packs/`. They are sent to
players when they join. `force-resources=on` in `server.properties` makes accepting them mandatory.
