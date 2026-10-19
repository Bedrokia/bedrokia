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
/// Fired when a container is about to open for a player.
/// If cancelled, the window is not opened.
/// </summary>
public sealed class ContainerOpenEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public byte WindowId { get; }
    public byte ContainerType { get; }
    public BlockPos Position { get; }
    public string ContainerKind { get; }
    public Inventory Items { get; }

    public ContainerOpenEvent(BedrockConnection player, byte windowId, byte containerType, BlockPos position, string containerKind, Inventory items)
    {
        Player = player;
        WindowId = windowId;
        ContainerType = containerType;
        Position = position;
        ContainerKind = containerKind;
        Items = items;
    }
}
