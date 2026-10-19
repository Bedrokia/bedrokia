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

namespace Bedrokia.Server.Events;

/// <summary>
/// Base class for events that can be cancelled by plugins or handlers.
/// When cancelled, the server skips or undoes the associated action.
/// </summary>
public abstract class CancellableEvent
{
    /// <summary>Gets or sets whether this event has been cancelled.</summary>
    public bool Cancelled { get; set; }

    /// <summary>Cancels the event.</summary>
    public void Cancel() => Cancelled = true;

    /// <summary>Uncancels the event.</summary>
    public void Uncancel() => Cancelled = false;
}
