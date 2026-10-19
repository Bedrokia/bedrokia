<div align="center">

```
 ____  _____ ____  ____   ___  _  __ ___    _
| __ )| ____|  _ \|  _ \ / _ \| |/ /|_ _|  / \
|  _ \|  _| | | | | |_) | | | | ' /  | |  / _ \
| |_) | |___| |_| |  _ <| |_| | . \  | | / ___ \
|____/|_____|____/|_| \_\\___/|_|\_\|___/_/   \_\
```

**Serveur Minecraft: Bedrock Edition écrit en C# / .NET 10**

Bedrock 26.50 (protocole 2193) · NetherNet / WebRTC · Plugins en C# sans compilation · Apache-2.0

🇫🇷 **Français** · 🇬🇧 [English](README.en.md)

[📖 Documentation](https://nexiumpvp2.github.io/bedrokia/) · [📝 Changelog](https://nexiumpvp2.github.io/bedrokia/projet/changelog/) · [⬇️ Téléchargements](https://github.com/Nexiumpvp2/bedrokia/releases/latest) · [🦖 Egg Pterodactyl](deploy/pterodactyl/egg-bedrokia.json)

</div>

---

> [!WARNING]
> **Ce projet est codé avec l'IA, et un peu d'humain.**
> La quasi-totalité du code de Bedrokia a été écrite par une intelligence artificielle (Claude, d'Anthropic, via
> Claude Code), guidée, testée en jeu et corrigée par un humain. Le code peut contenir des erreurs que personne n'a
> encore vues : testez avant de l'utiliser pour un vrai serveur, et signalez les bugs dans les *issues*.

## 📖 Documentation

Toute la doc est sur **[nexiumpvp2.github.io/bedrokia](https://nexiumpvp2.github.io/bedrokia/)**, mise à jour
automatiquement à chaque version :

| Page | Contenu |
|---|---|
| 🚀 [Installation](https://nexiumpvp2.github.io/bedrokia/demarrer/installation/) | Linux, Windows, Docker, systemd, ports à ouvrir |
| 🦖 [Pterodactyl](https://nexiumpvp2.github.io/bedrokia/demarrer/pterodactyl/) | Installer l'egg et régler les ports |
| ⚙️ [Configuration](https://nexiumpvp2.github.io/bedrokia/demarrer/configuration/) | `server.properties`, `world.properties`, fichiers du serveur |
| 💬 [Commandes](https://nexiumpvp2.github.io/bedrokia/demarrer/commandes/) | Toutes les commandes et les permissions |
| 🌍 [Mondes et packs](https://nexiumpvp2.github.io/bedrokia/demarrer/mondes/) | Mondes plats ou vides, import de mondes Bedrock, packs de ressources |
| 🧩 [Créer un plugin](https://nexiumpvp2.github.io/bedrokia/plugins/) | `plugin.yml`, événements, commandes, config, API |
| 🗡️ [Objets](https://nexiumpvp2.github.io/bedrokia/plugins/objets/) et 🧱 [blocs](https://nexiumpvp2.github.io/bedrokia/plugins/blocs/) personnalisés | Contenu moddé depuis un plugin |
| 📝 [Changelog](https://nexiumpvp2.github.io/bedrokia/projet/changelog/) | Toutes les versions et leurs changements |
| 🗺️ [Feuille de route](https://nexiumpvp2.github.io/bedrokia/projet/roadmap/) | Ce qui marche, ce qui manque |
| 🤖 [Le projet et l'IA](https://nexiumpvp2.github.io/bedrokia/projet/ia/) | Pourquoi, comment, contribuer |

## 🤖 Pourquoi ce projet ?

Bedrokia est une **expérience** :

- **tester l'IA** sur un vrai logiciel complexe : un protocole réseau binaire, un moteur de jeu, la génération de
  mondes, un système de plugins… pas un petit script ;
- montrer qu'**une IA peut aussi maintenir le projet** dans le temps : corriger les bugs, ajouter des fonctions, écrire
  les tests et la doc, avec des développeurs qui relisent et décident ;
- montrer que **l'IA est l'avenir du développement** : un humain qui sait ce qu'il veut, et une IA qui écrit le code,
  peuvent construire en quelques jours ce qui demandait des mois.

**Tous les mainteneurs sont les bienvenus**, humains ou IA, débutants ou experts. Ouvrez une *issue*, proposez une
*pull request*, ou demandez à votre IA préférée de corriger un bug : tant que les tests passent et que le code reste
lisible, c'est cool. 😎

## ✨ Ce que fait Bedrokia

| Domaine | Détails |
|---|---|
| **Connexion** | Bedrock 26.50, NetherNet (WebRTC) sur un seul port TCP + une plage UDP, authentification Xbox Live, chiffrement |
| **Monde** | Génération vanilla (biomes, relief, arbres, minerais), monde plat ou vide, sauvegarde, import de mondes Bedrock (`.mcworld` / LevelDB) |
| **Blocs** | Pose orientée (escaliers, bûches, portes, lits…), coffres, coffre de l'Ender, tonneaux, fours / hauts fourneaux / fumoirs, portes, trappes, leviers, boutons, lits (réapparition, nuit sautée), murets connectés, gravité (sable, gravier), eau et lave qui coulent, cultures, arbres, TNT et explosions |
| **Joueurs** | Inventaire complet, artisanat (toutes les recettes vanilla), enchantements, expérience, faim, dégâts, chute, mort, armure, objets lâchés et ramassés |
| **Mobs** | Animaux et monstres qui apparaissent, se déplacent, attaquent, lâchent des objets ; creepers qui explosent |
| **Serveur** | `server.properties` comme PocketMine-MP, ops, liste blanche, bannissements, permissions et groupes, ~35 commandes, packs de ressources, annonce des nouvelles versions dans la console |
| **Plugins** | Plugins C# organisés comme Bukkit / PocketMine (`plugin.yml`, `src/`, `resources/config.yml`), **compilés automatiquement au démarrage**, événements annulables, commandes, titres, scoreboard, effets, particules |

Ce qui manque encore : coffres doubles, panneaux éditables, enclume, tailleur de pierre, meule, Nether et End,
redstone, grottes et structures. Voir la [feuille de route](https://nexiumpvp2.github.io/bedrokia/projet/roadmap/).

## 🚀 Démarrage rapide

1. Télécharger `bedrokia` (Linux) ou `bedrokia.exe` (Windows) depuis les [releases](https://github.com/Nexiumpvp2/bedrokia/releases).
   C'est un exécutable unique : **aucun .NET à installer**.
2. Le lancer dans un dossier vide :

```bash
chmod +x bedrokia && ./bedrokia
```

3. Ouvrir les ports : **TCP 19132** et **UDP 19132 à 19156** (port du serveur + nombre de joueurs + 5).
4. Dans Minecraft : *Jouer → Serveurs → Ajouter un serveur*, adresse de la machine, port 19132.

**Mettre à jour = remplacer un seul fichier** (comme le `.phar` de PocketMine-MP) : télécharger le nouveau `bedrokia`,
remplacer l'ancien, redémarrer. Mondes, joueurs et plugins sont gardés.

Au premier lancement, le serveur crée ses fichiers, comme PocketMine-MP :

```
server.properties   réglages (commentés)
ops.txt  white-list.txt  banned-players.txt
worlds/             mondes (un dossier par monde, avec world.properties)
players/            sauvegarde des joueurs
plugins/            plugins
plugin_data/        données et config des plugins
resource_packs/     packs de ressources (dossiers ou .mcpack)
```

## 🦖 Pterodactyl

Un egg est fourni : [`deploy/pterodactyl/egg-bedrokia.json`](deploy/pterodactyl/egg-bedrokia.json).
*Admin → Nests → Import Egg*, puis créer un serveur avec. Le serveur a besoin de son port principal **et** d'une plage
de ports UDP (ajouter des allocations, ou régler la variable `WEBRTC_PORTS`). Détails :
[doc Pterodactyl](https://nexiumpvp2.github.io/bedrokia/demarrer/pterodactyl/).

## 🧩 Plugins en 30 secondes

```
plugins/Hello/
├── plugin.yml
└── src/Main.cs
```

```yaml
# plugin.yml
name: Hello
version: 1.0.0
main: Hello.Main
author: Moi
commands:
  hello:
    description: Dit bonjour.
    permission: everyone
```

```csharp
// src/Main.cs
namespace Hello;

public sealed class Main : Plugin
{
    public override void OnEnable() =>
        Events.PlayerJoin += player => player.SendTitle("§6Salut", $"§e{player.Name}");

    public override bool OnCommand(CommandSender sender, string command, string[] args)
    {
        sender.Reply("§aBonjour !");
        return true;
    }
}
```

Redémarrer le serveur : le plugin est compilé et chargé, la console l'affiche (ou affiche l'erreur exacte avec le
fichier et la ligne). Plugins d'exemple complets : [`examples/plugins`](examples/plugins).
Documentation complète : [plugins](https://nexiumpvp2.github.io/bedrokia/plugins/).

## 🛠️ Compiler

Prérequis : [SDK .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet test Bedrokia.slnx
```

```bash
dotnet publish src/Bedrokia.Cli -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o out/linux-x64
```

(`win-x64` pour Windows.) **Chaque push sur `main` publie automatiquement une nouvelle release** (v0.3.1, v0.3.2...) avec les deux
exécutables, l'egg et les plugins d'exemple.

## 📁 Structure

```
src/Bedrokia.Protocol/            encodage Bedrock, paquets, NBT, login
src/Bedrokia.Transport.NetherNet/ transport NetherNet (WebRTC)
src/Bedrokia.Server/              moteur : monde, blocs, joueurs, mobs, commandes, plugins
src/Bedrokia.Cli/                 exécutable `bedrokia`
tests/Bedrokia.Tests/             tests automatiques
examples/plugins/                 plugins d'exemple
deploy/                           service systemd, egg Pterodactyl
docs/ docs-en/                    documentation en français et en anglais (Material for MkDocs, sur GitHub Pages)
```

## ⚖️ Licence et sources

Bedrokia est sous **licence Apache 2.0**. Le comportement vanilla est repris de projets open source sous licence
MIT ou Apache : [Dragonfly](https://github.com/df-mc/dragonfly), [gophertunnel](https://github.com/Sandertv/gophertunnel),
[Glowstone](https://github.com/GlowstoneMC/Glowstone), [minecraft-data](https://github.com/PrismarineJS/minecraft-data),
[Cuberite](https://github.com/cuberite/cuberite). Voir [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
Aucun code de PocketMine-MP, PowerNukkitX ou Bedrock Dedicated Server n'est utilisé.

Bedrokia n'est pas affilié à Mojang ni à Microsoft. Minecraft est une marque de Mojang AB.
