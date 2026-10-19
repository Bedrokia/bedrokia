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

using System.Net;
using System.Security.Cryptography;
using Bedrokia.Protocol;
using Bedrokia.Server;
using Bedrokia.Server.Core;
using Bedrokia.Server.Events;
using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Tests;

public sealed class StatusEffectsTests
{
    private sealed class FakeTransport : IGameTransport
    {
        public IPEndPoint RemoteEndPoint { get; } = new(IPAddress.Loopback, 19132);
        public ECDsa? PeerPublicKey => null;
        public List<byte[]> Sent { get; } = [];
        public void Send(byte[] batch) => Sent.Add(batch);
        public void Disconnect(string reason) { }
    }

    private static (BedrokiaServer Server, BedrockConnection Player) CreateTestEnvironment()
    {
        var logger = new Logger(LogLevel.Error, null);
        var server = new BedrokiaServer(new ServerConfig { WorldsDirectory = TestWorlds.NewDirectory(), Port = 0, OnlineMode = false }, logger);
        var conn = new BedrockConnection(server, new FakeTransport(), 100);
        return (server, conn);
    }

    [Fact]
    public void AddEffect_And_StackingRules()
    {
        var (server, player) = CreateTestEnvironment();

        // Add Speed I for 100 ticks
        var effect1 = new EffectInstance(EffectType.Speed, amplifier: 0, durationTicks: 100);
        Assert.True(player.AddEffect(effect1));
        Assert.Single(player.GetActiveEffects());
        Assert.Equal(0, player.GetEffect(EffectType.Speed)?.Amplifier);
        Assert.Equal(100, player.GetEffect(EffectType.Speed)?.DurationTicks);

        // Lower amplifier is ignored
        var effectLower = new EffectInstance(EffectType.Speed, amplifier: 0, durationTicks: 50);
        player.AddEffect(effectLower);
        Assert.Equal(100, player.GetEffect(EffectType.Speed)?.DurationTicks);

        // Higher amplifier replaces existing effect
        var effectHigher = new EffectInstance(EffectType.Speed, amplifier: 1, durationTicks: 60);
        Assert.True(player.AddEffect(effectHigher));
        Assert.Equal(1, player.GetEffect(EffectType.Speed)?.Amplifier);
        Assert.Equal(60, player.GetEffect(EffectType.Speed)?.DurationTicks);

        // Equal amplifier with longer duration replaces duration
        var effectLonger = new EffectInstance(EffectType.Speed, amplifier: 1, durationTicks: 200);
        Assert.True(player.AddEffect(effectLonger));
        Assert.Equal(200, player.GetEffect(EffectType.Speed)?.DurationTicks);
    }

    [Fact]
    public void Regeneration_Ticks_HealPlayer()
    {
        var (server, player) = CreateTestEnvironment();
        player.SetFood(10, 0);
        player.Damage(10, 0, 0, armour: false);
        var initialHealth = player.Health;
        Assert.True(initialHealth < BedrockConnection.MaxHealth);

        // Regen I (amplifier 0) heals 1 HP every 50 ticks
        player.AddEffect(new EffectInstance(EffectType.Regeneration, amplifier: 0, durationTicks: 100));

        // Tick 49: no heal yet
        for (var i = 0; i < 49; i++)
            player.TickHealth();
        Assert.Equal(initialHealth, player.Health);

        // Tick 50: heals 1 HP
        player.TickHealth();
        Assert.Equal(initialHealth + 1, player.Health);
    }

    [Fact]
    public void Poison_DoesNotDropBelowOneHealth()
    {
        var (server, player) = CreateTestEnvironment();
        player.SetFood(10, 0);
        // Lower health to 1.5
        player.Damage(18.5f, 0, 0, armour: false);
        Assert.Equal(1.5f, player.Health);

        // Poison I (amplifier 0) damages 1 HP every 25 ticks
        player.AddEffect(new EffectInstance(EffectType.Poison, amplifier: 0, durationTicks: 100));

        for (var i = 0; i < 25; i++)
            player.TickHealth();

        // 1.5 - 1 = 0.5 -> but poison cannot drop below 1 HP!
        Assert.Equal(1f, player.Health);
        Assert.False(player.Dead);

        // Another 25 ticks: health should stay at 1
        for (var i = 0; i < 25; i++)
            player.TickHealth();

        Assert.Equal(1f, player.Health);
        Assert.False(player.Dead);
    }

    [Fact]
    public void Wither_CanKillPlayer()
    {
        var (server, player) = CreateTestEnvironment();
        player.SetFood(10, 0);
        player.Damage(19.5f, 0, 0, armour: false);
        Assert.Equal(0.5f, player.Health);

        // Wither I (amplifier 0) deals 1 damage every 40 ticks
        player.AddEffect(new EffectInstance(EffectType.Wither, amplifier: 0, durationTicks: 100));

        for (var i = 0; i < 40; i++)
            player.TickHealth();

        Assert.True(player.Dead);
        Assert.Equal(0, player.Health);
    }

    [Fact]
    public void Strength_And_Weakness_ModifyMeleeDamage()
    {
        var (server, attacker) = CreateTestEnvironment();
        attacker.Position = (0f, 0f, 0f);
        attacker.IsInGame = true;

        var victim = new BedrockConnection(server, new FakeTransport(), 101)
        {
            Position = (1f, 0f, 0f),
            IsInGame = true
        };
        victim.SetFood(10, 0);
        server.AddPlayer(victim);
        server.Config.Pvp = true;

        // Base hand damage = 1
        attacker.AttackEntity(victim.EntityId);
        Assert.Equal(19f, victim.Health);

        // Add Strength I (amplifier 0): +3 damage -> total 1 + 3 = 4 damage
        attacker.AddEffect(new EffectInstance(EffectType.Strength, amplifier: 0, durationTicks: 200));
        for (var i = 0; i < 10; i++) victim.TickHealth(); // Clear hurt ticks

        attacker.AttackEntity(victim.EntityId);
        Assert.Equal(15f, victim.Health); // 19 - 4 = 15

        // Add Weakness I (amplifier 0): -4 damage -> total 4 - 4 = 0 damage
        attacker.AddEffect(new EffectInstance(EffectType.Weakness, amplifier: 0, durationTicks: 200));
        for (var i = 0; i < 10; i++) victim.TickHealth(); // Clear hurt ticks

        attacker.AttackEntity(victim.EntityId);
        Assert.Equal(15f, victim.Health); // No damage taken
    }

    [Fact]
    public void Resistance_ReducesIncomingDamage()
    {
        var (server, player) = CreateTestEnvironment();
        // Resistance II (amplifier 1) = 40% damage reduction
        player.AddEffect(new EffectInstance(EffectType.Resistance, amplifier: 1, durationTicks: 200));

        player.Damage(10, 0, 0, armour: false);

        // 10 damage reduced by 40% is 6 damage taken -> health 20 - 6 = 14
        Assert.Equal(14f, player.Health);
    }

    [Fact]
    public void PlayerEffectChangeEvent_CanBeCancelled()
    {
        var (server, player) = CreateTestEnvironment();
        var fired = false;

        server.Events.PlayerEffectChange += e =>
        {
            fired = true;
            Assert.Equal(EffectType.Speed, e.NewEffect?.Type);
            e.Cancel();
        };

        var result = player.AddEffect(new EffectInstance(EffectType.Speed, amplifier: 0, durationTicks: 100));
        Assert.True(fired);
        Assert.False(result);
        Assert.Null(player.GetEffect(EffectType.Speed));
    }

    [Fact]
    public void ClearEffects_OnDeath_And_MilkBucket()
    {
        var (server, player) = CreateTestEnvironment();
        player.AddEffect(new EffectInstance(EffectType.Speed, amplifier: 0, durationTicks: 200));
        player.AddEffect(new EffectInstance(EffectType.Regeneration, amplifier: 0, durationTicks: 200));
        Assert.Equal(2, player.GetActiveEffects().Count);

        // Clear via ClearEffects (simulating milk bucket)
        player.ClearEffects();
        Assert.Empty(player.GetActiveEffects());

        // Re-add effect and kill player
        player.AddEffect(new EffectInstance(EffectType.Strength, amplifier: 0, durationTicks: 200));
        Assert.NotEmpty(player.GetActiveEffects());

        player.Damage(100, 0, 0, armour: false);
        Assert.True(player.Dead);
        Assert.Empty(player.GetActiveEffects());
    }
}
