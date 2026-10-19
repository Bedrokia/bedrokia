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
/// Fired when a player edits, signs, or adds a page to a book.
/// </summary>
public sealed class BookEditEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public int Slot { get; }
    public string Action { get; }
    public int Page { get; }
    public string Text { get; set; }

    public BookEditEvent(BedrockConnection player, int slot, string action, int page, string text)
    {
        Player = player;
        Slot = slot;
        Action = action;
        Page = page;
        Text = text;
    }
}
