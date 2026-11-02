using Bedrokia.Protocol;
using Bedrokia.Server.Game;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when a firework rocket reaches its apex and explodes into particles.
/// </summary>
public sealed class FireworkExplodeEvent(World world, double x, double y, double z, NbtList? explosions = null) : CancellableEvent
{
    /// <summary>The world where the firework exploded.</summary>
    public World World { get; } = world;

    /// <summary>X position of the explosion.</summary>
    public double X { get; } = x;

    /// <summary>Y position of the explosion.</summary>
    public double Y { get; } = y;

    /// <summary>Z position of the explosion.</summary>
    public double Z { get; } = z;

    /// <summary>The explosion NBT structures specifying particle effects.</summary>
    public NbtList? Explosions { get; } = explosions;
}
