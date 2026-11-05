// â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
// â”‚  ____  _____ ____  ____   ___  _  __ ___    _                     â”‚
// â”‚ | __ )| ____|  _ \|  _ \ / _ \| |/ /|_ _|  / \                    â”‚
// â”‚ |  _ \|  _| | | | | |_) | | | | ' /  | |  / _ \                   â”‚
// â”‚ | |_) | |___| |_| |  _ <| |_| | . \  | | / ___ \                  â”‚
// â”‚ |____/|_____|____/|_| \_\\___/|_|\_\|___/_/   \_\                 â”‚
// â”‚                                                                   â”‚
// â”‚  Bedrokia - Minecraft: Bedrock Edition server software            â”‚
// â”‚  Licensed under the Apache License 2.0 - see LICENSE              â”‚
// â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜

using Bedrokia.Server.Entities;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>A player hit (<see cref="Attack"/>) or used an NPC. NPCs never take damage.</summary>
public sealed class NpcInteractEvent(BedrockConnection player, Npc npc, bool attack) : CancellableEvent, IPlayerEvent{
    public BedrockConnection Player => player;
    public Npc Npc => npc;
    public bool Attack => attack;
}
