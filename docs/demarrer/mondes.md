---
title: Mondes et packs
---

# Mondes et packs de ressources

## Mondes

Le monde chargé est `worlds/<level-name>/`. Chaque monde a son `world.properties` (voir [Configuration](configuration.md)) :
monde vanilla (`normal`), plat (`flat`), vide (`void`) ou Nether (`nether`).

### Le Nether

Le Nether est le monde `worlds/<level-name>/nether/`, créé au premier démarrage avec `generator=nether`.

- Un cadre d'obsidienne (intérieur de 2×3 à 21×21) allumé au briquet devient un portail.
- Rester 4 secondes dans le portail (tout de suite en créatif) fait changer de dimension.
- Les distances sont divisées par 8 dans le Nether. Un portail existant à 16 blocs près est réutilisé, sinon un
  portail est construit à l'arrivée.
- Comme en vanilla :
    - les lits explosent ;
    - l'eau s'évapore ;
    - la lave coule plus vite et plus loin ;
    - mourir dans le Nether ramène dans le monde normal.
- Le joueur se reconnecte dans la dimension où il s'est déconnecté.

> Les mobs du Nether, les forteresses et les bastions ne sont pas encore là.

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
