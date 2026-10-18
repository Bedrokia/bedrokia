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

using Bedrokia.Server.Game;

namespace Bedrokia.Tests;

public class BlockSystemsTests
{
    private static List<(int X, int Y, int Z, BlockType Block)> Place(string name, int face, float clickY, float yaw, float pitch = 0) =>
        BlockPlacement.Place(Blocks.ByName(name)!, 0, 64, 0, face, clickY, yaw, pitch, (_, _, _) => true);

    [Fact]
    public void Blocks_are_oriented_like_vanilla()
    {
        Assert.Equal("x", Place("oak_log", 4, 0.5f, 0).Single().Block.States["pillar_axis"]);
        Assert.Equal("north", Place("furnace", 1, 0, 0).Single().Block.States["minecraft:cardinal_direction"]); // looking south: faces the player
        Assert.Equal(2, Place("oak_stairs", 1, 0, 0).Single().Block.States["weirdo_direction"]); // climbs away (south)
        Assert.Equal((byte)1, Place("oak_stairs", 2, 0.8f, 0).Single().Block.States["upside_down_bit"]);
        Assert.Equal("top", Place("oak_slab", 3, 0.7f, 0).Single().Block.States["minecraft:vertical_half"]);
        Assert.Equal("east", Place("torch", 5, 0.5f, 0).Single().Block.States["torch_facing_direction"]);
        Assert.Equal(3, Place("ladder", 3, 0.5f, 0).Single().Block.States["facing_direction"]);
    }

    [Fact]
    public void Doors_and_beds_take_two_blocks_and_break_together()
    {
        var door = Place("wooden_door", 1, 0, 90);
        Assert.Equal(2, door.Count);
        Assert.Equal((0, 65, 0), (door[1].X, door[1].Y, door[1].Z));
        Assert.Equal((byte)1, door[1].Block.States["upper_block_bit"]);
        Assert.Equal((0, 64, 0), BlockPlacement.OtherHalf(door[1].Block, 0, 65, 0));

        var bed = Place("bed", 1, 0, 180); // looking north: the head is north of the foot
        Assert.Equal((0, 64, -1), (bed[1].X, bed[1].Y, bed[1].Z));
        Assert.Equal((0, 64, 0), BlockPlacement.OtherHalf(bed[1].Block, 0, 64, -1));

        Assert.Empty(BlockPlacement.Place(Blocks.ByName("wooden_door")!, 0, 64, 0, 1, 0, 0, 0, (_, y, _) => y == 64)); // no room on top
    }

    [Fact]
    public void Furnaces_know_what_to_cook_and_burn()
    {
        Assert.Equal("iron_ingot", Smelting.Result("Furnace", ItemStack.Of("raw_iron")!));
        Assert.Equal("iron_ingot", Smelting.Result("BlastFurnace", ItemStack.Of("raw_iron")!));
        Assert.Null(Smelting.Result("Smoker", ItemStack.Of("raw_iron")!));
        Assert.Equal("cooked_beef", Smelting.Result("Smoker", ItemStack.Of("beef")!));
        Assert.Equal("charcoal", Smelting.Result("Furnace", ItemStack.Of("oak_log")!));
        Assert.Equal(1600, Smelting.BurnTicks(ItemStack.Of("coal")!));
        Assert.Equal(300, Smelting.BurnTicks(ItemStack.Of("oak_planks")!));
        Assert.Equal(0, Smelting.BurnTicks(ItemStack.Of("stone")!));
    }

    [Fact]
    public void A_chest_and_its_contents_are_saved_with_the_chunk_and_sent_to_clients()
    {
        var chunk = new Chunk(2, -3);
        chunk.SetBlock(1, 70, 2, Blocks.ByName("chest")!);
        var chest = BlockEntity.For(Blocks.ByName("chest")!)!;
        chest.Items![5] = ItemStack.Of("diamond", 12);
        chunk.SetBlockEntity(1, 70, 2, chest);

        var copy = Chunk.Deserialize(2, -3, chunk.Serialize());
        var saved = copy.GetBlockEntity(1, 70, 2)!;
        Assert.Equal("Chest", saved.Id);
        Assert.Equal(12, saved.Items![5]!.Count);
        Assert.Equal("minecraft:diamond", saved.Items[5]!.Type.Name);

        var nbt = saved.NetworkNbt(2 * 16 + 1, 70, -3 * 16 + 2);
        Assert.Equal("Chest", nbt["id"]);
        Assert.Equal(33, nbt["x"]);
    }
}

public class WallAndPluginTests
{
    [Fact]
    public void Walls_connect_to_their_neighbours()
    {
        var wall = Blocks.ByName("cobblestone_wall")!;
        var world = new Dictionary<(int, int, int), BlockType> { [(1, 0, 0)] = wall, [(-1, 0, 0)] = Blocks.Stone };
        BlockType? Get(int x, int y, int z) => world.GetValueOrDefault((x, y, z), Blocks.Air);

        var shaped = Walls.Connect(wall, Get, 0, 0, 0);
        Assert.Equal("short", shaped.States["wall_connection_type_east"]);
        Assert.Equal("short", shaped.States["wall_connection_type_west"]);
        Assert.Equal("none", shaped.States["wall_connection_type_north"]);
        Assert.Equal((byte)0, shaped.States["wall_post_bit"]); // straight line: no post
        Assert.Equal((byte)1, Walls.Connect(wall, (_, _, _) => Blocks.Air, 0, 0, 0).States["wall_post_bit"]);
    }

    [Fact]
    public void Beds_doors_and_signs_place_their_block()
    {
        Assert.Equal("minecraft:bed", Bedrokia.Server.Network.BedrockConnection.PlacedBlock(ItemStack.Of("bed"))?.Name);
        Assert.Equal("minecraft:standing_sign", Bedrokia.Server.Network.BedrockConnection.PlacedBlock(ItemStack.Of("oak_sign"))?.Name);
        Assert.Null(Bedrokia.Server.Network.BedrockConnection.PlacedBlock(ItemStack.Of("wheat")));
        Assert.Equal("§aHi", ItemStack.Of("stick")!.WithName("§aHi", "lore").CustomName);
    }

    [Fact]
    public void A_failing_plugin_handler_does_not_stop_the_others()
    {
        using var logger = new Bedrokia.Server.Core.Logger(Bedrokia.Server.Core.LogLevel.Error, null);
        var events = new Bedrokia.Server.Plugins.ServerEvents(logger);
        var ticks = 0;
        events.Tick += _ => throw new InvalidOperationException("broken plugin");
        events.Tick += _ => ticks++;
        events.OnTick(1);
        Assert.Equal(1, ticks);
    }
}
