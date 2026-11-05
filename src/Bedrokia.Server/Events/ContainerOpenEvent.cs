// â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
// â”‚  ____  _____ ____  ____   ___  _  __ ___    _                     â”‚
// â”‚ | __ )| ____|  _ \|  _ \ / _ \| |/ /|_ _|  / \                    â”‚
// â”‚ |  _ \|  _| | | | | |_) | | | | ' /  | |  / _ \                   â”‚
// â”‚ | |_) | |___| |_| |  _ <| |_| | . \  | | / ___ \                  â”‚
// â”‚ |____/|_____|____/|_| \_\\___/|_|\_\|___/_/   \_\                 â”‚
// â”‚                                                                   â”‚
// â”‚  Bedrokia - Minecraft: Bedrock Edition server software            â”‚
// â”‚  Licensed under the Apache License 2.0 - see LICENSE              â”‚
// â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜

using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a container is about to open for a player.
/// If cancelled, the window is not opened.
/// </summary>
public sealed class ContainerOpenEvent : CancellableEvent, IPlayerEvent{
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
