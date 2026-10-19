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

using System.Net.Http.Headers;
using System.Text.Json;

namespace Bedrokia.Server.Core;

/// <summary>
/// Like PocketMine-MP's update notifier: at startup and every few hours, asks GitHub for the latest Bedrokia release
/// and warns in the console when it is newer than the running version, with the link to download it.
/// </summary>
public static class UpdateChecker
{
    public const string LatestReleaseApi = "https://api.github.com/repos/Nexiumpvp2/bedrokia/releases/latest";
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    /// <summary>Checks until <paramref name="stop"/> is cancelled. Network errors are only logged in debug.</summary>
    public static async Task RunAsync(string currentVersion, Logger logger, CancellationToken stop)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(Product.Name, Clean(currentVersion)));
        http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        string? announced = null;
        while (!stop.IsCancellationRequested)
        {
            try
            {
                using var json = JsonDocument.Parse(await http.GetStringAsync(LatestReleaseApi, stop));
                var tag = json.RootElement.GetProperty("tag_name").GetString() ?? "";
                var url = json.RootElement.GetProperty("html_url").GetString() ?? "";
                if (IsNewer(tag, currentVersion) && tag != announced)
                {
                    announced = tag;
                    logger.Warn($"A new {Product.Name} version is available: {tag} (this server runs {Clean(currentVersion)}).");
                    logger.Warn($"Download it: {url}");
                    logger.Warn("To update: stop the server, replace the bedrokia file, start it again.");
                }
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException
                                          or InvalidOperationException)
            {
                if (stop.IsCancellationRequested)
                    return;
                logger.Debug($"Update check failed: {e.Message}");
            }
            try
            {
                await Task.Delay(Interval, stop);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>True when release tag "v1.2.3" is newer than version "1.2.2" (build metadata "+sha" ignored).</summary>
    public static bool IsNewer(string tag, string current) =>
        Version.TryParse(Clean(tag), out var latest) && Version.TryParse(Clean(current), out var running) && latest > running;

    // "v0.3.7" or "0.3.7+abc123" -> "0.3.7"
    private static string Clean(string version) => version.TrimStart('v', 'V').Split('+', '-')[0];
}
