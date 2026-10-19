---
title: Mondes et packs
---

# Mondes et packs de ressources

## Mondes

Le monde chargé est `worlds/<level-name>/`. Chaque monde a son `world.properties` (voir [Configuration](configuration.md)) :
monde vanilla (`normal`), plat (`flat`) ou vide (`void`).

### Importer un monde Bedrock

Déposer dans `worlds/` :

- un fichier **`.mcworld`** (export depuis Minecraft), ou
- un dossier de monde Bedrock (avec `db/` et `level.dat`), par exemple venant d'un Realm ou de BDS.

Au démarrage, Bedrokia le convertit dans son format (blocs et biomes), garde le point d'apparition et crée un
`world.properties` en mode `void` pour que rien ne soit généré autour. Mettre ensuite `level-name` sur ce monde.

> Les entités, coffres et autres données de blocs du monde importé ne sont pas encore repris.

## Packs de ressources

Déposer dans `resource_packs/` des `.mcpack`, des `.zip`, ou des dossiers de pack (avec `manifest.json`). Ils sont
envoyés aux joueurs à la connexion. `force-resources=on` dans `server.properties` oblige à les accepter.
