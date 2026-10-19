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
/// Fired when a portal structure is formed (nether portal or end portal).
/// </summary>
public sealed class PortalCreateEvent : CancellableEvent
{
    public string PortalType { get; }
    public List<BlockPos> Blocks { get; }
    public BedrockConnection? Creator { get; }

    public PortalCreateEvent(string portalType, List<BlockPos> blocks, BedrockConnection? creator)
    {
        PortalType = portalType;
        Blocks = blocks;
        Creator = creator;
    }
}
