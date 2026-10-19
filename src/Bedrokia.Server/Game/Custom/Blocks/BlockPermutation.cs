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

public sealed class BlockPermutation
{
    public BlockPermutation(string condition, IReadOnlyList<IBlockComponent>? components = null)
    {
        Condition = condition;
        Components = components ?? [];
    }

    public string Condition { get; }
    public IReadOnlyList<IBlockComponent> Components { get; }

    public OrderedDictionary<string, object> BuildNbt()
    {
        var compDict = new OrderedDictionary<string, object>();
        foreach (var c in Components)
            compDict[c.Name] = c.BuildNbt();

        return new OrderedDictionary<string, object>
        {
            ["condition"] = Condition,
            ["components"] = compDict
        };
    }
}
