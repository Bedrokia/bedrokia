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

namespace Bedrokia.Server.Game;

/// <summary>An item lying in the world, waiting to be picked up. Position is the bottom of its box.</summary>
public sealed class DroppedItem(long entityId, ItemStack stack, float x, float y, float z, long spawnTick)
{
    public long EntityId { get; } = entityId;
    public ItemStack Stack { get; } = stack;
    public float X { get; set; } = x;
    public float Y { get; set; } = y;
    public float Z { get; set; } = z;
    public long SpawnTick { get; } = spawnTick;
    public float Vx { get; set; }
    public float Vy { get; set; }
    public float Vz { get; set; }
    public bool OnGround { get; set; }

    /// <summary>Ticks before it can be picked up (40 when thrown by a player, so it does not come straight back).</summary>
    public int PickupDelay { get; set; } = DroppedItems.PickupDelayTicks;

    // What the clients were last told, so that only changes are sent (like Dragonfly's Movement.Send).
    internal (float X, float Y, float Z) SentPosition = (float.NaN, 0, 0);
    internal (float X, float Y, float Z) SentVelocity;
}

/// <summary>
/// Item entities with vanilla physics, ported from Dragonfly (MIT) entity/item.go and entity/movement.go:
/// gravity 0.04 and drag 0.02 per tick (drag applied first), ground friction 0.6, collision with solid blocks.
/// Removed after 5 minutes like vanilla. Game thread only.
/// </summary>
public sealed class DroppedItems(World world)
{
    public const int PickupDelayTicks = 10;
    public const int DespawnTicks = 6000;

    /// <summary>Height clients add to an item's position (Dragonfly itemType.NetworkOffset).</summary>
    public const float NetworkOffset = 0.125f;

    private const float Gravity = 0.04f, Drag = 0.02f, GroundFriction = 0.6f, HalfWidth = 0.125f;

    private readonly List<DroppedItem> _items = [];

    public IReadOnlyList<DroppedItem> All => _items;

    /// <summary>A new item with a small random push, like items popping out of a broken block.</summary>
    public DroppedItem Spawn(long entityId, ItemStack stack, float x, float y, float z, long tick)
    {
        var item = new DroppedItem(entityId, stack, x, y, z, tick)
        {
            Vx = (Random.Shared.NextSingle() - 0.5f) * 0.2f,
            Vy = 0.2f,
            Vz = (Random.Shared.NextSingle() - 0.5f) * 0.2f,
        };
        _items.Add(item);
        return item;
    }

    public void Remove(DroppedItem item) => _items.Remove(item);

    /// <summary>Items older than the despawn delay, removed from the list.</summary>
    public List<DroppedItem> Expire(long tick)
    {
        var expired = _items.Where(i => tick - i.SpawnTick > DespawnTicks || i.Y < Chunk.MinY - 64).ToList();
        foreach (var item in expired)
            _items.Remove(item);
        return expired;
    }

    public void Tick()
    {
        foreach (var item in _items)
        {
            if (world.LoadedChunk((int)MathF.Floor(item.X) >> 4, (int)MathF.Floor(item.Z) >> 4) is null)
                continue; // frozen in unloaded chunks
            item.Vy = item.Vy * (1 - Drag) - Gravity;
            var friction = 1 - Drag;
            if (item.OnGround)
                friction *= GroundFriction;
            item.Vx *= friction;
            item.Vz *= friction;

            // Vertical, then each horizontal axis, stopping on solid blocks.
            var ny = item.Y + item.Vy;
            if (Collides(item.X, ny, item.Z))
            {
                if (item.Vy < 0)
                {
                    item.Y = MathF.Floor(ny) + 1;
                    item.OnGround = true;
                }
                item.Vy = 0;
            }
            else
            {
                item.Y = ny;
                item.OnGround = Collides(item.X, item.Y - 0.01f, item.Z);
            }
            if (Collides(item.X + item.Vx, item.Y, item.Z))
                item.Vx = 0;
            else
                item.X += item.Vx;
            if (Collides(item.X, item.Y, item.Z + item.Vz))
                item.Vz = 0;
            else
                item.Z += item.Vz;
            if (MathF.Abs(item.Vx) < 0.001f)
                item.Vx = 0;
            if (MathF.Abs(item.Vz) < 0.001f)
                item.Vz = 0;
        }
    }

    // The item's 0.25 box against solid blocks.
    private bool Collides(float x, float y, float z)
    {
        for (var bx = (int)MathF.Floor(x - HalfWidth); bx <= (int)MathF.Floor(x + HalfWidth); bx++)
        for (var bz = (int)MathF.Floor(z - HalfWidth); bz <= (int)MathF.Floor(z + HalfWidth); bz++)
        for (var by = (int)MathF.Floor(y); by <= (int)MathF.Floor(y + 0.24f); by++)
            if (Entities.BlockPhysics.IsSolid(world.GetBlock(bx, by, bz)))
                return true;
        return false;
    }
}
