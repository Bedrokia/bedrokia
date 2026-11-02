<div align="center">

```
 ____  _____ ____  ____   ___  _  __ ___    _
| __ )| ____|  _ \|  _ \ / _ \| |/ /|_ _|  / \
|  _ \|  _| | | | | |_) | | | | ' /  | |  / _ \
| |_) | |___| |_| |  _ <| |_| | . \  | | / ___ \
|____/|_____|____/|_| \_\\___/|_|\_\|___/_/   \_\
```

**A Minecraft: Bedrock Edition server written in C# / .NET 10**

Bedrock 26.60 (protocol 2225) · NetherNet / WebRTC · C# plugins without a build step · Apache-2.0

🇬🇧 **English** · 🇫🇷 [Français](README.fr.md)

[⬇️ Downloads](https://github.com/Nexiumpvp2/bedrokia/releases/latest) · [🦖 Pterodactyl egg](deploy/pterodactyl/egg-bedrokia.json) · [💬 Discord](https://discord.gg/C2VTugEMMa)

[![Discord](https://img.shields.io/badge/Discord-join-5865F2?logo=discord&logoColor=white)](https://discord.gg/C2VTugEMMa)

</div>

---

> [!WARNING]
> **This project is coded with AI, and a bit of human.**
> Almost all of Bedrokia's code was written by an artificial intelligence (Claude, by Anthropic, through Claude Code),
> guided, tested in game and corrected by a human. The code may contain bugs nobody has seen yet: test it before
> running a real server, and report bugs in the *issues*.

## 🤖 Why this project?

Bedrokia is an **experiment**:

- **test AI** on real, complex software: a binary network protocol, a game engine, world generation, a plugin
  system… not a small script;
- show that **an AI can also maintain the project** over time: fix bugs, add features, write the tests and the docs,
  with developers who review and decide;
- show that **AI is the future of software development**: a human who knows what they want and an AI that writes
  the code can build in days what used to take months.

**Every maintainer is welcome**, human or AI, beginner or expert. Open an *issue*, send a *pull request*, or ask your
favourite AI to fix a bug: as long as the tests pass and the code stays readable, it's cool. 😎

## ✨ What Bedrokia does

| Area | Details |
|---|---|
| **Connection** | Bedrock 26.50, NetherNet (WebRTC) on one port (TCP + UDP) for every player, Xbox Live authentication, encryption |
| **World** | Vanilla generation (biomes, terrain, trees, ores), flat or void worlds, saving, Bedrock world import (`.mcworld` / LevelDB) |
| **Blocks** | Oriented placement (stairs, logs, doors, beds…), chests, ender chests, barrels, furnaces / blast furnaces / smokers, anvils (repair, rename, combine enchantments, falling damage), grindstone (disenchant, XP refund), stonecutter, enchanting table, doors, trapdoors, levers, buttons, beds (respawn, skip the night), connected walls, gravity (sand, gravel, anvils), flowing water and lava, crops, trees, TNT and explosions |
| **Players** | Full inventory, crafting (every vanilla recipe), enchanting ecosystem (enchanting table, anvil merging/repair, grindstone, mending, unbreaking, combat/protection formulas), experience, hunger, damage, falls, death, armour, dropped and picked-up items |
| **Mobs** | Animals and monsters that spawn, move, attack and drop items; creepers that explode |
| **Server** | PocketMine-like `server.properties`, ops, whitelist, bans, permissions and groups, ~35 commands, resource packs, new versions announced in the console |
| **Plugins** | C# plugins organised like Bukkit / PocketMine (`plugin.yml`, `src/`, `resources/config.yml`), **compiled automatically at startup**, cancellable events, commands, titles, scoreboard, effects, particles, custom blocks and items |

Still missing: double chests, editable signs, Nether and End, redstone, caves and
structures.

## 🚀 Quick start

1. Download `bedrokia` (Linux) or `bedrokia.exe` (Windows) from the [releases](https://github.com/Nexiumpvp2/bedrokia/releases).
   It is a single executable: **no .NET to install**.
2. Run it in an empty folder:

```bash
chmod +x bedrokia && ./bedrokia
```

3. Open the port **19132** in **TCP and UDP**.
4. In Minecraft: *Play → Servers → Add Server*, the machine's address, port 19132.

**Updating = replacing one file**: download the new `bedrokia`, replace the old one, restart. Worlds, players and
plugins are kept. The console tells you when a new version is out.

On first start, the server creates its files:

```
server.properties   settings (with comments)
ops.txt  white-list.txt  banned-players.txt
worlds/             worlds (one folder per world, with world.properties)
players/            player saves
plugins/            plugins
plugin_data/        plugin data and configs
resource_packs/     resource packs (folders or .mcpack)
```

## 🦖 Pterodactyl

An egg is included: [`deploy/pterodactyl/egg-bedrokia.json`](deploy/pterodactyl/egg-bedrokia.json).
*Admin → Nests → Import Egg*, then create a server with it. The server only needs its main port (TCP and UDP).

## 🧩 Plugins in 30 seconds

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
author: Me
commands:
  hello:
    description: Says hello.
    permission: everyone
```

```csharp
// src/Main.cs
namespace Hello;

public sealed class Main : Plugin
{
    public override void OnEnable() =>
        Events.PlayerJoin += player => player.SendTitle("§6Hi", $"§e{player.Name}");

    public override bool OnCommand(CommandSender sender, string command, string[] args)
    {
        sender.Reply("§aHello!");
        return true;
    }
}
```

Restart the server: the plugin is compiled and loaded, and the console says so (or shows the exact error with the
file and line). Full example plugins: [`examples/plugins`](examples/plugins).

## 🛠️ Build

Requirement: the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet test Bedrokia.slnx
```

```bash
dotnet publish src/Bedrokia.Cli -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o out/linux-x64
```

(`win-x64` for Windows.) **Every push to `main` publishes a new release automatically** (v0.3.1, v0.3.2...) with both
executables, the egg and the example plugins.

## 📁 Layout

```
src/Bedrokia.Protocol/            Bedrock encoding, packets, NBT, login
src/Bedrokia.Transport.NetherNet/ NetherNet transport (WebRTC)
src/Bedrokia.Server/              engine: world, blocks, players, mobs, commands, plugins
src/Bedrokia.Cli/                 the `bedrokia` executable
tests/Bedrokia.Tests/             automated tests
examples/plugins/                 example plugins
deploy/                           systemd service, Pterodactyl egg
```

## ⚖️ Licence and sources

Bedrokia is under the **Apache 2.0 licence**. Vanilla behaviour is taken from open source projects under the MIT or
Apache licences: [Dragonfly](https://github.com/df-mc/dragonfly), [gophertunnel](https://github.com/Sandertv/gophertunnel),
[Glowstone](https://github.com/GlowstoneMC/Glowstone), [minecraft-data](https://github.com/PrismarineJS/minecraft-data),
[Cuberite](https://github.com/cuberite/cuberite). See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
No code from PocketMine-MP, PowerNukkitX or Bedrock Dedicated Server is used.

Bedrokia is not affiliated with Mojang or Microsoft. Minecraft is a trademark of Mojang AB.
