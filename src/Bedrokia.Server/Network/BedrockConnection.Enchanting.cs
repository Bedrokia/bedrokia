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

using Bedrokia.Server.Core;
using Bedrokia.Server.Game;

namespace Bedrokia.Server.Network;

/// <summary>Experience levels and the enchanting table (ported from Dragonfly, MIT, session/handler_enchanting.go).</summary>
public sealed partial class BedrockConnection : IEnchanter
{
    public const byte WindowEnchanting = 2, ContainerTypeEnchantment = 3;

    public int ExperienceLevel { get; private set; }

    /// <summary>Progress towards the next level, 0..1.</summary>
    public float ExperienceProgress { get; private set; }

    private int _enchantSeed = Random.Shared.Next();
    private (int X, int Y, int Z) _enchantTable;
    private ItemStack? _lastEnchantInput;

    /// <summary>Points needed to go from <paramref name="level"/> to the next one (vanilla curve).</summary>
    public static int PointsForLevel(int level) => level >= 30 ? 112 + (level - 30) * 9 : level >= 15 ? 37 + (level - 15) * 5 : 7 + level * 2;

    public void AddExperience(int points)
    {
        if (points <= 0 || Dead)
            return;
        var total = ExperienceProgress * PointsForLevel(ExperienceLevel) + points;
        while (total >= PointsForLevel(ExperienceLevel))
        {
            total -= PointsForLevel(ExperienceLevel);
            ExperienceLevel++;
        }
        ExperienceProgress = (float)(total / PointsForLevel(ExperienceLevel));
        SendVitals();
    }

    public void SetExperienceLevel(int level)
    {
        ExperienceLevel = Math.Max(0, level);
        ExperienceProgress = 0;
        SendVitals();
    }

    private const byte ContainerTypeAnvil = 5;

    // Anvil window; after each use (survival) it wears out one time in eight or so: anvil, chipped, damaged, gone.
    private void OpenAnvil(int x, int y, int z)
    {
        var world = World;
        Inventory.AnvilUsed = () =>
        {
            if (GameMode != GameMode.Creative && Random.Shared.NextDouble() < 0.12 && world.GetBlock(x, y, z) is { } anvil)
            {
                var next = anvil.Name switch { "minecraft:anvil" => "minecraft:chipped_anvil", "minecraft:chipped_anvil" => "minecraft:damaged_anvil", _ => null };
                var worn = next is null ? Blocks.Air : Blocks.ByKey(new BlockType(0, next, anvil.States).Key) ?? Blocks.ByName(next) ?? Blocks.Air;
                _server.SetBlockAndUpdate(x, y, z, worn, world);
                if (worn.IsAir)
                {
                    _server.PlaySound("random.anvil_break", x + 0.5, y + 0.5, z + 0.5, world);
                    CloseWindowFromServer(WindowEnchanting, ContainerTypeAnvil);
                    return;
                }
            }
            _server.PlaySound("random.anvil_use", x + 0.5, y + 0.5, z + 0.5, world);
        };
        Send(InventoryPackets.ContainerOpen(WindowEnchanting, ContainerTypeAnvil, x, y, z));
    }

    private void OpenEnchantingTable(int x, int y, int z)
    {
        _enchantTable = (x, y, z);
        _lastEnchantInput = null;
        Inventory.Enchanter = this;
        Send(InventoryPackets.ContainerOpen(WindowEnchanting, ContainerTypeEnchantment, x, y, z));
    }

    /// <summary>After each inventory request: offer the options when the item in the table changed.</summary>
    private void RefreshEnchantOptions()
    {
        if (Inventory.Enchanter is null)
            return;
        var input = Inventory.Ui[PlayerInventory.EnchantInputSlot];
        if (ReferenceEquals(input, _lastEnchantInput))
            return;
        _lastEnchantInput = input;
        if (input is not null && Options(input) is { } options)
            Send(Enchanting.OptionsPacket(options.Costs, options.Options));
    }

    public (int[] Costs, List<(int Id, int Level)>[] Options)? Options(ItemStack item) =>
        Enchanting.Options(item, Bookshelves(), _enchantSeed);

    public bool SpendLevels(int requirement, int cost)
    {
        if (GameMode == GameMode.Creative)
            return true;
        if (ExperienceLevel < requirement || ExperienceLevel < cost)
            return false;
        SetExperienceLevel(ExperienceLevel - cost);
        return true;
    }

    public void Reseed()
    {
        _enchantSeed = Random.Shared.Next();
        _lastEnchantInput = null;
    }

    // Bookshelves two blocks away with air in between (Dragonfly searchBookshelves), at most 15.
    private int Bookshelves()
    {
        var world = World;
        var (tx, ty, tz) = _enchantTable;
        var shelves = 0;
        for (var x = -1; x <= 1; x++)
        for (var z = -1; z <= 1; z++)
        for (var y = 0; y <= 1; y++)
        {
            if (x == 0 && z == 0 || world.GetBlock(tx + x, ty + y, tz + z) is not { IsAir: true })
                continue;
            if (IsShelf(tx + x * 2, ty + y, tz + z * 2))
                shelves++;
            if (x != 0 && z != 0)
            {
                if (IsShelf(tx + x * 2, ty + y, tz + z))
                    shelves++;
                if (IsShelf(tx + x, ty + y, tz + z * 2))
                    shelves++;
            }
        }
        return Math.Min(shelves, 15);

        bool IsShelf(int x, int y, int z) => world.GetBlock(x, y, z)?.Name == "minecraft:bookshelf";
    }

    /// <summary>Experience dropped by a block mined in survival (vanilla amounts; given directly, no orbs).</summary>
    /// <summary>Experience a vanilla block gives when mined with the right tool (ores), 0 for most blocks.</summary>
    public static int BlockExperience(string name) => name.Replace("deepslate_", "").Replace("lit_", "") switch
    {
        "minecraft:coal_ore" => Random.Shared.Next(0, 3),
        "minecraft:diamond_ore" or "minecraft:emerald_ore" => Random.Shared.Next(3, 8),
        "minecraft:lapis_ore" or "minecraft:nether_quartz_ore" => Random.Shared.Next(2, 6),
        "minecraft:redstone_ore" => Random.Shared.Next(1, 6),
        "minecraft:nether_gold_ore" => Random.Shared.Next(0, 2),
        _ => 0,
    };
}
