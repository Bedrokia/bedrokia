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

using System.Numerics;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player places an entity into the world (boat, minecart, armor stand, end crystal).
/// </summary>
public sealed class EntityPlaceEvent : CancellableEvent
{
    public BedrockConnection Player { get; }
    public string Identifier { get; }
    public Vector3 Position { get; }

    public EntityPlaceEvent(BedrockConnection player, string identifier, Vector3 position)
    {
        Player = player;
        Identifier = identifier;
        Position = position;
    }
}
