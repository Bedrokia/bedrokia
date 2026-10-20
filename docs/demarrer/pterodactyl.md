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

Bedrokia utilise **NetherNet** : tous les joueurs passent par **l'allocation principale du serveur**, en TCP et en UDP.
Aucune allocation en plus n'est nécessaire avec `WEBRTC_PORTS` sur `auto`.

(Avec une plage `premier-dernier` dans `WEBRTC_PORTS`, chaque joueur a son propre port : il faut alors allouer toute la plage.)

Dans Docker, le serveur ne connaît que son adresse interne (172.x) : l'egg écrit donc l'IP de l'allocation dans `public-ip`.
Si l'allocation est sur `0.0.0.0`, mettre soi-même l'IP publique dans `public-ip`, sinon la console affiche *WebRTC connection failed*.

Si les joueurs restent bloqués sur « Connexion au serveur… », vérifier `public-ip` et que le port est ouvert en UDP.

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

## Mettre à jour / changer de version

Il suffit de **remplacer un fichier** :

1. télécharger `bedrokia` depuis la [release voulue](https://github.com/Nexiumpvp2/bedrokia/releases) ;
2. dans le panel : *Files* → supprimer l'ancien `bedrokia` → *Upload* le nouveau ;
3. attendre la fin de l'upload (100 %), puis redémarrer.

Pas besoin de réinstaller : l'egg lance une copie (`.bedrokia-run`), donc le fichier peut être remplacé même serveur allumé, et les mondes, joueurs, plugins et
réglages sont gardés. (*Settings → Reinstall Server* marche aussi : il télécharge la version de `VERSION`.)
