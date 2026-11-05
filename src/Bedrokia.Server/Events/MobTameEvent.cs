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

using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player tames an animal (wolf, cat, parrot, horse).
/// </summary>
public sealed class MobTameEvent : CancellableEvent, IPlayerEvent{
    public BedrockConnection Player { get; }
    public ulong EntityId { get; }
    public string Identifier { get; }

    public MobTameEvent(BedrockConnection player, ulong entityId, string identifier)
    {
        Player = player;
        EntityId = entityId;
        Identifier = identifier;
    }
}
