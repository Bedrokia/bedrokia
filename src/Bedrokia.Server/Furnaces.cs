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
    private readonly HashSet<(World World, long Pos)> _activeFurnaces = [];
    private readonly List<(World World, long Pos)> _furnaceScratch = [];

    internal void WatchFurnace((int X, int Y, int Z) at, World world) => _activeFurnaces.Add((world, BlockPos.Pack(at.X, at.Y, at.Z)));
    internal void WatchFurnace(int x, int y, int z, World world) => _activeFurnaces.Add((world, BlockPos.Pack(x, y, z)));

    /// <summary>Closes the container at <paramref name="at"/> for everyone looking into it (it was broken).</summary>
    internal void CloseContainerFor((int X, int Y, int Z) at, World world)
    {
        for (var i = 0; i < _players.Count; i++)
        {
            var player = _players[i];
            if (player.OpenContainerAt == at && player.World == world)
                player.CloseContainer(notifyClient: true);
        }
        _activeFurnaces.Remove((world, BlockPos.Pack(at.X, at.Y, at.Z)));
    }

    internal void TickFurnaces()
    {
        if (_activeFurnaces.Count == 0)
            return;

        _furnaceScratch.Clear();
        foreach (var key in _activeFurnaces)
            _furnaceScratch.Add(key);

        for (var i = 0; i < _furnaceScratch.Count; i++)
        {
            var key = _furnaceScratch[i];
            var (world, packedPos) = key;
            var (x, y, z) = BlockPos.Unpack(packedPos);
            var at = (x, y, z);
            if (world.GetBlockEntity(x, y, z) is not { Items: { } items } furnace || furnace.Id is not ("Furnace" or "BlastFurnace" or "Smoker"))
            {
                _activeFurnaces.Remove(key);
                continue;
            }
            var input = items[0];
            var fuel = items[1];
            var output = items[2];
            var result = input is null ? null : Smelting.Result(furnace.Id, input) is { } name ? ItemStack.Of(name) : null;
            var canCook = result is not null && (output is null || output.StacksWith(result) && output.Count < output.MaxCount);

            if (furnace.BurnTicks <= 0 && canCook && fuel is not null && Smelting.BurnTicks(fuel) is var burn and > 0)
            {
                if (Events.HasFurnaceBurn)
                {
                    var burnEv = new FurnaceBurnEvent(new BlockPos(x, y, z), fuel, burn);
                    if (!Events.OnFurnaceBurn(burnEv))
                        goto skipBurn;
                    burn = burnEv.BurnDuration;
                }
                if (Events.HasFuelBurn)
                {
                    var fuelEv = new FuelBurnEvent(new BlockPos(x, y, z), furnace.Id.ToLowerInvariant(), fuel, burn);
                    if (!Events.OnFuelBurn(fuelEv))
                        goto skipBurn;
                    burn = fuelEv.BurnTicks;
                }
                furnace.BurnTicks = furnace.BurnDuration = burn;
                var container = fuel.Type.Name == "minecraft:lava_bucket" ? ItemStack.Of("bucket") : null;
                fuel.SetCount(fuel.Count - 1);
                items[1] = fuel.Count > 0 ? fuel : container;
                SetLit(world, x, y, z, true);
                SendFurnaceContents(world, at, items);
            }
            skipBurn:
            if (furnace.BurnTicks > 0)
            {
                furnace.BurnTicks--;
                if (canCook && ++furnace.CookTicks >= (furnace.Id == "Furnace" ? Smelting.FurnaceCookTicks : Smelting.FastCookTicks))
                {
                    furnace.CookTicks = 0;
                    if (Events.HasFurnaceSmelt)
                    {
                        var smeltEv = new FurnaceSmeltEvent(new BlockPos(x, y, z), input!, result!);
                        if (!Events.OnFurnaceSmelt(smeltEv))
                            goto skipSmelt;
                        result = smeltEv.Result;
                    }
                    if (Events.HasItemCook)
                    {
                        var cookEv = new ItemCookEvent(new BlockPos(x, y, z), furnace.Id.ToLowerInvariant(), input!, result!);
                        if (!Events.OnItemCook(cookEv))
                            goto skipSmelt;
                        result = cookEv.Output;
                    }
                    input!.SetCount(input.Count - 1);
                    items[0] = input.Count > 0 ? input : null;
                    if (output is null)
                        items[2] = result;
                    else
                        output.SetCount(output.Count + (result?.Count ?? 1));
                    SendFurnaceContents(world, at, items);
                }
                skipSmelt:
                if (!canCook)
                    furnace.CookTicks = 0;
            }
            else
            {
                furnace.CookTicks = 0;
                SetLit(world, x, y, z, false);
                if (!canCook || fuel is null)
                    _activeFurnaces.Remove(key); // idle: nothing to burn or nothing to cook
            }
            world.LoadedChunk(x >> 4, z >> 4)!.Dirty = true;
            if (CurrentTick % 4 == 0)
            {
                for (var pIdx = 0; pIdx < _players.Count; pIdx++)
                {
                    var viewer = _players[pIdx];
                    if (viewer.OpenContainerAt == at && viewer.World == world)
                    {
                        viewer.Send(BedrockConnection.ContainerData(0, furnace.CookTicks), BedrockConnection.ContainerData(1, furnace.BurnTicks),
                            BedrockConnection.ContainerData(2, furnace.BurnDuration));
                    }
                }
            }
        }
    }

    private void SendFurnaceContents(World world, (int X, int Y, int Z) at, Inventory items)
    {
        var packet = InventoryPackets.InventoryContent(BedrockConnection.WindowContainer, items);
        for (var i = 0; i < _players.Count; i++)
        {
            var viewer = _players[i];
            if (viewer.OpenContainerAt == at && viewer.World == world)
                viewer.Send(packet);
        }
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
