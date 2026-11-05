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
/// Fired when the in-game world time changes or is set.
/// </summary>
public sealed class TimeChangeEvent(World world, long oldTime, long newTime) : CancellableEvent
{
    /// <summary>The world whose time changed.</summary>
    public World World { get; } = world;

    /// <summary>The previous time in ticks.</summary>
    public long OldTime { get; } = oldTime;

    /// <summary>The new time in ticks; can be modified.</summary>
    public long NewTime { get; set; } = newTime;
}

