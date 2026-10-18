---
title: Installation
---

# Installation

Bedrokia est livré en **exécutable unique** : rien à installer (pas de .NET, pas de Java).

## Télécharger

Sur la page des [releases](https://github.com/Nexiumpvp2/bedrokia/releases) :

- `bedrokia-linux-x64` pour Linux (64 bits) ;
- `bedrokia-win-x64.exe` pour Windows ;
- `egg-bedrokia.json` pour Pterodactyl ;
- `example-plugins.zip`, les plugins d'exemple.

## Linux

```bash
mkdir bedrokia && cd bedrokia
curl -L -o bedrokia https://github.com/Nexiumpvp2/bedrokia/releases/latest/download/bedrokia-linux-x64
chmod +x bedrokia
./bedrokia
```

Taper `stop` dans la console pour arrêter proprement (Ctrl+C marche aussi).

### Ports à ouvrir

Les joueurs passent par **NetherNet** (WebRTC), comme les serveurs Bedrock récents :

| Port | Protocole | Rôle |
|---|---|---|
| `server-port` (19132) | TCP | connexion (signalisation) |
| de `server-port` à `server-port + max-players + 4` | UDP | trafic des joueurs |

Avec 20 joueurs : TCP 19132 et UDP 19132-19156. La plage peut être changée avec `webrtc-ports` dans
[server.properties](configuration).

```bash
sudo ufw allow 19132/tcp && sudo ufw allow 19132:19156/udp
```

### Service systemd

Le fichier [`deploy/bedrokia.service`](https://github.com/Nexiumpvp2/bedrokia/blob/main/deploy/bedrokia.service)
lance le serveur au démarrage de la machine, depuis `/opt/bedrokia` :

```bash
sudo useradd --system --home-dir /opt/bedrokia --shell /usr/sbin/nologin bedrokia
sudo install -d -o bedrokia -g bedrokia /opt/bedrokia
sudo install -o bedrokia -g bedrokia -m 755 bedrokia /opt/bedrokia/bedrokia
sudo install -m 644 bedrokia.service /etc/systemd/system/
sudo systemctl daemon-reload && sudo systemctl enable --now bedrokia
journalctl -u bedrokia -f
```

## Windows

Mettre `bedrokia-win-x64.exe` dans un dossier vide et le lancer. Autoriser le pare-feu (TCP et UDP).

## Docker

```bash
docker build -t bedrokia .
docker run -it -p 19132:19132/tcp -p 19132-19156:19132-19156/udp -v bedrokia-data:/data bedrokia
```

## Compiler soi-même

Prérequis : [SDK .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
git clone https://github.com/Nexiumpvp2/bedrokia.git && cd bedrokia
dotnet test Bedrokia.slnx
dotnet publish src/Bedrokia.Cli -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o out/linux-x64
```

## Mettre à jour

Arrêter le serveur, remplacer l'exécutable, relancer. Les mondes, joueurs et plugins sont gardés.
