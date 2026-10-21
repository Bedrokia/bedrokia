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

namespace Bedrokia.Server.Entities;

/// <summary>Entity packets (layouts from gophertunnel, MIT, protocol 2193).</summary>
internal static class EntityPackets
{
    public const uint AddActorId = 13, MoveActorAbsoluteId = 18, ActorEventId = 27, SetActorMotionId = 40, SetActorDataId = 39,
        AnimateId = 44, RespawnId = 45;

    public const byte EventHurt = 2, EventDeath = 3;
    public const byte EventLoveParticles = 21;
    // Actor flag bits (gophertunnel entity_metadata.go).
    public const long FlagOnFire = 1L << 0, FlagInLove = 1L << 7, FlagIgnited = 1L << 10, FlagBaby = 1L << 11, FlagAngry = 1L << 25,
        FlagSheared = 1L << 31, FlagSaddled = 1L << 8, FlagTamed = 1L << 28, FlagWasdControlled = 1L << 44, FlagCanPowerJump = 1L << 45, FlagBreathing = 1L << 35, FlagHasCollision = 1L << 48, FlagGravity = 1L << 49;

    public static byte[] AddActor(long entityId, string type, double x, double y, double z, float yaw, float health,
        float maxHealth, float speed, float width, float height, long flags, int? colour = null, int? variant = null, float scale = 1, string? nameTag = null,
        OrderedDictionary<string, object>? displayNbt = null, long? leashHolder = null, float pitch = 0, bool showName = true)
    {
        var w = new BedrockWriter().VarUInt32(AddActorId).VarInt64(entityId).VarUInt64((ulong)entityId).String(type)
            .Vec3((float)x, (float)y, (float)z).Vec3(0, 0, 0)
            .Float32(pitch).Float32(yaw).Float32(yaw).Float32(yaw);
        w.VarUInt32(2); // attributes: name, min, value, max
        w.String("minecraft:health").Float32(0).Float32(health).Float32(maxHealth);
        w.String("minecraft:movement").Float32(0).Float32(speed).Float32(float.MaxValue);
        Metadata(w, flags, width, height, colour, variant, scale, nameTag, displayNbt, leashHolder, showName);
        return w.VarUInt32(0).VarUInt32(0) // entity properties (ints, floats)
            .VarUInt32(0) // links
            .ToArray();
    }

    /// <summary>SetActorData with a container (keys 44, 45): what the client opens on this entity, and how many slots.</summary>
    public static byte[] ContainerData(long entityId, sbyte containerType, int size)
    {
        const uint keyContainerType = 44, keyContainerSize = 45, typeByte = 0, typeInt32 = 2;
        return new BedrockWriter().VarUInt32(SetActorDataId).VarUInt64((ulong)entityId).VarUInt32(2)
            .VarUInt32(keyContainerType).VarUInt32(typeByte).Byte((byte)typeByte).Byte(unchecked((byte)containerType))
            .VarUInt32(keyContainerSize).VarUInt32(typeInt32).Byte((byte)typeInt32).VarInt32(size)
            .VarUInt32(0).VarUInt32(0).VarUInt64(0).ToArray();
    }

    /// <summary>SetActorData with only the target (key 6): who a mob attacks (0: nobody).</summary>
    public static byte[] SetTarget(long entityId, long targetId)
    {
        const uint keyTarget = 6, typeInt64 = 7;
        return new BedrockWriter().VarUInt32(SetActorDataId).VarUInt64((ulong)entityId)
            .VarUInt32(1).VarUInt32(keyTarget).VarUInt32(typeInt64).Byte((byte)typeInt64).VarInt64(targetId)
            .VarUInt32(0).VarUInt32(0).VarUInt64(0).ToArray();
    }

    public static byte[] SetData(long entityId, long flags, float width, float height, int? colour = null, float scale = 1, string? nameTag = null, long? leashHolder = null)
    {
        var w = new BedrockWriter().VarUInt32(SetActorDataId).VarUInt64((ulong)entityId);
        Metadata(w, flags, width, height, colour, scale: scale, nameTag: nameTag, leashHolder: leashHolder);
        return w.VarUInt32(0).VarUInt32(0).VarUInt64(0).ToArray();
    }

    // Key, type (+ legacy type byte), value; keys from gophertunnel entity_metadata.go.
    private static void Metadata(BedrockWriter w, long flags, float width, float height, int? colour, int? variant = null, float scale = 1,
        string? nameTag = null, OrderedDictionary<string, object>? displayNbt = null, long? leashHolder = null, bool showName = true)
    {
        const uint typeString = 4, typeCompound = 5;
        if (nameTag is not null && showName)
            flags |= (1L << 14) | (1L << 15); // show name, always show name
        if (leashHolder is not null)
            flags |= 1L << 30; // leashed
        const uint typeByte = 0, typeInt16 = 1, typeInt32 = 2, typeFloat = 3, typeInt64 = 7;
        void Key(uint key, uint type) => w.VarUInt32(key).VarUInt32(type).Byte((byte)type);
        w.VarUInt32(6u + (colour is null ? 0u : 1u) + (variant is null ? 0u : 1u) + (nameTag is null ? 0u : 1u) + (displayNbt is null ? 0u : 1u) + (leashHolder is null ? 0u : 1u));
        Key(0, typeInt64); w.VarInt64(flags);
        Key(7, typeInt16); w.Int16(300); // air supply
        Key(42, typeInt16); w.Int16(300); // max air supply
        if (nameTag is not null)
        {
            Key(4, typeString); w.String(nameTag);
        }
        if (displayNbt is not null) // a firework rocket's item NBT: the stars it bursts into
        {
            Key(16, typeCompound); w.Bytes(Nbt.Write(displayNbt, network: true));
        }
        if (leashHolder is { } holder) // the entity holding the lead: a player or a leash knot
        {
            Key(37, typeInt64); w.VarInt64(holder);
        }
        Key(38, typeFloat); w.Float32(scale); // 0.5 for babies
        Key(53, typeFloat); w.Float32(width);
        Key(54, typeFloat); w.Float32(height);
        if (colour is { } c)
        {
            Key(3, typeByte); w.Byte((byte)c);
        }
        if (variant is { } v) // falling block: the block's network id
        {
            Key(2, typeInt32); w.VarInt32(v);
        }
    }

    public static byte[] MoveAbsolute(long entityId, double x, double y, double z, float pitch, float yaw, bool onGround, bool teleport = false) =>
        new BedrockWriter().VarUInt32(MoveActorAbsoluteId).VarUInt64((ulong)entityId)
            .Byte((byte)((onGround ? 1 : 0) | (teleport ? 2 : 0)))
            .Vec3((float)x, (float)y, (float)z)
            .Byte(Angle(pitch)).Byte(Angle(yaw)).Byte(Angle(yaw))
            .ToArray();

    public static byte[] Event(long entityId, byte type, int data = 0) =>
        new BedrockWriter().VarUInt32(ActorEventId).VarUInt64((ulong)entityId).Byte(type).VarInt32(data).Bool(false).ToArray();

    public static byte[] Motion(long entityId, double vx, double vy, double vz) =>
        new BedrockWriter().VarUInt32(SetActorMotionId).VarUInt64((ulong)entityId).Vec3((float)vx, (float)vy, (float)vz).VarUInt64(0).ToArray();

    /// <summary>Animate: 1 = swing arm.</summary>
    public static byte[] Swing(long entityId) =>
        new BedrockWriter().VarUInt32(AnimateId).Byte(1).VarUInt64((ulong)entityId).Float32(0).Bool(false).ToArray();

    /// <summary>Critical hit stars around the entity hit (Animate action 4).</summary>
    public static byte[] CriticalHit(long entityId) =>
        new BedrockWriter().VarUInt32(AnimateId).Byte(4).VarUInt64((ulong)entityId).Float32(0).Bool(false).ToArray();

    /// <summary>Respawn state 1 (ready to spawn) at the eye position.</summary>
    public static byte[] Respawn(long entityId, float x, float y, float z) =>
        new BedrockWriter().VarUInt32(RespawnId).Vec3(x, y, z).Byte(1).VarUInt64((ulong)entityId).ToArray();

    private static byte Angle(float degrees) => (byte)(int)Math.Round(((degrees % 360) + 360) % 360 / (360f / 256f));
}
