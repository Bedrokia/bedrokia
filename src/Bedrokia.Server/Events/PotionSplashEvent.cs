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
/// Fired when a splash or lingering potion impacts the ground and creates an effect cloud/area.
/// </summary>
public sealed class PotionSplashEvent : CancellableEvent
{
    public ulong EntityId { get; }
    public int PotionId { get; }
    public Vector3 Position { get; }
    public bool Lingering { get; }
    public float Radius { get; set; }

    public PotionSplashEvent(ulong entityId, int potionId, Vector3 position, bool lingering, float radius)
    {
        EntityId = entityId;
        PotionId = potionId;
        Position = position;
        Lingering = lingering;
        Radius = radius;
    }
}
