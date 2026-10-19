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

using Bedrokia.Server.Entities;
using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when an entity or mob dies.
/// Plugins may modify <see cref="Drops"/> and <see cref="DroppedXp"/> before loot is spawned.
/// </summary>
public sealed class EntityDeathEvent
{
    public Mob Entity { get; }
    public BedrockConnection? Killer { get; }
    public List<ItemStack> Drops { get; }
    public int DroppedXp { get; set; }

    public EntityDeathEvent(Mob entity, BedrockConnection? killer, List<ItemStack> drops, int droppedXp)
    {
        Entity = entity;
        Killer = killer;
        Drops = drops;
        DroppedXp = droppedXp;
    }
}
