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

namespace Bedrokia.Server.Core;

/// <summary>
/// Server folder layout, like PocketMine-MP: server.properties, ops.txt, white-list.txt, banned-players.txt,
/// players/, worlds/, plugins/, plugin_data/, resource_packs/ and server.log in the working directory.
/// </summary>
public static class ServerLayout
{
    /// <summary>Creates the folders and moves files left by older Bedrokia versions (config/, worlds/*/players/).</summary>
    public static void Prepare(string worldsDirectory)
    {
        foreach (var directory in new[] { "players", "plugins", "plugin_data", "resource_packs", "data" })
            Directory.CreateDirectory(directory);

        MoveIfMissing(Path.Combine("config", "ops.txt"), "ops.txt");
        MoveIfMissing(Path.Combine("config", "whitelist.txt"), "white-list.txt");
        MoveIfMissing(Path.Combine("config", "banned-players.txt"), "banned-players.txt");
        MoveIfMissing(Path.Combine("config", "permissions.json"), "permissions.json");
        MoveIfMissing(Path.Combine("config", "nethernet-identity.pem"), Path.Combine("data", "nethernet-identity.pem"));
        if (Directory.Exists(worldsDirectory))
            foreach (var oldPlayers in Directory.GetDirectories(worldsDirectory).Select(w => Path.Combine(w, "players")).Where(Directory.Exists))
            {
                foreach (var file in Directory.GetFiles(oldPlayers, "*.json"))
                    MoveIfMissing(file, Path.Combine("players", Path.GetFileName(file)));
                DeleteIfEmpty(oldPlayers);
            }
        // server.json was converted into server.properties by ServerConfig.LoadOrCreate.
        if (File.Exists("server.properties") && File.Exists(Path.Combine("config", "server.json")))
            File.Move(Path.Combine("config", "server.json"), Path.Combine("data", "server.json.old"), overwrite: true);
        DeleteIfEmpty("config");
    }

    private static void MoveIfMissing(string from, string to)
    {
        if (File.Exists(from) && !File.Exists(to))
            File.Move(from, to);
    }

    private static void DeleteIfEmpty(string directory)
    {
        try
        {
            if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                Directory.Delete(directory);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Locked by a sync tool (OneDrive...) or an explorer window: an empty leftover folder is harmless.
        }
    }
}
