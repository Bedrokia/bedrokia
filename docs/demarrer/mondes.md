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

> Les forteresses et les bastions ne sont pas encore là.

### Les mobs

- `mobs=off` dans `server.properties` retire tous les mobs : apparitions naturelles, œufs et `/summon`. Les animaux
  restent sauvegardés et reviennent avec `mobs=on`.
- `spawn-animals` et `spawn-monsters` règlent chaque famille séparément.
- **Animaux** : ils apparaissent sur l'herbe, avec le ciel au-dessus. L'espèce dépend du biome : vaches, cochons,
  moutons et poules partout ; chevaux dans les plaines, loups et renards dans la taïga ; lapins dans le désert et la
  neige ; meuh-meuh sur les îles champignons, etc.
- Les animaux sont **sauvegardés avec le monde** (`entities.json`), même quand leurs chunks ne sont plus chargés.
- **Monstres** : ils apparaissent dans le noir, la nuit dehors ou à toute heure dans les grottes, mais jamais près
  d'une torche ou d'une autre source de lumière. Ce sont des husks dans le désert et des strays dans la neige. Dans
  le Nether apparaissent piglins zombifiés, piglins, hoglins, cubes de magma et endermen, selon le biome.
- Comme en vanilla, les monstres disparaissent loin des joueurs.
- **Comportement** :
    - les monstres trouvent leur chemin autour des murs (pathfinding) et ne voient pas à travers les blocs ;
    - les mobs neutres n'attaquent que le joueur qui les frappe : piglins zombifiés (tout le groupe), endermen,
      loups, abeilles, golems de fer, ours polaires, lamas…
    - les animaux suivent le joueur qui tient leur nourriture.
- **Élevage** : nourrir deux animaux (blé, carottes, graines…) les fait se reproduire. Le bébé grandit en
  20 minutes, plus vite si on le nourrit.
- Les cisailles tondent les moutons (la laine repousse quand ils mangent de l'herbe), le seau trait les vaches, et
  les poules pondent des œufs.
- Les mobs subissent les dégâts de chute, de feu et de lave. Ceux qui meurent en brûlant lâchent leur viande cuite.

> Il n'y a pas encore de mobs aquatiques ni volants en apparition naturelle, de dressage, de commerce avec les
> villageois ni de raids.

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
