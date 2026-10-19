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


using Bedrokia.Protocol;
using Bedrokia.Server.Entities;

namespace Bedrokia.Server;

/// <summary>World effects for plugins: particles and lightning.</summary>
public sealed partial class BedrokiaServer
{
    /// <summary>Shows a particle effect (e.g. "minecraft:heart_particle", "minecraft:villager_happy",
    /// "minecraft:basic_flame_particle", "minecraft:endrod") to every player.</summary>
    public void SpawnParticle(string name, float x, float y, float z) =>
        Broadcast(new BedrockWriter().VarUInt32(118).Byte(0).VarInt64(-1).Vec3(x, y, z).String(name).Bool(false).ToArray());

    /// <summary>A lightning bolt (visual and thunder, sets nothing on fire) that hurts mobs and players within 3 blocks.</summary>
    public void StrikeLightning(double x, double y, double z)
    {
        var id = NextEntityId();
        Broadcast(EntityPackets.AddActor(id, "minecraft:lightning_bolt", x, y, z, 0, 1, 1, 0, 0, 0, 0));
        Schedule(20, () => Broadcast(Packets.RemoveActor(id)));
        foreach (var player in _players.Where(p => p.IsInGame))
        {
            var (px, py, pz) = player.Position;
            if (Math.Abs(px - x) < 3 && Math.Abs(py - Network.BedrockConnection.EyeHeight - y) < 4 && Math.Abs(pz - z) < 3)
                player.Damage(5, x, z, source: DamageSource.Custom("lightning"));
        }
        foreach (var mob in Mobs.All.ToList())
            if (Math.Abs(mob.X - x) < 3 && Math.Abs(mob.Y - y) < 4 && Math.Abs(mob.Z - z) < 3)
                Mobs.Damage(mob, 5, (x, z), source: DamageSource.Custom("lightning"));
    }
}
