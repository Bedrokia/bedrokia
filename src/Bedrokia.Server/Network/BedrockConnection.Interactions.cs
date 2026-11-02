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

using Bedrokia.Protocol;
using Bedrokia.Server.Core;
using Bedrokia.Server.Game;
using Bedrokia.Server.Game.Custom.Items;

namespace Bedrokia.Server.Network;

/// <summary>
/// Right click on blocks and with items on blocks, following Dragonfly's (MIT) block/*.go Activate and
/// item/*.go UseOnBlock: doors, trapdoors, gates, levers, buttons, beds, tables and containers; hoes, shovels,
/// bone meal, flint and steel, buckets, seeds.
/// </summary>
public sealed partial class BedrockConnection
{
    private const int LevelEventClick = 1000, LevelEventDoor = 1003, LevelEventFizz = 1004, LevelEventCropGrowth = 2005;

    /// <summary>Bed this player last slept in: their respawn point while it exists.</summary>
    public (int X, int Y, int Z)? BedSpawn { get; private set; }

    /// <summary>Uses the clicked block; returns false if it does nothing (then the held item is used or placed).</summary>
    private bool UseBlock(BlockType block, int x, int y, int z, int face = 0)
    {
        if (_server.Events.HasPlayerInteractBlock)
        {
            var interactEv = new PlayerInteractBlockEvent(this, new BlockPos(x, y, z), block, face);
            if (!_server.Events.OnPlayerInteractBlock(interactEv))
            {
                Send(Packets.UpdateBlock(x, y, z, block.NetworkId));
                return true;
            }
        }
        var name = block.Name;
        // Mobs on a lead get tied to the fence.
        if (name.EndsWith("_fence") && _server.Mobs.TieToFence(this, x, y, z))
            return true;
        if (TryOpenContainer(block, x, y, z))
            return true;
        // A custom crafting table: the vanilla crafting screen, for its own recipes too.
        if (Game.Custom.Blocks.CustomBlockRegistry.Get(name)?.Components.GetValueOrDefault(Game.Custom.Blocks.BlockComponentIds.CraftingTable) is Game.Custom.Blocks.CraftingTableBlockComponent table)
        {
            OpenCustomCraftingTable(table.CraftingTags);
            return true;
        }
        switch (name)
        {
            case "minecraft:enchanting_table":
                OpenEnchantingTable(x, y, z);
                return true;
            case "minecraft:anvil" or "minecraft:chipped_anvil" or "minecraft:damaged_anvil":
                OpenAnvil(x, y, z);
                return true;
            case "minecraft:crafting_table":
                Inventory.CraftingTableOpen = true;
                Inventory.CraftingTags = ["crafting_table"];
                Send(InventoryPackets.ContainerOpen(InventoryPackets.WindowCraftingTable, InventoryPackets.ContainerTypeWorkbench, x, y, z));
                return true;
            case "minecraft:grindstone":
                Inventory.GrindstoneOpen = true;
                Inventory.GrindstonePos = (x, y, z);
                Send(InventoryPackets.ContainerOpen(InventoryPackets.WindowGrindstone, InventoryPackets.ContainerTypeGrindstone, x, y, z));
                return true;
            case "minecraft:stonecutter_block" or "minecraft:stonecutter":
                Inventory.StonecutterOpen = true;
                Inventory.StonecutterPos = (x, y, z);
                Send(InventoryPackets.ContainerOpen(InventoryPackets.WindowStonecutter, InventoryPackets.ContainerTypeStonecutter, x, y, z));
                return true;
            case "minecraft:bed" when World.Dimension == 1: // beds explode in the Nether
                _server.SetBlockAndUpdate(x, y, z, Blocks.Air, World);
                if (BlockPlacement.OtherHalf(block, x, y, z) is { } head && World.GetBlock(head.X, head.Y, head.Z)?.Name == name)
                    _server.SetBlockAndUpdate(head.X, head.Y, head.Z, Blocks.Air, World);
                _server.Explode(x + 0.5, y + 0.5, z + 0.5, 5, World);
                return true;
            case "minecraft:bed":
                UseBed(block, x, y, z);
                return true;
            case "minecraft:lever":
                _server.SetBlockAndUpdate(x, y, z, Toggle(block, "open_bit"), World);
                _server.BroadcastIn(World, Packets.LevelEvent(LevelEventClick, x, y, z, 0));
                return true;
        }
        if (name.EndsWith("_door") && name != "minecraft:iron_door")
        {
            var opened = Toggle(block, "open_bit");
            _server.SetBlockAndUpdate(x, y, z, opened, World);
            if (BlockPlacement.OtherHalf(block, x, y, z) is { } other && World.GetBlock(other.X, other.Y, other.Z) is { } half && half.Name == name)
                _server.SetBlockAndUpdate(other.X, other.Y, other.Z, With(half, "open_bit", opened.States["open_bit"]), World);
            _server.BroadcastIn(World, Packets.LevelEvent(LevelEventDoor, x, y, z, 0));
            return true;
        }
        if (name.Contains("trapdoor") && name != "minecraft:iron_trapdoor" || name.Contains("fence_gate"))
        {
            _server.SetBlockAndUpdate(x, y, z, Toggle(block, "open_bit"), World);
            _server.BroadcastIn(World, Packets.LevelEvent(LevelEventDoor, x, y, z, 0));
            return true;
        }
        if (name.EndsWith("_button") && block.States.TryGetValue("button_pressed_bit", out var pressed) && Convert.ToInt32(pressed) == 0)
        {
            _server.SetBlockAndUpdate(x, y, z, With(block, "button_pressed_bit", (byte)1), World);
            _server.BroadcastIn(World, Packets.LevelEvent(LevelEventClick, x, y, z, 0));
            _server.Schedule(name.Contains("stone") || name.Contains("polished_blackstone") ? 20 : 30, () =>
            {
                if (World.GetBlock(x, y, z) is { } now && now.Name == name)
                    _server.SetBlockAndUpdate(x, y, z, With(now, "button_pressed_bit", (byte)0), World);
            });
            return true;
        }
        return false;
    }

    /// <summary>Uses the held item on the clicked block; returns true when it did something.</summary>
    private bool UseItemOn(ItemStack held, BlockType clicked, int x, int y, int z, int face)
    {
        var item = held.Type.Name.StartsWith("minecraft:") ? held.Type.Name["minecraft:".Length..] : held.Type.Name;
        var (fx, fy, fz) = FaceOffset(face);
        var world = World;
        var survival = GameMode != GameMode.Creative;
        if (UseSpecialItemOn(item, clicked, x, y, z, face))
            return true;

        if (item.EndsWith("_hoe") && clicked.Name is "minecraft:grass_block" or "minecraft:dirt" or "minecraft:dirt_path"
            && face != 0 && world.GetBlock(x, y + 1, z) is { IsAir: true })
        {
            _server.SetBlockAndUpdate(x, y, z, Blocks.ByName("farmland")!, World);
            WearHeld(1);
            return true;
        }
        if (item.EndsWith("_shovel") && clicked.Name == "minecraft:grass_block" && face != 0 && world.GetBlock(x, y + 1, z) is { IsAir: true })
        {
            _server.SetBlockAndUpdate(x, y, z, Blocks.ByName("grass_path") ?? Blocks.ByName("dirt_path") ?? clicked, World);
            WearHeld(1);
            return true;
        }
        if (item == "bone_meal")
        {
            if (!_server.Grow(x, y, z, boneMeal: true, World))
                return false;
            _server.BroadcastIn(World, Packets.LevelEvent(LevelEventCropGrowth, x, y, z, 0));
            if (survival)
                Send(Inventory.ConsumeHeld());
            return true;
        }
        if (item == "flint_and_steel")
        {
            if (clicked.Name == "minecraft:tnt")
            {
                _server.SetBlockAndUpdate(x, y, z, Blocks.Air, World);
                _server.PrimeTnt(x, y, z, world: World);
            }
            else if (clicked.Name == "minecraft:obsidian" && TryLightPortal(x + fx, y + fy, z + fz))
            {
            }
            else if (world.GetBlock(x + fx, y + fy, z + fz) is { IsAir: true } && Blocks.ByName("fire") is { } fire)
                _server.SetBlockAndUpdate(x + fx, y + fy, z + fz, fire, World);
            else
                return false;
            _server.BroadcastIn(World, Packets.LevelEvent(LevelEventFizz, x, y, z, 0));
            WearHeld(1);
            return true;
        }
        if (item == "bucket")
        {
            foreach (var (bx, by, bz) in new[] { (x, y, z), (x + fx, y + fy, z + fz) })
                if (world.GetBlock(bx, by, bz) is { } liquid && Fluids.IsSource(liquid))
                {
                    if (Server?.Events is { HasPlayerBucketFill: true } fillEvs)
                    {
                        var fillEv = new PlayerBucketFillEvent(this, bx, by, bz, liquid);
                        if (!fillEvs.OnPlayerBucketFill(fillEv)) return true;
                    }
                    _server.SetBlockAndUpdate(bx, by, bz, Blocks.Air, World);
                    GiveBucket(liquid.Name.Contains("lava") ? "lava_bucket" : "water_bucket");
                    return true;
                }
            return false;
        }
        if (item is "water_bucket" or "lava_bucket")
        {
            if (world.GetBlock(x + fx, y + fy, z + fz) is not { } target || BlockPhysics(target) || SpawnProtected(x + fx, z + fz))
                return false;
            if (Server?.Events is { HasPlayerBucketEmpty: true } emptyEvs)
            {
                var emptyEv = new PlayerBucketEmptyEvent(this, x + fx, y + fy, z + fz, item);
                if (!emptyEvs.OnPlayerBucketEmpty(emptyEv)) return true;
            }
            if (item == "water_bucket" && World.Dimension == 1) // water evaporates in the Nether
            {
                _server.BroadcastIn(World, Packets.LevelEvent(LevelEventFizz, x + fx, y + fy, z + fz, 0));
                if (survival)
                    GiveBucket("bucket");
                return true;
            }
            _server.SetBlockAndUpdate(x + fx, y + fy, z + fz, Fluids.Source(item == "water_bucket" ? "water" : "lava"), World);
            if (survival)
                GiveBucket("bucket");
            return true;
        }
        if (CropFor(item) is { } crop && face == 1 && world.GetBlock(x, y + 1, z) is { IsAir: true } && CanPlant(crop, clicked, x, y, z))
        {
            _server.SetBlockAndUpdate(x, y + 1, z, crop, World);
            if (survival)
                Send(Inventory.ConsumeHeld());
            return true;
        }
        return false;
    }

    /// <summary>
    /// Block an item places: its own block for block items, else the block of the same or a related name
    /// (beds, doors, signs, cauldrons, repeaters... are plain items in Bedrock, the client sends no block for them).
    /// </summary>
    internal static BlockType? PlacedBlock(ItemStack? item)
    {
        if (item is null)
            return null;
        var name = item.Type.Name.StartsWith("minecraft:") ? item.Type.Name["minecraft:".Length..] : item.Type.Name;
        if (name is "wheat" or "beetroot") // harvested items sharing the name of their crop
            return null;
        if (item.Block is { } block)
            return block;
        if (CustomItemRegistry.Get(item.Type.Name)?.GetComponent<BlockPlacerItemComponent>() is { } placer)
            return Blocks.ByName(placer.Block);
        return Blocks.ByName(name switch
        {
            "oak_sign" => "standing_sign",
            "repeater" => "unpowered_repeater",
            "comparator" => "unpowered_comparator",
            "redstone" => "redstone_wire",
            "string" => "trip_wire",
            _ when name.EndsWith("_sign") && !name.EndsWith("hanging_sign") => name.Replace("_sign", "_standing_sign"),
            _ => name,
        });
    }

    /// <summary>Armour slot an item is worn in (0 head .. 3 feet), null if it is not armour.</summary>
    internal static int? ArmourSlot(string item)
    {
        var n = item.StartsWith("minecraft:") ? item["minecraft:".Length..] : item;
        return n switch
        {
            var s when s.EndsWith("_helmet") => 0,
            var s when s.EndsWith("_chestplate") || s is "elytra" => 1,
            var s when s.EndsWith("_leggings") => 2,
            var s when s.EndsWith("_boots") => 3,
            _ => null,
        };
    }

    // Dragonfly item/armour.go: using armour swaps it with the piece worn in its slot.
    private bool TryEquipHeld()
    {
        if (Inventory.Held is not { } held || ArmourSlot(held.Type.Name) is not { } slot)
            return false;
        var worn = Inventory.Armour[slot];
        Inventory.Armour[slot] = held;
        Inventory.Main[Inventory.SelectedSlot] = worn;
        Send(InventoryPackets.InventorySlot(InventoryPackets.WindowArmour, slot, held),
            InventoryPackets.InventorySlot(InventoryPackets.WindowInventory, Inventory.SelectedSlot, worn));
        return true;
    }

    // Seeds and other items that plant a block of another name.
    private static BlockType? CropFor(string item) => item switch
    {
        "wheat_seeds" => Blocks.ByName("wheat"),
        "carrot" => Blocks.ByName("carrots"),
        "potato" => Blocks.ByName("potatoes"),
        "beetroot_seeds" => Blocks.ByName("beetroot"),
        "pumpkin_seeds" => Blocks.ByName("pumpkin_stem"),
        "melon_seeds" => Blocks.ByName("melon_stem"),
        "sugar_cane" => Blocks.ByName("reeds"),
        "sweet_berries" => Blocks.ByName("sweet_berry_bush"),
        _ => null,
    };

    private bool CanPlant(BlockType crop, BlockType ground, int x, int y, int z)
    {
        if (crop.Name == "minecraft:reeds")
            return ground.Name is "minecraft:grass_block" or "minecraft:dirt" or "minecraft:sand" or "minecraft:red_sand" or "minecraft:reeds"
                   && (ground.Name == "minecraft:reeds" || new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }
                       .Any(d => World.GetBlock(x + d.Item1, y, z + d.Item2) is { } b && b.Name.Contains("water")));
        if (crop.Name == "minecraft:sweet_berry_bush")
            return ground.Name is "minecraft:grass_block" or "minecraft:dirt" or "minecraft:podzol";
        return ground.Name == "minecraft:farmland";
    }

    private void GiveBucket(string name)
    {
        if (GameMode == GameMode.Creative && name == "bucket")
            return;
        Send(Inventory.ConsumeHeld());
        if (ItemStack.Of(name) is { } bucket)
        {
            var (packets, remaining) = Inventory.Add(bucket);
            Send([.. packets]);
            if (remaining > 0)
                DropFromHand(bucket);
        }
    }

    // Beds: respawn point, and at night every player in bed skips to the morning.
    private void UseBed(BlockType bed, int x, int y, int z)
    {
        if (Server?.Events is { HasPlayerBedEnter: true } bedEvs)
        {
            var enterEv = new PlayerBedEnterEvent(this, x, y, z);
            if (!bedEvs.OnPlayerBedEnter(enterEv)) return;
        }
        BedSpawn = (x, y + 1, z);
        SendMessage("§7Respawn point set.");
        var time = _server.World.Time % 24000;
        if (time is < 12542 or > 23459)
        {
            SendMessage("§7You can only sleep at night.");
            return;
        }
        _sleepingNight = _server.World.Time / 24000;
        var players = _server.Players.Where(p => p.IsInGame && p.World == _server.World).ToList(); // players in the Nether do not count
        if (players.All(p => p._sleepingNight == _sleepingNight))
        {
            _server.World.Time = (_server.World.Time / 24000 + 1) * 24000;
            if (Server?.Events is { HasPlayerSleepSkipNight: true } skipEvs) skipEvs.OnPlayerSleepSkipNight(new PlayerSleepSkipNightEvent(this));
            _server.Broadcast(Packets.SetTime((int)(_server.World.Time % int.MaxValue)));
            _server.BroadcastMessage("§eGood morning! The night was skipped.");
        }
        else
            _server.BroadcastMessage($"§7{Name} is sleeping ({players.Count(p => p._sleepingNight == _sleepingNight)}/{players.Count}).");
        _ = bed;
    }

    private long _sleepingNight = -1;
    /// <summary>Wakes up the player if they were sleeping in bed.</summary>
    public void WakeUp()
    {
        if (_sleepingNight != -1)
        {
            _sleepingNight = -1;
            if (Server?.Events is { HasPlayerBedLeave: true } leaveEvs) leaveEvs.OnPlayerBedLeave(new PlayerBedLeaveEvent(this));
        }
    }


    private static (int X, int Y, int Z) FaceOffset(int face) => face switch
    {
        0 => (0, -1, 0), 1 => (0, 1, 0), 2 => (0, 0, -1), 3 => (0, 0, 1), 4 => (-1, 0, 0), _ => (1, 0, 0),
    };

    private static bool BlockPhysics(BlockType block) => Entities.BlockPhysics.IsSolid(block);

    internal static BlockType With(BlockType block, string key, object value)
    {
        if (!block.States.ContainsKey(key))
            return block;
        var states = new OrderedDictionary<string, object>(block.States) { [key] = value };
        return Blocks.ByKey(new BlockType(0, block.Name, states).Key) ?? block;
    }

    private static BlockType Toggle(BlockType block, string key) =>
        block.States.TryGetValue(key, out var value) ? With(block, key, (byte)(Convert.ToInt32(value) == 0 ? 1 : 0)) : block;
}
