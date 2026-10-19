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

namespace Bedrokia.Server.Entities;

/// <summary>
/// Falling sand and gravel: the block becomes a "minecraft:falling_block" entity that clients animate, then lands
/// on the first solid block (or drops as an item if its landing spot is taken). Physics from vanilla (gravity 0.04,
/// drag 0.98 per tick). Game thread only.
/// </summary>
public sealed class FallingBlocks(BedrokiaServer server)
{
    private sealed class Falling(long entityId, int x, double y, int z, BlockType block)
    {
        public long EntityId { get; } = entityId;
        public int X { get; } = x;
        public int Z { get; } = z;
        public double Y = y;
        public double Vy;
        public BlockType Block { get; } = block;
        public int Ticks;
    }

    /// <summary>Height clients add to a falling block's position (Dragonfly fallingBlockType.NetworkOffset).</summary>
    private const double NetworkOffset = 0.49;

    private readonly List<Falling> _falling = [];

    /// <summary>Number of falling blocks currently active in the world.</summary>
    public int Count => _falling.Count;

    public void Spawn(int x, int y, int z, BlockType block)
    {
        var falling = new Falling(server.NextEntityId(), x, y, z, block);
        _falling.Add(falling);
        server.Broadcast(EntityPackets.AddActor(falling.EntityId, "minecraft:falling_block", x + 0.5, y + NetworkOffset, z + 0.5, 0, 1, 1, 0,
            0.98f, 0.98f, EntityPackets.FlagGravity | EntityPackets.FlagHasCollision, variant: unchecked((int)block.NetworkId)));
    }

    public void Tick()
    {
        foreach (var falling in _falling.ToList())
        {
            falling.Ticks++;
            falling.Vy = falling.Vy * 0.98 - 0.04; // drag before gravity, as Dragonfly
            var next = falling.Y + falling.Vy;
            var floor = (int)Math.Floor(next);
            var landed = BlockPhysics.IsSolid(server.World.GetBlock(falling.X, floor, falling.Z));
            if (!landed && falling.Ticks < 600 && next > Chunk.MinY)
            {
                falling.Y = next;
                server.Broadcast(EntityPackets.MoveAbsolute(falling.EntityId, falling.X + 0.5, falling.Y + NetworkOffset, falling.Z + 0.5, 0, 0, false));
                server.Broadcast(EntityPackets.Motion(falling.EntityId, 0, falling.Vy, 0));
                continue;
            }
            _falling.Remove(falling);
            server.Broadcast(Bedrokia.Protocol.Packets.RemoveActor(falling.EntityId));
            var landY = floor + 1;
            var target = server.World.GetBlock(falling.X, landY, falling.Z);
            if (target is not null && !BlockPhysics.IsSolid(target))
            {
                server.World.SetBlock(falling.X, landY, falling.Z, falling.Block);
                server.Broadcast(Bedrokia.Protocol.Packets.UpdateBlock(falling.X, landY, falling.Z, falling.Block.NetworkId));
            }
            else if (ItemStack.Of(falling.Block.Name) is { } item)
                server.DropItem(item, falling.X + 0.5f, landY + 0.5f, falling.Z + 0.5f);
        }
    }
}
