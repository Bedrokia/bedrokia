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
/// Fired when a player changes dimension (Overworld, Nether, End).
/// If cancelled, the dimension change is aborted.
/// </summary>
public sealed class PlayerDimensionChangeEvent(BedrockConnection player, int fromDimension, int toDimension) : CancellableEvent, IPlayerEvent
{
    /// <summary>The player changing dimension.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>Origin dimension ID (0 = Overworld, 1 = Nether, 2 = The End).</summary>
    public int FromDimension { get; } = fromDimension;

    /// <summary>Destination dimension ID.</summary>
    public int ToDimension { get; set; } = toDimension;
}

