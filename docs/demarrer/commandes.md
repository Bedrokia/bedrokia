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
| `perm user <joueur> <info\|setgroup\|add\|remove> [valeur]` (`permission`) | gère les permissions et le groupe d'un joueur |
| `perm group <groupe> <list\|add\|remove\|setprefix\|setparent> [valeur]` | gère les permissions, l'héritage et le préfixe d'un groupe |

## Système de Permissions

Bedrokia intègre un système granulaire de permissions avec groupes (grades), héritage et préfixes dans le chat :
- **Jokers et Négations** :
  - `*` : donne accès à toutes les commandes.
  - `bedrokia.command.*` : donne accès à toutes les commandes de Bedrokia.
  - `-bedrokia.command.stop` : interdit explicitement la commande `/stop` (même si `*` est accordé).
- **Opérateurs** : Les opérateurs possèdent automatiquement `*` par défaut, sauf négation explicite.
- **Autocomplétion dynamique** : Dès qu'une permission ou un groupe est modifié via `/perm`, la liste des commandes autorisées est immédiatement rafraîchie sur le client Bedrock du joueur.
- **Stockage sécurisé** : Les permissions et groupes sont enregistrés de façon atomique dans `permissions.json` (avec assainissement strict des pseudos pour prévenir toute faille d'injection ou de traversée de chemin).
