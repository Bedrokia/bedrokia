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

using Bedrokia.Server.Core;

namespace Bedrokia.Tests;

public sealed class ServerConfigTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("bedrokia-tests").FullName;
    private string ConfigPath => Path.Combine(_dir, "server.properties");

    [Fact]
    public void Creates_defaults_then_reads_them_back()
    {
        var created = ServerConfig.LoadOrCreate(ConfigPath);
        Assert.True(File.Exists(ConfigPath));

        var loaded = ServerConfig.LoadOrCreate(ConfigPath);
        Assert.Equal(created.Port, loaded.Port);
        Assert.Equal(created.Motd, loaded.Motd);
        Assert.Equal(GameMode.Survival, loaded.GameMode);
    }

    [Fact]
    public void Reads_pocketmine_style_properties()
    {
        Write("""
            # admin comment
            server-port=19200
            gamemode=creative
            white-list=on
            level-seed=hello
            """);
        var config = ServerConfig.LoadOrCreate(ConfigPath);
        Assert.Equal(19200, config.Port);
        Assert.Equal(GameMode.Creative, config.GameMode);
        Assert.True(config.Whitelist);
        Assert.Equal(99162322, config.Seed); // Java "hello".hashCode(), like vanilla text seeds
    }

    [Fact]
    public void Converts_a_legacy_json_config()
    {
        Directory.CreateDirectory(Path.Combine(_dir, "config"));
        File.WriteAllText(Path.Combine(_dir, "config", "server.json"), """{ "Port": 19300, "GameMode": "Creative" }""");

        var config = ServerConfig.LoadOrCreate(ConfigPath);

        Assert.Equal(19300, config.Port);
        Assert.Contains("server-port=19300", File.ReadAllText(ConfigPath));
    }

    [Theory]
    [InlineData("server-port=0")]
    [InlineData("server-port=70000")]
    [InlineData("max-players=0")]
    [InlineData("motd=a;b")]
    [InlineData("gamemode=hardcore")]
    [InlineData("server-prot=19132")]
    [InlineData("white-list=maybe")]
    [InlineData("no equals sign")]
    public void Rejects_invalid_files(string content)
    {
        Write(content);
        Assert.Throws<InvalidDataException>(() => ServerConfig.LoadOrCreate(ConfigPath));
    }

    private void Write(string content) => File.WriteAllText(ConfigPath, content);

    public void Dispose() => Directory.Delete(_dir, recursive: true);
}
