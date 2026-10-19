---
title: Créer un plugin
---

# Créer un plugin

Les plugins Bedrokia s'écrivent en **C#** et s'organisent comme les plugins Java (Bukkit / Spigot).
**Pas besoin de compiler** : le serveur compile le code au démarrage (avec Roslyn). Un plugin cassé n'empêche pas le
serveur de démarrer : la console dit lequel, pourquoi, et où.

## Structure

```
plugins/
└── MonPlugin/
    ├── plugin.yml              description du plugin (obligatoire)
    ├── src/                    le code C# (tous les .cs, sous-dossiers compris)
    │   ├── MonPlugin.cs
    │   └── Commands/...
    └── resources/
        └── config.yml          config par défaut, copiée dans plugin_data/MonPlugin/ au 1er lancement
```

Formats acceptés aussi : un fichier seul `plugins/MonPlugin.cs`, ou une DLL `plugins/MonPlugin.dll`
(bibliothèque .NET 10 qui référence `Bedrokia.Server`).

## plugin.yml

```yaml
name: MonPlugin              # obligatoire : lettres, chiffres, _ - .
version: 1.0.0
main: MonPlugin.Main         # obligatoire : nom complet de la classe qui hérite de Plugin
api: 1.0.0
author: Moi
description: Ce que fait le plugin.
website: https://exemple.fr
depend: [AutrePlugin]        # chargés avant ; s'il manque, ce plugin n'est pas chargé
softdepend: [Optionnel]      # chargés avant s'ils sont présents
commands:
  bonjour:
    description: Dit bonjour.
    usage: "[joueur]"
    permission: everyone     # everyone, op, ou un nœud granulaire (ex: monplugin.command.vip)
    aliases: [salut, hello]
```

## La classe principale

```csharp
namespace MonPlugin;

public sealed class Main : Plugin
{
    public override void OnEnable()
    {
        Log("Démarré !");
        Events.PlayerJoin += e => e.Player.SendMessage("§aBienvenue " + e.Player.Name);
    }

    public override void OnDisable()
    {
        // sauvegarder ses données ici
    }

    // Appelé pour les commandes déclarées dans plugin.yml. false = affiche l'usage.
    public override bool OnCommand(CommandSender sender, string command, string[] args)
    {
        if (command == "bonjour")
        {
            if (!sender.HasPermission("monplugin.command.bonjour"))
            {
                sender.Reply("§cVous n'avez pas la permission.");
                return true;
            }
            sender.Reply(Config.GetString("message", "Bonjour !"));
            return true;
        }
        return false;
    }
}
```

Ces `using` sont automatiques : `System`, `System.Collections.Generic`, `System.IO`, `System.Linq`,
`System.Threading.Tasks`, `Bedrokia.Server`, `Bedrokia.Server.Commands`, `Bedrokia.Server.Game`,
`Bedrokia.Server.Network`, `Bedrokia.Server.Permissions`, `Bedrokia.Server.Plugins`.

Tout le code des plugins s'exécute sur le **thread du jeu** (20 ticks par seconde) : pas besoin de verrous, mais il ne
faut pas bloquer (pas de `Thread.Sleep`, pas de requête réseau synchrone).

## Ce que fournit `Plugin`

| Membre | Rôle |
|---|---|
| `Name`, `Version`, `Info` | infos de `plugin.yml` (`Info.Author`, `Info.Commands`...) |
| `Server` | le serveur : joueurs, monde, commandes |
| `Events` | les événements (voir plus bas) |
| `Config` | `config.yml` du dossier de données |
| `DataFolder` | `plugin_data/<Name>/` pour ses fichiers |
| `SaveResource(nom, remplacer)` | copie un fichier de `resources/` vers le dossier de données |
| `ReloadConfig()` | relit `config.yml` |
| `Log`, `LogWarning`, `LogError` | écrit dans la console, préfixé du nom du plugin |
| `RegisterCommand(nom, description, usage, opSeulement, action)` | ajoute une commande depuis le code |
| `RegisterCommand(nom, description, usage, permission, action)` | commande avec un nœud de permission (`""` pour tous, `"op"`, ou `"monplugin.fly"`) |
| `UnregisterCommand(nom)` | retire une commande, par exemple une commande du serveur que le plugin remplace |
| `RunNextTick(action)` / `ScheduleDelayed(ticks, action)` / `ScheduleRepeating(délai, période, action)` | tâches planifiées (20 ticks = 1 s), annulables avec `.Cancel()`, arrêtées quand le plugin s'arrête |

## Config

`resources/config.yml` est copié dans `plugin_data/MonPlugin/config.yml` au premier lancement ; ensuite, le
propriétaire du serveur le modifie librement. Lecture par chemin à points :

```csharp
var titre = Config.GetString("sidebar.title", "Mon serveur");
var delai = Config.GetInt("staff.cooldown", 20);
var actif = Config.GetBool("sidebar.enabled", true);
var lignes = Config.GetList("sidebar.lines");   // List<string>
Config.Set("stats.joins", 42);
Config.Save();
```

## Événements

S'abonner avec `+=` dans `OnEnable`. Les événements *annulables* ont `Cancelled` : à `true`, le serveur annule
l'action (le bloc revient, le message n'est pas envoyé...).

| Événement | Argument | Annulable |
|---|---|---|
| `PlayerLogin` | `PlayerLoginEvent` : `Player`, `KickReason` (modifiable) | oui (le joueur est expulsé) |
| `PlayerJoin` | `PlayerJoinEvent` : `Player`, `BroadcastMessage` (modifiable, `null` = pas de message) | non |
| `PlayerQuit` | `PlayerQuitEvent` : `Player`, `BroadcastMessage` (modifiable) | non |
| `PlayerMove` | `PlayerMoveEvent` : `Player`, `From`, `To`, `Yaw`, `Pitch`, `IsBlockChange` | oui (renvoyé en arrière) |
| `PlayerCommand` | `PlayerCommandEvent` : `Player`, `CommandLine` (modifiable) | oui |
| `PlayerToggleAction` | `PlayerToggleActionEvent` : `Player`, `Action` (sneak, sprint...), `NewState` | oui |
| `PlayerHeal` | `PlayerHealEvent` : `Player`, `Amount` (modifiable), `Cause` | oui |
| `PlayerHungerChange` | `PlayerHungerChangeEvent` : `Player`, `OldFood`, `NewFood` et `NewSaturation` (modifiables) | oui |
| `PlayerEffectChange` | `PlayerEffectChangeEvent` : `Player`, `OldEffect`, `NewEffect`, `Cause` | oui |
| `PlayerConsumeItem` | `PlayerConsumeItemEvent` : `Player`, `Item` (nourriture, potion, lait) | oui |
| `PlayerPickupItem` | `PlayerPickupItemEvent` : `Player`, `Item` | oui |
| `PlayerHeldSlotChange` | `PlayerHeldSlotChangeEvent` : `Player`, `OldSlot`, `NewSlot` | oui |
| `PlayerInteractBlock` | `PlayerInteractBlockEvent` : `Player`, `Position`, `Block`, `Face` | oui |
| `PlayerInteractEntity` | `PlayerInteractEntityEvent` : `Player`, `EntityId`, `Mob` | oui |
| `ContainerOpen` / `ContainerClose` | `ContainerOpenEvent` : `Player`, `Position`, `ContainerKind`, `Items` ; `ContainerCloseEvent` : `Player` | oui / non |
| `PlayerCraftItem` | `PlayerCraftItemEvent` : `Player`, `Result` (modifiable), `Ingredients` | oui |
| `PlayerEnchantItem` | `PlayerEnchantItemEvent` : `Player`, `Item`, `LevelCost` et `LapisCost` (modifiables) | oui |
| `PlayerChat` | `PlayerChatEvent` : `Player`, `Message` (modifiable) | oui |
| `PlayerDamage` | `PlayerDamageEvent` : `Player`, `Damage` (modifiable), `Source`, `ApplyKnockback` (modifiable) | oui |
| `PlayerDeath` | `PlayerDeathEvent` : `Player`, `Source`, `DeathMessage` (modifiable), `KeepInventory` (modifiable), `Drops` (modifiable) | non |
| `PlayerRespawn` | `PlayerRespawnEvent` : `Player`, `RespawnPosition` (modifiable) | non |
| `PlayerDropItem` | `PlayerDropItemEvent` : `Player`, `Item` | oui |
| `InventoryTransaction` | `InventoryTransactionEvent` : `Player`, `Type`, `Item`, `Count`, `Slot` | oui |
| `InventoryClick` | alias de `InventoryTransaction` | oui |
| `BlockBreak` | `BlockEvent` : `Player`, `X`, `Y`, `Z`, `Block` | oui |
| `BlockPlace` | `BlockEvent` | oui |
| `ItemUse` | `ItemUseEvent` : `Player`, `Item`, position et `Block` cliqués, `Face` | oui |
| `EntityDamage` | `EntityDamageEvent` : `Entity`, `Damage` (modifiable), `Source` | oui |
| `EntitySpawn` | `EntitySpawnEvent` : `Kind`, `X`, `Y`, `Z` (modifiables) | oui |
| `EntityDespawn` | `EntityDespawnEvent` : `Entity`, `Reason` | non |
| `EntityDeath` | `EntityDeathEvent` : `Entity`, `Killer`, `Drops`, `DroppedXp` (modifiables) | non |
| `EntityTarget` | `EntityTargetEvent` : `Entity`, `Target` (modifiable) | oui |
| `Explosion` | `ExplosionEvent` : `Position`, `Radius`, `AffectedBlocks` (modifiables) | oui |
| `BlockBurn` / `BlockSpread` / `BlockGrow` | le feu brûle un bloc ; un bloc s'étend (melon, citrouille) ; une plante pousse (`NewBlock` modifiable) | oui |
| `FluidFlow` | `FluidFlowEvent` : `From`, `To`, `FluidBlock` (modifiable) | oui |
| `FurnaceSmelt` / `FurnaceBurn` | un four cuit (`Result` modifiable) ; un four brûle un combustible (`BurnDuration` modifiable) | oui |
| `Tick` | numéro du tick (20 par seconde) | non |

```csharp
Events.BlockBreak += e =>
{
    if (e.Block.Name == "minecraft:diamond_ore" && e.Player.GameMode == GameMode.Creative)
        e.Cancelled = true;
};
```

Si un gestionnaire plante, l'erreur complète s'affiche **une fois** avec le nom du plugin et de l'événement ; les
autres plugins et le serveur continuent.

## Joueurs (`BedrockConnection`)

| Membre | Rôle |
|---|---|
| `Name`, `Position` (x, y des yeux, z), `Yaw`, `Pitch`, `GameMode`, `IsOp` | état |
| `SendMessage(texte)` | message dans le chat (codes couleur `§`) |
| `SendTitle(titre, sousTitre, fadeIn, stay, fadeOut)` | grand titre à l'écran |
| `SendActionBar(texte)` | texte au-dessus de la barre d'objets |
| `SetSidebar(titre, lignes)` / `ClearSidebar()` | scoreboard sur le côté (15 lignes max) |
| `AddEffect(effet, niveau, secondes, particules)` / `RemoveEffect(effet)` | effets (`BedrockConnection.Effects.Speed`, `JumpBoost`, `NightVision`...) |
| `SetSpeed(vitesse)` | vitesse de marche (vanilla 0.1) |
| `AddEffect(new EffectInstance(EffectType.Poison, niveau, ticks))` / `RemoveEffect(EffectType)` / `ClearEffects()` / `GetActiveEffects()` | effets gérés par le serveur (régénération, poison, wither, force, résistance...) ; potions et lait aussi |
| `SetHealth(vie)` / `SetFood(faim, saturation)` | soigner, nourrir (20 = plein) |
| `CanFly`, `God`, `Frozen`, `Vanished` | vol en survie, invincible, immobile, invisible pour les autres joueurs |
| `ClearInventory(armure)` / `SyncInventory()` | vider l'inventaire ; renvoyer l'inventaire après avoir changé des cases |
| `ChangeDimension(monde, x, y, z)` | envoyer dans un autre monde (`Server.World`, `Server.Nether`) |
| `SetMotion(vx, vy, vz)` | pousse le joueur |
| `GiveItem(stack)` | donne un objet (le surplus tombe au sol) |
| `HeldItem`, `ArmourItem(0..3)` | objet en main, armure portée |
| `Teleport(x, y, z)`, `Disconnect(raison)` | téléporte, expulse |

## Serveur et monde

| Membre | Rôle |
|---|---|
| `Server.Players` | joueurs connectés (filtrer avec `IsInGame`) |
| `Server.BroadcastMessage(texte)` | message à tous |
| `Server.World.GetBlock(x, y, z)` | bloc à une position |
| `Server.SetBlockAndUpdate(x, y, z, bloc)` | pose un bloc et prévient les joueurs |
| `Server.SpawnParticle(nom, x, y, z)` | particules (`minecraft:heart_particle`, `minecraft:villager_happy`, `minecraft:basic_flame_particle`...) |
| `Server.StrikeLightning(x, y, z)` | éclair |
| `Server.Explode(x, y, z, rayon)` | explosion |
| `Server.DropItem(stack, x, y, z)` | objet au sol |
| `Server.Schedule(ticks, action)` | lance une action plus tard |
| `Server.Plugins.Get(nom)` | un autre plugin |
| `Server.Config` | réglages de `server.properties` |

## Objets personnalisés

Sur une base vanilla, avec nom, description et brillance :

```csharp
var epee = Enchanting.With(ItemStack.Of("diamond_sword")!, [(9, 5)])     // 9 = tranchant
    .WithName("§c§lÉpée du dragon", "§7Forgée dans le Nether");
player.GiveItem(epee);

if (player.HeldItem?.CustomName == "§c§lÉpée du dragon") { /* pouvoir spécial */ }
```

Les nouveaux blocs et objets personnalisés sont supportés via l'API Custom Blocks & Items (`CustomBlockBuilder`, `CustomItemBuilder`). Voir l'exemple `CustomContent`.

## Console au démarrage

```
[INFO] Loading 3 plugin(s) from plugins/...
[ERROR] Plugin 'Cassé' could not be loaded: 1 compile error(s):
    plugins\Cassé\src\Main.cs(12,9): error CS0103: The name 'joueur' does not exist in the current context
[INFO]   [OK]     CustomContent v1.0.0 by Bedrokia (folder: CustomContent) - Blocs et items personnalises.
[INFO]   [OK]     Listeners v1.0.0 by Bedrokia (folder: Listeners) - Gestion des événements avec listeners séparés.
[INFO]   [OK]     Ping v1.0.0 by Bedrokia (folder: Ping) - Commande /ping.
[ERROR]   [FAILED] Cassé (see the error above)
[WARN] Plugins: 2 loaded, 1 failed (3.4 s).
```

## Développer avec un IDE

Pour l'autocomplétion, ajouter un `.csproj` à côté de `plugin.yml` qui référence `src/Bedrokia.Server` (voir
[examples/plugins](https://github.com/Nexiumpvp2/bedrokia/tree/main/examples/plugins)). Le serveur l'ignore : il
compile `src/` lui-même.

## Exemples

- **CustomContent** : enregistrement complet de blocs et d'items personnalisés (épée en rubis, pioche, armure, pomme consommable, minerais et lampes) avec leurs composants Bedrock.
- **Listeners** : gestion modulaire des événements (connexion d'un joueur, casse de blocs) avec un `Main` et des listeners séparés dans différents fichiers.
- **Ping** : commande `/ping` (et alias `/pong`) avec gestion des arguments, configuration `config.yml` et action bar.
- **MagicItems** : bâton de foudre (clic droit = éclair), bottes de vitesse (Vitesse + Saut), tremplin (bloc qui
  propulse). Commande `/magie`.
- **Showcase** : titre de bienvenue, scoreboard en direct, spirale de flammes `/fx`, annonces `/annonce`. Tout est
  réglable dans son `config.yml`.
