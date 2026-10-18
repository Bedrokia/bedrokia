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

using Bedrokia.Server.Core;
using Bedrokia.Server.Game;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Plugins;

/// <summary>An event a handler can cancel: the server then undoes or skips the action.</summary>
public abstract class CancellableEvent
{
    public bool Cancelled { get; set; }
}

public sealed class PlayerChatEvent(BedrockConnection player, string message) : CancellableEvent
{
    public BedrockConnection Player => player;

    /// <summary>Can be changed by a handler.</summary>
    public string Message { get; set; } = message;
}

public sealed class BlockEvent(BedrockConnection player, int x, int y, int z, BlockType block) : CancellableEvent
{
    public BedrockConnection Player => player;
    public int X => x;
    public int Y => y;
    public int Z => z;

    /// <summary>The block broken, placed, or clicked.</summary>
    public BlockType Block => block;
}

public sealed class ItemUseEvent(BedrockConnection player, ItemStack item, int x, int y, int z, BlockType block, int face) : CancellableEvent
{
    public BedrockConnection Player => player;
    public ItemStack Item => item;
    public int X => x;
    public int Y => y;
    public int Z => z;
    public BlockType Block => block;
    public int Face => face;
}

/// <summary>
/// Hooks for plugins. A handler that throws is logged and skipped; the other handlers and the server go on.
/// </summary>
public sealed class ServerEvents(Logger logger)
{
    public event Action<BedrockConnection>? PlayerJoin;
    public event Action<BedrockConnection>? PlayerQuit;

    /// <summary>The player moved (position or rotation).</summary>
    public event Action<BedrockConnection>? PlayerMove;

    public event Action<PlayerChatEvent>? PlayerChat;
    public event Action<BlockEvent>? BlockBreak;
    public event Action<BlockEvent>? BlockPlace;

    /// <summary>Right click on a block with an item in hand, before vanilla uses it.</summary>
    public event Action<ItemUseEvent>? ItemUse;

    /// <summary>Every game tick (20 per second), with the tick number.</summary>
    public event Action<long>? Tick;

    internal void OnJoin(BedrockConnection player) => Fire(PlayerJoin, player);
    internal void OnQuit(BedrockConnection player) => Fire(PlayerQuit, player);
    internal void OnMove(BedrockConnection player) => Fire(PlayerMove, player);
    internal void OnTick(long tick) => Fire(Tick, tick);

    internal bool OnChat(PlayerChatEvent e) => Fire(PlayerChat, e);
    internal bool OnBreak(BlockEvent e) => Fire(BlockBreak, e);
    internal bool OnPlace(BlockEvent e) => Fire(BlockPlace, e);
    internal bool OnItemUse(ItemUseEvent e) => Fire(ItemUse, e);

    private readonly HashSet<Delegate> _reported = [];

    // Returns false when a handler cancelled the event.
    private bool Fire<T>(Action<T>? handlers, T e, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(handlers))] string name = "")
    {
        if (handlers is null)
            return true;
        foreach (var handler in handlers.GetInvocationList().Cast<Action<T>>())
        {
            try
            {
                handler(e);
            }
            catch (Exception ex)
            {
                // Full error once per handler: a broken Tick or PlayerMove handler would flood the console.
                if (_reported.Add(handler))
                    logger.Error($"Plugin '{handler.Method.DeclaringType?.Assembly.GetName().Name}' crashed in event {name} "
                                 + $"({handler.Method.DeclaringType?.Name}.{handler.Method.Name}); later errors of this handler are hidden: {ex}");
            }
        }
        return e is not CancellableEvent { Cancelled: true };
    }
}
