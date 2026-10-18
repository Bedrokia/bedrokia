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

using Bedrokia.Server.Plugins;
using Listeners.Listeners;

namespace Listeners;

public sealed class ListenersPlugin : Plugin
{
    private PlayerJoinListener _joinListener = null!;
    private BlockBreakListener _breakListener = null!;

    public override void OnEnable()
    {
        _joinListener = new PlayerJoinListener(this);
        _breakListener = new BlockBreakListener(this);

        Events.PlayerJoin += _joinListener.OnPlayerJoin;
        Events.BlockBreak += _breakListener.OnBlockBreak;

        Log("Listeners enregistres avec succes.");
    }

    public override void OnDisable()
    {
        if (_joinListener is not null)
            Events.PlayerJoin -= _joinListener.OnPlayerJoin;

        if (_breakListener is not null)
            Events.BlockBreak -= _breakListener.OnBlockBreak;
    }
}
