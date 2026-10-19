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
/// Fired when a player's hunger or saturation level changes.
/// If cancelled, the change is reverted and resynchronized with the client.
/// </summary>
public sealed class PlayerHungerChangeEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public int OldFood { get; }
    public int NewFood { get; set; }
    public float NewSaturation { get; set; }

    public PlayerHungerChangeEvent(BedrockConnection player, int oldFood, int newFood, float newSaturation)
    {
        Player = player;
        OldFood = oldFood;
        NewFood = newFood;
        NewSaturation = newSaturation;
    }
}
