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
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player repairs or renames an item in an anvil.
/// </summary>
public sealed class AnvilUseEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public ItemStack Item { get; }
    public string NewName { get; }
    public int CostLevels { get; set; }

    public AnvilUseEvent(BedrockConnection player, ItemStack item, string newName, int costLevels)
    {
        Player = player;
        Item = item;
        NewName = newName;
        CostLevels = costLevels;
    }
}
