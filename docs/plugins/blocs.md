---
title: Blocs personnalisés
---

# Blocs personnalisés

Bedrokia embarque un moteur natif de blocs personnalises base sur le systeme de composants NBT officiel de Minecraft Bedrock (v1.26.x+).

Ce systeme permet de declarer des blocs uniques avec textures personnalisees, proprietes physiques, emissions de lumiere, boites de collision et permutations d'etat directement en C# depuis vos plugins. Les definitions sont automatiquement injectees dans le protocole reseau lors de la phase de chargement du joueur (`StartGamePacket.CustomBlocks`).

---

## 1. Identifiant et namespace

Comme pour les items, chaque bloc possede un identifiant unique sous la forme `namespace:nom_du_bloc`.

Le namespace est totalement personnalisable :
- `mon_serveur:minerai_rubis`
- `pvp:bloc_obsidienne_renforce`
- `kitmap:spawner_personnalise`
- `bedrokia:ruby_block`

Remarques :
- Si vous omettez le namespace (par exemple `CustomBlockBuilder.Create("bloc_rubis")`), Bedrokia applique automatiquement le namespace `custom:` (`custom:bloc_rubis`).
- L'identifiant doit etre unique a l'echelle du serveur.

---

## 2. Exemple rapide dans un plugin

L'enregistrement des blocs s'effectue dans la methode `OnEnable()` de votre plugin grace au `CustomBlockBuilder`.

```csharp
using Bedrokia.Server.Game.Custom.Blocks;
using Bedrokia.Server.Plugins;

namespace MonPlugin;

public sealed class MonPlugin : Plugin
{
    public override void OnEnable()
    {
        // Bloc solide simple avec friction et durete
        CustomBlockBuilder.Create("mon_serveur:bloc_rubis")
            .WithDisplayName("§cBloc de Rubis")
            .WithTexture("ruby_block")
            .WithHardness(2.0f)
            .WithBlastResistance(9.0f)
            .WithFriction(0.6f)
            .Register();

        // Lampe eclairante
        CustomBlockBuilder.Create("mon_serveur:lampe_rubis")
            .WithDisplayName("§6Lampe de Rubis")
            .WithTexture("ruby_lamp")
            .WithLightEmission(15)
            .WithHardness(0.5f)
            .Register();
    }
}
```

---

## 3. Reference de l'API CustomBlockBuilder

### Affichage et Textures

- `Create(string identifier)` : Initialise un nouveau builder avec l'identifiant specifie.
- `WithDisplayName(string name)` : Nom affiche au joueur (supporte les codes de mise en forme `§a`, `§c`, etc.).
- `WithTexture(string texture, string renderMethod = "opaque")` : Associe une texture pour toutes les faces du bloc.
  - `renderMethod` : `"opaque"` (bloc plein standard), `"alpha_test"` (transparence decoupee comme les feuilles ou le verre), `"blend"` (transparence douce avec canal alpha).
- `WithMaterials(params BlockMaterial[] materials)` : Permet d'appliquer des textures distinctes selon les faces (`"up"`, `"down"`, `"north"`, `"south"`, `"east"`, `"west"`, ou `"*"` pour toutes).
- `WithGeometry(string geometryIdentifier)` : Modele 3D geometrique a appliquer (par defaut `"minecraft:geometry.full_block"`).
- `WithTransformation(rx, ry, rz, sx, sy, sz)` : Applique une rotation ou une echelle sur le modele visuel du bloc.

### Proprietes Physiques et Resistance

- `WithHardness(float secondsToDestroy)` : Temps de base requis pour casser le bloc (en secondes).
- `WithBlastResistance(float resistance)` : Resistance aux explosions (TNT, creepers).
- `WithFriction(float friction)` : Adherence du bloc pour les entites qui marchent dessus (`0.6f` par defaut, `0.98f` pour un bloc glissant style glace).
- `WithBreathability(bool solid)` : Indique si le bloc est plein (`true`) ou s'il permet de respirer a l'interieur (`false` comme l'air ou l'eau).
- `WithReplaceable()` : Autorise le remplacement direct du bloc par un autre sans devoir le casser au prealable (style hautes herbes ou fleurs).
- `WithFlammable(int catchOdds = 5, int burnOdds = 20)` : Rend le bloc inflammable et regle sa vitesse de combustion.

### Lumiere

- `WithLightEmission(byte lightLevel)` : Niveau de lumiere emis par le bloc (de 0 a 15).
- `WithLightDampening(byte lightFilter)` : Quantite de lumiere absorbee par le bloc (0 pour laisser passer toute la lumiere comme le verre, 15 pour bloquer totalement la lumiere).

### Boites de Collision et de Selection

- `WithCollision(bool enabled = true, float[]? origin = null, float[]? size = null)` : Active ou desactive la boite de collision physique qui empeche les entites de traverser le bloc.
  - `origin` : Coordonnees minimales relatives du cube `[x, y, z]` (ex: `[-8.0f, 0.0f, -8.0f]`).
  - `size` : Dimensions du cube `[width, height, depth]` (ex: `[16.0f, 16.0f, 16.0f]` pour un bloc complet).
- `WithSelectionBox(bool enabled = true, float[]? origin = null, float[]? size = null)` : Boite de contour visee par le joueur avec son curseur.

### Redstone et Tables d'Artisanat

- `WithCraftingTable(params string[] craftingTags)` : Transforme le bloc en table de craft avec les tags d'artisanat autorises (`"crafting_table"`).
- `WithRedstoneConductivity(bool redstoneConductor = true)` : Indique si le bloc conduit le signal de redstone comme un bloc plein opaque.
- `WithRedstoneProducer(int power = 15)` : Transforme le bloc en source d'alimentation redstone emettant un signal d'intensite donnee (de 1 a 15).

### Etats, Proprietes et Permutations

Un meme bloc peut posseder plusieurs variantes d'etats visuels ou physiques selon ses proprietes :

- `WithProperty(string name, params object[] values)` : Declare une propriete d'etat avec ses valeurs admises (booleens, entiers ou chaines).
- `WithPermutation(string condition, params IBlockComponent[] components)` : Modifie dynamiquement les composants lorsque la condition MoLang est remplie.

Exemple :
```csharp
CustomBlockBuilder.Create("mon_serveur:interrupteur_magique")
    .WithDisplayName("Interrupteur Magique")
    .WithProperty("mon_serveur:actif", false, true)
    .WithPermutation("q.block_property('mon_serveur:actif') == true",
        new LightEmissionBlockComponent(15),
        new MaterialInstancesBlockComponent([new BlockMaterial("interrupteur_on")])
    )
    .WithPermutation("q.block_property('mon_serveur:actif') == false",
        new LightEmissionBlockComponent(0),
        new MaterialInstancesBlockComponent([new BlockMaterial("interrupteur_off")])
    )
    .Register();
```

### Inventaire Creatif et Tags

- `WithTags(params string[] tags)` : Attribue des tags gameplay au bloc (par exemple `"minecraft:is_pickaxe_item_destructible"`).
- `WithCreative(CreativeCategory category, string group = "", bool isHidden = false)` : Emplacement du bloc dans l'inventaire Creatif du client.

### Validation et Enregistrement

- `Build()` : Genere l'objet `CustomBlock` configure.
- `Register()` : Enregistre directement le bloc dans le registre central `CustomBlockRegistry` du serveur.

---

## 4. Methodes raccourcies (Factories predefinies)

Des templates rapides sont fournis pour creer directement les types de blocs les plus courants :

```csharp
// Bloc solide standard
CustomBlockBuilder.CreateSimpleBlock(
    identifier: "mon_serveur:bloc_améthyste",
    displayName: "Bloc d'Amethyste",
    texture: "amethyst_block",
    hardness: 1.5f,
    resistance: 6.0f
).Register();

// Bloc lumineux (style glowstone ou balise)
CustomBlockBuilder.CreateLightBlock(
    identifier: "mon_serveur:lanterne_infernale",
    displayName: "Lanterne Infernale",
    texture: "infernal_lamp",
    lightLevel: 15
).Register();

// Bloc transparent (style verre)
CustomBlockBuilder.CreateTransparentBlock(
    identifier: "mon_serveur:verre_renforce",
    displayName: "Verre Renforce",
    texture: "reinforced_glass",
    hardness: 3.0f
).Register();
```

---

## 5. Configuration cote Resource Pack

Pour afficher les textures personnalisees sur le client Minecraft Bedrock :

### Structure des dossiers du pack
```text
mon_pack/
├── manifest.json
├── blocks.json
├── textures/
│   ├── blocks/
│   │   ├── ruby_block.png
│   │   └── ruby_lamp.png
│   └── terrain_texture.json
```

### Configuration de textures/terrain_texture.json
```json
{
  "resource_pack_name": "mon_serveur_pack",
  "texture_name": "atlas.terrain",
  "texture_data": {
    "ruby_block": {
      "textures": "textures/blocks/ruby_block"
    },
    "ruby_lamp": {
      "textures": "textures/blocks/ruby_lamp"
    }
  }
}
```

### Configuration de blocks.json
```json
{
  "format_version": "1.21.0",
  "mon_serveur:bloc_rubis": {
    "textures": "ruby_block",
    "sound": "stone"
  },
  "mon_serveur:lampe_rubis": {
    "textures": "ruby_lamp",
    "sound": "glass"
  }
}
```

---

## 6. Exemple complet dans le code source

Un exemple complet demontrant l'enregistrement de plusieurs blocs moddes (bloc decoratif, minerai et lampe) est disponible dans le depot :
- Repertoire : `examples/plugins/CustomContent/`
- Fichier source : `examples/plugins/CustomContent/src/CustomContentPlugin.cs`
- Configuration : `examples/plugins/CustomContent/plugin.yml`
