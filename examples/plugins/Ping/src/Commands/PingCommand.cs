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
using Bedrokia.Server.Network;

namespace Ping.Commands;

public sealed class PingCommand(PingPlugin plugin)
{
    public bool Execute(CommandSender sender, string[] args)
    {
        if (args.Length > 0 && sender.IsOp)
        {
            var targetName = args[0];
            var target = plugin.Server.Players.FirstOrDefault(p => p.IsInGame && p.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
            if (target is null)
            {
                sender.Reply($"§cJoueur '{targetName}' introuvable.");
                return true;
            }

            sender.Reply($"§aPing de {target.Name} valide (en jeu).");
            return true;
        }

        var message = plugin.Config.GetString("message", "§aPong ! §7({player})");
        sender.Reply(message.Replace("{player}", sender.Name));

        if (sender.Player is { } player)
        {
            var actionbar = plugin.Config.GetString("actionbar", "§ePong !");
            player.SendActionBar(actionbar);
        }

        return true;
    }
}
