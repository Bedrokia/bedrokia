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
using Bedrokia.Server.Plugins;

namespace Showcase;

/// <summary>
/// Example plugin that shows off the API: welcome title, live sidebar, particles and announcements.
/// /fx                - flame spiral around you
/// /annonce &lt;texte&gt; - title on every screen (operators)
/// Texts and sidebar lines come from config.yml.
/// </summary>
public sealed class ShowcasePlugin : Plugin
{
    private readonly Dictionary<BedrockConnection, long> _joinedAt = [];
    private readonly List<(BedrockConnection Player, int Step)> _spirals = [];
    private long _tick;

    public override void OnEnable()
    {
        Events.PlayerJoin += e => OnJoin(e.Player);
        Events.PlayerQuit += e =>
        {
            var player = e.Player;
            _joinedAt.Remove(player);
            _spirals.RemoveAll(s => s.Player == player);
        };
        Events.Tick += OnTick;
        Events.PlayerChat += e =>
        {
            foreach (var word in Config.GetList("highlight"))
                e.Message = e.Message.Replace(word, $"§6§l{word}§r", StringComparison.OrdinalIgnoreCase);
        };
    }

    // /fx and /annonce, declared in plugin.yml.
    public override bool OnCommand(CommandSender sender, string command, string[] args)
    {
        switch (command)
        {
            case "fx":
                if (sender.Player is { } player)
                    _spirals.Add((player, 0));
                return true;
            case "annonce" when args.Length > 0:
                foreach (var target in Server.Players.Where(p => p.IsInGame))
                    target.SendTitle("§c§lANNONCE", "§f" + string.Join(' ', args), stay: 80);
                sender.Reply("§aAnnonce envoyée.");
                return true;
            default:
                return false;
        }
    }

    private void OnJoin(BedrockConnection player)
    {
        _joinedAt[player] = _tick;
        player.SendTitle(Config.GetString("welcome.title", "§6§lBEDROKIA"),
            Config.GetString("welcome.subtitle", "§eBienvenue §f{player} §e!").Replace("{player}", player.Name), fadeIn: 20, stay: 80);
        var (x, y, z) = player.Position;
        for (var i = 0; i < 16; i++)
        {
            var angle = i * MathF.PI / 8;
            Server.SpawnParticle("minecraft:heart_particle", x + MathF.Cos(angle) * 1.5f, y - 0.5f, z + MathF.Sin(angle) * 1.5f);
        }
    }

    private void OnTick(long tick)
    {
        _tick = tick;
        // Spirals: two flames per tick for 2 seconds, climbing around the player.
        for (var i = _spirals.Count - 1; i >= 0; i--)
        {
            var (player, step) = _spirals[i];
            var (x, y, z) = player.Position;
            var angle = step * 0.5f;
            var height = y - BedrockConnection.EyeHeight + step * 0.05f;
            Server.SpawnParticle("minecraft:basic_flame_particle", x + MathF.Cos(angle), height, z + MathF.Sin(angle));
            Server.SpawnParticle("minecraft:basic_flame_particle", x - MathF.Cos(angle), height, z - MathF.Sin(angle));
            if (step >= 40)
                _spirals.RemoveAt(i);
            else
                _spirals[i] = (player, step + 1);
        }
        if (tick % 20 != 0 || !Config.GetBool("sidebar.enabled", true))
            return;
        var online = Server.Players.Count(p => p.IsInGame);
        var time = Server.World.Time % 24000;
        var clock = $"{(time / 1000 + 6) % 24:00}:{time % 1000 * 60 / 1000:00}";
        var title = Config.GetString("sidebar.title", "§6§lBEDROKIA");
        var lines = Config.GetList("sidebar.lines");
        foreach (var player in Server.Players.Where(p => p.IsInGame))
        {
            var (x, y, z) = player.Position;
            var played = (tick - _joinedAt.GetValueOrDefault(player, tick)) / 20;
            player.SetSidebar(title, [.. lines.Select(line => line
                .Replace("{player}", player.Name).Replace("{online}", online.ToString()).Replace("{max}", Server.Config.MaxPlayers.ToString())
                .Replace("{time}", clock).Replace("{x}", ((int)x).ToString()).Replace("{y}", ((int)(y - BedrockConnection.EyeHeight)).ToString())
                .Replace("{z}", ((int)z).ToString()).Replace("{played}", $"{played / 60}m {played % 60:00}s"))]);
        }
    }
}
