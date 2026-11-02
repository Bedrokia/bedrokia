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
/// Fired when a player crosses into a new chunk.
/// </summary>
public sealed class PlayerChunkEnterEvent(BedrockConnection player, int chunkX, int chunkZ, int previousChunkX, int previousChunkZ) : IPlayerEvent
{
    /// <summary>The player entering the chunk.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>New chunk X coordinate.</summary>
    public int ChunkX { get; } = chunkX;

    /// <summary>New chunk Z coordinate.</summary>
    public int ChunkZ { get; } = chunkZ;

    /// <summary>Previous chunk X coordinate.</summary>
    public int PreviousChunkX { get; } = previousChunkX;

    /// <summary>Previous chunk Z coordinate.</summary>
    public int PreviousChunkZ { get; } = previousChunkZ;
}

