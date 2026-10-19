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

public enum EffectChangeCause
{
    Plugin,
    Potion,
    Beacon,
    Milk,
    Expiration,
    Death,
}

/// <summary>
/// Fired when an effect is about to be added, modified or removed from a player.
/// If cancelled, the change is not applied.
/// </summary>
public sealed class PlayerEffectChangeEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public EffectInstance? OldEffect { get; }
    public EffectInstance? NewEffect { get; }
    public EffectChangeCause Cause { get; }

    public PlayerEffectChangeEvent(BedrockConnection player, EffectInstance? oldEffect, EffectInstance? newEffect, EffectChangeCause cause)
    {
        Player = player;
        OldEffect = oldEffect;
        NewEffect = newEffect;
        Cause = cause;
    }
}
