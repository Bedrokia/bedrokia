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

using System.Net.Sockets;
using System.Runtime.InteropServices;
using Bedrokia.Server;
using Bedrokia.Server.Core;

// Everything is relative to the working directory, laid out like PocketMine-MP (see ServerLayout).
Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("""
     ____  _____ ____  ____   ___  _  __ ___    _
    | __ )| ____|  _ \|  _ \ / _ \| |/ /|_ _|  / \
    |  _ \|  _| | | | | |_) | | | | ' /  | |  / _ \
    | |_) | |___| |_| |  _ <| |_| | . \  | | / ___ \
    |____/|_____|____/|_| \_\\___/|_|\_\|___/_/   \_\
    """);
Console.ResetColor();
Console.WriteLine($"    {Product.Name} {BedrokiaServer.Version} - Minecraft: Bedrock Edition server{Environment.NewLine}");

ServerConfig config;
try
{
    config = ServerConfig.LoadOrCreate(ServerConfig.DefaultPath);
    ServerLayout.Prepare(config.WorldsDirectory);
}
catch (Exception e) when (e is InvalidDataException or IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Invalid configuration: {e.Message}");
    return 1;
}

using var logger = new Logger(config.LogLevel, "server.log");
AppDomain.CurrentDomain.UnhandledException += (_, e) => logger.Error($"Fatal error: {e.ExceptionObject}");

var server = new BedrokiaServer(config, logger);

// Ctrl+C, docker stop and systemd all end in a clean shutdown.
using var sigint = PosixSignalRegistration.Create(PosixSignal.SIGINT, ctx => { ctx.Cancel = true; server.Stop(); });
using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx => { ctx.Cancel = true; server.Stop(); });

new Thread(() =>
{
    // ReadLine returns null when there is no console (e.g. container without -i): the server keeps running.
    while (Console.ReadLine() is { } line)
        server.EnqueueCommand(line);
}) { IsBackground = true, Name = "Console" }.Start();

try
{
    await server.RunAsync();
    return 0;
}
catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
{
    logger.Error($"UDP port {config.Port} is already in use. Change server-port in server.properties.");
    return 1;
}
catch (Exception e)
{
    logger.Error($"Fatal error: {e}");
    return 1;
}
