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

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bedrokia.Server.Core;

public enum GameMode
{
    Survival = 0,
    Creative = 1,
    Adventure = 2,
    Spectator = 6, // Bedrock's id for spectator
}

public enum Difficulty
{
    Peaceful = 0,
    Easy = 1,
    Normal = 2,
    Hard = 3,
}

/// <summary>Server settings, stored in <c>server.properties</c> (same format and key names as PocketMine-MP).</summary>
public sealed class ServerConfig
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, // typos in the file are reported, not ignored
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>First line shown in the client's server list.</summary>
    public string Motd { get; set; } = "Bedrokia Server";

    /// <summary>Second line shown in the client's server list.</summary>
    public string LevelName { get; set; } = "world";

    public int Port { get; set; } = 19132;

    /// <summary>World seed; null picks a random one when the world is created (then kept in its level.json).</summary>
    public long? Seed { get; set; }

    /// <summary>Folder holding the worlds; the world itself is in a subfolder named <see cref="LevelName"/>.</summary>
    public string WorldsDirectory { get; set; } = "worlds";
    public int MaxPlayers { get; set; } = 20;
    public GameMode GameMode { get; set; } = GameMode.Survival;

    /// <summary>
    /// Verify players with Xbox Live. Turning it off lets anyone join under any name: only for local tests
    /// or networks you fully control.
    /// </summary>
    public bool OnlineMode { get; set; } = true;

    /// <summary>Maximum chunk radius sent to players.</summary>
    public int ViewDistance { get; set; } = 8;

    /// <summary>
    /// UDP ports of the players' WebRTC (NetherNet) connections: "auto" (default), every player on the UDP port with
    /// the same number as <see cref="Port"/>; or "first-last", one port of the range per player (the old way).
    /// </summary>
    public string WebRtcPorts { get; set; } = "auto";

    /// <summary>Every player shares the UDP port <see cref="Port"/> ("auto").</summary>
    public bool SharesWebRtcPort => WebRtcPorts.Equals("auto", StringComparison.OrdinalIgnoreCase);

    /// <summary>IPv4 address players reach the server at, given to WebRTC; empty = the machine's own (set it behind Docker/NAT).</summary>
    public string PublicIp { get; set; } = "";

    public (int First, int Last) WebRtcPortRange() =>
        SharesWebRtcPort
            ? (Port, Port)
            : ParsePortRange(WebRtcPorts) ?? throw new InvalidDataException("webrtc-ports must be auto or look like 19132-19160.");

    /// <summary>The range for the WebRTC library; null when the port is shared or with server-port 0 (tests).</summary>
    internal SIPSorcery.Sys.PortRange? ParseWebRtcPorts()
    {
        var (first, last) = WebRtcPortRange();
        if (SharesWebRtcPort || first == 0)
            return null;
        first += first & 1; // SIPSorcery needs an even first port (RTP/RTCP pairs); an odd server-port crashed at startup
        return new SIPSorcery.Sys.PortRange(first, Math.Max(first + 1, last));
    }

    public Difficulty Difficulty { get; set; } = Difficulty.Normal;
    public bool Pvp { get; set; } = true;
    public bool SpawnAnimals { get; set; } = true;
    public bool SpawnMonsters { get; set; } = true;

    /// <summary>Radius around the world spawn where only operators may build (0 = off).</summary>
    public int SpawnProtection { get; set; } = 16;

    /// <summary>Players always join in <see cref="GameMode"/> instead of their saved one.</summary>
    public bool ForceGameMode { get; set; }

    public bool KeepInventory { get; set; }

    /// <summary>Looks for a newer Bedrokia release on GitHub and says so in the console.</summary>
    public bool CheckUpdates { get; set; } = true;

    /// <summary>Players must accept the resource packs to join.</summary>
    public bool ForceResourcePacks { get; set; }

    /// <summary>Seconds between automatic saves of the world and the players.</summary>
    public int AutosaveInterval { get; set; } = 60;

    private static (int First, int Last)? ParsePortRange(string value)
    {
        var parts = value.Split('-');
        return parts.Length == 2 && int.TryParse(parts[0], out var first) && int.TryParse(parts[1], out var last)
               && first is >= 1024 and <= 65535 && last >= first && last <= 65535
            ? (first, last)
            : null;
    }

    public LogLevel LogLevel { get; set; } = LogLevel.Info;
    /// <summary>Only players in white-list.txt (and operators) may join; toggle in game with "whitelist on|off".</summary>
    public bool Whitelist { get; set; }

    public const string DefaultPath = "server.properties";

    // server.properties keys: PocketMine-MP names where the setting exists there, then Bedrokia-specific ones.
    private static readonly (string Key, string Comment, Func<ServerConfig, string> Get, Action<ServerConfig, string> Set)[] Keys =
    [
        ("motd", "Name shown in the server list", c => c.Motd, (c, v) => c.Motd = v),
        ("server-port", "Port players join on (TCP; UDP from the same number, see webrtc-ports)", c => c.Port.ToString(), (c, v) => c.Port = int.Parse(v)),
        ("max-players", "", c => c.MaxPlayers.ToString(), (c, v) => c.MaxPlayers = int.Parse(v)),
        ("gamemode", "survival, creative or adventure (for new players)", c => c.GameMode.ToString().ToLowerInvariant(), (c, v) => c.GameMode = ParseGameMode(v)),
        ("white-list", "on/off: only players in white-list.txt may join", c => OnOff(c.Whitelist), (c, v) => c.Whitelist = ParseBool(v)),
        ("xbox-auth", "on/off: verify players with Xbox Live (off = anyone can use any name)", c => OnOff(c.OnlineMode), (c, v) => c.OnlineMode = ParseBool(v)),
        ("view-distance", "Maximum chunk radius sent to players (2-32)", c => c.ViewDistance.ToString(), (c, v) => c.ViewDistance = int.Parse(v)),
        ("level-name", "World folder inside worlds/", c => c.LevelName, (c, v) => c.LevelName = v),
        ("level-seed", "Seed for a NEW world (number or text, empty = random); an existing world keeps its seed", c => c.Seed?.ToString() ?? "", (c, v) => c.Seed = ParseSeed(v)),
        ("difficulty", "peaceful, easy, normal or hard", c => c.Difficulty.ToString().ToLowerInvariant(), (c, v) => c.Difficulty = Enum.Parse<Difficulty>(v, ignoreCase: true)),
        ("pvp", "on/off: players can hurt each other", c => OnOff(c.Pvp), (c, v) => c.Pvp = ParseBool(v)),
        ("spawn-animals", "on/off: animals spawn naturally", c => OnOff(c.SpawnAnimals), (c, v) => c.SpawnAnimals = ParseBool(v)),
        ("spawn-monsters", "on/off: monsters spawn at night (never in peaceful)", c => OnOff(c.SpawnMonsters), (c, v) => c.SpawnMonsters = ParseBool(v)),
        ("spawn-protection", "Radius around the world spawn where only operators can build (0 = off)", c => c.SpawnProtection.ToString(), (c, v) => c.SpawnProtection = int.Parse(v)),
        ("force-gamemode", "on/off: players always join in the gamemode above instead of their saved one", c => OnOff(c.ForceGameMode), (c, v) => c.ForceGameMode = ParseBool(v)),
        ("keep-inventory", "on/off: players keep their items when they die", c => OnOff(c.KeepInventory), (c, v) => c.KeepInventory = ParseBool(v)),
        ("force-resources", "on/off: players must accept the server's resource packs to join", c => OnOff(c.ForceResourcePacks), (c, v) => c.ForceResourcePacks = ParseBool(v)),
        ("autosave-interval", "Seconds between automatic saves", c => c.AutosaveInterval.ToString(), (c, v) => c.AutosaveInterval = int.Parse(v)),
        ("webrtc-ports", "UDP port(s) for players: auto = all on the UDP port of server-port, or first-last = one port per player", c => c.WebRtcPorts, (c, v) => c.WebRtcPorts = v),
        ("public-ip", "IPv4 players connect to, for a server in Docker or behind NAT (Pterodactyl); empty = this machine's addresses", c => c.PublicIp, (c, v) => c.PublicIp = v),
        ("check-updates", "on/off: tell in the console when a new Bedrokia version is released", c => OnOff(c.CheckUpdates), (c, v) => c.CheckUpdates = ParseBool(v)),
        ("log-level", "debug, info, warn or error", c => c.LogLevel.ToString().ToLowerInvariant(), (c, v) => c.LogLevel = Enum.Parse<LogLevel>(v, ignoreCase: true)),
    ];

    private static string OnOff(bool value) => value ? "on" : "off";

    private static bool ParseBool(string value) => value.ToLowerInvariant() switch
    {
        "on" or "true" or "yes" or "1" => true,
        "off" or "false" or "no" or "0" => false,
        _ => throw new FormatException("expected on or off"),
    };

    private static GameMode ParseGameMode(string value)
    {
        var mode = value.ToLowerInvariant() switch
        {
            "0" or "survival" => GameMode.Survival,
            "1" or "creative" => GameMode.Creative,
            "2" or "adventure" => GameMode.Adventure,
            _ => throw new FormatException("expected survival, creative or adventure"),
        };
        return mode;
    }

    /// <summary>Empty = random; numbers as is; text hashed like Java's String.hashCode (as vanilla does).</summary>
    internal static long? ParseSeed(string value)
    {
        if (value.Length == 0)
            return null;
        if (long.TryParse(value, out var number))
            return number;
        var hash = 0;
        foreach (var c in value)
            hash = unchecked(31 * hash + c);
        return hash;
    }

    /// <summary>Writes the current values back (also used by commands that change settings).</summary>
    public void Save(string path = DefaultPath)
    {
        var lines = new List<string> { $"# {Product.Name} server settings", $"# {Product.CreatedBy} - {DateTime.Now:yyyy-MM-dd HH:mm}" };
        foreach (var (key, comment, get, _) in Keys)
        {
            if (comment.Length > 0)
                lines.Add($"# {comment}");
            lines.Add($"{key}={get(this)}");
        }
        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(directory);
        File.WriteAllLines(path + ".tmp", lines);
        File.Move(path + ".tmp", path, overwrite: true);
    }

    /// <summary>
    /// Loads <paramref name="path"/> (key=value, '#' comments), or writes the defaults there if it does not exist.
    /// A config/server.json from older Bedrokia versions is converted on first start.
    /// </summary>
    /// <exception cref="InvalidDataException">Unknown key or invalid value.</exception>
    public static ServerConfig LoadOrCreate(string path)
    {
        if (!File.Exists(path))
        {
            var legacy = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, "config", "server.json");
            var config = File.Exists(legacy) ? LoadLegacyJson(legacy) : new ServerConfig();
            config.Save(path);
            return config;
        }

        var loaded = new ServerConfig();
        var lineNumber = 0;
        var upgrade = false;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in File.ReadAllLines(path))
        {
            lineNumber++;
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith('!'))
                continue;
            var separator = line.IndexOf('=');
            if (separator <= 0)
                throw new InvalidDataException($"{path}:{lineNumber}: expected key=value.");
            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (key is "enable-nethernet" or "enable-raknet")
            {
                upgrade = true; // settings of older versions (RakNet was removed, NetherNet is always on)
                continue;
            }
            if (key == "webrtc-ports" && value == "19140-19239")
            {
                upgrade = true; // old default: now "auto" (same port number as server-port)
                continue;
            }
            seen.Add(key);
            var entry = Keys.FirstOrDefault(k => k.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (entry.Key is null)
                throw new InvalidDataException($"{path}:{lineNumber}: unknown setting '{key}'.");
            try
            {
                entry.Set(loaded, value);
            }
            catch (Exception e) when (e is FormatException or OverflowException or ArgumentException)
            {
                throw new InvalidDataException($"{path}:{lineNumber}: invalid value for {key} ({e.Message}).");
            }
        }
        loaded.Validate(path);
        // Add the settings this version knows but the file does not have yet, and drop obsolete ones.
        if (upgrade || Keys.Any(k => !seen.Contains(k.Key)))
            loaded.Save(path);
        return loaded;
    }

    private static ServerConfig LoadLegacyJson(string path)
    {
        var options = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() },
        };
        try
        {
            var config = JsonSerializer.Deserialize<ServerConfig>(File.ReadAllText(path), options)
                         ?? throw new InvalidDataException($"{path}: file is empty.");
            config.Validate(path);
            return config;
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"{path}: {e.Message}", e);
        }
    }

    internal void Validate(string source)
    {
        if (Port is < 1 or > 65535)
            throw new InvalidDataException($"{source}: Port must be between 1 and 65535 (got {Port}).");
        if (MaxPlayers < 1)
            throw new InvalidDataException($"{source}: MaxPlayers must be at least 1 (got {MaxPlayers}).");
        if (!WebRtcPorts.Equals("auto", StringComparison.OrdinalIgnoreCase) && ParsePortRange(WebRtcPorts) is null)
            throw new InvalidDataException($"{source}: webrtc-ports must be auto or look like 19132-19160 (ports 1024-65535).");
        if (AutosaveInterval < 10)
            throw new InvalidDataException($"{source}: autosave-interval must be at least 10 seconds.");
        if (SpawnProtection < 0)
            throw new InvalidDataException($"{source}: spawn-protection cannot be negative.");
        if (ViewDistance is < 2 or > 32)
            throw new InvalidDataException($"{source}: ViewDistance must be between 2 and 32 (got {ViewDistance}).");
        if (!Enum.IsDefined(GameMode))
            throw new InvalidDataException($"{source}: GameMode must be Survival, Creative or Adventure.");
        // ';' is the field separator of the server-list pong.
        if (LevelName.Length == 0 || LevelName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || LevelName is "." or "..")
            throw new InvalidDataException($"{source}: LevelName must be a valid folder name.");
        if (Motd.Contains(';') || LevelName.Contains(';'))
            throw new InvalidDataException($"{source}: Motd and LevelName cannot contain ';'.");
    }
}
