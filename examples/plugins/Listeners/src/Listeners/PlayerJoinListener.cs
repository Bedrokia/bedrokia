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

namespace Listeners.Listeners;

public sealed class PlayerJoinListener(ListenersPlugin plugin)
{
    public void OnPlayerJoin(BedrockConnection player)
    {
        var message = plugin.Config.GetString("join.message", "§aBienvenue sur le serveur, {player} !");
        player.SendMessage(message.Replace("{player}", player.Name));

        var title = plugin.Config.GetString("join.title", "§6§lBEDROKIA");
        var subtitle = plugin.Config.GetString("join.subtitle", "§eBon jeu !");
        player.SendTitle(title, subtitle);

        plugin.Log($"Joueur connecte: {player.Name}");
    }
}
