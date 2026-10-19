---
title: Objets personnalisés
---

# Objets personnalisés

Bedrokia dispose d'un moteur natif d'objets personnalises inspire de l'architecture par composants NBT du protocole officiel Minecraft Bedrock.

Ce systeme permet d'enregistrer des objets uniques (armes, outils, armures, nourriture, objets a effets) directement depuis le code d'un plugin C# sans configuration complexe. Le serveur s'occupe automatiquement de generer la structure NBT requise et de synchroniser le catalogue des items avec les clients Bedrock lors de leur connexion (`ItemRegistryPacket`).

---

## 1. Identifiant et choix du namespace

Chaque objet est identifie par une chaine de caracteres sous la forme `namespace:nom_de_l_item`.

Le namespace est totalement libre :
- `mon_serveur:epee_legendaire`
- `pvp:marteau_de_guerre`
- `kitmap:stick_de_force`
- `bedrokia:ruby_sword`

Remarques :
- Si aucun namespace n'est precise (par exemple `CustomItemBuilder.Create("epee_rubis")`), Bedrokia applique automatiquement le namespace par defaut `custom:` (`custom:epee_rubis`).
- L'identifiant doit etre unique a l'echelle du serveur.

---

## 2. Exemple rapide dans un plugin

L'enregistrement des items s'effectue dans la methode `OnEnable()` de votre plugin via la classe fluide `CustomItemBuilder`.

```csharp
using Bedrokia.Server.Game.Custom.Items;
using Bedrokia.Server.Plugins;

namespace MonPlugin;

public sealed class MonPlugin : Plugin
{
    public override void OnEnable()
    {
        // Enregistrement d'une epee personnalisee
        CustomItemBuilder.Create("mon_serveur:epee_rubis")
            .WithDisplayName("§cEpee en Rubis")
            .WithIcon("ruby_sword")
            .WithDamage(9)
            .WithDurability(1561)
            .WithMaxStackSize(1)
            .WithHandEquipped(true)
            .Register();

        // Enregistrement d'une pomme magique consommable
        CustomItemBuilder.Create("mon_serveur:pomme_magique")
            .WithDisplayName("§6Pomme Magique")
            .WithIcon("golden_apple")
            .WithFood(nutrition: 8, saturationModifier: 1.2f, canAlwaysEat: true)
            .WithMaxStackSize(64)
            .WithFoil(true)
            .Register();
    }
}
```

---

## 3. Reference de l'API CustomItemBuilder

### Proprietes Generales

- `Create(string identifier)` : Initialise un nouveau builder avec l'identifiant specifie.
- `WithDisplayName(string displayName)` : Nom affiche au joueur. Prend en charge les codes de couleur Minecraft (`§a`, `§c`, `§l`, etc.).
- `WithIcon(string textureName)` : Identifiant court de la texture associee dans le fichier `item_texture.json` du pack de ressources.
- `WithMaxStackSize(int maxStackSize)` : Nombre maximal d'items cumulables par case dans l'inventaire (generalement 1 ou 64).
- `WithHandEquipped(bool handEquipped = true)` : Si active, l'item est tenu en main comme un outil/arme (modele 3D vertical) plutot qu'a plat comme un objet generique.
- `WithFoil(bool foil = true)` : Applique une brillance scintillante d'enchantement permanente sur l'item.
- `WithFireResistant(bool fireResistant = true)` : L'item devient insensible a la lave et au feu lorsqu'il est au sol (comportement identique a la netherite).
- `WithAllowOffHand(bool allow = true)` : Autorise le port de l'item dans la main secondaire.
- `WithCanDestroyInCreative(bool canDestroy = true)` : Indique si l'item peut casser des blocs en mode Creatif.

### Combat et Durabilite

- `WithDamage(int damage)` : Quantite de points de degats infliges par coup lorsque l'item est utilise comme arme.
- `WithDurability(int maxDurability, int minDamageChance = 1, int maxDamageChance = 1)` : Definit la barre de durabilite de l'objet et la probabilite de perte de durabilite par usage.

### Outils et Minage

- `WithDigger(IReadOnlyList<DestroySpeed> speeds, bool useEfficiency = true)` : Configure l'item pour accelerer la destruction de blocs ou de categories de blocs specifiques.

Exemple :
```csharp
CustomItemBuilder.Create("mon_serveur:super_pioche")
    .WithDisplayName("Super Pioche")
    .WithIcon("super_pickaxe")
    .WithDurability(2000)
    .WithDigger([
        new DestroySpeed("minecraft:stone", speed: 12),
        new DestroySpeed("minecraft:is_pickaxe_item_destructible", speed: 10)
    ])
    .Register();
```

### Armures et Equipements

- `WithArmor(int protection, string textureType = "iron")` : Valeur brute de points de defense apportee par l'armure.
- `WithWearable(string slot, int protection = 0)` : Emplacement d'armure equipe.
  - `"slot.armor.head"` : Casque
  - `"slot.armor.chest"` : Plastron
  - `"slot.armor.legs"` : Jambieres
  - `"slot.armor.feet"` : Bottes

### Nourriture et Consommables

- `WithFood(int nutrition, float saturationModifier, bool canAlwaysEat = false, string usingConvertsTo = "minecraft:air")` : Transforme l'item en aliment consommable avec une duree de consommation et une animation automatique.
  - `nutrition` : Nombre de demi-gigots restaures.
  - `saturationModifier` : Multiplicateur de saturation.
  - `canAlwaysEat` : Si defini sur `true`, le joueur peut consommer l'item meme lorsque sa barre de faim est pleine (comme pour les pommes d'or).
  - `usingConvertsTo` : Item laisse dans l'inventaire apres consommation (par exemple un bol ou une fiole).

### Cooldowns et Utilisation

- `WithCooldown(string category, float durationSeconds)` : Declenche un delai d'attente sur tous les items de cette categorie apres utilisation.
- `WithFuel(float durationSeconds)` : Permet d'utiliser l'objet comme combustible dans un fourneau en specifiant le temps de cuisson en secondes.
- `OnUse(Action<ItemUseEvent> handler)` : Permet d'executer une action C# personnalisee des que le joueur effectue un clic droit / utilise l'objet.

### Validation et Enregistrement

- `Build()` : Construit l'instance `CustomItem` sans l'enregistrer dans le serveur.
- `Register()` : Construit et enregistre directement l'item dans `CustomItemRegistry`.

---

## 4. Methodes raccourcies (Factories predefinies)

Des methodes statiques d'assistance permettent de construire rapidement des objets standards sans devoir configurer chaque composant :

```csharp
// Arme standard
CustomItemBuilder.CreateWeapon(
    identifier: "mon_serveur:dague",
    displayName: "Dague Sombre",
    texture: "dark_dagger",
    damage: 7,
    durability: 850
).Register();

// Outil standard
CustomItemBuilder.CreateTool(
    identifier: "mon_serveur:pioche_rubis",
    displayName: "Pioche en Rubis",
    texture: "ruby_pickaxe",
    speedMultiplier: 8,
    durability: 1561
).Register();

// Piece d'armure
CustomItemBuilder.CreateArmor(
    identifier: "mon_serveur:plastron_rubis",
    displayName: "Plastron en Rubis",
    texture: "ruby_chestplate",
    slot: "slot.armor.chest",
    protection: 8,
    durability: 528
).Register();

// Nourriture
CustomItemBuilder.CreateFood(
    identifier: "mon_serveur:pain_epice",
    displayName: "Pain d'Epice",
    texture: "gingerbread",
    nutrition: 5,
    saturation: 0.8f,
    canAlwaysEat: false
).Register();
```

---

## 5. Configuration cote Resource Pack

Pour que le client Minecraft Bedrock affiche la texture souhaitee pour votre item, vous devez lui associer une texture via un Resource Pack Bedrock.

### Structure des dossiers du pack
```text
mon_pack/
├── manifest.json
├── textures/
│   ├── items/
│   │   ├── ruby_sword.png
│   │   └── ruby_apple.png
│   └── item_texture.json
```

### Configuration de textures/item_texture.json
```json
{
  "resource_pack_name": "mon_serveur_pack",
  "texture_name": "atlas.items",
  "texture_data": {
    "ruby_sword": {
      "textures": "textures/items/ruby_sword"
    },
    "ruby_apple": {
      "textures": "textures/items/ruby_apple"
    }
  }
}
```

La valeur renseignee dans `.WithIcon("ruby_sword")` correspond directement a la cle declaree dans `texture_data`.

---

## 6. Exemple complet de plugin

Un exemple complet et fonctionnel sans commande superflue est disponible dans les sources de Bedrokia :
- Repertoire : `examples/plugins/CustomContent/`
- Fichier source : `examples/plugins/CustomContent/src/CustomContentPlugin.cs`
- Configuration : `examples/plugins/CustomContent/plugin.yml`
