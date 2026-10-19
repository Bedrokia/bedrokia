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

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when an entity or mob is killed.
/// If cancelled, the entity survives with 1 HP and drops/XP are suppressed.
/// </summary>
public sealed class EntityKilledEvent : CancellableEvent
{
    public ulong EntityId { get; }
    public string Identifier { get; }
    public ulong KillerEntityId { get; }
    public int Experience { get; set; }
    public bool DropItems { get; set; }
    public List<ItemStack> Drops { get; }

    public EntityKilledEvent(ulong entityId, string identifier, ulong killerEntityId, List<ItemStack> drops, int experience = 0, bool dropItems = true)
    {
        EntityId = entityId;
        Identifier = identifier;
        KillerEntityId = killerEntityId;
        Drops = drops;
        Experience = experience;
        DropItems = dropItems;
    }
}
