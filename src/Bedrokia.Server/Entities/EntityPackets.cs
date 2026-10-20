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
    public const long FlagOnFire = 1L << 0,
        FlagShowName = 1L << 14,
        FlagAlwaysShowName = 1L << 15,
        FlagCanClimb = 1L << 16,
        FlagBaby = 1L << 24,
        FlagTamed = 1L << 32,
        FlagBreathing = 1L << 35,
        FlagSaddled = 1L << 43,
        FlagHasCollision = 1L << 48,
        FlagGravity = 1L << 49,
        FlagFireImmune = 1L << 50,
        FlagChested = 1L << 53,
        FlagCharged = 1L << 54;

    public static byte[] AddActor(long entityId, string type, double x, double y, double z, float yaw, float health,
        float maxHealth, float speed, float width, float height, long flags, int? colour = null, int? variant = null,
        float scale = 1f, string? nameTag = null, bool alwaysShowName = false, int? markVariant = null, int? skinId = null,
        IReadOnlyList<(uint Index, int Value)>? intProperties = null,
        IReadOnlyList<(uint Index, float Value)>? floatProperties = null,
        IReadOnlyList<(long VehicleId, long RiderId, byte Type, bool Immediate, bool RiderInitiated)>? links = null)
    {
        var w = new BedrockWriter().VarUInt32(AddActorId).VarInt64(entityId).VarUInt64((ulong)entityId).String(type)
            .Vec3((float)x, (float)y, (float)z).Vec3(0, 0, 0)
            .Float32(0).Float32(yaw).Float32(yaw).Float32(yaw);
        w.VarUInt32(2); // attributes: name, min, value, max
        w.String("minecraft:health").Float32(0).Float32(health).Float32(maxHealth);
        w.String("minecraft:movement").Float32(0).Float32(speed).Float32(float.MaxValue);
        Metadata(w, flags, width, height, colour, variant, scale, nameTag, alwaysShowName, markVariant, skinId);
        WriteProperties(w, intProperties, floatProperties);
        if (links is { Count: > 0 })
        {
            w.VarUInt32((uint)links.Count);
            foreach (var l in links)
                w.VarInt64(l.VehicleId).VarInt64(l.RiderId).Byte(l.Type).Bool(l.Immediate).Bool(l.RiderInitiated);
        }
        else
        {
            w.VarUInt32(0);
        }
        return w.ToArray();
    }

    public static byte[] SetData(long entityId, long flags, float width, float height, int? colour = null,
        float scale = 1f, string? nameTag = null, bool alwaysShowName = false, int? variant = null, int? markVariant = null, int? skinId = null,
        IReadOnlyList<(uint Index, int Value)>? intProperties = null,
        IReadOnlyList<(uint Index, float Value)>? floatProperties = null)
    {
        var w = new BedrockWriter().VarUInt32(SetActorDataId).VarUInt64((ulong)entityId);
        Metadata(w, flags, width, height, colour, variant, scale, nameTag, alwaysShowName, markVariant, skinId);
        WriteProperties(w, intProperties, floatProperties);
        return w.VarUInt64(0).ToArray();
    }

    // Key, type (+ legacy type byte), value; keys from gophertunnel entity_metadata.go.
    private static void Metadata(BedrockWriter w, long flags, float width, float height, int? colour, int? variant = null,
        float scale = 1f, string? nameTag = null, bool alwaysShowName = false, int? markVariant = null, int? skinId = null)
    {
        const uint typeByte = 0, typeInt16 = 1, typeInt32 = 2, typeFloat = 3, typeString = 4, typeInt64 = 7;
        void Key(uint key, uint type) => w.VarUInt32(key).VarUInt32(type).Byte((byte)type);
        
        uint count = 6u;
        if (colour is not null) count++;
        if (variant is not null) count++;
        if (nameTag is not null) count++;
        if (alwaysShowName) count++;
        if (markVariant is not null) count++;
        if (skinId is not null) count++;

        w.VarUInt32(count);
        Key(0, typeInt64); w.VarInt64(flags);
        Key(7, typeInt16); w.Int16(300); // air supply
        Key(42, typeInt16); w.Int16(300); // max air supply
        Key(38, typeFloat); w.Float32(scale);
        Key(53, typeFloat); w.Float32(width);
        Key(54, typeFloat); w.Float32(height);
        if (colour is { } c) { Key(3, typeByte); w.Byte((byte)c); }
        if (variant is { } v) { Key(2, typeInt32); w.VarInt32(v); }
        if (nameTag is not null) { Key(4, typeString); w.String(nameTag); }
        if (alwaysShowName) { Key(81, typeByte); w.Byte(1); }
        if (markVariant is { } mv) { Key(43, typeInt32); w.VarInt32(mv); }
        if (skinId is { } si) { Key(103, typeInt32); w.VarInt32(si); }
    }

    private static void WriteProperties(BedrockWriter w, IReadOnlyList<(uint Index, int Value)>? intProps, IReadOnlyList<(uint Index, float Value)>? floatProps)
    {
        if (intProps is { Count: > 0 })
        {
            w.VarUInt32((uint)intProps.Count);
            foreach (var (idx, val) in intProps)
                w.VarUInt32(idx).VarInt32(val);
        }
        else
        {
            w.VarUInt32(0);
        }

        if (floatProps is { Count: > 0 })
        {
            w.VarUInt32((uint)floatProps.Count);
            foreach (var (idx, val) in floatProps)
                w.VarUInt32(idx).Float32(val);
        }
        else
        {
            w.VarUInt32(0);
        }
    }

    public static byte[] EntityAttributes(long entityId, float health, float maxHealth, float speed, float underwaterSpeed = 0.02f, float flyingSpeed = 0.4f, float knockbackResistance = 0f)
    {
        var attributes = new (string Name, float Min, float Max, float Value, float Default)[]
        {
            ("minecraft:health", 0, maxHealth, health, maxHealth),
            ("minecraft:movement", 0, float.MaxValue, speed, speed),
            ("minecraft:underwater_movement", 0, float.MaxValue, underwaterSpeed, underwaterSpeed),
            ("minecraft:flying_speed", 0, float.MaxValue, flyingSpeed, flyingSpeed),
            ("minecraft:knockback_resistance", 0, 1.0f, knockbackResistance, 0f),
        };
        var w = new BedrockWriter().VarUInt32(PacketId.UpdateAttributes).VarUInt64((ulong)entityId).VarUInt32((uint)attributes.Length);
        foreach (var (name, min, max, value, def) in attributes)
            w.Float32(min).Float32(max).Float32(value).Float32(min).Float32(max).Float32(def).String(name).VarUInt32(0);
        return w.VarUInt64(0).ToArray();
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

    /// <summary>Respawn state 1 (ready to spawn) at the eye position.</summary>
    public static byte[] Respawn(long entityId, float x, float y, float z) =>
        new BedrockWriter().VarUInt32(RespawnId).Vec3(x, y, z).Byte(1).VarUInt64((ulong)entityId).ToArray();

    private static byte Angle(float degrees) => (byte)(int)Math.Round(((degrees % 360) + 360) % 360 / (360f / 256f));
}
