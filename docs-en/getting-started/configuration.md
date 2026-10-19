---
title: Configuration
---

# Configuration

Everything lives in the server folder. The files are created on first start, with comments.

```
server.properties     server settings
ops.txt               operators (one name per line)
white-list.txt        players allowed when white-list=on
banned-players.txt    banned players
worlds/<name>/        a world, with its world.properties
players/              each player's save (position, inventory, XP...)
plugins/              plugins
plugin_data/<name>/   each plugin's config and data
resource_packs/       resource packs
permissions.json      permission groups and per-player permissions (/perm)
data/                 the server's identity key (do not share)
server.log            log
```

## server.properties

| Key | Default | Role |
|---|---|---|
| `motd` | `Bedrokia Server` | name in the server list |
| `server-port` | `19132` | connection port (TCP), and first UDP port |
| `max-players` | `20` | maximum players |
| `gamemode` | `survival` | `survival`, `creative`, `adventure` (new players) |
| `force-gamemode` | `off` | `on`: everyone joins in this mode |
| `white-list` | `off` | `on`: only players in `white-list.txt` (and ops) can join |
| `xbox-auth` | `on` | verifies Xbox Live accounts. **Keep `on` in production**: with `off`, anyone can use any name |
| `view-distance` | `8` | chunk radius sent to players (2 to 32) |
| `level-name` | `world` | world loaded from `worlds/` |
| `level-seed` | *(empty)* | seed of a **new** world |
| `difficulty` | `normal` | `peaceful`, `easy`, `normal`, `hard` |
| `pvp` | `on` | players can hurt each other |
| `spawn-animals` | `on` | animals spawn |
| `spawn-monsters` | `on` | monsters spawn at night (never in peaceful) |
| `spawn-protection` | `16` | radius around spawn where only ops can build (`0` = off) |
| `keep-inventory` | `off` | keep items on death |
| `force-resources` | `off` | players must accept the resource packs |
| `autosave-interval` | `60` | seconds between automatic saves |
| `webrtc-ports` | `auto` | players' UDP ports: `auto` (server port + `max-players` + 5) or `first-last` |
| `check-updates` | `on` | checks every 6 h for a new version and announces it in the console with its link |
| `log-level` | `info` | `debug`, `info`, `warn`, `error` |

Restart the server after a change. An invalid value is reported at startup.

## world.properties

Each world has its own `worlds/<name>/world.properties`:

| Key | Role |
|---|---|
| `generator` | `normal` (vanilla terrain and biomes), `flat` or `void` |
| `flat-layers` | layers of a flat world, bottom to top: `bedrock,3*stone,2*dirt,grass_block` |
| `flat-biome` | biome of flat or void worlds: `plains`, `desert`, `ice_plains`, `mushroom_island`... |
| `daylight-cycle` | `on`/`off`: time passes |

Changing the generator only affects chunks that were never generated.
