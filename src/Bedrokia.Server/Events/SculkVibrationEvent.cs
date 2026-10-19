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

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a sculk sensor detects a vibration.
/// </summary>
public sealed class SculkVibrationEvent : CancellableEvent
{
    public BlockPos Pos { get; }
    public ulong SourceEntityId { get; }
    public string VibrationType { get; }
    public float Distance { get; set; }

    public SculkVibrationEvent(BlockPos pos, ulong sourceEntityId, string vibrationType, float distance)
    {
        Pos = pos;
        SourceEntityId = sourceEntityId;
        VibrationType = vibrationType;
        Distance = distance;
    }
}
