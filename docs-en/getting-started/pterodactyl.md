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

Bedrokia uses **NetherNet**: every player goes through **the server's main allocation**, in TCP and UDP.
No extra allocation is needed with `WEBRTC_PORTS` on `auto`.

(With a `first-last` range in `WEBRTC_PORTS`, each player gets a port of their own: then the whole range must be allocated.)

In Docker the server only knows its internal address (172.x), so the egg writes the allocation's IP into `public-ip`.
If the allocation is on `0.0.0.0`, set the public IP in `public-ip` yourself, otherwise the console shows *WebRTC connection failed*.

If players stay stuck on "Connecting to server…", check `public-ip` and that the port is open in UDP.

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
3. wait for the upload to reach 100 %, then restart.
3. restart.

No reinstall needed: the egg runs a copy (`.bedrokia-run`), so the file can be replaced even while the server runs, and worlds, players, plugins and settings are
kept. (*Settings → Reinstall Server* works too: it downloads the version in `VERSION`.)
