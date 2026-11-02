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

using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a player dies.
/// Handlers can customize the death message, adjust keep-inventory rules, or inspect/modify drops.
/// </summary>
public sealed class PlayerDeathEvent(
    BedrockConnection player,
    DamageSource? source,
    string? deathMessage,
    bool keepInventory,
    List<ItemStack> drops) : IPlayerEvent
{
    public BedrockConnection Player => player;
    public DamageSource? Source => source;

    /// <summary>Message broadcast to the server, or null to silence.</summary>
    public string? DeathMessage { get; set; } = deathMessage;

    /// <summary>Whether the player keeps their inventory instead of dropping items on the ground.</summary>
    public bool KeepInventory { get; set; } = keepInventory;

    /// <summary>Items to drop upon death if KeepInventory is false.</summary>
    public List<ItemStack> Drops => drops;
}
