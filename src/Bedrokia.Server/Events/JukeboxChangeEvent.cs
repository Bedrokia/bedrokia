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
/// Fired when a music disc is inserted or ejected from a jukebox.
/// </summary>
public sealed class JukeboxChangeEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public BlockPos Pos { get; }
    public short DiscItemId { get; }
    public bool Inserted { get; }

    public JukeboxChangeEvent(BedrockConnection player, BlockPos pos, short discItemId, bool inserted)
    {
        Player = player;
        Pos = pos;
        DiscItemId = discItemId;
        Inserted = inserted;
    }
}
