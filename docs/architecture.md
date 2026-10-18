---
title: Architecture
---

# Architecture

```
src/Bedrokia.Protocol/            encodage Bedrock (varints, NBT), paquets, compression, chiffrement, login Xbox Live
src/Bedrokia.Transport.NetherNet/ NetherNet : signalisation TCP + WebRTC (SIPSorcery) en UDP
src/Bedrokia.Server/              le moteur
  BedrokiaServer.cs                 boucle de jeu (20 ticks/s), connexions, commandes intégrées
  BlockTicks.cs, Furnaces.cs        blocs vivants : ticks aléatoires, cultures, TNT, fours
  Core/                             server.properties, journal, dossiers, listes ops/whitelist/bans
  Commands/                         gestion des commandes et paquet AvailableCommands
  Network/                          une connexion = un joueur : login, mouvements, inventaire, interactions, santé
  Game/                             monde, chunks, blocs, objets, recettes, enchantements, fluides, génération
  Game/Generation/                  biomes, relief, arbres (portés de Glowstone)
  Game/Import/                      lecture LevelDB et import des mondes Bedrock
  Entities/                         mobs, objets au sol, blocs qui tombent
  Plugins/                          chargement des plugins, événements, config
  Data/                             données vanilla (états de blocs, objets, recettes), venant de Dragonfly
src/Bedrokia.Cli/                 l'exécutable : console, bannière, arrêt propre
tests/Bedrokia.Tests/             tests automatiques (protocole, monde, blocs, inventaire, plugins...)
examples/plugins/                 plugins d'exemple
```

## Principes

- **Un seul thread de jeu.** Les paquets réseau sont mis en file et traités pendant le tick : pas de verrous dans la
  logique du jeu, et les plugins tournent sur ce même thread.
- **Le serveur décide.** Le client prédit (pose de bloc, inventaire), le serveur valide et corrige.
- **Vanilla d'abord.** Chaque comportement est repris d'un projet open source compatible (MIT / Apache) ; la source
  est citée en commentaire au-dessus du code.
- **Simple.** Pas de couche d'abstraction sans besoin ; un fichier = un sujet.
