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

namespace Bedrokia.Server.Game.Custom.Blocks;

public sealed record BreathabilityBlockComponent(bool Solid = true) : IBlockComponent
{
    public string Name => BlockComponentIds.Breathability;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["breathable"] = Solid ? "solid" : "air"
    };
}

public sealed record CollisionBoxBlockComponent(
    bool Enabled = true,
    float[]? Origin = null,
    float[]? Size = null) : IBlockComponent
{
    public string Name => BlockComponentIds.CollisionBox;
    public OrderedDictionary<string, object> BuildNbt()
    {
        var dict = new OrderedDictionary<string, object>
        {
            ["enabled"] = (byte)(Enabled ? 1 : 0)
        };
        if (Origin is { Length: 3 } && Size is { Length: 3 })
            dict["boxes"] = BoxComponents.Boxes(Origin, Size);
        return dict;
    }
}

public sealed record SelectionBoxBlockComponent(
    bool Enabled = true,
    float[]? Origin = null,
    float[]? Size = null) : IBlockComponent
{
    public string Name => BlockComponentIds.SelectionBox;
    public OrderedDictionary<string, object> BuildNbt()
    {
        var dict = new OrderedDictionary<string, object>
        {
            ["enabled"] = (byte)(Enabled ? 1 : 0)
        };
        if (Origin is { Length: 3 } && Size is { Length: 3 })
            dict["boxes"] = BoxComponents.Boxes(Origin, Size);
        return dict;
    }
}

public sealed record GeometryBlockComponent(
    string Identifier = "minecraft:geometry.full_block",
    string Culling = "",
    string CullingLayer = "",
    string CullingShape = "") : IBlockComponent
{
    public string Name => BlockComponentIds.Geometry;
    public OrderedDictionary<string, object> BuildNbt()
    {
        var dict = new OrderedDictionary<string, object>
        {
            ["identifier"] = Identifier
        };
        if (!string.IsNullOrEmpty(Culling))
            dict["culling"] = Culling;
        if (!string.IsNullOrEmpty(CullingLayer))
            dict["culling_layer"] = CullingLayer;
        if (!string.IsNullOrEmpty(CullingShape))
            dict["culling_shape"] = CullingShape;
        return dict;
    }
}

public sealed record BlockMaterial(
    string Texture,
    string Target = "*",
    string RenderMethod = "opaque",
    bool FaceDimming = true,
    bool AmbientOcclusion = true,
    string TintMethod = "none")
{
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["texture"] = Texture,
        ["render_method"] = RenderMethod,
        ["face_dimming"] = (byte)(FaceDimming ? 1 : 0),
        ["ambient_occlusion"] = AmbientOcclusion ? 1.0f : 0.0f, // ponytail: TintMethod is not sent, like Dragonfly
    };
}

public sealed record MaterialInstancesBlockComponent(IReadOnlyList<BlockMaterial> Materials) : IBlockComponent
{
    public string Name => BlockComponentIds.MaterialInstances;
    public OrderedDictionary<string, object> BuildNbt()
    {
        var dict = new OrderedDictionary<string, object>();
        foreach (var mat in Materials)
            dict[mat.Target] = mat.BuildNbt();
        return new OrderedDictionary<string, object>
        {
            ["mappings"] = new OrderedDictionary<string, object>(),
            ["materials"] = dict
        };
    }
}

public sealed record DestructibleByMiningBlockComponent(float SecondsToDestroy) : IBlockComponent
{
    public string Name => BlockComponentIds.DestructibleByMining;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["value"] = SecondsToDestroy
    };
}

public sealed record DestructibleByExplosionBlockComponent(float ExplosionResistance) : IBlockComponent
{
    public string Name => BlockComponentIds.DestructibleByExplosion;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["explosion_resistance"] = ExplosionResistance
    };
}

public sealed record DisplayNameBlockComponent(string Value) : IBlockComponent
{
    public string Name => BlockComponentIds.DisplayName;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["value"] = Value
    };
}

public sealed record FrictionBlockComponent(float Friction) : IBlockComponent
{
    public string Name => BlockComponentIds.Friction;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["value"] = Friction
    };
}

public sealed record LightEmissionBlockComponent(byte LightLevel) : IBlockComponent
{
    public string Name => BlockComponentIds.LightEmission;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["emission"] = (int)LightLevel
    };
}

public sealed record LightDampeningBlockComponent(byte LightFilter) : IBlockComponent
{
    public string Name => BlockComponentIds.LightDampening;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["lightLevel"] = (int)LightFilter
    };
}

public sealed record FlammableBlockComponent(int CatchOdds = 5, int BurnOdds = 20) : IBlockComponent
{
    public string Name => BlockComponentIds.Flammable;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["catch_chance_modifier"] = CatchOdds,
        ["destroy_chance_modifier"] = BurnOdds
    };
}

public sealed record ReplaceableBlockComponent : IBlockComponent
{
    public string Name => BlockComponentIds.Replaceable;
    public OrderedDictionary<string, object> BuildNbt() => [];
}

public sealed record CraftingTableBlockComponent(IReadOnlyList<string> CraftingTags, int GridSize = 3) : IBlockComponent
{
    public string Name => BlockComponentIds.CraftingTable;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["crafting_tags"] = new NbtList(Nbt.String, CraftingTags.Cast<object>().ToList()),
        ["grid_size"] = GridSize
    };
}

public sealed record MapColorBlockComponent(string Color) : IBlockComponent
{
    public string Name => BlockComponentIds.MapColor;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["color"] = Color
    };
}

public sealed record TransformationBlockComponent(
    int RotationX = 0,
    int RotationY = 0,
    int RotationZ = 0,
    float ScaleX = 1.0f,
    float ScaleY = 1.0f,
    float ScaleZ = 1.0f,
    float TranslationX = 0.0f,
    float TranslationY = 0.0f,
    float TranslationZ = 0.0f) : IBlockComponent
{
    public string Name => BlockComponentIds.Transformation;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["rotation"] = new NbtList(Nbt.Float, [(float)RotationX, (float)RotationY, (float)RotationZ]),
        ["scale"] = new NbtList(Nbt.Float, [ScaleX, ScaleY, ScaleZ]),
        ["translation"] = new NbtList(Nbt.Float, [TranslationX, TranslationY, TranslationZ])
    };
}

public sealed record PlacementFilterBlockComponent(
    IReadOnlyList<string>? AllowedFaces = null,
    IReadOnlyList<string>? BlockFilter = null) : IBlockComponent
{
    public string Name => BlockComponentIds.PlacementFilter;
    public OrderedDictionary<string, object> BuildNbt()
    {
        var condition = new OrderedDictionary<string, object>();
        if (AllowedFaces is { Count: > 0 })
            condition["allowed_faces"] = new NbtList(Nbt.String, AllowedFaces.Cast<object>().ToList());
        if (BlockFilter is { Count: > 0 })
        {
            var filter = BlockFilter.Select(b => (object)new OrderedDictionary<string, object> { ["name"] = b }).ToList();
            condition["block_filter"] = new NbtList(Nbt.Compound, filter);
        }
        return new OrderedDictionary<string, object>
        {
            ["conditions"] = new NbtList(Nbt.Compound, [condition])
        };
    }
}

public sealed record EntityFallOnBlockComponent(float MinFallDistance = 3.0f) : IBlockComponent
{
    public string Name => BlockComponentIds.EntityFallOn;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["min_fall_distance"] = MinFallDistance
    };
}

public sealed record TickBlockComponent(int Interval = 20, bool Looping = true) : IBlockComponent
{
    public string Name => BlockComponentIds.Tick;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["interval_range"] = new NbtList(Nbt.Int, [Interval, Interval]),
        ["looping"] = (byte)(Looping ? 1 : 0)
    };
}

public sealed record OnInteractBlockComponent(string? Event = null) : IBlockComponent
{
    public string Name => BlockComponentIds.OnInteract;
    public OrderedDictionary<string, object> BuildNbt()
    {
        var dict = new OrderedDictionary<string, object>();
        if (!string.IsNullOrEmpty(Event))
            dict["event"] = Event;
        return dict;
    }
}

public sealed record OnPlayerPlacingBlockComponent(string? Event = null) : IBlockComponent
{
    public string Name => BlockComponentIds.OnPlayerPlacing;
    public OrderedDictionary<string, object> BuildNbt()
    {
        var dict = new OrderedDictionary<string, object>();
        if (!string.IsNullOrEmpty(Event))
            dict["event"] = Event;
        return dict;
    }
}

public sealed record SupportBlockComponent(string Shape = "solid") : IBlockComponent
{
    public string Name => BlockComponentIds.Support;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["shape"] = Shape
    };
}

public sealed record MovableBlockComponent(string MovementType = "all", string StickyType = "none") : IBlockComponent
{
    public string Name => BlockComponentIds.Movable;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["movement_type"] = MovementType,
        ["sticky"] = StickyType
    };
}

public sealed record RedstoneConductivityBlockComponent(bool RedstoneConductor = true) : IBlockComponent
{
    public string Name => BlockComponentIds.RedstoneConductivity;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["redstone_conductor"] = (byte)(RedstoneConductor ? 1 : 0)
    };
}

public sealed record RedstoneProducerBlockComponent(int Power = 15) : IBlockComponent
{
    public string Name => BlockComponentIds.RedstoneProducer;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["power"] = Power
    };
}

public sealed record RedstoneConsumerBlockComponent : IBlockComponent
{
    public string Name => BlockComponentIds.RedstoneConsumer;
    public OrderedDictionary<string, object> BuildNbt() => [];
}

public sealed record ItemVisualBlockComponent(
    GeometryBlockComponent? Geometry = null,
    MaterialInstancesBlockComponent? MaterialInstances = null) : IBlockComponent
{
    public string Name => BlockComponentIds.ItemVisual;
    public OrderedDictionary<string, object> BuildNbt()
    {
        var dict = new OrderedDictionary<string, object>();
        if (Geometry is not null)
            dict["geometry"] = Geometry.BuildNbt();
        if (MaterialInstances is not null)
            dict["material_instances"] = MaterialInstances.BuildNbt();
        return dict;
    }
}

public sealed record LootBlockComponent(string Table) : IBlockComponent
{
    public string Name => BlockComponentIds.Loot;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["table"] = Table
    };
}

public sealed record CustomComponentsBlockComponent(IReadOnlyList<string> Components) : IBlockComponent
{
    public string Name => BlockComponentIds.CustomComponents;
    public OrderedDictionary<string, object> BuildNbt() => new()
    {
        ["components"] = new NbtList(Nbt.String, Components.Cast<object>().ToList())
    };
}

internal static class BoxComponents
{
    // Add-on origin/size (pixels, origin x/z from the block centre) to the network "boxes" list of absolute
    // min/max pixels, as Dragonfly (MIT) server/internal/blockinternal/components.go sends it.
    public static NbtList Boxes(float[] origin, float[] size)
    {
        float minX = origin[0] + 8, minY = origin[1], minZ = origin[2] + 8;
        return new NbtList(Nbt.Compound,
        [
            new OrderedDictionary<string, object>
            {
                ["minX"] = minX, ["minY"] = minY, ["minZ"] = minZ,
                ["maxX"] = minX + size[0], ["maxY"] = minY + size[1], ["maxZ"] = minZ + size[2],
            },
        ]);
    }
}
