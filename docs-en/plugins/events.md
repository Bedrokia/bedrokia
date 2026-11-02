---
title: Event System
---

# Event System

Bedrokia features a high-performance, strongly-typed, and modular C# event system allowing plugins to intercept, modify, or cancel player actions, world dynamics, and server lifecycle operations.

---

## 1. Architectural Highlights

- **Uniform Player Accessor:** Every single event involving a player implements `IPlayerEvent` and exposes both `event.Player` and the uniform accessor method `event.player()`.
- **Cancellable Events:** Events derived from `CancellableEvent` can be cancelled via `e.Cancel()` or `e.Cancelled = true`.
- **Fast-path & Performance:** The server checks fast-path `Has*` boolean flags before allocating event arguments or firing handlers.
- **Convenient API Hooks:** In addition to standard C# event delegates, `ServerEvents` exposes dedicated snake_case helper methods (e.g., `on_player_attack`, `on_player_fall`, etc.).

---

## 2. Plugin Usage Example

```csharp
using Bedrokia.Server.Core;
using Bedrokia.Server.Events;
using Bedrokia.Server.Plugins;

namespace MyPlugin;

public sealed class MyPlugin : Plugin
{
    public override void OnEnable()
    {
        // 1. Cancelling an event
        Events.PlayerDamage += e =>
        {
            if (e.player().GameMode == GameMode.Creative)
                e.Cancel();
        };

        // 2. Modifying properties
        Events.PlayerAttack += e =>
        {
            e.Damage *= 1.5f; // +50% attack damage bonus
        };

        // 3. Using snake_case hooks
        Events.on_player_afk(e =>
        {
            var p = e.player();
            p.SendMessage(e.IsAfk ? "§7You are now AFK." : "§aWelcome back!");
        });
    }
}
```

---

## 3. Complete Catalog of Available Events

### Server, Network & Plugins

| Event | Description | Cancellable | Player Accessor |
|---|---|:---:|:---:|
| `ServerStartedEvent` | Server has started and is ready to accept connections | No | - |
| `ServerStopEvent` | Server is shutting down | No | - |
| `PluginEnableEvent` | A plugin has been enabled | No | - |
| `PluginDisableEvent` | A plugin has been disabled | No | - |
| `ConsoleCommandEvent` | Console command executed (modifiable via `CommandLine`) | **Yes** | - |
| `DataPacketReceiveEvent` | Inbound Bedrock packet received | **Yes** | `event.player()` |
| `DataPacketSendEvent` | Outbound Bedrock packet about to be sent | **Yes** | `event.player()` |

### Player & Bedrock Specifics

| Event | Description | Cancellable | Player Accessor |
|---|---|:---:|:---:|
| `PlayerPreLoginEvent` | Pre-login stage before full handshake | **Yes** | `event.player()` |
| `PlayerLoginEvent` | Handshake and authentication completed | No | `event.player()` |
| `PlayerJoinEvent` | Player spawns into the world | No | `event.player()` |
| `PlayerQuitEvent` | Player disconnects from the server | No | `event.player()` |
| `PlayerKickEvent` | Player kicked (reason modifiable) | **Yes** | `event.player()` |
| `PlayerChatEvent` | Player sends a chat message | **Yes** | `event.player()` |
| `PlayerCommandEvent` | Player executes a command | **Yes** | `event.player()` |
| `PlayerMoveEvent` | Player movement (`To` destination modifiable) | **Yes** | `event.player()` |
| `PlayerWarpEvent` | Instant teleportation / warp | **Yes** | `event.player()` |
| `PlayerAttackEvent` | Player attacks an entity (damage modifiable) | **Yes** | `event.player()` |
| `PlayerDamageEvent` | Player takes damage | **Yes** | `event.player()` |
| `PlayerDeathEvent` | Player dies (drops and message modifiable) | No | `event.player()` |
| `PlayerRespawnEvent` | Player respawns | No | `event.player()` |
| `PlayerFallEvent` | Player lands and takes fall damage (distance and damage modifiable) | **Yes** | `event.player()` |
| `PlayerToggleFlightEvent` | Player toggles flying on or off | **Yes** | `event.player()` |
| `PlayerToggleCrawlEvent` | Player toggles crawling on or off | **Yes** | `event.player()` |
| `PlayerToggleActionEvent` | Physical action toggle (Sneak, Sprint, Swim, Glide, Jump) | **Yes** | `event.player()` |
| `PlayerBedEnterEvent` | Player attempts to enter a bed | **Yes** | `event.player()` |
| `PlayerBedLeaveEvent` | Player leaves a bed | No | `event.player()` |
| `PlayerSleepSkipNightEvent` | Sleeping players cause the night to skip to morning | No | `event.player()` |
| `PlayerAfkEvent` | Player AFK state changes | **Yes** | `event.player()` |
| `PlayerMissSwingEvent` | Player left-clicks into thin air (missed swing) | **Yes** | `event.player()` |
| `ResourcePackClientResponseEvent` | Client resource pack loading progress | No | `event.player()` |
| `PlayerViewDistanceChangeEvent` | View distance / chunk radius change requested (modifiable) | No | `event.player()` |
| `PlayerGameModeChangeEvent` | Game mode changes (`NewGameMode` modifiable) | **Yes** | `event.player()` |
| `PlayerChangeSkinEvent` | Player updates skin data | **Yes** | `event.player()` |
| `PlayerEmoteEvent` | Player triggers an emote animation | **Yes** | `event.player()` |
| `PlayerTransferEvent` | Player transferred to another server (address and port modifiable) | **Yes** | `event.player()` |
| `PlayerFormResponseEvent` | Client responds to a Modal Form UI | **Yes** | `event.player()` |
| `PlayerInputModeChangeEvent` | Player switches input mode (mouse, touch, gamepad) | No | `event.player()` |
| `PlayerExhaustEvent` | Player hunger exhaustion accumulated (`Amount` modifiable) | **Yes** | `event.player()` |
| `PlayerArmorChangeEvent` | Armor equipment slot changed (item modifiable) | **Yes** | `event.player()` |
| `PlayerBucketFillEvent` | Empty bucket collects liquid | **Yes** | `event.player()` |
| `PlayerBucketEmptyEvent` | Liquid bucket placed into world | **Yes** | `event.player()` |
| `PlayerDimensionChangeEvent` | Player transitions between dimensions | **Yes** | `event.player()` |
| `PlayerWorldChangeEvent` | Player changes world | No | `event.player()` |
| `PlayerChunkEnterEvent` | Player enters a new chunk | No | `event.player()` |
| `PlayerEffectChangeEvent` | Status potion effect added, refreshed, or removed | **Yes** | `event.player()` |

### Blocks, Inventories & World

| Event | Description | Cancellable | Player Accessor |
|---|---|:---:|:---:|
| `BlockEvent` | Block placed or broken (`IsPlace` / `IsBreak`) | **Yes** | `event.player()` (if player) |
| `BlockUseEvent` | Player right-clicks or interacts with a block | **Yes** | `event.player()` |
| `StonecutterUseEvent` | Stonecutter craft performed (output modifiable) | **Yes** | `event.player()` |
| `LeavesDecayEvent` | Natural leaf decay | **Yes** | - |
| `MoistureChangeEvent` | Farmland moisture level change | **Yes** | - |
| `WorldLoadEvent` | World loaded into memory | No | - |
| `WorldSaveEvent` | World saved to disk | No | - |
| `SpawnChangeEvent` | World spawn point changed | **Yes** | - |
| `ChunkLoadEvent` | Chunk loaded into memory | No | - |
| `ChunkPopulateEvent` | Chunk generated and populated | No | - |
| `TimeChangeEvent` | World time changed | **Yes** | - |

### Entities, Mobs & Vehicles

| Event | Description | Cancellable | Player Accessor |
|---|---|:---:|:---:|
| `VehicleCreateEvent` | Boat or minecart entity created | **Yes** | - |
| `VehicleDamageEvent` | Vehicle damaged by a player (damage modifiable) | **Yes** | `event.player()` |
| `VehicleDestroyEvent` | Vehicle destroyed | **Yes** | - |
| `VehicleEnterEvent` | Player enters a vehicle or mounts an entity | **Yes** | `event.player()` |
| `VehicleExitEvent` | Player exits a vehicle | **Yes** | `event.player()` |
| `SheepShearEvent` | Sheep sheared with shears | **Yes** | `event.player()` |
| `SheepRegrowWoolEvent` | Sheep regrows wool after eating grass | **Yes** | - |
| `FireworkExplodeEvent` | Firework rocket explodes at apex | **Yes** | - |

---

## 4. Convenient Snake_Case API Hooks

For rapid scripting and clear event binding, `Events` exposes:
- `on_player_swing_miss(e => ...)`
- `on_player_fly_toggle(e => ...)`
- `on_player_crawl(e => ...)`
- `on_player_attack(e => ...)`
- `on_player_fall(e => ...)`
- `on_player_world_change(e => ...)`
- `on_player_chunk_enter(e => ...)`
- `on_resource_pack_status(e => ...)`
- `on_player_afk(e => ...)`

