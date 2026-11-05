# Changelog

Format : [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/). Versions : [SemVer](https://semver.org/lang/fr/).

## [0.4.0] - 2026-10-20

### Ajouté
- **Système complet d'Entités Personnalisées (`Custom Entities`)** :
  - Support du protocole Bedrock : `AvailableActorIdentifiersPacket` (ID 119 avec `idlist`), `SyncActorPropertyPacket` (ID 165), `SetActorLinkPacket` (ID 41), `MobArmourEquipmentPacket` (ID 32), `BossEventPacket` (ID 74), `PropertySyncData`.
  - API plugins avec `CustomEntityBuilder`, `CustomEntityRegistry`, composants, groupes de composants dynamiques et propriétés synchronisées.
  - Cycle de vie complet, gestion des barres de boss, IA de déplacement et combat, montures / cavaliers, et génération automatique des œufs d'apparition (`Spawn Eggs`).
- **Écosystème complet d'enchantements & tables de travail** :
  - Enclume (`Anvil`) : réparation, renommage, combinaison d'enchantements, coût en niveaux, dégradation par usure.
  - Meule (`Grindstone`) : désenchantement d'objets et récupération d'expérience.
  - Tailleur de pierre (`Stonecutter`) : recettes de taille de pierre 1:1.
  - Effets d'enchantements complets en jeu : Fortune, Silk Touch, Efficiency, Sharpness, Protection, Fire Protection, Feather Falling, Blast Protection, Projectile Protection, Unbreaking, Mending, Power, Punch, Flame, Infinity.
- Port unique UDP partagé par tous les joueurs.
- Sélecteurs de cibles `@s`, `@p`, `@r`, `@a`, `@e` dans les commandes.

### Corrigé
- Calcul du multiplicateur d'enchantement Fortune sur les minerais aligné strictement sur la distribution discrète vanilla.

## [0.3.0] - 2026-10-18

Version réécrite et testée en jeu.

### Ajouté
- Transport **NetherNet** (WebRTC) : un port TCP + une plage UDP. RakNet retiré.
- Monde vanilla : biomes, relief, arbres, minerais ; mondes plats et vides (`world.properties`) ; import de mondes Bedrock.
- Inventaire, artisanat (toutes les recettes), enchantements, expérience, faim, dégâts, mort, armure.
- Blocs : orientation, coffres, coffre de l'Ender, tonneaux, fours, portes, trappes, leviers, boutons, lits, murets connectés,
  gravité, eau et lave, cultures, arbres, poudre d'os, seaux, briquet, TNT et explosions.
- Mobs : apparition, IA, attaques, drops, creepers.
- `server.properties`, ops, liste blanche, bannissements, ~35 commandes, packs de ressources.
- **Plugins** : format dossier (`plugin.yml`, `src/`, `resources/config.yml`) compilé au démarrage, fichiers `.cs` et DLL ;
  dépendances, commandes déclarées, événements annulables, API titres / scoreboard / effets / particules / foudre ;
  bilan de chargement et erreurs nommées dans la console ; commande `/plugin`. Deux plugins d'exemple.
- Egg Pterodactyl, releases automatiques sur tag `v*`, documentation GitHub Pages.

### Corrigé
- Armure déséquipée après un clic droit puis l'ouverture de l'inventaire.
- Lits, portes, panneaux impossibles à poser en survie (objets sans bloc associé côté client).
- Démarrage impossible avec un port impair.

## [0.2.0] - 2026-10-14

### Ajouté
- Projet `Bedrokia.Protocol` : encodage Bedrock (varints, NBT minimal), lots de paquets avec compression deflate
  et chiffrement AES-256-CTR + somme de contrôle.
- Login Bedrock protocole 2193 (26.50) : RequestNetworkSettings, Login, vérification du jeton OpenID Xbox Live
  (clés publiées par le service d'autorisation Minecraft, rafraîchies toutes les 30 min), vérification de la
  signature des données client, échange de clés ECDH P-384, packs de ressources (aucun), StartGame, spawn.
- Monde superflat infini envoyé autour du joueur pendant ses déplacements.
- Chat entre joueurs, messages d'arrivée/départ, reconnexion depuis un autre endroit (l'ancienne session est expulsée).
- Commandes console `list`, `say`, `kick`.
- Configuration `OnlineMode` et `ViewDistance`.
- Service systemd (`deploy/bedrokia.service`) et procédure de déploiement Linux.

### Modifié
- Les paquets réseau sont traités sur le thread de jeu (plus de risque d'interblocage entre connexions).

### Limites connues
- Pas encore testé avec un vrai client Bedrock.
- Les joueurs ne se voient pas ; monde non modifiable et non sauvegardé ; registre d'objets vide.
- RakNet uniquement (les clients 26.60+ exigeront NetherNet).

## [0.1.0] - 2026-10-14

### Ajouté
- Socle .NET 10 : configuration `config/server.json` (validée), journaux console + `logs/`, boucle de tick 20 TPS.
- Console interactive : `help`, `version`, `stop`. Arrêt propre sur Ctrl+C / SIGTERM.
- Transport RakNet : pings hors connexion et sessions connectées (fiabilité, ordre, fragmentation, ACK/NACK,
  renvoi, ping, timeout, déconnexion, serveur plein).
- CI Linux + Windows, artefacts `linux-x64` et `win-x64`, Dockerfile.
