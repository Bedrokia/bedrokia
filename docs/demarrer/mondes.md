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
- **Mobs aquatiques** : morues, saumons, poissons tropicaux, poissons-globes, calamars et dauphins dans les océans
  et les rivières, selon la température de l'eau ; calamars luisants dans l'eau des grottes ; noyés dans l'eau sombre.
  Les poissons nagent sans sortir de l'eau et étouffent sur la terre ferme.
- **Mobs volants** : chauves-souris dans les grottes ; phantoms la nuit au-dessus des joueurs à découvert, à partir
  du troisième jour du monde (ils tournent en l'air puis plongent pour mordre) ; ghasts dans les grands espaces du
  Nether.
- **Attaques à distance** : les squelettes, strays et pillards tirent de vraies flèches, qu'on voit voler et qui
  peuvent rater. Les ghasts lancent des boules de feu qui explosent, les blazes des rafales de petites boules de feu.
- Comme en vanilla, les monstres disparaissent loin des joueurs, ainsi que les poissons et les chauves-souris.
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

> Il n'y a pas encore de dressage, de commerce avec les villageois ni de raids.

### Les items

Tous les items de Minecraft existent : ils sont dans l'inventaire créatif, s'obtiennent avec `/give` et
s'artisanent. En plus de la nourriture, des armures, des seaux, du briquet et de la poudre d'os, ceux-ci ont leur
usage vanilla :

- **Usure** : les outils, armes et armures s'usent (barre de durabilité) et se cassent. Solidité les ménage.
- **Arc** : on le tend (pleine puissance en 1 s), il consomme une flèche (pas avec Infinité ni en créatif). Puissance,
  Frappe et Flamme marchent. Les flèches plantées se ramassent.
- **Arbalète** : chargée en 1,25 s (moins avec Charge rapide), elle tire fort ; Tir multiple tire trois flèches.
- **Trident** : on le lance (8 dégâts) ; il reste là où il tombe, ou revient avec Loyauté. En mêlée : 9 dégâts.
- **Canne à pêche** : on la lance dans l'eau, on attend que le bouchon plonge, et on la ramène à ce moment-là pour
  attraper un poisson, un déchet ou un trésor (Appât et Chance de la mer marchent).
- **Bouclier** : accroupi, il bloque les attaques et les explosions qui viennent de face.
- **Totem d'immortalité** : dans une main, il évite la mort une fois (Régénération, Absorption, Résistance au feu).
- **Masse** : frapper en tombant ajoute des dégâts selon la hauteur de chute, et annule les dégâts de chute.
- **Hache** : écorce les bûches, les tiges et le bois.
- **Cisailles** : tondent les moutons, sculptent les citrouilles, et récoltent feuilles, herbes et lianes.
- **Étiquette** (renommée à l'enclume) : nomme un mob, qui ne disparaît plus.
- **Seau d'eau sur un poisson** : on l'attrape dans le seau ; le seau de poisson le relâche.
- **Fiole vide** : se remplit d'eau. **Boule de feu** : comme le briquet.
- **Fiole d'expérience**, **charge de vent** (repousse tout autour) et **potion persistante** se lancent.
- **Œuf** : un poussin sort une fois sur huit.
- **Feu d'artifice** : lancé depuis un bloc, ou propulse le joueur qui vole en élytres.
- **Corne de chèvre** : sonne, avec 7 secondes de recharge.

> Pas encore : bateaux, wagonnets, selles, laisses, cartes, ballots et longue-vue (l'arbalète tire dès qu'elle est
> chargée, sans rester armée).

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
