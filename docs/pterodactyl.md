---
title: Pterodactyl
---

# Pterodactyl

L'egg [`egg-bedrokia.json`](https://github.com/Nexiumpvp2/bedrokia/blob/main/deploy/pterodactyl/egg-bedrokia.json)
installe la dernière release de Bedrokia et règle `server.properties` depuis le panel.

## Installer l'egg

1. Panel → **Admin → Nests** → créer un nest « Minecraft Bedrock » (ou utiliser un existant).
2. **Import Egg** → choisir `egg-bedrokia.json`.
3. Créer un serveur avec l'egg *Bedrokia*. Image Docker : `ghcr.io/parkervcp/yolks:debian`.

## Ports (important)

Bedrokia utilise **NetherNet** : un port TCP pour se connecter, puis un port UDP par joueur.

- L'allocation principale du serveur donne le **port TCP** (et le premier port UDP).
- Il faut **aussi** des ports UDP pour les joueurs. Deux possibilités :
  1. **Allocations à la suite** : donner au serveur les ports *principal* à *principal + joueurs + 4*
     (par ex. 25600 à 25624 pour 20 joueurs) et laisser `WEBRTC_PORTS` sur `auto`.
  2. **Plage choisie** : réserver une plage d'allocations et mettre `WEBRTC_PORTS` à `premier-dernier`
     (par ex. `25700-25730`).

Si les joueurs restent bloqués sur « Connexion au serveur… », c'est presque toujours la plage UDP qui n'est pas ouverte.

## Variables

| Variable | Défaut | Rôle |
|---|---|---|
| `VERSION` | `latest` | release installée (`latest` ou un tag, ex. `v0.3.0`) |
| `MOTD` | `Bedrokia Server` | nom dans la liste des serveurs |
| `MAX_PLAYERS` | `20` | joueurs maximum |
| `GAMEMODE` | `survival` | mode des nouveaux joueurs |
| `DIFFICULTY` | `normal` | `peaceful`, `easy`, `normal`, `hard` |
| `LEVEL_NAME` | `world` | dossier du monde dans `worlds/` |
| `XBOX_AUTH` | `on` | vérification Xbox Live |
| `WEBRTC_PORTS` | `auto` | ports UDP des joueurs |

Les autres réglages se modifient dans `server.properties` depuis le gestionnaire de fichiers du panel.

## Plugins

Envoyer le dossier du plugin (`plugin.yml`, `src/`, `resources/`) dans `plugins/` avec le gestionnaire de fichiers,
puis redémarrer. Le panel affiche le bilan de chargement dans la console.

## Mettre à jour

*Settings → Reinstall Server* télécharge la release choisie dans `VERSION`. Les mondes et plugins sont gardés.
