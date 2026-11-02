---
title: Système d'Événements
---

# Système d'Événements

Bedrokia dispose d'un système d'événements C# performant, typé et modulaire, permettant aux plugins d'intercepter, modifier ou annuler les actions des joueurs, du monde et du serveur.

---

## 1. Principes d'Architecture

- **Uniformisation Joueur :** Absolument tous les événements impliquant un joueur implémentent `IPlayerEvent` et exposent `event.Player` ainsi que la méthode uniforme `event.player()`.
- **Événements Annulables :** Les événements dérivés de `CancellableEvent` peuvent être annulés via `e.Cancel()` ou `e.Cancelled = true`.
- **Fast-path & Performance :** Le serveur vérifie l'existence d'abonnés via des drapeaux `Has*` optimisés avant toute allocation d'instance.
- **Hooks d'API Dédiés :** En plus des événements standards C#, `ServerEvents` expose des méthodes utilitaires en snake_case (ex: `on_player_attack`, `on_player_fall`, etc.).

---

## 2. Exemple d'Utilisation dans un Plugin

```csharp
using Bedrokia.Server.Core;
using Bedrokia.Server.Events;
using Bedrokia.Server.Plugins;

namespace MonServeur;

public sealed class MonPlugin : Plugin
{
    public override void OnEnable()
    {
        // 1. Annulation d'un événement
        Events.PlayerDamage += e =>
        {
            if (e.player().GameMode == GameMode.Creative)
                e.Cancel();
        };

        // 2. Modification de propriétés
        Events.PlayerAttack += e =>
        {
            e.Damage *= 1.5f; // Bonus de 50% de dégâts
        };

        // 3. Utilisation des hooks snake_case
        Events.on_player_afk(e =>
        {
            var p = e.player();
            p.SendMessage(e.IsAfk ? "§7Vous êtes désormais AFK." : "§aBon retour en jeu !");
        });
    }
}
```

---

## 3. Liste Complète des Événements Disponibles

### Serveur, Réseau & Plugins

| Événement | Description | Annulable | Accès Joueur |
|---|---|:---:|:---:|
| `ServerStartedEvent` | Le serveur a démarré et est prêt à accepter les connexions | Non | - |
| `ServerStopEvent` | Le serveur s'arrête | Non | - |
| `PluginEnableEvent` | Un plugin vient d'être activé | Non | - |
| `PluginDisableEvent` | Un plugin vient d'être désactivé | Non | - |
| `ConsoleCommandEvent` | Commande saisie dans la console (modifiable via `CommandLine`) | **Oui** | - |
| `DataPacketReceiveEvent` | Paquet Bedrock entrant reçu | **Oui** | `event.player()` |
| `DataPacketSendEvent` | Paquet Bedrock sortant sur le point d'être envoyé | **Oui** | `event.player()` |

### Joueur & Spécificités Bedrock

| Événement | Description | Annulable | Accès Joueur |
|---|---|:---:|:---:|
| `PlayerPreLoginEvent` | Connexion initiale avant authentification complète | **Oui** | `event.player()` |
| `PlayerLoginEvent` | Authentification validée | Non | `event.player()` |
| `PlayerJoinEvent` | Le joueur entre dans le monde | Non | `event.player()` |
| `PlayerQuitEvent` | Déconnexion du joueur | Non | `event.player()` |
| `PlayerKickEvent` | Expulsion du joueur (raison modifiable) | **Oui** | `event.player()` |
| `PlayerChatEvent` | Envoi d'un message dans le chat | **Oui** | `event.player()` |
| `PlayerCommandEvent` | Exécution d'une commande par un joueur | **Oui** | `event.player()` |
| `PlayerMoveEvent` | Déplacement (destination `To` modifiable) | **Oui** | `event.player()` |
| `PlayerWarpEvent` | Téléportation / changement de position immédiat | **Oui** | `event.player()` |
| `PlayerAttackEvent` | Attaque sur une entité (dégâts modifiables) | **Oui** | `event.player()` |
| `PlayerDamageEvent` | Le joueur subit des dégâts | **Oui** | `event.player()` |
| `PlayerDeathEvent` | Mort du joueur (drops et message modifiables) | Non | `event.player()` |
| `PlayerRespawnEvent` | Réapparition du joueur | Non | `event.player()` |
| `PlayerFallEvent` | Chute avec dégâts de chute (distance et dégâts modifiables) | **Oui** | `event.player()` |
| `PlayerToggleFlightEvent` | Activation ou désactivation du vol | **Oui** | `event.player()` |
| `PlayerToggleCrawlEvent` | Activation ou désactivation du ramper (crawl) | **Oui** | `event.player()` |
| `PlayerToggleActionEvent` | Bascule d'action (Sneak, Sprint, Swim, Glide, Jump) | **Oui** | `event.player()` |
| `PlayerBedEnterEvent` | Entrée dans un lit | **Oui** | `event.player()` |
| `PlayerBedLeaveEvent` | Sortie d'un lit | Non | `event.player()` |
| `PlayerSleepSkipNightEvent` | Nuit passée grâce aux joueurs endormis | Non | `event.player()` |
| `PlayerAfkEvent` | Changement de statut AFK (inactivité/reprise) | **Oui** | `event.player()` |
| `PlayerMissSwingEvent` | Clic gauche dans le vide | **Oui** | `event.player()` |
| `ResourcePackClientResponseEvent` | Progression du chargement des packs du client | Non | `event.player()` |
| `PlayerViewDistanceChangeEvent` | Distance d'affichage / rayon de chunks (modifiable) | Non | `event.player()` |
| `PlayerGameModeChangeEvent` | Changement de mode de jeu (`NewGameMode` modifiable) | **Oui** | `event.player()` |
| `PlayerChangeSkinEvent` | Changement de skin par le client | **Oui** | `event.player()` |
| `PlayerEmoteEvent` | Animation d'emote déclenchée | **Oui** | `event.player()` |
| `PlayerTransferEvent` | Transfert vers un autre serveur Bedrock (IP/Port modifiables) | **Oui** | `event.player()` |
| `PlayerFormResponseEvent` | Réponse à une interface UI Modal Form | **Oui** | `event.player()` |
| `PlayerInputModeChangeEvent` | Changement de périphérique d'entrée (tactile, souris, manette) | Non | `event.player()` |
| `PlayerExhaustEvent` | Épuisement de faim (`Amount` modifiable) | **Oui** | `event.player()` |
| `PlayerArmorChangeEvent` | Équipement d'armure changé (slot et item modifiables) | **Oui** | `event.player()` |
| `PlayerBucketFillEvent` | Remplissage d'un seau (eau, lave) | **Oui** | `event.player()` |
| `PlayerBucketEmptyEvent` | Vidage d'un seau de liquide dans le monde | **Oui** | `event.player()` |
| `PlayerDimensionChangeEvent` | Changement de dimension (Overworld, Nether, End) | **Oui** | `event.player()` |
| `PlayerWorldChangeEvent` | Changement de monde | Non | `event.player()` |
| `PlayerChunkEnterEvent` | Entrée dans un nouveau chunk | Non | `event.player()` |
| `PlayerEffectChangeEvent` | Ajout, prolongation ou retrait d'effet de potion | **Oui** | `event.player()` |

### Blocs, Inventaires & Monde

| Événement | Description | Annulable | Accès Joueur |
|---|---|:---:|:---:|
| `BlockEvent` | Pose ou cassure de bloc (`IsPlace` / `IsBreak`) | **Oui** | `event.player()` (si joueur) |
| `BlockUseEvent` | Interaction clic droit avec un bloc | **Oui** | `event.player()` |
| `StonecutterUseEvent` | Craft au tailleur de pierre (résultat modifiable) | **Oui** | `event.player()` |
| `LeavesDecayEvent` | Disparition naturelle des feuilles | **Oui** | - |
| `MoistureChangeEvent` | Changement d'humidité de la terre labourée | **Oui** | - |
| `WorldLoadEvent` | Chargement d'un monde | Non | - |
| `WorldSaveEvent` | Sauvegarde d'un monde | Non | - |
| `SpawnChangeEvent` | Modification du point de spawn mondial | **Oui** | - |
| `ChunkLoadEvent` | Chargement d'un chunk en mémoire | Non | - |
| `ChunkPopulateEvent` | Génération et peuplement initial d'un chunk | Non | - |
| `TimeChangeEvent` | Modification de l'heure du monde | **Oui** | - |

### Entités, Créatures & Véhicules

| Événement | Description | Annulable | Accès Joueur |
|---|---|:---:|:---:|
| `VehicleCreateEvent` | Création et apparition d'un bateau ou wagonnet | **Oui** | - |
| `VehicleDamageEvent` | Dégâts infligés à un véhicule (dégâts modifiables) | **Oui** | `event.player()` |
| `VehicleDestroyEvent` | Destruction d'un véhicule | **Oui** | - |
| `VehicleEnterEvent` | Montée d'un joueur dans un véhicule ou une monture | **Oui** | `event.player()` |
| `VehicleExitEvent` | Descente d'un joueur d'un véhicule | **Oui** | `event.player()` |
| `SheepShearEvent` | Tonte d'un mouton à la cisaille | **Oui** | `event.player()` |
| `SheepRegrowWoolEvent` | Repousse de la laine d'un mouton broutant | **Oui** | - |
| `FireworkExplodeEvent` | Explosion d'un feu d'artifice | **Oui** | - |

---

## 4. Méthodes d'API Utilitaires (Snake_Case)

Pour les scripts et une syntaxe rapide, `Events` expose directement :
- `on_player_swing_miss(e => ...)`
- `on_player_fly_toggle(e => ...)`
- `on_player_crawl(e => ...)`
- `on_player_attack(e => ...)`
- `on_player_fall(e => ...)`
- `on_player_world_change(e => ...)`
- `on_player_chunk_enter(e => ...)`
- `on_resource_pack_status(e => ...)`
- `on_player_afk(e => ...)`

