---
title: Commands
---

# Commands

In game, with `/`. In the console, without `/` (the console is always an operator). Plugin commands are added to
this list and show up in the game's auto-completion.

## Everyone

| Command | Role |
|---|---|
| `help` | available commands |
| `version` | server and Bedrock version |
| `plugin [name]` (`plugins`) | plugin list, or one plugin's details (author, commands, dependencies, folder) |
| `list` | players online |
| `seed` | world seed |
| `me <action...>` | action in the chat |
| `tell <player> <message...>` (`msg`, `w`) | private message |
| `spawn` | back to the world spawn |

## Operators

| Command | Role |
|---|---|
| `stop` | stops the server cleanly |
| `save-all` | saves everything now |
| `say <message...>` | message to everyone |
| `gamemode <mode> [player]` (`gm`) | changes the game mode: `survival`, `creative`, `adventure`, `spectator` (or 0 to 3) |
| `give <player> <item> [amount]` | gives items |
| `clear [player]` | empties the inventory |
| `tp <target> [destination] [x] [y] [z]` (`teleport`) | teleports |
| `time <set\|add> <value>` | changes the time of day |
| `setworldspawn` | sets the world spawn to your position |
| `op <player>` / `deop <player>` | gives or removes operator status |
| `whitelist <on\|off\|add\|remove\|list> [player]` | whitelist |
| `kick <player> [reason...]` | kicks |
| `ban <player> [reason...]` / `pardon <player>` (`unban`) / `banlist` | bans |
| `summon <mob>` | spawns a mob |
| `enchant <enchantment> [level]` | enchants the held item |
| `xp <amount>[L] [player]` | gives experience (`30L` = 30 levels) |
| `perm user <player> <info\|setgroup\|add\|remove> [value]` (`permission`) | a player's permissions and group |
| `perm group <group> <list\|add\|remove\|setprefix\|setparent> [value]` | a group's permissions, parent and chat prefix |
| `tps` | shows server performance (TPS, tick duration MSPT, memory) |
| `timings [on\|off\|reset\|report]` | detailed subsystem performance profiler (entities, blocks, world, tasks) |

## Permissions

Bedrokia has permission nodes with groups (ranks), inheritance and chat prefixes:

- **Wildcards and negations**:
    - `*`: every command.
    - `bedrokia.command.*`: every Bedrokia command.
    - `-bedrokia.command.stop`: explicitly denies `/stop` (even with `*`).
- **Operators** have `*` by default, unless explicitly denied.
- **Live auto-completion**: when a permission or a group changes with `/perm`, the player's command list is
  refreshed right away.
- **Storage**: groups and permissions are saved in `permissions.json`, each player under their exact name
  (case-insensitive). A broken file is copied to `permissions.json.broken-<date>` and the error shows in the console.
