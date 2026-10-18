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
using Bedrokia.Server.Game;
using Bedrokia.Server.Network;
using Bedrokia.Server.Plugins;
using Effects = Bedrokia.Server.Network.BedrockConnection.Effects;

namespace MagicItems;

/// <summary>
/// Example plugin: three special items built on vanilla ones (custom name, lore and glint) with powers.
/// /magie foudre   - lightning staff: right click on a block and lightning strikes it
/// /magie bottes   - speed boots: Speed and Jump Boost while worn
/// /magie tremplin - jump pad block: placed, it launches whoever steps on it
/// Names, cooldown and strengths come from config.yml.
/// </summary>
public sealed class MagicItemsPlugin : Plugin
{
    private readonly HashSet<(int X, int Y, int Z)> _pads = [];
    private readonly HashSet<BedrockConnection> _boosted = [];
    private readonly Dictionary<BedrockConnection, long> _cooldown = [];
    private long _tick;

    private string StaffName => Config.GetString("staff.name", "§e§lBâton de foudre");
    private string BootsName => Config.GetString("boots.name", "§b§lBottes de vitesse");
    private string PadName => Config.GetString("jump-pad.name", "§a§lTremplin");

    private string PadsFile => Path.Combine(DataFolder, "tremplins.txt");

    public override void OnEnable()
    {
        if (File.Exists(PadsFile))
            foreach (var line in File.ReadAllLines(PadsFile))
                if (line.Split(' ') is [var x, var y, var z] && int.TryParse(x, out var px) && int.TryParse(y, out var py) && int.TryParse(z, out var pz))
                    _pads.Add((px, py, pz));

        Events.ItemUse += OnItemUse;
        Events.BlockPlace += e =>
        {
            if (e.Player.HeldItem?.CustomName == PadName)
                _pads.Add((e.X, e.Y, e.Z));
        };
        Events.BlockBreak += e => _pads.Remove((e.X, e.Y, e.Z));
        Events.PlayerMove += OnMove;
        Events.PlayerQuit += player =>
        {
            _boosted.Remove(player);
            _cooldown.Remove(player);
        };
        Events.Tick += OnTick;
        Log($"{_pads.Count} tremplin(s) chargé(s).");
    }

    public override void OnDisable() => File.WriteAllLines(PadsFile, _pads.Select(p => $"{p.X} {p.Y} {p.Z}"));

    // /magie, declared in plugin.yml.
    public override bool OnCommand(CommandSender sender, string command, string[] args)
    {
        if (sender.Player is not { } player)
        {
            sender.Reply("Commande réservée aux joueurs.");
            return true;
        }
        ItemStack? item = args.FirstOrDefault()?.ToLowerInvariant() switch
        {
            "foudre" => Enchanting.With(ItemStack.Of("blaze_rod")!, [(17, 1)]).WithName(StaffName, "§7Clic droit sur un bloc :", "§7la foudre s'abat dessus."),
            "bottes" => Enchanting.With(ItemStack.Of("diamond_boots")!, [(17, 3)]).WithName(BootsName, "§7Vitesse et saut", "§7quand elles sont portées."),
            "tremplin" => ItemStack.Of("slime_block", 16)!.WithName(PadName, "§7Pose-le puis marche dessus."),
            _ => null,
        };
        if (item is null)
            return false; // shows the usage from plugin.yml
        player.GiveItem(item);
        sender.Reply($"§aTu as reçu : {item.CustomName}");
        return true;
    }

    private void OnItemUse(ItemUseEvent e)
    {
        if (e.Item.CustomName != StaffName)
            return;
        e.Cancelled = true; // the staff never does its vanilla thing
        if (_cooldown.GetValueOrDefault(e.Player) > _tick)
        {
            e.Player.SendActionBar("§cLe bâton se recharge...");
            return;
        }
        _cooldown[e.Player] = _tick + Config.GetInt("staff.cooldown", 20);
        Server.StrikeLightning(e.X + 0.5, e.Y + 1, e.Z + 0.5);
        e.Player.SendActionBar("§e⚡ Foudre !");
    }

    private void OnMove(BedrockConnection player)
    {
        var (x, y, z) = player.Position;
        var below = ((int)Math.Floor(x), (int)Math.Floor(y - BedrockConnection.EyeHeight - 0.05), (int)Math.Floor(z));
        if (!_pads.Contains(below) || _cooldown.GetValueOrDefault(player) > _tick)
            return;
        if (Server.World.GetBlock(below.Item1, below.Item2, below.Item3)?.Name != "minecraft:slime_block")
        {
            _pads.Remove(below); // replaced by something else (explosion, plugin...)
            return;
        }
        _cooldown[player] = _tick + 10;
        var yaw = player.Yaw * Math.PI / 180;
        var forward = Config.GetDouble("jump-pad.forward", 1.2);
        player.SetMotion(-Math.Sin(yaw) * forward, Config.GetDouble("jump-pad.up", 1.3), Math.Cos(yaw) * forward);
        for (var i = 0; i < 8; i++)
            Server.SpawnParticle("minecraft:villager_happy", below.Item1 + 0.5f + MathF.Cos(i * MathF.PI / 4) * 0.7f, below.Item2 + 1.1f,
                below.Item3 + 0.5f + MathF.Sin(i * MathF.PI / 4) * 0.7f);
    }

    private void OnTick(long tick)
    {
        _tick = tick;
        if (tick % 40 != 0)
            return;
        foreach (var player in Server.Players.Where(p => p.IsInGame))
        {
            if (player.ArmourItem(3)?.CustomName == BootsName)
            {
                player.AddEffect(Effects.Speed, Config.GetInt("boots.speed-level", 1), 3, particles: false);
                player.AddEffect(Effects.JumpBoost, Config.GetInt("boots.jump-level", 0), 3, particles: false);
                _boosted.Add(player);
            }
            else if (_boosted.Remove(player))
            {
                player.RemoveEffect(Effects.Speed);
                player.RemoveEffect(Effects.JumpBoost);
            }
        }
    }
}
