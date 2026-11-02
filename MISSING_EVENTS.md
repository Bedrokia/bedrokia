# Audit des Événements Minecraft Bedrock pour Bedrokia

Ce document récapitule l'audit complet du système d'événements de **Bedrokia** (`src/Bedrokia.Server/`).
Conformément aux directives de développement, **aucun événement "coquille vide" n'a été créé**. Seuls les événements dont la mécanique sous-jacente ou les paquets réseau sont effectivement gérés par le serveur ont été implémentés et câblés.

---

## 1. Événements non ajoutés (Fonctionnalités ou mécaniques manquantes)

Le tableau suivant répertorie les événements MCBE standards qui ne sont pas encore présents dans Bedrokia en raison de fonctionnalités ou de paquets réseau non implémentés à ce jour dans le moteur :

| Nom de l'événement | Catégorie | Fonctionnalité / Mécanique / Paquet manquant dans `Bedrokia.Server` |
|---|---|---|
| `QueryRegenerateEvent` | Réseau & Serveur | Serveur de requêtes UDP RakNet (UT3 / GameSpy4 ping) absent. Seul le pong de déconnexion/motd non connecté RakNet est actuellement câblé. |
| `PlayerItemCooldownEvent` | Joueur & Inventaire | Système de cooldown des items (gestion des timers serveurs pour ender pearls, fruits de chorus, boucliers et paquet `CooldownPacket`). |
| `WorldUnloadEvent` | Monde | Déchargement dynamique de mondes à chaud. Toutes les dimensions et mondes instanciés dans `BedrokiaServer` demeurent résidents en mémoire jusqu'à l'arrêt du serveur. |
| `ChunkUnloadEvent` | Monde & Chunks | Déchargement dynamique et éviction LRU des chunks inactifs. Les chunks chargés restent actuellement en cache mémoire. |
| `WeatherChangeEvent` | Monde & Météo | Système de météo unifié. Bedrokia utilise actuellement des événements spécialisés distincts : `RainChangeEvent`, `ThunderChangeEvent` et `WeatherEffectEvent`. |
| `CartographyUseEvent` | Blocs & UI | Table de cartographie : UI `CartographyTable`, manipulation de cartes (agrandissement, clonage, verrouillage) et recettes spécifiques non implémentées. |
| `LoomUseEvent` | Blocs & UI | Métier à tisser : UI `Loom`, sélection de motifs de bannières et gestion des recettes de bannières non implémentées. |
| `CauldronChangeEvent` | Blocs | Ticking et états des chaudrons (eau teintée, lave, neige poudreuse, fiole de potion) non gérés. |
| `ComposterUseEvent` | Blocs | Logique de compostage : niveaux de compost (0 à 8), consommation d'items organiques et éjection de farine d'os. |
| `CampfireCookEvent` | Blocs | Feux de camp : ticking individuel des 4 emplacements d'aliments et cuisson passive non implémentés. |
| `FarmlandTrampleEvent` | Blocs | Détection physique de la chute ou du saut d'une entité/joueur sur de la terre labourée (`farmland`) la transformant en terre (`dirt`). |
| `SpongeAbsorbEvent` | Blocs | Algorithme d'absorption d'eau par l'éponge sèche (recherche en largeur BFS dans un rayon de blocs d'eau et mutation en éponge humide). |
| `SnowmanTrailEvent` | Mobs & Blocs | IA du Bonhomme de Neige générant des couches de neige sous ses pas selon le biome et la température. |
| `ChiseledBookshelfInteractEvent` | Blocs | Bibliothèque sculptée : stockage et retrait individuel des 6 livres, orientation et paquets réseau de synchronisation d'état. |
| `DecoratedPotShatterEvent` | Blocs | Vase décoré : brisure par projectile ou outil, gestion des tessons et butins contenus. |
| `BellRingEvent` | Blocs | Cloche : interaction, vibration sonore et surbrillance des raiders à proximité. |
| `TargetBlockHitEvent` | Blocs & Redstone | Cible : calcul du niveau de puissance de signal redstone en fonction de la distance de l'impact au centre. |
| `RespawnAnchorUseEvent` | Blocs | Ancre de réapparition : utilisation comme point de réapparition et explosion hors du Nether. |
| `RespawnAnchorChargeEvent` | Blocs | Ancre de réapparition : insertion de glowstone pour recharger les 4 niveaux. |
| `BlockWaxEvent` | Blocs | Application de rayon de miel pour cirer les blocs de cuivre. |
| `BlockScrapeEvent` | Blocs | Raclage à la hache pour enlever la cire ou l'oxydation des blocs de cuivre. |
| `CopperOxidationEvent` | Blocs | Ticking aléatoire d'oxydation progressive des différentes formes de cuivre. |
| `PlayerBrushBlockEvent` | Blocs & Archéologie | Brossage de gravier/sable suspect (`suspicious_gravel`, `suspicious_sand`) et extraction d'artefacts. |
| `SculkCatalystBloomEvent` | Blocs & Sculk | Catalyseur de sculk : absorption d'XP à la mort d'une créature proche pour propager des veines de sculk. |
| `TrialSpawnerSpawnEvent` | Blocs & Trial Chambers | Générateur d'épreuve : vagues de mobs adaptées au nombre de joueurs. |
| `TrialSpawnerEjectLootEvent` | Blocs & Trial Chambers | Générateur d'épreuve : éjection des récompenses après victoire. |
| `VaultOpenEvent` | Blocs & Trial Chambers | Coffre-fort des chambres d'épreuves : insertion de la clé et table de butin par joueur. |
| `TurtleEggTrampleEvent` | Blocs & Mobs | Piétinement et cassure des œufs de tortue par des monstres (notamment les zombies). |
| `TurtleEggHatchEvent` | Blocs & Mobs | Ticking nocturne, fissuration progressive et éclosion des bébés tortues. |
| `DaylightDetectorChangeEvent` | Blocs & Redstone | Calcul du niveau de luminosité solaire ambiante et mise à jour de la puissance redstone émise (mode normal ou inversé). |
| `EntityRegainHealthEvent` | Entités & Mobs | Régénération de santé passive des animaux et monstres (la régénération du joueur est couverte par `PlayerHealEvent`). |
| `EntityTeleportEvent` | Entités & Mobs | Téléportation d'entités non-joueur (ex: Enderman, Shulker, Chorus fruit sur mob). Les joueurs disposent de `PlayerWarpEvent` et `PlayerMoveEvent`. |
| `EntityEffectChangeEvent` | Entités & Mobs | Application d'effets de potion sur les créatures. Actuellement, seul le joueur possède un gestionnaire d'effets (`StatusEffects`) et l'événement `PlayerEffectChangeEvent`. |
| `EntityTransformEvent` | Entités & Mobs | Transformation d'entités (ex: Villageois en Zombie-Villageois, Zombie en Noyé, Têtard en Grenouille). |
| `EntityPickUpItemEvent` | Entités & Mobs | Ramassage d'items au sol par des créatures (Zombies, Renards, Piglins). Les joueurs disposent de `PlayerPickupItemEvent`. |
| `VillagerAcquireTradeEvent` | Mobs & Villageois | Système d'apprentissage de nouvelles offres commerciales lors de la montée de niveau du villageois. |
| `VillagerCareerChangeEvent` | Mobs & Villageois | Prise de métier dynamique par détection d'un bloc de travail à portée. |
| `VillagerReplenishTradeEvent` | Mobs & Villageois | Restockage quotidien des échanges du villageois lors de son interaction avec son poste de travail. |
| `SlimeSplitEvent` | Mobs | Division d'un Slime ou d'un Magma Cube à sa mort en entités de taille inférieure. |
| `CreeperPowerEvent` | Mobs | Surcharge d'un Creeper frappé par un éclair le transformant en Creeper chargé. |
| `PigZapEvent` | Mobs | Mutation d'un cochon frappé par un éclair en Piglin Zombie. |
| `EndermanChangeBlockEvent` | Mobs | IA de ramassage ou de pose de blocs par l'Enderman. |
| `PiglinBarterEvent` | Mobs | Troc des Piglins recevant un lingot d'or et jetant des items en récompense. |
| `AllayDuplicateEvent` | Mobs | Duplication de l'Allay par insertion d'un éclat d'améthyste près d'un juke-box en lecture. |
| `AxolotlPlayDeadEvent` | Mobs | Mécanique de camouflage / simulation de mort de l'Axolotl avec régénération rapide. |
| `FrogEatEntityEvent` | Mobs | Attaque de langue de grenouille avalant de petits slimes ou magma cubes pour produire des froglights. |
| `GoatRamEntityEvent` | Mobs | Charge et coup de tête de la chèvre projetant une entité. |
| `SnifferDigEvent` | Mobs | Flairage et déterrage de graines antiques par le renifleur. |
| `ArmadilloRollEvent` | Mobs | Enroulement du tatou en boule protectrice face au danger. |
| `WindChargeBurstEvent` | Mobs & Projectiles | Explosion d'onde de choc de la charge de vent (Breeze ou tir joueur). |
| `HangingPlaceEvent` / `HangingBreakEvent` | Entités | Pose et destruction d'entités suspendues (tableaux, peintures). |
| `ItemFrameUseEvent` / `ItemFrameDropItemEvent` | Entités & Blocs | Entité cadre d'item (`item_frame`, `glow_item_frame`) : insertion, rotation et chute de l'item. |
| `AreaEffectCloudApplyEvent` | Entités & Potions | Nuage d'effet persistant (souffle du dragon, potion persistante) appliquant des effets de zone dans le temps. |
| `RaidTriggerEvent` | Village & Illagers | Déclenchement d'un raid de village par l'effet Bad Omen / Mauvais Présage. |

---

## 2. Événements partiellement supportés ou fusionnés

Certaines mécaniques sont regroupées de manière idiomatique dans Bedrokia au sein de classes d'événements existantes pour optimiser les performances et la cohérence de l'API :

### `BlockEvent`
- **Rôle :** Gère de manière unifiée toutes les modifications de blocs dans le monde.
- **Support :**
  - Pose de bloc (`IsPlace == true`) : correspond aux événements de type `BlockPlaceEvent`.
  - Destruction de bloc (`IsBreak == true`) : correspond aux événements de type `BlockBreakEvent`.
- **Propriétés disponibles :** `Player`, `World`, `X`, `Y`, `Z`, `Block`, `PreviousBlock`, `IsBreak`, `IsPlace`.
- **Cancellable :** Oui (`CancellableEvent`).

### `PlayerToggleActionEvent`
- **Rôle :** Centralise les actions de bascule de postures et d'états physiques via l'énumération `ToggleActionType`.
- **Types couverts :**
  - `ToggleActionType.Sneak` (S'accroupir)
  - `ToggleActionType.Sprint` (Sprinter)
  - `ToggleActionType.Swim` (Nager)
  - `ToggleActionType.Glide` (Planer en Élytres)
  - `ToggleActionType.Jump` (Sauter)
- **Spécificités Bedrock :** Le vol (`PlayerToggleFlightEvent`) et le ramper (`PlayerToggleCrawlEvent`) font l'objet d'événements dédiés et distincts car ils correspondent à des flags réseau Bedrock d'autorisation et d'état indépendants (`InputFlagStartFlying`, `InputFlagStartCrawling`).

### `PlayerEffectChangeEvent`
- **Rôle :** Regroupe tous les changements d'effets de statut du joueur (`StatusEffects`).
- **Support :**
  - Ajout d'un effet (`Action == EffectAction.Add`)
  - Rafraîchissement / Prolongation (`Action == EffectAction.Update`)
  - Suppression / Expiration / Consommation de lait (`Action == EffectAction.Remove`)
- **Propriétés :** `Player`, `Effect`, `Duration`, `Amplifier`, `Action`.

### `IPlayerEvent` et uniformisation `event.player()`
- **Architecture :** Tous les événements impliquant un joueur implémentent `IPlayerEvent` :
  ```csharp
  public interface IPlayerEvent
  {
      BedrockConnection Player { get; }
      BedrockConnection player() => Player;
  }
  ```
- **Convention :** Les scripts et plugins peuvent indifféremment accéder à `event.Player` ou invoquer `event.player()`.

### Hooks Snake_Case exposés dans `ServerEvents`
La classe `ServerEvents` fournit les hooks d'API snake_case directs suivants :
- `on_player_swing_miss(Action<PlayerMissSwingEvent>)` : Clic gauche dans le vide sans cible.
- `on_player_fly_toggle(Action<PlayerToggleFlightEvent>)` : Bascule de l'état de vol.
- `on_player_crawl(Action<PlayerToggleCrawlEvent>)` : Bascule de l'état accroupi / ramper dans un espace d'un bloc.
- `on_player_attack(Action<PlayerAttackEvent>)` : Coup porté à une entité avec dégâts modifiables et ID de cible.
- `on_player_fall(Action<PlayerFallEvent>)` : Chute avec distance et dégâts modifiables.
- `on_player_world_change(Action<PlayerWorldChangeEvent>)` : Changement de monde / dimension.
- `on_player_chunk_enter(Action<PlayerChunkEnterEvent>)` : Franchissement de frontière de chunk.
- `on_resource_pack_status(Action<ResourcePackClientResponseEvent>)` : Progression du chargement des packs de ressources du client.
- `on_player_afk(Action<PlayerAfkEvent>)` : Changement de statut AFK (inactivité / reprise).

