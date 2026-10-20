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
    /// "minecraft:basic_flame_particle", "minecraft:endrod") to every player of <paramref name="world"/> (the overworld by default).</summary>
    public void SpawnParticle(string name, float x, float y, float z, Game.World? world = null) =>
        BroadcastIn(world ?? World, new BedrockWriter().VarUInt32(118).Byte((byte)(world ?? World).Dimension).VarInt64(-1).Vec3(x, y, z).String(name).Bool(false).ToArray());

    /// <summary>
    /// Plays a sound (e.g. "random.pop", "random.totem", "ambient.weather.thunder", "random.orb") at a position for
    /// the players of <paramref name="world"/> (the overworld by default). PlaySound layout from gophertunnel (MIT).
    /// </summary>
    public void PlaySound(string name, double x, double y, double z, Game.World? world = null, float volume = 1, float pitch = 1) =>
        BroadcastIn(world ?? World, SoundPacket(name, x, y, z, volume, pitch));

    internal static byte[] SoundPacket(string name, double x, double y, double z, float volume, float pitch) =>
        new BedrockWriter().VarUInt32(86).String(name).BlockPos((int)(x * 8), (int)(y * 8), (int)(z * 8)) // position in 1/8 blocks
            .Float32(volume).Float32(pitch).VarInt32(0).Bool(false).Bool(false).Bool(false).ToArray(); // no loop, no handle

    /// <summary>A lightning bolt (visual and thunder, sets nothing on fire) that hurts mobs and players within 3 blocks.</summary>
    public void StrikeLightning(double x, double y, double z, Game.World? world = null)
    {
        world ??= World;
        var id = NextEntityId();
        BroadcastIn(world, EntityPackets.AddActor(id, "minecraft:lightning_bolt", x, y, z, 0, 1, 1, 0, 0, 0, 0));
        Schedule(20, () => BroadcastIn(world, Packets.RemoveActor(id)));
        foreach (var player in _players.Where(p => p.IsInGame && p.World == world))
        {
            var (px, py, pz) = player.Position;
            if (Math.Abs(px - x) < 3 && Math.Abs(py - Network.BedrockConnection.EyeHeight - y) < 4 && Math.Abs(pz - z) < 3)
                player.Damage(5, x, z, source: DamageSource.Custom("lightning"));
        }
        foreach (var mob in Mobs.All.ToList())
            if (mob.World == world && Math.Abs(mob.X - x) < 3 && Math.Abs(mob.Y - y) < 4 && Math.Abs(mob.Z - z) < 3)
                Mobs.Damage(mob, 5, (x, z), source: DamageSource.Custom("lightning"));
    }
}
