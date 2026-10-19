---
title: Create a plugin
---

# Create a plugin

Bedrokia plugins are written in **C#** and organised like Java (Bukkit / Spigot) or PocketMine-MP plugins.
**No build step**: the server compiles the code at startup (with Roslyn). A broken plugin does not stop the server
from starting: the console says which one, why, and where.

## Layout

```
plugins/
└── MyPlugin/
    ├── plugin.yml              plugin description (required)
    ├── src/                    the C# code (every .cs, sub-folders included)
    │   ├── MyPlugin.cs
    │   └── Commands/...
    └── resources/
        └── config.yml          default config, copied to plugin_data/MyPlugin/ on first start
```

Also accepted: a single file `plugins/MyPlugin.cs`, or a DLL `plugins/MyPlugin.dll` (a .NET 10 library that
references `Bedrokia.Server`).

## plugin.yml

```yaml
name: MyPlugin               # required: letters, digits, _ - .
version: 1.0.0
main: MyPlugin.Main          # required: full name of the class that derives from Plugin
api: 1.0.0
author: Me
description: What the plugin does.
website: https://example.com
depend: [OtherPlugin]        # loaded first; if missing, this plugin is not loaded
softdepend: [Optional]       # loaded first when present
commands:
  hello:
    description: Says hello.
    usage: "[player]"
    permission: everyone     # everyone, op, or a permission node (e.g. myplugin.command.vip)
    aliases: [hi]
```

## The main class

```csharp
namespace MyPlugin;

public sealed class Main : Plugin
{
    public override void OnEnable()
    {
        Log("Started!");
        Events.PlayerJoin += player => player.SendMessage("§aWelcome " + player.Name);
    }

    public override void OnDisable()
    {
        // save your data here
    }

    // Called for the commands declared in plugin.yml. false = show the usage.
    public override bool OnCommand(CommandSender sender, string command, string[] args)
    {
        if (command == "hello")
        {
            if (!sender.HasPermission("myplugin.command.hello"))
            {
                sender.Reply("§cYou do not have permission.");
                return true;
            }
            sender.Reply(Config.GetString("message", "Hello!"));
            return true;
        }
        return false;
    }
}
```

These `using` directives are automatic: `System`, `System.Collections.Generic`, `System.IO`, `System.Linq`,
`System.Threading.Tasks`, `Bedrokia.Server`, `Bedrokia.Server.Commands`, `Bedrokia.Server.Game`,
`Bedrokia.Server.Network`, `Bedrokia.Server.Permissions`, `Bedrokia.Server.Plugins`.

All plugin code runs on the **game thread** (20 ticks per second): no locks needed, but never block (no
`Thread.Sleep`, no synchronous network request).

## What `Plugin` gives you

| Member | Role |
|---|---|
| `Name`, `Version`, `Info` | plugin.yml data (`Info.Author`, `Info.Commands`...) |
| `Server` | the server: players, world, commands |
| `Events` | the events (see below) |
| `Config` | `config.yml` in the data folder |
| `DataFolder` | `plugin_data/<Name>/` for your files |
| `SaveResource(name, replace)` | copies a file from `resources/` to the data folder |
| `ReloadConfig()` | reads `config.yml` again |
| `Log`, `LogWarning`, `LogError` | writes to the console, prefixed with the plugin name |
| `RegisterCommand(name, description, usage, opOnly, action)` | adds a command from code |

## Config

`resources/config.yml` is copied to `plugin_data/MyPlugin/config.yml` on first start; the server owner then edits it
freely. Read it with dotted paths:

```csharp
var title = Config.GetString("sidebar.title", "My server");
var delay = Config.GetInt("staff.cooldown", 20);
var enabled = Config.GetBool("sidebar.enabled", true);
var lines = Config.GetList("sidebar.lines");   // List<string>
Config.Set("stats.joins", 42);
Config.Save();
```

## Events

Subscribe with `+=` in `OnEnable`. *Cancellable* events have `Cancelled`: set it to `true` and the server undoes the
action (the block comes back, the message is not sent...).

| Event | Argument | Cancellable |
|---|---|---|
| `PlayerJoin` | the player | no |
| `PlayerQuit` | the player | no |
| `PlayerMove` | the player (position or rotation changed) | no |
| `PlayerChat` | `PlayerChatEvent`: `Player`, `Message` (editable) | yes |
| `BlockBreak` | `BlockEvent`: `Player`, `X`, `Y`, `Z`, `Block` | yes |
| `BlockPlace` | `BlockEvent` | yes |
| `ItemUse` | `ItemUseEvent`: `Player`, `Item`, clicked position and `Block`, `Face` | yes |
| `Tick` | tick number (20 per second) | no |

```csharp
Events.BlockBreak += e =>
{
    if (e.Block.Name == "minecraft:diamond_ore" && e.Player.GameMode == GameMode.Creative)
        e.Cancelled = true;
};
```

If a handler crashes, the full error is shown **once** with the plugin and event names; the other plugins and the
server keep running.

## Players (`BedrockConnection`)

| Member | Role |
|---|---|
| `Name`, `Position` (x, eye y, z), `Yaw`, `Pitch`, `GameMode`, `IsOp` | state |
| `SendMessage(text)` | chat message (`§` colour codes) |
| `SendTitle(title, subtitle, fadeIn, stay, fadeOut)` | big title on screen |
| `SendActionBar(text)` | text above the hotbar |
| `SetSidebar(title, lines)` / `ClearSidebar()` | sidebar scoreboard (15 lines max) |
| `AddEffect(effect, level, seconds, particles)` / `RemoveEffect(effect)` | effects (`BedrockConnection.Effects.Speed`, `JumpBoost`, `NightVision`...) |
| `SetSpeed(speed)` | walk speed (vanilla 0.1) |
| `SetMotion(vx, vy, vz)` | pushes the player |
| `GiveItem(stack)` | gives an item (what does not fit drops on the ground) |
| `HeldItem`, `ArmourItem(0..3)` | item in hand, worn armour |
| `Teleport(x, y, z)`, `Disconnect(reason)` | teleports, kicks |

## Server and world

| Member | Role |
|---|---|
| `Server.Players` | connected players (filter with `IsInGame`) |
| `Server.BroadcastMessage(text)` | message to everyone |
| `Server.World.GetBlock(x, y, z)` | block at a position |
| `Server.SetBlockAndUpdate(x, y, z, block)` | places a block and tells the players |
| `Server.SpawnParticle(name, x, y, z)` | particles (`minecraft:heart_particle`, `minecraft:villager_happy`, `minecraft:basic_flame_particle`...) |
| `Server.StrikeLightning(x, y, z)` | lightning bolt |
| `Server.Explode(x, y, z, radius)` | explosion |
| `Server.DropItem(stack, x, y, z)` | item on the ground |
| `Server.Schedule(ticks, action)` | runs an action later |
| `Server.Plugins.Get(name)` | another plugin |
| `Server.Config` | `server.properties` settings |

## Custom-looking items

Based on a vanilla item, with a name, lore and glint:

```csharp
var sword = Enchanting.With(ItemStack.Of("diamond_sword")!, [(9, 5)])     // 9 = sharpness
    .WithName("§c§lDragon Sword", "§7Forged in the Nether");
player.GiveItem(sword);

if (player.HeldItem?.CustomName == "§c§lDragon Sword") { /* special power */ }
```

Brand-new blocks and items are supported through the custom content API (`CustomBlockBuilder`, `CustomItemBuilder`):
see [Custom items](items.md), [Custom blocks](blocks.md) and the `CustomContent` example.

## Console at startup

```
[INFO] Loading 3 plugin(s) from plugins/...
[ERROR] Plugin 'Broken' could not be loaded: 1 compile error(s):
    plugins\Broken\src\Main.cs(12,9): error CS0103: The name 'player' does not exist in the current context
[INFO]   [OK]     CustomContent v1.0.0 by Bedrokia (folder: CustomContent) - Custom blocks and items.
[INFO]   [OK]     Ping v1.0.0 by Bedrokia (folder: Ping) - /ping command.
[ERROR]   [FAILED] Broken (see the error above)
[WARN] Plugins: 2 loaded, 1 failed (3.4 s).
```

## Develop with an IDE

For auto-completion, add a `.csproj` next to `plugin.yml` that references `src/Bedrokia.Server` (see
[examples/plugins](https://github.com/Nexiumpvp2/bedrokia/tree/main/examples/plugins)). The server ignores it: it
compiles `src/` by itself.

## Examples

- **CustomContent**: custom blocks and items (ruby sword, pickaxe, armour, apple, ores and lamps) with their Bedrock
  components.
- **Listeners**: events split into separate listener files.
- **Ping**: `/ping` command (alias `/pong`) with arguments, `config.yml` and action bar.
- **MagicItems**: lightning staff (right click = lightning), speed boots (Speed + Jump Boost), jump pad block.
  Command `/magie`.
- **Showcase**: welcome title, live scoreboard, flame spiral `/fx`, announcements `/annonce`. Everything is set in
  its `config.yml`.
