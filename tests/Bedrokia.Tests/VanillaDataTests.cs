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
using Bedrokia.Server.Game;

namespace Bedrokia.Tests;

public class VanillaDataTests
{
    [Fact]
    public void All_vanilla_block_states_are_loaded()
    {
        Assert.True(Blocks.Count > 10_000, $"only {Blocks.Count} states");
        Assert.True(Blocks.ById(0).IsAir);
        Assert.Equal(-604749536, (int)Blocks.Air.NetworkId);
        Assert.NotNull(Blocks.ByName("minecraft:diamond_block"));
        Assert.Same(Blocks.Water, Blocks.ByNetworkId(Blocks.Water.NetworkId));
        Assert.True(Blocks.Bedrock.Unbreakable);
        Assert.False(Blocks.Stone.Unbreakable);
    }

    [Fact]
    public void All_vanilla_items_and_creative_inventory_are_loaded()
    {
        Assert.True(Items.Count > 1_000, $"only {Items.Count} items");
        Assert.NotNull(Items.ByName("diamond_sword"));
        Assert.True(Items.CreativeItemCount > 1_000, $"only {Items.CreativeItemCount} creative items");
        Assert.Equal(PacketId.ItemRegistry, new BedrockReader(Items.RegistryPacket).VarUInt32());
        Assert.Equal(PacketId.CreativeContent, new BedrockReader(Items.CreativeContentPacket).VarUInt32());
    }

    [Fact]
    public void Nbt_round_trips_in_both_encodings()
    {
        var root = new OrderedDictionary<string, object>
        {
            ["b"] = (byte)7,
            ["s"] = (short)-2,
            ["i"] = 123456,
            ["l"] = -9876543210L,
            ["f"] = 1.5f,
            ["str"] = "héllo",
            ["list"] = new NbtList(Nbt.Int, [1, 2, 3]),
            ["nested"] = new OrderedDictionary<string, object> { ["x"] = 1 },
            ["ints"] = new[] { 4, 5 },
        };
        foreach (var network in new[] { true, false })
        {
            var back = Nbt.ReadAll(Nbt.Write(root, network), network).Single();
            Assert.Equal(root.Keys, back.Keys);
            Assert.Equal("héllo", back["str"]);
            Assert.Equal(-9876543210L, back["l"]);
            Assert.Equal([1, 2, 3], ((NbtList)back["list"]).Values.Cast<int>());
        }
    }
}

public class SurvivalRulesTests
{
    private static ItemStack Tool(string name) => ItemStack.Of(name)!;

    [Fact]
    public void Blocks_drop_vanilla_items()
    {
        var random = new Random(1);
        Assert.Equal("minecraft:dirt", Assert.Single(DropRules.Drops(Blocks.GrassBlock, null, random)).Type.Name);
        Assert.Equal("minecraft:oak_log", Assert.Single(DropRules.Drops(Blocks.OakLog, null, random)).Type.Name);
        Assert.Equal("minecraft:cobblestone", Assert.Single(DropRules.Drops(Blocks.Stone, Tool("wooden_pickaxe"), random)).Type.Name);
        Assert.Empty(DropRules.Drops(Blocks.Stone, null, random)); // needs a pickaxe
        Assert.Empty(DropRules.Drops(Blocks.IronOre, Tool("wooden_pickaxe"), random)); // needs stone tier
        Assert.Equal("minecraft:raw_iron", Assert.Single(DropRules.Drops(Blocks.IronOre, Tool("stone_pickaxe"), random)).Type.Name);
        Assert.Empty(DropRules.Drops(Blocks.Glass, null, random));
    }

    [Fact]
    public void Inventory_merges_then_fills_empty_slots()
    {
        var inventory = new Inventory(3);
        inventory[1] = ItemStack.Of("dirt", 60);
        var (changed, remaining) = inventory.Add(ItemStack.Of("dirt", 10)!);

        Assert.Equal(0, remaining);
        Assert.Equal(64, inventory[1]!.Count);
        Assert.Equal(6, inventory[0]!.Count);
        Assert.Equal([1, 0], changed);
        Assert.Equal(1, ItemStack.Of("diamond_sword")!.MaxCount);
    }

    [Fact]
    public void Vanilla_recipes_are_loaded()
    {
        Assert.True(Recipes.Count > 1_000, $"only {Recipes.Count} recipes");
        Assert.Equal(Recipes.CraftingDataId, new BedrockReader(Recipes.CraftingDataPacket).VarUInt32());
    }

    [Fact]
    public void Crafting_planks_in_the_2x2_grid_then_taking_them()
    {
        var planks = Enumerable.Range(1, Recipes.Count + 200).Select(id => Recipes.ByNetworkId((uint)id))
            .First(r => r is { Block: "crafting_table" } && r.Outputs[0].Type.Name == "minecraft:oak_planks"
                        && r.Inputs.Count(i => !i.Empty) == 1 && r.Inputs.Any(i => i.Matches(ItemStack.Of("oak_log")!)))!;
        var inventory = new Bedrokia.Server.Network.PlayerInventory();
        inventory.Ui[28] = ItemStack.Of("oak_log", 2);

        // Request -1: craft recipe (variant 10 = id 12), then place the 4 planks from created output (60:50) to hotbar slot 0.
        var request = new BedrockWriter().VarUInt32(1).VarInt32(-1).VarUInt32(2)
            .VarUInt32(10).Byte(12).VarUInt32(planks.NetworkId).Byte(1)
            .VarUInt32(1).Byte(1).Byte(4).Byte(60).Bool(false).Byte(50).Int32(-1).Byte(28).Bool(false).Byte(0).Int32(0)
            .VarUInt32(0).Int32(0).ToArray();
        var reader = new BedrockReader(request);
        var responses = inventory.HandleRequests(ref reader, Bedrokia.Server.Core.GameMode.Survival, _ => { });

        Assert.Single(responses); // no resync: the request succeeded
        Assert.Equal(4, inventory.Main[0]!.Count);
        Assert.Equal("minecraft:oak_planks", inventory.Main[0]!.Type.Name);
        Assert.Equal(1, inventory.Ui[28]!.Count);
    }

    [Fact]
    public void Crafting_in_the_3x3_grid_works_even_if_the_table_flag_was_cleared()
    {
        var planks = Enumerable.Range(1, Recipes.Count + 200).Select(id => Recipes.ByNetworkId((uint)id))
            .First(r => r is { Block: "crafting_table" } && r.Outputs[0].Type.Name == "minecraft:oak_planks"
                        && r.Inputs.Count(i => !i.Empty) == 1 && r.Inputs.Any(i => i.Matches(ItemStack.Of("oak_log")!)))!;
        var inventory = new Bedrokia.Server.Network.PlayerInventory { CraftingTableOpen = false };
        inventory.Ui[33] = ItemStack.Of("oak_log", 2);

        var request = new BedrockWriter().VarUInt32(1).VarInt32(-1).VarUInt32(2)
            .VarUInt32(10).Byte(12).VarUInt32(planks.NetworkId).Byte(1)
            .VarUInt32(1).Byte(1).Byte(4).Byte(60).Bool(false).Byte(50).Int32(-1).Byte(28).Bool(false).Byte(0).Int32(0)
            .VarUInt32(0).Int32(0).ToArray();
        var reader = new BedrockReader(request);
        var responses = inventory.HandleRequests(ref reader, Bedrokia.Server.Core.GameMode.Survival, _ => { });

        Assert.Single(responses);
        Assert.Equal(4, inventory.Main[0]!.Count);
        Assert.Equal(1, inventory.Ui[33]!.Count);
    }
}

public class InventoryMenuTests
{
    // Take 1 item from the open container's slot 0 to the cursor.
    private static void TakeFromMenu(Bedrokia.Server.Network.PlayerInventory inventory, ItemStack stack)
    {
        var request = new BedrockWriter().VarUInt32(1).VarInt32(-1).VarUInt32(1)
            .VarUInt32(0).Byte(0).Byte(1).Byte(7).Bool(false).Byte(0).Int32(stack.NetworkId).Byte(59).Bool(false).Byte(0).Int32(0)
            .VarUInt32(0).Int32(0).ToArray();
        var reader = new BedrockReader(request);
        inventory.HandleRequests(ref reader, Bedrokia.Server.Core.GameMode.Survival, _ => { });
    }

    [Fact]
    public void A_menu_listener_can_refuse_or_allow_a_click()
    {
        var menu = new Bedrokia.Server.Network.InventoryMenu("Shop");
        var diamond = ItemStack.Of("diamond")!;
        menu.Inventory[0] = diamond;
        var inventory = new Bedrokia.Server.Network.PlayerInventory { OpenContainer = (menu.Inventory, "Menu") };
        (int Slot, ItemStack? Clicked)? seen = null;
        inventory.MenuListener = (slot, clicked, _) =>
        {
            seen = (slot, clicked);
            return false;
        };
        TakeFromMenu(inventory, diamond);
        Assert.Equal((0, diamond), seen);
        Assert.Same(diamond, menu.Inventory[0]); // refused: still in the menu
        Assert.Null(inventory.Ui[0]);

        inventory.MenuListener = (_, _, _) => true;
        TakeFromMenu(inventory, diamond);
        Assert.Null(menu.Inventory[0]);
        Assert.Same(diamond, inventory.Ui[0]); // on the cursor
    }
}
