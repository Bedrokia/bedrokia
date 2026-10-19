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

using System.Numerics;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a projectile hits an entity or block.
/// </summary>
public sealed class ProjectileHitEvent : CancellableEvent
{
    public ulong ProjectileEntityId { get; }
    public string Identifier { get; }
    public ulong OwnerId { get; }
    public ulong TargetEntityId { get; }
    public Vector3 HitPosition { get; }
    public float Damage { get; set; }

    public ProjectileHitEvent(ulong projectileEntityId, string identifier, ulong ownerId, ulong targetEntityId, Vector3 hitPosition, float damage)
    {
        ProjectileEntityId = projectileEntityId;
        Identifier = identifier;
        OwnerId = ownerId;
        TargetEntityId = targetEntityId;
        HitPosition = hitPosition;
        Damage = damage;
    }
}
