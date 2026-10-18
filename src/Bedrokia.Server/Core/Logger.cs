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

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error,
}

/// <summary>Thread-safe logger writing to the console and, optionally, to a file (appended).</summary>
public sealed class Logger : IDisposable
{
    private readonly Lock _lock = new();
    private readonly StreamWriter? _file;

    public Logger(LogLevel minLevel, string? filePath)
    {
        MinLevel = minLevel;
        if (filePath is null)
            return;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
        _file = new StreamWriter(filePath, append: true) { AutoFlush = true };
    }

    public LogLevel MinLevel { get; }

    public void Debug(string message) => Log(LogLevel.Debug, message);
    public void Info(string message) => Log(LogLevel.Info, message);
    public void Warn(string message) => Log(LogLevel.Warn, message);
    public void Error(string message) => Log(LogLevel.Error, message);

    public void Log(LogLevel level, string message)
    {
        if (level < MinLevel)
            return;
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level.ToString().ToUpperInvariant()}] {message}";
        lock (_lock)
        {
            (level >= LogLevel.Warn ? Console.Error : Console.Out).WriteLine(line);
            _file?.WriteLine(line);
        }
    }

    public void Dispose()
    {
        lock (_lock)
            _file?.Dispose();
    }
}
