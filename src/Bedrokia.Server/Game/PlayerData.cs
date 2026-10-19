// ┌───────────────────────────────────────────────────────────────────┐
// │  ____  _____ ____  ____   ___  _  __ ___    _                     │
// │ | __ )| ____|  _ \|  _ \ / _ \| |/ /|_ _|  / \                    │
// │ |  _ \|  _| | | | | |_) | | | | ' /  | |  / _ \                   │
// │ | |_) | |___| |_| |  _ <| |_| | . \  | | / ___ \                  │
// │ |____/|_____|____/|_| \_\\___/|_|\_\|___/_/   \_\                 │
// │                                                                   │
// │  Bedrokia - Minecraft: Bedrock Edition server software            │
// │  Licensed under the Apache License 2.0 - see LICENSE              │
// └───────────────────────────────────────────────────────────────────┘

using System.Text.Json;
using Bedrokia.Server.Core;

namespace Bedrokia.Server.Game;

/// <summary>What is kept of a player between sessions: position, game mode and items. One JSON file per player.</summary>

public sealed record PlayerData(float X, float Y, float Z, GameMode GameMode, int SelectedSlot,
    List<PlayerData.SavedStack> Main, List<PlayerData.SavedStack> Armour, List<PlayerData.SavedStack> Offhand,
    int Level = 0, float Progress = 0, List<PlayerData.SavedStack>? EnderChest = null, int Dimension = 0)
{
    /// <param name="Nbt">Item NBT (enchantments...) as base64 little-endian NBT, null when none.</param>
    public sealed record SavedStack(int Slot, string Item, int Count, short Meta, string? Block, string? Nbt = null);

    public static List<SavedStack> Save(Inventory inventory) =>
        inventory.Slots.Select((stack, slot) => (stack, slot)).Where(s => s.stack is not null)
            .Select(s => new SavedStack(s.slot, s.stack!.Type.Name, s.stack.Count, s.stack.Meta, s.stack.Block?.Key,
                s.stack.Nbt is { Count: > 0 } nbt ? Convert.ToBase64String(Bedrokia.Protocol.Nbt.Write(nbt, network: false)) : null)).ToList();

    public static void Load(Inventory inventory, List<SavedStack> saved)
    {
        inventory.Clear();
        foreach (var s in saved)
        {
            if (Items.ByName(s.Item) is not { } type)
                continue; // item removed from the game data
            var block = s.Block is null ? null : Blocks.ByKey(s.Block);
            var nbt = s.Nbt is null ? null : Bedrokia.Protocol.Nbt.ReadAll(Convert.FromBase64String(s.Nbt), network: false).FirstOrDefault();
            inventory[s.Slot] = new ItemStack(type, Math.Clamp(s.Count, 1, 255), s.Meta, block?.NetworkId ?? 0, nbt);
        }
    }

    /// <summary>Folder of the player files, next to the worlds like PocketMine-MP's players/.</summary>
    public const string Directory = "players";

    private static string File(Guid uuid) => Path.Combine(Directory, $"{uuid}.json");

    /// <summary>Null for a new player or an unreadable file (logged by the caller as a new player).</summary>
    public static PlayerData? Read(Guid uuid)
    {
        var file = File(uuid);
        if (!System.IO.File.Exists(file))
            return null;
        try
        {
            return JsonSerializer.Deserialize<PlayerData>(System.IO.File.ReadAllText(file));
        }
        catch (JsonException)
        {
            System.IO.File.Move(file, file + ".corrupt", overwrite: true);
            return null;
        }
    }

    public void Write(Guid uuid)
    {
        var file = File(uuid);
        System.IO.Directory.CreateDirectory(Directory);
        System.IO.File.WriteAllText(file + ".tmp", JsonSerializer.Serialize(this));
        System.IO.File.Move(file + ".tmp", file, overwrite: true);
    }
}
