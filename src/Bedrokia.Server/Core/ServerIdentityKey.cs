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

using System.Security.Cryptography;

namespace Bedrokia.Server.Core;

/// <summary>
/// The server's long-lived NetherNet identity key. Clients trust it on first use, so it must survive restarts.
/// </summary>
public static class ServerIdentityKey
{
    /// <summary>Loads the P-384 key from <paramref name="path"/> (PEM), creating and saving a new one if absent.</summary>
    /// <exception cref="InvalidDataException">The file exists but is not a valid key.</exception>
    // Retries briefly while another process is still writing the file.
    private static string ReadShared(string path)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var text = File.ReadAllText(path);
                if (text.Contains("END") || attempt >= 20)
                    return text;
            }
            catch (IOException) when (attempt < 20)
            {
            }
            Thread.Sleep(50);
        }
    }

    public static ECDsa LoadOrCreate(string path)
    {
        var key = ECDsa.Create();
        if (File.Exists(path))
        {
            try
            {
                key.ImportFromPem(ReadShared(path));
            }
            catch (Exception e) when (e is ArgumentException or CryptographicException)
            {
                key.Dispose();
                throw new InvalidDataException($"{path} is not a valid key. Delete it to generate a new one.", e);
            }
            if (key.KeySize == 384)
                return key;
            key.Dispose();
            throw new InvalidDataException($"{path} is not a P-384 key. Delete it to generate a new one.");
        }

        key.GenerateKey(ECCurve.NamedCurves.nistP384);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        try
        {
            // CreateNew: if another server in the same folder creates it at the same time, use theirs.
            using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            file.Write(System.Text.Encoding.ASCII.GetBytes(key.ExportECPrivateKeyPem()));
        }
        catch (IOException) when (File.Exists(path))
        {
            key.Dispose();
            return LoadOrCreate(path);
        }
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite); // private key: owner only
        return key;
    }
}
