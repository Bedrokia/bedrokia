---
title: Pterodactyl
---

# Pterodactyl

The [`egg-bedrokia.json`](https://github.com/Nexiumpvp2/bedrokia/blob/main/deploy/pterodactyl/egg-bedrokia.json) egg
installs the latest Bedrokia release and sets `server.properties` from the panel.

## Install the egg

1. Panel → **Admin → Nests** → create a "Minecraft Bedrock" nest (or use an existing one).
2. **Import Egg** → pick `egg-bedrokia.json`.
3. Create a server with the *Bedrokia* egg. Docker image: `ghcr.io/parkervcp/yolks:debian`.

## Ports (important)

Bedrokia uses **NetherNet**: one TCP port to connect, then one UDP port per player.

- The server's main allocation gives the **TCP port** (and the first UDP port).
- It **also** needs UDP ports for the players. Two ways:
  1. **Consecutive allocations**: give the server the ports *main* to *main + players + 4*
     (e.g. 25600 to 25624 for 20 players) and leave `WEBRTC_PORTS` on `auto`.
  2. **Chosen range**: reserve a range of allocations and set `WEBRTC_PORTS` to `first-last`
     (e.g. `25700-25730`).

In Docker the server only knows its internal address (172.x), so the egg writes the allocation's IP into `public-ip`.
If the allocation is on `0.0.0.0`, set the public IP in `public-ip` yourself, otherwise the console shows *WebRTC connection failed*.

If players stay stuck on "Connecting to server…", the UDP range is almost always what is not open.

## Variables

| Variable | Default | Role |
|---|---|---|
| `VERSION` | `latest` | installed release (`latest` or a tag, e.g. `v0.3.0`) |
| `MOTD` | `Bedrokia Server` | name in the server list |
| `MAX_PLAYERS` | `20` | maximum players |
| `GAMEMODE` | `survival` | game mode of new players |
| `DIFFICULTY` | `normal` | `peaceful`, `easy`, `normal`, `hard` |
| `LEVEL_NAME` | `world` | world folder in `worlds/` |
| `XBOX_AUTH` | `on` | Xbox Live verification |
| `WEBRTC_PORTS` | `auto` | players' UDP ports |

Other settings are edited in `server.properties` with the panel's file manager.

## Plugins

Upload the plugin folder (`plugin.yml`, `src/`, `resources/`) into `plugins/` with the file manager, then restart.
The console shows the loading summary.

## Update / change version

Just **replace one file**:

1. download `bedrokia` from the [release you want](https://github.com/Nexiumpvp2/bedrokia/releases);
2. in the panel: *Files* → delete the old `bedrokia` → *Upload* the new one;
3. restart.

No reinstall needed: the egg restores the executable bit at startup, and worlds, players, plugins and settings are
kept. (*Settings → Reinstall Server* works too: it downloads the version in `VERSION`.)
