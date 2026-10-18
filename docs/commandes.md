---
title: Commandes
---

# Commandes

Dans le jeu, avec `/`. Dans la console, sans `/` (la console est toujours opérateur). Les commandes des plugins
s'ajoutent à cette liste et apparaissent dans l'autocomplétion du jeu.

## Tout le monde

| Commande | Rôle |
|---|---|
| `help` | liste des commandes disponibles |
| `version` | version du serveur et de Bedrock |
| `plugin [nom]` (`plugins`) | liste des plugins, ou détails d'un plugin (auteur, commandes, dépendances, dossier) |
| `list` | joueurs en ligne |
| `seed` | graine du monde |
| `me <action...>` | action dans le chat |
| `tell <joueur> <message...>` (`msg`, `w`) | message privé |
| `spawn` | retour au spawn du monde |

## Opérateurs

| Commande | Rôle |
|---|---|
| `stop` | arrête proprement le serveur |
| `save-all` | sauvegarde tout maintenant |
| `say <message...>` | message à tous |
| `gamemode <mode> [joueur]` (`gm`) | change le mode de jeu |
| `give <joueur> <objet> [nombre]` | donne des objets |
| `clear [joueur]` | vide l'inventaire |
| `tp <cible> [destination] [x] [y] [z]` (`teleport`) | téléporte |
| `time <set\|add> <valeur>` | change l'heure |
| `setworldspawn` | met le spawn du monde à ta position |
| `op <joueur>` / `deop <joueur>` | donne ou retire le statut d'opérateur |
| `whitelist <on\|off\|add\|remove\|list> [joueur]` | liste blanche |
| `kick <joueur> [raison...]` | expulse |
| `ban <joueur> [raison...]` / `pardon <joueur>` (`unban`) / `banlist` | bannissements |
| `summon <mob>` | fait apparaître un mob |
| `enchant <enchantement> [niveau]` | enchante l'objet en main |
| `xp <nombre>[L] [joueur]` | donne de l'expérience (`30L` = 30 niveaux) |
