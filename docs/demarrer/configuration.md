---
title: Configuration
---

# Configuration

Tout est dans le dossier du serveur. Les fichiers sont créés au premier lancement, avec des
commentaires.

```
server.properties     réglages du serveur
ops.txt               opérateurs (un pseudo par ligne)
white-list.txt        joueurs autorisés quand white-list=on
banned-players.txt    joueurs bannis
worlds/<nom>/         un monde, avec world.properties
players/              sauvegarde de chaque joueur (position, inventaire, XP...)
plugins/              plugins
plugin_data/<nom>/    config et données de chaque plugin
resource_packs/       packs de ressources
data/                 clé d'identité du serveur (ne pas partager)
server.log            journal
```

## server.properties

| Clé | Défaut | Rôle |
|---|---|---|
| `motd` | `Bedrokia Server` | nom dans la liste des serveurs |
| `server-port` | `19132` | port de connexion (TCP) et des joueurs (UDP, même numéro) |
| `max-players` | `20` | joueurs maximum |
| `gamemode` | `survival` | `survival`, `creative`, `adventure` (nouveaux joueurs) |
| `force-gamemode` | `off` | `on` : tout le monde rejoint dans ce mode |
| `white-list` | `off` | `on` : seuls les joueurs de `white-list.txt` (et les ops) entrent |
| `xbox-auth` | `on` | vérifie les comptes Xbox Live. **Laisser `on` en production** : à `off`, n'importe qui prend n'importe quel pseudo |
| `view-distance` | `8` | rayon de chunks envoyé (2 à 32) |
| `level-name` | `world` | monde chargé dans `worlds/` |
| `level-seed` | *(vide)* | graine d'un **nouveau** monde |
| `difficulty` | `normal` | `peaceful`, `easy`, `normal`, `hard` |
| `pvp` | `on` | les joueurs peuvent se blesser |
| `pvp-reach` | `4` | portée maximale d'un coup entre joueurs, des yeux à la hitbox, compensée selon le ping (dernière seconde de positions) ; `0` = pas de vérification |
| `obsidian-durability` | `0` | explosions de TNT nécessaires pour casser l'obsidienne à moins de 3 blocs (raids factions) ; `0` = jamais, comme vanilla |
| `mobs` | `on` | mobs dans le monde (`off` : aucun mob, ni naturel, ni œuf, ni `/summon` ; les animaux sauvegardés reviennent avec `on`) |
| `spawn-animals` | `on` | apparition des animaux |
| `spawn-monsters` | `on` | apparition des monstres dans le noir : la nuit, dans les grottes, dans le Nether (jamais en peaceful) |
| `spawn-protection` | `16` | rayon autour du spawn où seuls les ops construisent (`0` = désactivé) |
| `keep-inventory` | `off` | garder ses objets à la mort |
| `force-resources` | `off` | obliger les joueurs à accepter les packs de ressources |
| `autosave-interval` | `60` | secondes entre deux sauvegardes automatiques |
| `webrtc-ports` | `auto` | ports UDP des joueurs : `auto` (tous sur le port UDP de `server-port`) ou `premier-dernier` (un port par joueur) |
| `public-ip` | *(vide)* | IPv4 utilisée par les joueurs, à remplir si le serveur tourne dans Docker ou derrière un NAT (l'egg Pterodactyl la remplit) |
| `check-updates` | `on` | vérifie toutes les 6 h s'il existe une nouvelle version, et l'annonce dans la console avec son lien |
| `log-level` | `info` | `debug`, `info`, `warn`, `error` |

Redémarrer le serveur après une modification. Une valeur invalide est signalée au démarrage.

## world.properties

Chaque monde a son fichier `worlds/<nom>/world.properties` :

| Clé | Rôle |
|---|---|
| `generator` | `normal` (relief et biomes vanilla), `flat`, `void` ou `nether` |
| `flat-layers` | couches d'un monde plat, de bas en haut : `bedrock,3*stone,2*dirt,grass_block` |
| `flat-biome` | biome des mondes plats ou vides : `plains`, `desert`, `ice_plains`, `mushroom_island`... |
| `daylight-cycle` | `on`/`off` : le temps passe |

Changer le générateur ne touche que les chunks jamais générés.
