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

using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player selects a different slot on their hotbar.
/// If cancelled, the change is reverted and the client is forced back to <see cref="OldSlot"/>.
/// </summary>
public sealed class PlayerHeldSlotChangeEvent : CancellableEvent
{
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
