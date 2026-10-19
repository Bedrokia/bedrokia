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

global using Bedrokia.Server.Events;
using Bedrokia.Server.Core;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Plugins;

/// <summary>
/// Central event hub for the server and plugins.
/// A handler that throws is logged and skipped; remaining handlers and the server proceed.
/// </summary>
public sealed class ServerEvents(Logger logger)
{
    // --- Player connection & movement ---
    public event Action<BedrockConnection>? PlayerJoin;
    public event Action<BedrockConnection>? PlayerQuit;

    /// <summary>The player moved (position or rotation).</summary>
    public event Action<BedrockConnection>? PlayerMove;

    public event Action<PlayerChatEvent>? PlayerChat;

    // --- Combat & damage ---
    public event Action<PlayerDamageEvent>? PlayerDamage;
    public event Action<PlayerDeathEvent>? PlayerDeath;
    public event Action<PlayerRespawnEvent>? PlayerRespawn;

    // --- Items & inventory ---
    public event Action<PlayerDropItemEvent>? PlayerDropItem;
    public event Action<InventoryTransactionEvent>? InventoryTransaction;
    public event Action<InventoryTransactionEvent>? InventoryClick
    {
        add => InventoryTransaction += value;
        remove => InventoryTransaction -= value;
    }

    // --- World & blocks ---
    public event Action<BlockEvent>? BlockBreak;
    public event Action<BlockEvent>? BlockPlace;

    /// <summary>Right click on a block with an item in hand, before vanilla uses it.</summary>
    public event Action<ItemUseEvent>? ItemUse;

    // --- Entities & mobs ---
    public event Action<EntityDamageEvent>? EntityDamage;
    public event Action<EntitySpawnEvent>? EntitySpawn;
    public event Action<EntityDespawnEvent>? EntityDespawn;

    // --- Server lifecycle ---
    /// <summary>Every game tick (20 per second), with the tick number.</summary>
    public event Action<long>? Tick;

    // Internal dispatchers
    internal void OnJoin(BedrockConnection player) => Fire(PlayerJoin, player);
    internal void OnQuit(BedrockConnection player) => Fire(PlayerQuit, player);
    internal void OnMove(BedrockConnection player) => Fire(PlayerMove, player);
    internal void OnTick(long tick) => Fire(Tick, tick);

    internal bool OnChat(PlayerChatEvent e) => Fire(PlayerChat, e);
    internal bool OnBreak(BlockEvent e) => Fire(BlockBreak, e);
    internal bool OnPlace(BlockEvent e) => Fire(BlockPlace, e);
    internal bool OnItemUse(ItemUseEvent e) => Fire(ItemUse, e);

    internal bool OnPlayerDamage(PlayerDamageEvent e) => Fire(PlayerDamage, e);
    internal void OnPlayerDeath(PlayerDeathEvent e) => Fire(PlayerDeath, e);
    internal void OnPlayerRespawn(PlayerRespawnEvent e) => Fire(PlayerRespawn, e);
    internal bool OnPlayerDropItem(PlayerDropItemEvent e) => Fire(PlayerDropItem, e);
    internal bool OnInventoryTransaction(InventoryTransactionEvent e) => Fire(InventoryTransaction, e);

    internal bool OnEntityDamage(EntityDamageEvent e) => Fire(EntityDamage, e);
    internal bool OnEntitySpawn(EntitySpawnEvent e) => Fire(EntitySpawn, e);
    internal void OnEntityDespawn(EntityDespawnEvent e) => Fire(EntityDespawn, e);

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
