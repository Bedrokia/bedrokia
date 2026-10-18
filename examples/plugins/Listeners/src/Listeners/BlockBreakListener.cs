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

namespace Listeners.Listeners;

public sealed class BlockBreakListener(ListenersPlugin plugin)
{
    public void OnBlockBreak(BlockEvent e)
    {
        if (plugin.Config.GetBool("break.notify", true))
            e.Player.SendActionBar($"§7Bloc casse : §e{e.Block.Name} §7({e.X}, {e.Y}, {e.Z})");

        if (plugin.Config.GetBool("break.prevent-bedrock", true) && e.Block.Name == "minecraft:bedrock")
        {
            e.Cancelled = true;
            e.Player.SendMessage("§cTu ne peux pas casser ce bloc.");
        }
    }
}
