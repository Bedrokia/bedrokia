# Changelog

Format : [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/). Versions : [SemVer](https://semver.org/lang/fr/).

## [0.3.0] - 2026-10-18

Version réécrite en grande partie par IA (Claude, via Claude Code), testée en jeu par un humain.

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
