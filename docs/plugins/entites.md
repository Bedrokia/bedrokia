---
title: Entités personnalisées
---

# Entités personnalisées

Bedrokia embarque un moteur natif complet d'entités personnalisées (`Custom Entities`) basé sur le modèle de composants et le protocole officiel de Minecraft Bedrock (v1.26.x+).

Ce système permet de déclarer des créatures, monstres, boss et PNJs personnalisés directement en C# depuis vos plugins. Les définitions sont automatiquement injectées dans le protocole réseau lors de la phase de chargement du joueur (`AvailableActorIdentifiersPacket`, `SyncEntityPropertyPacket`, etc.).

---

## 1. Identifiant et namespace

Comme pour les items et les blocs, chaque entité possède un identifiant unique sous la forme `namespace:nom_de_l_entite`.

Le namespace est totalement personnalisable :
- `mon_serveur:golem_de_cristal`
- `rpg:dragon_infernal`
- `bedrokia:crystal_fox`

> **Note :** Si vous omettez le namespace (par exemple `CustomEntityBuilder.Create("goblin")`), Bedrokia applique automatiquement le namespace `custom:` (`custom:goblin`).

---

## 2. Exemple rapide dans un plugin

L'enregistrement des entités s'effectue dans la méthode `OnEnable()` de votre plugin grâce au `CustomEntityBuilder`.

```csharp
using Bedrokia.Server.Game.Custom.Entities;
using Bedrokia.Server.Plugins;

namespace MonPlugin;

public sealed class MonPlugin : Plugin
{
    public override void OnEnable()
    {
        // 1. Monstre hostile avec loot et attaque
        CustomEntityBuilder.CreateHostileMob("mon_serveur:golem_sombre", "Golem Sombre", health: 80, damage: 10f, speed: 0.22f)
            .WithScale(1.5f)
            .WithSpawnEgg(summonable: true, hasSpawnEgg: true)
            .Register();

        // 2. Boss avec barre de boss et changement de phase
        CustomEntityBuilder.CreateBoss("mon_serveur:seigneur_du_feu", "Seigneur du Feu", health: 300, damage: 15f, scale: 2.0f)
            .WithSpawnEgg(summonable: true, hasSpawnEgg: true)
            .WithIntProperty("custom:phase", min: 1, max: 2, defaultValue: 1, clientSync: true)
            .WithComponentGroup("phase2",
                new MovementEntityComponent(0.40f),
                new AttackEntityComponent(22f),
                new ScaleEntityComponent(2.5f))
            .OnHurt((entity, source) =>
            {
                if (entity.Health <= 150 && entity.GetProperty<int>("custom:phase") == 1)
                {
                    entity.SetProperty("custom:phase", 2);
                    entity.AddComponentGroup("phase2");
                }
            })
            .Register();

        // 3. PNJ interactif
        CustomEntityBuilder.CreateNpc("mon_serveur:marchand", "Marchand Ambulant")
            .WithInteract("Parler", "minecraft:emerald", (player, entity) =>
            {
                player.SendMessage("§a[Marchand] Bienvenue voyageur !");
            })
            .Register();
    }
}
```

---

## 3. Presets disponibles

Pour vous simplifier la création, `CustomEntityBuilder` fournit des méthodes de fabrique statiques prédéfinies :

- `CustomEntityBuilder.CreatePassiveMob(identifier, displayName, health = 10, speed = 0.25f)` (alias `CreateSimpleMob`) : configure automatiquement la flottaison, la panique en cas de coup et la déambulation aléatoire.
- `CustomEntityBuilder.CreateHostileMob(identifier, displayName, health = 20, damage = 3f, speed = 0.28f)` : configure la recherche de cible (`NearestAttackableTarget`) et l'attaque au corps à corps (`MeleeAttack`).
- `CustomEntityBuilder.CreateNpc(identifier, displayName)` : configure l'entité comme un PNJ persistant, sans gravité et regardant les joueurs à proximité.
- `CustomEntityBuilder.CreateBoss(identifier, bossBarTitle, health = 200, damage = 8f, scale = 1.5f)` : configure une barre de boss HUD synchronisée, des dégâts élevés et une taille imposante.

---

## 4. Spawning en jeu

Une entité enregistrée peut être invoquée de multiples manières :

### Via la commande serveur ou chat
```
/summon custom:golem_sombre
```

### Via son œuf d'apparition (Spawn Egg)
Si `.WithSpawnEgg(hasSpawnEgg: true)` est activé, un item d'œuf d'apparition `custom:golem_sombre_spawn_egg` est automatiquement généré et utilisable en faisant un clic droit sur un bloc.

### Directement via l'API C#
```csharp
// Spawning dans le monde
var instance = world.SpawnCustomEntity("mon_serveur:golem_sombre", x, y, z);

// Modification dynamique des propriétés
instance.SetProperty("custom:phase", 2);

// Ajout ou retrait d'un groupe de composants
instance.AddComponentGroup("phase2");
instance.RemoveComponentGroup("phase2");

// Interaction ou dégâts
instance.Damage(20, (x, z));
```

---

## 5. Propriétés et Component Groups

Bedrokia prend en charge les **Entity Properties** (`int`, `float`, `bool`, `enum`) synchronisées avec le client (`clientSync: true`). Ces propriétés permettent à vos Resource Packs de lire dynamiquement `q.property('custom:phase')` pour changer d'animation ou de texture.

Les **Component Groups** permettent quant à eux d'altérer dynamiquement les caractéristiques d'une entité au fil du combat (vitesse, taille, dégâts, vie maximale).
