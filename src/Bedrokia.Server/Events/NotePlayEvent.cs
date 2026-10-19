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
/// Fired when a note block plays a note.
/// </summary>
public sealed class NotePlayEvent : CancellableEvent
{
    public BedrockConnection? Player { get; }
    public BlockPos Pos { get; }
    public int Instrument { get; set; }
    public int Pitch { get; set; }

    public NotePlayEvent(BedrockConnection? player, BlockPos pos, int instrument, int pitch)
    {
        Player = player;
        Pos = pos;
        Instrument = instrument;
        Pitch = pitch;
    }
}
