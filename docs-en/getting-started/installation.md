---
title: Installation
---

# Installation

Bedrokia ships as a **single executable**: nothing to install (no .NET, no Java).

## Download

From the [releases](https://github.com/Nexiumpvp2/bedrokia/releases) page:

- `bedrokia` for Linux (64-bit);
- `bedrokia.exe` for Windows;
- `egg-bedrokia.json` for Pterodactyl;
- `example-plugins.zip`, the example plugins.

## Linux

```bash
mkdir bedrokia && cd bedrokia
curl -L -o bedrokia https://github.com/Nexiumpvp2/bedrokia/releases/latest/download/bedrokia
chmod +x bedrokia
./bedrokia
```

Type `stop` in the console to shut down cleanly (Ctrl+C works too).

### Ports to open

Players connect through **NetherNet** (WebRTC), like recent Bedrock servers:

| Port | Protocol | Role |
|---|---|---|
| `server-port` (19132) | TCP | connection (signalling) |
| `server-port` to `server-port + max-players + 4` | UDP | player traffic |

With 20 players: TCP 19132 and UDP 19132-19156. The range can be changed with `webrtc-ports` in
[server.properties](configuration.md).

```bash
sudo ufw allow 19132/tcp && sudo ufw allow 19132:19156/udp
```

### systemd service

[`deploy/bedrokia.service`](https://github.com/Nexiumpvp2/bedrokia/blob/main/deploy/bedrokia.service) starts the
server when the machine boots, from `/opt/bedrokia`:

```bash
sudo useradd --system --home-dir /opt/bedrokia --shell /usr/sbin/nologin bedrokia
sudo install -d -o bedrokia -g bedrokia /opt/bedrokia
sudo install -o bedrokia -g bedrokia -m 755 bedrokia /opt/bedrokia/bedrokia
sudo install -m 644 bedrokia.service /etc/systemd/system/
sudo systemctl daemon-reload && sudo systemctl enable --now bedrokia
journalctl -u bedrokia -f
```

## Windows

Put `bedrokia.exe` in an empty folder and run it. Allow it through the firewall (TCP and UDP).

## Docker

```bash
docker build -t bedrokia .
docker run -it -p 19132:19132/tcp -p 19132-19156:19132-19156/udp -v bedrokia-data:/data bedrokia
```

## Build it yourself

Requirement: the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
git clone https://github.com/Nexiumpvp2/bedrokia.git && cd bedrokia
dotnet test Bedrokia.slnx
dotnet publish src/Bedrokia.Cli -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o out/linux-x64
```

## Update

The server is **a single file**:

1. download `bedrokia` (or `bedrokia.exe`) from the [release you want](https://github.com/Nexiumpvp2/bedrokia/releases);
2. stop the server;
3. replace the old file with the new one;
4. start it again.

Nothing else to reinstall: worlds, players, plugins and settings are kept. To go back, put the old file back.
The console tells you when a new version is out (see `check-updates` in [configuration](configuration.md)).
