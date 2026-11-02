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

using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player selects a different slot on their hotbar.
/// If cancelled, the change is reverted and the client is forced back to <see cref="OldSlot"/>.
/// </summary>
public sealed class PlayerHeldSlotChangeEvent : CancellableEvent, IPlayerEvent{
    public BedrockConnection Player { get; }
    public int OldSlot { get; }
    public int NewSlot { get; set; }

    public PlayerHeldSlotChangeEvent(BedrockConnection player, int oldSlot, int newSlot)
    {
        Player = player;
        OldSlot = oldSlot;
        NewSlot = newSlot;
    }
}
