---
title: Worlds and packs
---

# Worlds and resource packs

## Worlds

The loaded world is `worlds/<level-name>/`. Each world has its `world.properties` (see
[Configuration](configuration.md)): vanilla world (`normal`), flat (`flat`) or empty (`void`).

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
