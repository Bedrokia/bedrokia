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
/// Fired when a player uses a smithing table to upgrade an item.
/// </summary>
public sealed class SmithingUseEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public ItemStack Input { get; }
    public ItemStack Result { get; set; }

    public SmithingUseEvent(BedrockConnection player, ItemStack input, ItemStack result)
    {
        Player = player;
        Input = input;
        Result = result;
    }
}
