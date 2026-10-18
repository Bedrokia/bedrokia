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

using Bedrokia.Server.Commands;
using Bedrokia.Server.Plugins;
using Ping.Commands;

namespace Ping;

public sealed class PingPlugin : Plugin
{
    private PingCommand _pingCommand = null!;

    public override void OnEnable()
    {
        _pingCommand = new PingCommand(this);
        Log("Plugin Ping active.");
    }

    public override bool OnCommand(CommandSender sender, string command, string[] args)
    {
        return command switch
        {
            "ping" or "pong" => _pingCommand.Execute(sender, args),
            _ => false,
        };
    }
}
