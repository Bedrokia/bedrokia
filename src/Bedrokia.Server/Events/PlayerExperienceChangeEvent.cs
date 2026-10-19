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
/// Fired when a player's experience points or level changes.
/// </summary>
public sealed class PlayerExperienceChangeEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public int Level { get; }
    public int TotalXp { get; }
    public int Amount { get; set; }

    public PlayerExperienceChangeEvent(BedrockConnection player, int level, int totalXp, int amount)
    {
        Player = player;
        Level = level;
        TotalXp = totalXp;
        Amount = amount;
    }
}
