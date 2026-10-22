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

namespace Bedrokia.Server.Game.Custom.Blocks;

public sealed class CustomBlockBuilder
{
    private readonly string _identifier;
    private string _material = "dirt";
    private CreativeInfo _creative = new(CreativeCategory.Construction);
    private readonly Dictionary<string, IBlockComponent> _components = [];
    private readonly List<BlockProperty> _properties = [];
    private readonly List<BlockPermutation> _permutations = [];
    private readonly List<string> _tags = [];

    public CustomBlockBuilder(string identifier)
    {
        _identifier = identifier.Contains(':') ? identifier : "custom:" + identifier;
        var texture = _identifier.Contains(':') ? _identifier[(_identifier.IndexOf(':') + 1)..] : _identifier;

        // Defaults as Dragonfly (MIT) server/internal/blockinternal sends them: a full cube needs no collision or
        // selection box, and breathability no longer exists in Bedrock.
        AddComponent(new GeometryBlockComponent());
        AddComponent(new MaterialInstancesBlockComponent([new BlockMaterial(texture)]));
        AddComponent(new DisplayNameBlockComponent(_identifier));
        AddComponent(new DestructibleByMiningBlockComponent(1.5f));
        AddComponent(new FrictionBlockComponent(0.6f));
    }

    public static CustomBlockBuilder Create(string identifier) => new(identifier);

    public CustomBlockBuilder WithMaterial(string material)
    {
        _material = material;
        return this;
    }

    public CustomBlockBuilder WithDisplayName(string name)
    {
        AddComponent(new DisplayNameBlockComponent(name));
        return this;
    }

    public CustomBlockBuilder SetDisplayName(string name) => WithDisplayName(name);
    public CustomBlockBuilder SetTexture(string texture, string renderMethod = "opaque") => WithTexture(texture, renderMethod);
    public CustomBlockBuilder SetGeometry(string geometryIdentifier) => WithGeometry(geometryIdentifier);
    public CustomBlockBuilder SetDestructibleByMining(float secondsToDestroy) => WithHardness(secondsToDestroy);
    public CustomBlockBuilder SetLightEmission(byte lightLevel) => WithLightEmission(lightLevel);
    public CustomBlockBuilder SetLightDampening(byte lightFilter) => WithLightDampening(lightFilter);
    public CustomBlockBuilder SetFriction(float friction) => WithFriction(friction);
    public CustomBlockBuilder SetCollision(bool enabled = true, float[]? origin = null, float[]? size = null) => WithCollision(enabled, origin, size);
    public CustomBlockBuilder SetSelectionBox(bool enabled = true, float[]? origin = null, float[]? size = null) => WithSelectionBox(enabled, origin, size);

    public CustomBlockBuilder WithTexture(string texture, string renderMethod = "opaque")
    {
        AddComponent(new MaterialInstancesBlockComponent([new BlockMaterial(texture, RenderMethod: renderMethod)]));
        return this;
    }

    public CustomBlockBuilder WithMaterials(params BlockMaterial[] materials)
    {
        AddComponent(new MaterialInstancesBlockComponent(materials));
        return this;
    }

    public CustomBlockBuilder WithGeometry(string geometryIdentifier)
    {
        AddComponent(new GeometryBlockComponent(geometryIdentifier));
        return this;
    }

    public CustomBlockBuilder WithHardness(float secondsToDestroy)
    {
        AddComponent(new DestructibleByMiningBlockComponent(secondsToDestroy));
        return this;
    }

    public CustomBlockBuilder WithBlastResistance(float resistance)
    {
        AddComponent(new DestructibleByExplosionBlockComponent(resistance));
        return this;
    }

    public CustomBlockBuilder WithLightEmission(byte lightLevel)
    {
        AddComponent(new LightEmissionBlockComponent(lightLevel));
        return this;
    }

    public CustomBlockBuilder WithLightDampening(byte lightFilter)
    {
        AddComponent(new LightDampeningBlockComponent(lightFilter));
        return this;
    }

    public CustomBlockBuilder WithFriction(float friction)
    {
        AddComponent(new FrictionBlockComponent(friction));
        return this;
    }

    public CustomBlockBuilder WithCollision(bool enabled = true, float[]? origin = null, float[]? size = null)
    {
        AddComponent(new CollisionBoxBlockComponent(enabled, origin, size));
        return this;
    }

    public CustomBlockBuilder WithSelectionBox(bool enabled = true, float[]? origin = null, float[]? size = null)
    {
        AddComponent(new SelectionBoxBlockComponent(enabled, origin, size));
        return this;
    }

    public CustomBlockBuilder WithBreathability(bool solid = true)
    {
        AddComponent(new BreathabilityBlockComponent(solid));
        return this;
    }

    public CustomBlockBuilder WithFlammable(int catchOdds = 5, int burnOdds = 20)
    {
        AddComponent(new FlammableBlockComponent(catchOdds, burnOdds));
        return this;
    }

    public CustomBlockBuilder WithReplaceable()
    {
        AddComponent(new ReplaceableBlockComponent());
        return this;
    }

    /// <summary>
    /// A crafting table titled <paramref name="tableName"/> crafting the recipes of <paramref name="craftingTags"/>
    /// ("crafting_table" for the vanilla ones, a tag of yours for recipes added with that block, see
    /// <see cref="Recipes.AddShaped"/>).
    /// </summary>
    public CustomBlockBuilder WithCraftingTable(string tableName, params string[] craftingTags)
    {
        AddComponent(new CraftingTableBlockComponent(craftingTags, tableName));
        return this;
    }

    public CustomBlockBuilder WithMapColor(string colorHex)
    {
        AddComponent(new MapColorBlockComponent(colorHex));
        return this;
    }

    public CustomBlockBuilder WithTransformation(int rx = 0, int ry = 0, int rz = 0, float sx = 1, float sy = 1, float sz = 1)
    {
        AddComponent(new TransformationBlockComponent(rx, ry, rz, sx, sy, sz));
        return this;
    }

    public CustomBlockBuilder WithTick(int interval = 20, bool looping = true)
    {
        AddComponent(new TickBlockComponent(interval, looping));
        return this;
    }

    public CustomBlockBuilder WithSupport(string shape = "solid")
    {
        AddComponent(new SupportBlockComponent(shape));
        return this;
    }

    public CustomBlockBuilder WithMovable(string movementType = "all", string stickyType = "none")
    {
        AddComponent(new MovableBlockComponent(movementType, stickyType));
        return this;
    }

    public CustomBlockBuilder WithRedstoneConductivity(bool redstoneConductor = true)
    {
        AddComponent(new RedstoneConductivityBlockComponent(redstoneConductor));
        return this;
    }

    public CustomBlockBuilder WithRedstoneProducer(int power = 15)
    {
        AddComponent(new RedstoneProducerBlockComponent(power));
        return this;
    }

    public CustomBlockBuilder WithLoot(string table)
    {
        AddComponent(new LootBlockComponent(table));
        return this;
    }

    public CustomBlockBuilder WithProperty(string name, params object[] values)
    {
        _properties.Add(new BlockProperty(name, values));
        return this;
    }

    public CustomBlockBuilder AddProperty(BlockProperty property)
    {
        _properties.Add(property);
        return this;
    }

    public CustomBlockBuilder WithPermutation(string condition, params IBlockComponent[] components)
    {
        _permutations.Add(new BlockPermutation(condition, components));
        return this;
    }

    public CustomBlockBuilder AddPermutation(BlockPermutation permutation)
    {
        _permutations.Add(permutation);
        return this;
    }

    public CustomBlockBuilder WithTags(params string[] tags)
    {
        _tags.AddRange(tags);
        return this;
    }

    public CustomBlockBuilder WithCreative(CreativeCategory category, string group = "", bool isHidden = false)
    {
        _creative = new CreativeInfo(category, group, isHidden);
        return this;
    }

    public CustomBlockBuilder AddComponent(IBlockComponent component)
    {
        _components[component.Name] = component;
        return this;
    }

    public static CustomBlockBuilder CreateSimpleBlock(string identifier, string displayName, string texture, float hardness = 1.5f, float resistance = 6.0f)
    {
        return Create(identifier)
            .WithDisplayName(displayName)
            .WithTexture(texture)
            .WithHardness(hardness)
            .WithBlastResistance(resistance);
    }

    public static CustomBlockBuilder CreateLightBlock(string identifier, string displayName, string texture, byte lightLevel = 15)
    {
        return Create(identifier)
            .WithDisplayName(displayName)
            .WithTexture(texture)
            .WithLightEmission(lightLevel);
    }

    public static CustomBlockBuilder CreateTransparentBlock(string identifier, string displayName, string texture, float hardness = 0.3f)
    {
        return Create(identifier)
            .WithDisplayName(displayName)
            .WithTexture(texture, "alpha_test")
            .WithHardness(hardness)
            .WithBreathability(false)
            .WithLightDampening(0);
    }

    public CustomBlock Build() => new(
        _identifier,
        _material,
        _creative,
        _components,
        _properties,
        _permutations,
        _tags);

    public CustomBlock Register()
    {
        var block = Build();
        CustomBlockRegistry.Register(block);
        return block;
    }
}
