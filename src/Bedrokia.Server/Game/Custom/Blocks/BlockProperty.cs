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

public sealed class BlockProperty
{
    public BlockProperty(string name, IReadOnlyList<object> values)
    {
        Name = name;
        Values = values;
    }

    public string Name { get; }
    public IReadOnlyList<object> Values { get; }

    public OrderedDictionary<string, object> BuildNbt()
    {
        var list = new List<object>();
        byte elementType = Nbt.String;

        foreach (var val in Values)
        {
            switch (val)
            {
                case bool b:
                    elementType = Nbt.Byte;
                    list.Add((byte)(b ? 1 : 0));
                    break;
                case int i:
                    elementType = Nbt.Int;
                    list.Add(i);
                    break;
                case string s:
                    elementType = Nbt.String;
                    list.Add(s);
                    break;
                default:
                    elementType = Nbt.String;
                    list.Add(val?.ToString() ?? "");
                    break;
            }
        }

        return new OrderedDictionary<string, object>
        {
            ["name"] = Name,
            ["enum"] = new NbtList(elementType, list)
        };
    }
}
