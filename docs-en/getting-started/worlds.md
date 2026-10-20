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
    Fortresses and bastions are not there yet.

### Mobs

- `mobs=off` in `server.properties` removes every mob: natural spawning, spawn eggs and `/summon`. Animals stay
  saved and come back with `mobs=on`.
- `spawn-animals` and `spawn-monsters` turn each family on or off.
- **Animals** spawn on grass under the open sky. The species depends on the biome: cows, pigs, sheep and chickens
  everywhere; horses in plains, wolves and foxes in taiga; rabbits in deserts and snow; mooshrooms on mushroom
  islands, and so on.
- Animals are **saved with the world** (`entities.json`), even when their chunks are no longer loaded.
- **Monsters** spawn in the dark, outside at night or at any time in caves, but never near a torch or another light
  source. They are husks in deserts and strays in snow. The Nether has zombified piglins, piglins, hoglins, magma
  cubes and endermen, depending on the biome.
- **Water mobs**: cod, salmon, tropical fish, pufferfish, squids and dolphins in oceans and rivers, by water
  temperature; glow squids in cave water; drowned in dark water. Fish swim without leaving the water and suffocate on
  land.
- **Flying mobs**: bats in caves; phantoms at night above players under the open sky, from the third day of the
  world (they circle, then swoop down to bite); ghasts in the open spaces of the Nether.
- **Ranged attacks**: skeletons, strays and pillagers shoot real arrows, which you see flying and which can miss.
  Ghasts throw exploding fireballs, blazes bursts of small fireballs.
- As in vanilla, monsters despawn far from players, and so do fish and bats.
- **Behaviour**:
    - monsters find their way around walls (pathfinding) and do not see through blocks;
    - neutral mobs only attack the player who hits them: zombified piglins (the whole group), endermen, wolves,
      bees, iron golems, polar bears, llamas…
    - animals follow a player holding their food.
- **Breeding**: feeding two animals (wheat, carrots, seeds…) makes them breed. The baby grows up in 20 minutes,
  faster when fed.
- Shears shear sheep (the wool grows back when they eat grass), a bucket milks cows, and chickens lay eggs.
- Mobs take fall, fire and lava damage. Those that die burning drop cooked meat.

!!! note
    There is no taming, villager trading or raids yet.

### Items

Every Minecraft item exists: they are in the creative inventory, can be given with `/give` and crafted. Besides
food, armour, buckets, flint and steel and bone meal, these have their vanilla use:

- **Wear**: tools, weapons and armour wear out (durability bar) and break. Unbreaking spares them.
- **Bow**: draw it (full power in 1 s); it uses an arrow (not with Infinity or in creative). Power, Punch and Flame
  work. Stuck arrows can be picked up.
- **Crossbow**: charged in 1.25 s (less with Quick Charge), it shoots hard; Multishot fires three arrows.
- **Trident**: thrown (8 damage), it stays where it lands or comes back with Loyalty. In melee: 9 damage.
- **Fishing rod**: cast it into water, wait for the bobber to dip, and reel in right then to catch a fish, junk or a
  treasure (Lure and Luck of the Sea work).
- **Shield**: while sneaking it blocks attacks and explosions from the front.
- **Totem of undying**: in a hand, it saves from death once (Regeneration, Absorption, Fire Resistance).
- **Mace**: hitting while falling adds damage from the fall height and cancels the fall damage.
- **Axe**: strips logs, stems and wood.
- **Shears**: shear sheep, carve pumpkins, and collect leaves, grass and vines.
- **Name tag** (renamed on an anvil): names a mob, which then never despawns.
- **Water bucket on a fish**: scoops it up; the fish bucket lets it out again.
- **Glass bottle**: fills with water. **Fire charge**: like flint and steel.
- **Bottle o' enchanting**, **wind charge** (pushes everything around away) and **lingering potion** can be thrown.
- **Egg**: a chick hatches one time in eight.
- **Firework rocket**: launched from a block, or boosts a player flying with an elytra.
- **Goat horn**: sounds, with a 7-second cooldown.

!!! note
    Not yet: boats, minecarts, saddles, leads, maps, bundles and the spyglass (the crossbow fires as soon as it is
    charged instead of staying loaded).

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
