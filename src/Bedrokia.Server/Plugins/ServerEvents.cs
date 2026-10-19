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
    /// <summary>Fast-path check: returns true if the delegate has any subscribed listeners.</summary>
    public bool HasListeners(Delegate? d) => d is not null;
    public static bool HasListeners<T>(Action<T>? d) => d is not null;

    // --- Player connection & movement ---
    public Action<PlayerLoginEvent>? PlayerLogin;
    public Action<PlayerJoinEvent>? PlayerJoin;
    public Action<PlayerQuitEvent>? PlayerQuit;
    public Action<PlayerMoveEvent>? PlayerMove;
    public Action<PlayerCommandEvent>? PlayerCommand;
    public Action<PlayerToggleActionEvent>? PlayerToggleAction;
    public Action<PlayerChatEvent>? PlayerChat;

    // --- Combat, health & effects ---
    public Action<PlayerDamageEvent>? PlayerDamage;
    public Action<PlayerDeathEvent>? PlayerDeath;
    public Action<PlayerRespawnEvent>? PlayerRespawn;
    public Action<PlayerHealEvent>? PlayerHeal;
    public Action<PlayerHungerChangeEvent>? PlayerHungerChange;
    public Action<PlayerEffectChangeEvent>? PlayerEffectChange;
    public Action<PlayerConsumeItemEvent>? PlayerConsumeItem;

    // --- Items, inventory & interactions ---
    public Action<PlayerDropItemEvent>? PlayerDropItem;
    public Action<PlayerPickupItemEvent>? PlayerPickupItem;
    public Action<PlayerHeldSlotChangeEvent>? PlayerHeldSlotChange;
    public Action<InventoryTransactionEvent>? InventoryTransaction;
    public event Action<InventoryTransactionEvent>? InventoryClick
    {
        add => InventoryTransaction += value;
        remove => InventoryTransaction -= value;
    }
    public Action<ContainerOpenEvent>? ContainerOpen;
    public Action<ContainerCloseEvent>? ContainerClose;
    public Action<PlayerCraftItemEvent>? PlayerCraftItem;
    public Action<PlayerEnchantItemEvent>? PlayerEnchantItem;

    // --- World & blocks ---
    public Action<BlockEvent>? BlockBreak;
    public Action<BlockEvent>? BlockPlace;
    public Action<ItemUseEvent>? ItemUse;
    public Action<PlayerInteractBlockEvent>? PlayerInteractBlock;
    public Action<BlockBurnEvent>? BlockBurn;
    public Action<BlockSpreadEvent>? BlockSpread;
    public Action<BlockGrowEvent>? BlockGrow;
    public Action<FluidFlowEvent>? FluidFlow;
    public Action<FurnaceSmeltEvent>? FurnaceSmelt;
    public Action<FurnaceBurnEvent>? FurnaceBurn;

    // --- Entities & mobs ---
    public Action<EntityDamageEvent>? EntityDamage;
    public Action<EntitySpawnEvent>? EntitySpawn;
    public Action<EntityDespawnEvent>? EntityDespawn;
    public Action<EntityDeathEvent>? EntityDeath;
    public Action<EntityTargetEvent>? EntityTarget;
    public Action<PlayerInteractEntityEvent>? PlayerInteractEntity;
    public Action<ExplosionEvent>? Explosion;

    // --- Server lifecycle ---
    /// <summary>Every game tick (20 per second), with the tick number.</summary>
    public Action<long>? Tick;

    // Internal dispatchers
    internal bool OnPlayerLogin(PlayerLoginEvent e) => Fire(PlayerLogin, e);
    internal void OnJoin(PlayerJoinEvent e) => Fire(PlayerJoin, e);
    internal void OnQuit(PlayerQuitEvent e) => Fire(PlayerQuit, e);
    internal bool OnMove(PlayerMoveEvent e) => Fire(PlayerMove, e);
    internal bool OnPlayerCommand(PlayerCommandEvent e) => Fire(PlayerCommand, e);
    internal bool OnPlayerToggleAction(PlayerToggleActionEvent e) => Fire(PlayerToggleAction, e);
    internal void OnTick(long tick) => Fire(Tick, tick);

    internal bool OnChat(PlayerChatEvent e) => Fire(PlayerChat, e);
    internal bool OnBreak(BlockEvent e) => Fire(BlockBreak, e);
    internal bool OnPlace(BlockEvent e) => Fire(BlockPlace, e);
    internal bool OnItemUse(ItemUseEvent e) => Fire(ItemUse, e);
    internal bool OnPlayerInteractBlock(PlayerInteractBlockEvent e) => Fire(PlayerInteractBlock, e);

    internal bool OnPlayerDamage(PlayerDamageEvent e) => Fire(PlayerDamage, e);
    internal void OnPlayerDeath(PlayerDeathEvent e) => Fire(PlayerDeath, e);
    internal void OnPlayerRespawn(PlayerRespawnEvent e) => Fire(PlayerRespawn, e);
    internal bool OnPlayerHeal(PlayerHealEvent e) => Fire(PlayerHeal, e);
    internal bool OnPlayerHungerChange(PlayerHungerChangeEvent e) => Fire(PlayerHungerChange, e);
    internal bool OnPlayerEffectChange(PlayerEffectChangeEvent e) => Fire(PlayerEffectChange, e);
    internal bool OnPlayerConsumeItem(PlayerConsumeItemEvent e) => Fire(PlayerConsumeItem, e);

    internal bool OnPlayerDropItem(PlayerDropItemEvent e) => Fire(PlayerDropItem, e);
    internal bool OnPlayerPickupItem(PlayerPickupItemEvent e) => Fire(PlayerPickupItem, e);
    internal bool OnPlayerHeldSlotChange(PlayerHeldSlotChangeEvent e) => Fire(PlayerHeldSlotChange, e);
    internal bool OnInventoryTransaction(InventoryTransactionEvent e) => Fire(InventoryTransaction, e);
    internal bool OnContainerOpen(ContainerOpenEvent e) => Fire(ContainerOpen, e);
    internal void OnContainerClose(ContainerCloseEvent e) => Fire(ContainerClose, e);
    internal bool OnPlayerCraftItem(PlayerCraftItemEvent e) => Fire(PlayerCraftItem, e);
    internal bool OnPlayerEnchantItem(PlayerEnchantItemEvent e) => Fire(PlayerEnchantItem, e);

    internal bool OnBlockBurn(BlockBurnEvent e) => Fire(BlockBurn, e);
    internal bool OnBlockSpread(BlockSpreadEvent e) => Fire(BlockSpread, e);
    internal bool OnBlockGrow(BlockGrowEvent e) => Fire(BlockGrow, e);
    internal bool OnFluidFlow(FluidFlowEvent e) => Fire(FluidFlow, e);
    internal bool OnFurnaceSmelt(FurnaceSmeltEvent e) => Fire(FurnaceSmelt, e);
    internal bool OnFurnaceBurn(FurnaceBurnEvent e) => Fire(FurnaceBurn, e);

    internal bool OnEntityDamage(EntityDamageEvent e) => Fire(EntityDamage, e);
    internal bool OnEntitySpawn(EntitySpawnEvent e) => Fire(EntitySpawn, e);
    internal void OnEntityDespawn(EntityDespawnEvent e) => Fire(EntityDespawn, e);
    internal void OnEntityDeath(EntityDeathEvent e) => Fire(EntityDeath, e);
    internal bool OnEntityTarget(EntityTargetEvent e) => Fire(EntityTarget, e);
    internal bool OnPlayerInteractEntity(PlayerInteractEntityEvent e) => Fire(PlayerInteractEntity, e);
    internal bool OnExplosion(ExplosionEvent e) => Fire(Explosion, e);

    private readonly HashSet<Delegate> _reported = [];

    /// <summary>Resets all registered event listeners and clears reported crashes.</summary>
    public void Clear()
    {
        PlayerLogin = null;
        PlayerJoin = null;
        PlayerQuit = null;
        PlayerMove = null;
        PlayerCommand = null;
        PlayerToggleAction = null;
        PlayerChat = null;

        PlayerDamage = null;
        PlayerDeath = null;
        PlayerRespawn = null;
        PlayerHeal = null;
        PlayerHungerChange = null;
        PlayerEffectChange = null;
        PlayerConsumeItem = null;

        PlayerDropItem = null;
        PlayerPickupItem = null;
        PlayerHeldSlotChange = null;
        InventoryTransaction = null;
        ContainerOpen = null;
        ContainerClose = null;
        PlayerCraftItem = null;
        PlayerEnchantItem = null;

        BlockBreak = null;
        BlockPlace = null;
        ItemUse = null;
        PlayerInteractBlock = null;
        BlockBurn = null;
        BlockSpread = null;
        BlockGrow = null;
        FluidFlow = null;
        FurnaceSmelt = null;
        FurnaceBurn = null;

        EntityDamage = null;
        EntitySpawn = null;
        EntityDespawn = null;
        EntityDeath = null;
        EntityTarget = null;
        PlayerInteractEntity = null;
        Explosion = null;

        Tick = null;
        _reported.Clear();
    }

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
