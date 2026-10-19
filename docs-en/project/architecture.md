---
title: Architecture
---

# Architecture

```
src/Bedrokia.Protocol/            Bedrock encoding (varints, NBT), packets, compression, encryption, Xbox Live login
src/Bedrokia.Transport.NetherNet/ NetherNet: TCP signalling + WebRTC (SIPSorcery) over UDP
src/Bedrokia.Server/              the engine
  BedrokiaServer.cs                 game loop (20 ticks/s), connections, built-in commands
  BlockTicks.cs, Furnaces.cs        living blocks: random ticks, crops, TNT, furnaces
  Core/                             server.properties, log, folders, ops/whitelist/bans, update checker
  Commands/                         command handling and the AvailableCommands packet
  Permissions/                      permission nodes, groups, prefixes
  Network/                          one connection = one player: login, movement, inventory, interactions, health
  Game/                             world, chunks, blocks, items, recipes, enchanting, fluids, generation
  Game/Custom/                      custom blocks and items for plugins
  Game/Generation/                  biomes, terrain, trees (ported from Glowstone)
  Game/Import/                      LevelDB reading and Bedrock world import
  Entities/                         mobs, dropped items, falling blocks
  Plugins/                          plugin loading, events, config
  Data/                             vanilla data (block states, items, recipes), from Dragonfly
src/Bedrokia.Cli/                 the executable: console, banner, clean shutdown
tests/Bedrokia.Tests/             automated tests (protocol, world, blocks, inventory, plugins...)
examples/plugins/                 example plugins
```

## Principles

- **One game thread.** Network packets are queued and handled during the tick: no locks in the game logic, and
  plugins run on that same thread.
- **The server decides.** The client predicts (block placement, inventory), the server checks and corrects.
- **Vanilla first.** Every behaviour comes from a compatible open source project (MIT / Apache); the source is cited
  in a comment above the code.
- **Simple.** No abstraction layer without a need; one file = one topic.
