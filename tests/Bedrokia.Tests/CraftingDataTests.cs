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

/// <summary>Decodes the CraftingData packet the way gophertunnel's Reader does (protocol 2193) and checks invariants.</summary>
public class CraftingDataTests
{
    [Fact]
    public void Crafting_data_packet_decodes_completely_and_is_consistent()
    {
        var r = new BedrockReader(Recipes.CraftingDataPacket);
        Assert.Equal(Recipes.CraftingDataId, r.VarUInt32());
        var problems = new List<string>();

        var shaped = r.VarUInt32();
        for (var i = 0; i < shaped; i++)
        {
            var id = r.String();
            var width = r.VarInt32();
            var height = r.VarInt32();
            var inputs = r.VarUInt32();
            if (inputs != width * height)
                problems.Add($"shaped {id}: {inputs} inputs for {width}x{height}");
            for (var j = 0; j < inputs; j++)
                Descriptor(ref r, problems);
            Outputs(ref r, problems);
            r.Bytes(16);
            var block = r.String();
            r.VarInt32();
            r.Bool();
            Unlock(ref r);
            r.VarUInt32();
            if (block.Length == 0)
                problems.Add($"shaped {id}: no block");
        }
        for (var list = 0; list < 1; list++)
        {
            var shapeless = r.VarUInt32();
            for (var i = 0; i < shapeless; i++)
                Shapeless(ref r, problems);
        }
        var multi = r.VarUInt32();
        for (var i = 0; i < multi; i++)
        {
            r.Bytes(16);
            r.VarUInt32();
        }
        var userData = r.VarUInt32();
        for (var i = 0; i < userData; i++)
            Shapeless(ref r, problems);
        for (var i = 0; i < 7; i++)
            Assert.Equal(0u, r.VarUInt32());
        Assert.True(r.Bool());
        Assert.Equal(0, r.Remaining);
        Assert.True(problems.Count == 0, string.Join("\n", problems.Take(20)));
    }

    [Fact]
    public void No_recipe_result_carries_a_damage_tag()
    {
        // A pristine tool/armour result with a Damage tag does not match what the client computes: the result
        // slot turns red and the craft is refused client side (same bug fixed in PowerNukkitX).
        var offenders = Enumerable.Range(1, Recipes.Count + 500).Select(i => Recipes.ByNetworkId((uint)i))
            .Where(r => r is not null).SelectMany(r => r!.Outputs)
            .Where(o => o.Nbt is not null && o.Nbt.ContainsKey("Damage")).Select(o => o.Type.Name).Distinct().ToList();
        Assert.True(offenders.Count == 0, string.Join(", ", offenders.Take(10)));
    }

    [Fact]
    public void Trim_data_names_only_known_items()
    {
        var r = new BedrockReader(Recipes.TrimDataPacket);
        Assert.Equal(Recipes.TrimDataId, r.VarUInt32());
        var patterns = r.VarUInt32();
        for (var i = 0; i < patterns; i++)
        {
            Assert.NotNull(Items.ByName(r.String()));
            r.String();
        }
        var materials = r.VarUInt32();
        for (var i = 0; i < materials; i++)
        {
            r.String();
            r.String();
            Assert.NotNull(Items.ByName(r.String()));
        }
        Assert.Equal(0, r.Remaining);
    }

    [Fact]
    public void Recipe_holes_are_empty_ingredients()
    {
        var chestplate = Enumerable.Range(1, Recipes.Count + 500).Select(i => Recipes.ByNetworkId((uint)i))
            .First(r => r is not null && r.Outputs[0].Type.Name == "minecraft:diamond_chestplate")!;
        Assert.Equal(1, chestplate.Inputs.Count(i => i.Empty));
    }

    private static void Shapeless(ref BedrockReader r, List<string> problems)
    {
        r.String();
        var inputs = r.VarUInt32();
        for (var j = 0; j < inputs; j++)
            Descriptor(ref r, problems);
        Outputs(ref r, problems);
        r.Bytes(16);
        r.String();
        r.VarInt32();
        Unlock(ref r);
        r.VarUInt32();
    }

    private static void Unlock(ref BedrockReader r)
    {
        if (!r.Bool())
            return;
        r.VarInt32();
        if (r.Bool())
            throw new InvalidDataException("unexpected unlock ingredients");
    }

    private static void Descriptor(ref BedrockReader r, List<string> problems)
    {
        var variant = r.VarUInt32();
        if (variant == 0)
            r.VarInt32();
        else
        {
            Assert.Equal(1u, variant);
            var type = r.String();
            var value = r.String();
            if (type == "name")
            {
                r.VarInt32();
                if (Items.ByName(value) is null)
                    problems.Add($"unknown input item {value}");
            }
            else
            {
                Assert.Equal("item_tag", type);
                r.VarInt32();
            }
        }
        r.VarInt32();
    }

    private static void Outputs(ref BedrockReader r, List<string> problems)
    {
        var outputs = r.VarUInt32();
        for (var j = 0; j < outputs; j++)
        {
            var network = r.VarInt32();
            r.Bytes(2);
            r.VarUInt32();
            r.VarInt32();
            r.ByteArray();
            if (Items.ByRuntimeId((short)network) is null)
                problems.Add($"unknown output runtime id {network}");
        }
    }

    [Fact]
    public void Plugins_add_shaped_recipes_sent_with_the_others()
    {
        var before = Recipes.CraftingDataPacket.Length;
        Recipes.AddShaped(["AAA", "ABA", "AAA"], new Dictionary<char, string> { ['A'] = "minecraft:dirt", ['B'] = "tag:minecraft:planks" }, ItemStack.Of("diamond")!);
        var recipe = Recipes.ByResult("minecraft:diamond").ToArray().Last();
        Assert.Same(recipe, Recipes.ByNetworkId(recipe.NetworkId));
        Assert.Equal(9, recipe.Inputs.Count);
        Assert.True(recipe.Inputs[4].Matches(ItemStack.Of("oak_planks")!));
        Assert.True(Recipes.CraftingDataPacket.Length > before);
    }
}
