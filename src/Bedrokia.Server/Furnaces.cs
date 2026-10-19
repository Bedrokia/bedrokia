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
using Bedrokia.Server.Network;

namespace Bedrokia.Server;

/// <summary>Furnaces cooking and containers closing for their viewers (vanilla furnace rules, Dragonfly block/smelter.go).</summary>
public sealed partial class BedrokiaServer
{
    // Furnaces that may be cooking; one stays here until it has nothing left to do.
    private readonly HashSet<(World World, (int X, int Y, int Z) At)> _furnaces = [];

    internal void WatchFurnace((int X, int Y, int Z) at, World world) => _furnaces.Add((world, at));

    /// <summary>Closes the container at <paramref name="at"/> for everyone looking into it (it was broken).</summary>
    internal void CloseContainerFor((int X, int Y, int Z) at, World world)
    {
        foreach (var player in _players.Where(p => p.OpenContainerAt == at && p.World == world))
            player.CloseContainer(notifyClient: true);
        _furnaces.Remove((world, at));
    }

    private void TickFurnaces()
    {
        foreach (var key in _furnaces.ToList())
        {
            var (world, at) = key;
            var (x, y, z) = at;
            if (world.GetBlockEntity(x, y, z) is not { Items: { } items } furnace || furnace.Id is not ("Furnace" or "BlastFurnace" or "Smoker"))
            {
                _furnaces.Remove(key);
                continue;
            }
            var input = items[0];
            var fuel = items[1];
            var output = items[2];
            var result = input is null ? null : Smelting.Result(furnace.Id, input) is { } name ? ItemStack.Of(name) : null;
            var canCook = result is not null && (output is null || output.StacksWith(result) && output.Count < output.MaxCount);

            if (furnace.BurnTicks <= 0 && canCook && fuel is not null && Smelting.BurnTicks(fuel) is var burn and > 0)
            {
                furnace.BurnTicks = furnace.BurnDuration = burn;
                var container = fuel.Type.Name == "minecraft:lava_bucket" ? ItemStack.Of("bucket") : null;
                fuel.SetCount(fuel.Count - 1);
                items[1] = fuel.Count > 0 ? fuel : container;
                SetLit(world, x, y, z, true);
                SendFurnaceContents(world, at, items);
            }
            if (furnace.BurnTicks > 0)
            {
                furnace.BurnTicks--;
                if (canCook && ++furnace.CookTicks >= (furnace.Id == "Furnace" ? Smelting.FurnaceCookTicks : Smelting.FastCookTicks))
                {
                    furnace.CookTicks = 0;
                    input!.SetCount(input.Count - 1);
                    items[0] = input.Count > 0 ? input : null;
                    if (output is null)
                        items[2] = result;
                    else
                        output.SetCount(output.Count + 1);
                    SendFurnaceContents(world, at, items);
                }
                else if (!canCook)
                    furnace.CookTicks = 0;
            }
            else
            {
                furnace.CookTicks = 0;
                SetLit(world, x, y, z, false);
                if (!canCook || fuel is null)
                    _furnaces.Remove(key); // idle: nothing to burn or nothing to cook
            }
            world.LoadedChunk(x >> 4, z >> 4)!.Dirty = true;
            if (CurrentTick % 4 == 0)
                foreach (var viewer in _players.Where(p => p.OpenContainerAt == at && p.World == world))
                    viewer.Send(BedrockConnection.ContainerData(0, furnace.CookTicks), BedrockConnection.ContainerData(1, furnace.BurnTicks),
                        BedrockConnection.ContainerData(2, furnace.BurnDuration));
        }
    }

    private void SendFurnaceContents(World world, (int X, int Y, int Z) at, Inventory items)
    {
        var packet = InventoryPackets.InventoryContent(BedrockConnection.WindowContainer, items);
        foreach (var viewer in _players.Where(p => p.OpenContainerAt == at && p.World == world))
            viewer.Send(packet);
    }

    // furnace <-> lit_furnace (same facing), and the same for blast furnaces and smokers.
    private void SetLit(World world, int x, int y, int z, bool lit)
    {
        if (world.GetBlock(x, y, z) is not { } block || !block.Name.StartsWith("minecraft:"))
            return;
        var name = block.Name["minecraft:".Length..];
        var target = lit ? (name.StartsWith("lit_") ? null : "lit_" + name) : (name.StartsWith("lit_") ? name[4..] : null);
        if (target is null || Blocks.ByKey(new BlockType(0, "minecraft:" + target, block.States).Key) is not { } swapped)
            return;
        var entity = world.GetBlockEntity(x, y, z);
        world.SetBlock(x, y, z, swapped);
        world.SetBlockEntity(x, y, z, entity); // same furnace, new look
        BroadcastIn(world, Packets.UpdateBlock(x, y, z, swapped.NetworkId));
    }
}
