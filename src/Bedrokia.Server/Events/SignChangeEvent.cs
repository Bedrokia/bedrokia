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

using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player writes or modifies text on a sign or hanging sign.
/// </summary>
public sealed class SignChangeEvent : CancellableEvent, IPlayerEvent{
    public BedrockConnection Player { get; }
    public BlockPos Pos { get; }
    public bool FrontSide { get; }
    public string Text { get; set; }

    public SignChangeEvent(BedrockConnection player, BlockPos pos, bool frontSide, string text)
    {
        Player = player;
        Pos = pos;
        FrontSide = frontSide;
        Text = text;
    }
}
