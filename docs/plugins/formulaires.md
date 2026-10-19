---
title: Formulaires & UI
---

# Formulaires & UI

Bedrokia intègre un système moderne, ultra-rapide (.NET 10, zéro réflexion) et hautement sécurisé de **Formulaires Bedrock** et de **Data-Driven UI (DDUI / DataStore)**, inspiré des principes de sécurité de Dragonfly (`df-mc/dragonfly`).

## 1. SimpleForm (Menu de Boutons)

Un formulaire classique avec un titre, du texte d'en-tête et une liste de boutons cliquables (avec icône locale ou URL optionnelle).

```csharp
using Bedrokia.Server.Game.Ui.Forms;

var form = new SimpleForm("Boutique")
    .Content("Choisissez un article à acheter :")
    .Button("Épée en diamant", "textures/items/diamond_sword", FormButtonImageType.Path, player =>
    {
        player.SendMessage("§aVous avez acheté l'épée !");
    })
    .Button("Pomme dorée", "textures/items/apple_golden", FormButtonImageType.Path, player =>
    {
        player.SendMessage("§eVous avez acheté une pomme dorée !");
    })
    .OnClose((player, reason) =>
    {
        if (reason == FormCancelReason.UserClosed)
            player.SendMessage("§7Boutique fermée.");
    });

player.SendForm(form);
```

---

## 2. ModalForm (Confirmation Oui / Non)

Un dialogue modal demandant une confirmation binaire (`true` ou `false`).

```csharp
var confirm = new ModalForm("Téléportation", "Voulez-vous être téléporté au spawn ?")
    .ConfirmText("Oui, téléporter")
    .CancelText("Annuler")
    .OnSubmit((player, accepted) =>
    {
        if (accepted)
        {
            var (x, y, z) = server.World.Spawn;
            player.Teleport(x + 0.5f, y, z + 0.5f);
            player.SendMessage("§aTéléporté !");
        }
        else
        {
            player.SendMessage("§cTéléportation annulée.");
        }
    });

player.SendForm(confirm);
```

---

## 3. CustomForm (Formulaire Interactif)

Permet de combiner du texte, des champs de saisie, des interrupteurs, des curseurs et des listes déroulantes.

```csharp
var custom = new CustomForm("Configuration du Joueur")
    .Label("Personnalisez vos préférences de jeu ci-dessous :")
    .Input("Pseudo affiché", placeholder: "Ex: Steve", defaultValue: player.Name, maxLength: 16)
    .Toggle("Activer les messages privés", defaultValue: true)
    .Slider("Volume des musiques", min: 0.0, max: 100.0, step: 5.0, defaultValue: 50.0)
    .StepSlider("Niveau de difficulté", ["Facile", "Normal", "Difficile"], defaultIndex: 1)
    .Dropdown("Langue", ["Français", "English", "Español"], defaultIndex: 0)
    .OnSubmit((player, result) =>
    {
        string nickname = result.GetString(1);
        bool pmsEnabled = result.GetBool(2);
        double volume = result.GetDouble(3);
        string difficulty = result.GetSelectedOption(4) ?? "Normal";
        string language = result.GetSelectedOption(5) ?? "Français";

        player.SendMessage($"§aPréférences enregistrées ! Langue: §e{language}§a, Difficulté: §e{difficulty}");
    })
    .OnClose((player, reason) =>
    {
        if (reason == FormCancelReason.InvalidResponse)
            player.SendMessage("§cDonnées invalides reçues du client.");
    });

player.SendForm(custom);
```

---

## 4. Sécurité Dragonfly & Protection Anti-DoS

Le serveur Bedrokia possède une **Autorité Absolue** sur toutes les réponses des formulaires :

- **Protection Anti-Replay** : Chaque formulaire a un `FormId` unique. Dès réception d'une réponse, le formulaire est **retiré immédiatement** avant l'exécution du code (empêche les doubles clics ou les attaques par double paquet).
- **File Active Bornée** : Maximum 3 formulaires en attente par joueur. Les formulaires trop anciens ou en surnombre sont fermés proprement (`UserBusy`).
- **Limites Anti-DoS strictes** : Les charges JSON dépassant 16 Ko ou une profondeur supérieure à 2 sont automatiquement rejetées.
- **Validation serveur par élément** :
  - `Input` : longueur bornée (`maxLength`, défaut 256) et suppression automatique des caractères de contrôle illégaux.
  - `Slider` : rejet des valeurs `NaN` et infinies, vérification des bornes `[min, max]`.
  - `Dropdown` & `StepSlider` : l'index est strictement validé par rapport au nombre d'options côté serveur.
- **Nettoyage automatique** : Tous les formulaires ouverts sont fermés lors de la déconnexion ou de la mort du joueur, ou après un délai d'inactivité (5 minutes).

---

## 5. Data-Driven UI (DDUI) & DataStore

Pour les interfaces personnalisées basées sur les packs de ressources et les écrans DDUI :

```csharp
using Bedrokia.Server.Game.Ui.Ddui;

// 1. Définir un DataStore avec schéma strict
var store = new DduiDataStore("MyHudStore")
    .DefineProperty("coins", typeof(double), clientWritable: false) // Serveur seul
    .DefineProperty("sound_mute", typeof(bool), clientWritable: true); // Modifiable par le client

store.Set("coins", 1500.0);
store.Set("sound_mute", false);

// 2. Afficher l'écran DDUI au joueur
player.ShowDduiScreen("custom_hud_screen", store);

// 3. Mettre à jour une valeur de manière réactive
player.SendDataStoreUpdate("MyHudStore", "coins", "1750");
```
