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

using Bedrokia.Server.Entities;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>A player hit (<see cref="Attack"/>) or used an NPC. NPCs never take damage.</summary>
public sealed class NpcInteractEvent(BedrockConnection player, Npc npc, bool attack) : CancellableEvent
{
    public BedrockConnection Player => player;
    public Npc Npc => npc;
    public bool Attack => attack;
}
