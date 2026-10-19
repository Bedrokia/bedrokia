---
title: Forms & UI
---

# Forms & UI

Bedrokia features a modern, ultra-optimized (.NET 10, zero reflection) and secure system for **Bedrock Forms** and **Data-Driven UI (DDUI / DataStore)**, built upon the strict server-authoritative security model of Dragonfly (`df-mc/dragonfly`).

## 1. SimpleForm (Button Menu)

A standard menu containing a title, optional body text, and a list of clickable buttons (with optional local texture path or URL icon).

```csharp
using Bedrokia.Server.Game.Ui.Forms;

var form = new SimpleForm("Shop")
    .Content("Select an item to purchase:")
    .Button("Diamond Sword", "textures/items/diamond_sword", FormButtonImageType.Path, player =>
    {
        player.SendMessage("§aYou bought a Diamond Sword!");
    })
    .Button("Golden Apple", "textures/items/apple_golden", FormButtonImageType.Path, player =>
    {
        player.SendMessage("§eYou bought a Golden Apple!");
    })
    .OnClose((player, reason) =>
    {
        if (reason == FormCancelReason.UserClosed)
            player.SendMessage("§7Shop closed.");
    });

player.SendForm(form);
```

---

## 2. ModalForm (Yes / No Confirmation)

A binary modal dialog asking for confirmation (`true` or `false`).

```csharp
var confirm = new ModalForm("Teleport", "Do you want to teleport to the world spawn?")
    .ConfirmText("Yes, Teleport")
    .CancelText("Cancel")
    .OnSubmit((player, accepted) =>
    {
        if (accepted)
        {
            var (x, y, z) = server.World.Spawn;
            player.Teleport(x + 0.5f, y, z + 0.5f);
            player.SendMessage("§aTeleported!");
        }
        else
        {
            player.SendMessage("§cTeleportation cancelled.");
        }
    });

player.SendForm(confirm);
```

---

## 3. CustomForm (Interactive Form)

Supports labels, headers, dividers, text inputs, toggles, sliders, step sliders, and dropdown menus.

```csharp
var custom = new CustomForm("Player Preferences")
    .Label("Customize your in-game preferences below:")
    .Input("Display Name", placeholder: "e.g. Steve", defaultValue: player.Name, maxLength: 16)
    .Toggle("Allow Private Messages", defaultValue: true)
    .Slider("Music Volume", min: 0.0, max: 100.0, step: 5.0, defaultValue: 50.0)
    .StepSlider("Difficulty Level", ["Easy", "Normal", "Hard"], defaultIndex: 1)
    .Dropdown("Language", ["English", "Français", "Español"], defaultIndex: 0)
    .OnSubmit((player, result) =>
    {
        string nickname = result.GetString(1);
        bool pmsEnabled = result.GetBool(2);
        double volume = result.GetDouble(3);
        string difficulty = result.GetSelectedOption(4) ?? "Normal";
        string language = result.GetSelectedOption(5) ?? "English";

        player.SendMessage($"§aSettings saved! Language: §e{language}§a, Difficulty: §e{difficulty}");
    })
    .OnClose((player, reason) =>
    {
        if (reason == FormCancelReason.InvalidResponse)
            player.SendMessage("§cInvalid data received from client.");
    });

player.SendForm(custom);
```

---

## 4. Dragonfly-Grade Security & Anti-DoS Protection

The Bedrokia server enforces **Absolute Server Authority** over all client form interactions:

- **Anti-Replay Protection**: Each form has an incremental, unique `FormId`. Upon receiving a response, the form record is **immediately removed before** executing callbacks (preventing double-click and retransmission exploits).
- **Bounded Form Queue**: Maximum 3 pending forms per player. Stale or overflowing forms are cleanly closed with `UserBusy`.
- **Strict Anti-DoS Limits**: Payloads exceeding 16 KB or JSON depth greater than 2 are instantly rejected.
- **Server-Side Element Validation**:
  - `Input`: bounded length (`maxLength`, default 256) and illegal control characters are automatically stripped.
  - `Slider`: rejects `NaN` and infinite values, enforces strict `[min, max]` bounds.
  - `Dropdown` & `StepSlider`: selection indices are strictly validated against the server-defined option list.
- **Automatic Lifecycle Cleanup**: Active forms and DDUI sessions are cleaned up when a player disconnects, dies, or after a timeout period (5 minutes).

---

## 5. Data-Driven UI (DDUI) & DataStore

Supports custom screens backed by client-side data stores:

```csharp
using Bedrokia.Server.Game.Ui.Ddui;

// 1. Define a DataStore with schema validation
var store = new DduiDataStore("MyHudStore")
    .DefineProperty("coins", typeof(double), clientWritable: false) // Server-only
    .DefineProperty("sound_mute", typeof(bool), clientWritable: true); // Client-editable

store.Set("coins", 1500.0);
store.Set("sound_mute", false);

// 2. Open DDUI screen for player
player.ShowDduiScreen("custom_hud_screen", store);

// 3. Reactively update property on client
player.SendDataStoreUpdate("MyHudStore", "coins", "1750");
```
