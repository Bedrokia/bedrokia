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
    public event Action<PlayerLoginEvent>? PlayerLogin;

    internal bool HasPlayerLogin => PlayerLogin is not null;
    public event Action<PlayerJoinEvent>? PlayerJoin;

    internal bool HasPlayerJoin => PlayerJoin is not null;
    public event Action<PlayerQuitEvent>? PlayerQuit;

    internal bool HasPlayerQuit => PlayerQuit is not null;
    public event Action<PlayerMoveEvent>? PlayerMove;

    internal bool HasPlayerMove => PlayerMove is not null;
    public event Action<PlayerCommandEvent>? PlayerCommand;

    internal bool HasPlayerCommand => PlayerCommand is not null;
    public event Action<PlayerToggleActionEvent>? PlayerToggleAction;

    internal bool HasPlayerToggleAction => PlayerToggleAction is not null;
    public event Action<PlayerChatEvent>? PlayerChat;

    internal bool HasPlayerChat => PlayerChat is not null;

    // --- Combat, health & effects ---
    public event Action<PlayerDamageEvent>? PlayerDamage;

    internal bool HasPlayerDamage => PlayerDamage is not null;
    public event Action<PlayerDeathEvent>? PlayerDeath;

    internal bool HasPlayerDeath => PlayerDeath is not null;
    public event Action<PlayerRespawnEvent>? PlayerRespawn;

    internal bool HasPlayerRespawn => PlayerRespawn is not null;
    public event Action<PlayerHealEvent>? PlayerHeal;

    internal bool HasPlayerHeal => PlayerHeal is not null;
    public event Action<PlayerHungerChangeEvent>? PlayerHungerChange;

    internal bool HasPlayerHungerChange => PlayerHungerChange is not null;
    public event Action<PlayerEffectChangeEvent>? PlayerEffectChange;

    internal bool HasPlayerEffectChange => PlayerEffectChange is not null;
    public event Action<PlayerConsumeItemEvent>? PlayerConsumeItem;

    internal bool HasPlayerConsumeItem => PlayerConsumeItem is not null;

    // --- Items, inventory & interactions ---
    public event Action<PlayerDropItemEvent>? PlayerDropItem;

    internal bool HasPlayerDropItem => PlayerDropItem is not null;
    public event Action<PlayerPickupItemEvent>? PlayerPickupItem;

    internal bool HasPlayerPickupItem => PlayerPickupItem is not null;
    public event Action<PlayerHeldSlotChangeEvent>? PlayerHeldSlotChange;

    internal bool HasPlayerHeldSlotChange => PlayerHeldSlotChange is not null;
    public event Action<InventoryTransactionEvent>? InventoryTransaction;

    internal bool HasInventoryTransaction => InventoryTransaction is not null;
    public event Action<InventoryTransactionEvent>? InventoryClick
    {
        add => InventoryTransaction += value;
        remove => InventoryTransaction -= value;
    }
    public event Action<ContainerOpenEvent>? ContainerOpen;

    internal bool HasContainerOpen => ContainerOpen is not null;
    public event Action<ContainerCloseEvent>? ContainerClose;

    internal bool HasContainerClose => ContainerClose is not null;
    public event Action<PlayerCraftItemEvent>? PlayerCraftItem;

    internal bool HasPlayerCraftItem => PlayerCraftItem is not null;
    public event Action<PlayerEnchantItemEvent>? PlayerEnchantItem;

    internal bool HasPlayerEnchantItem => PlayerEnchantItem is not null;

    // --- World & blocks ---
    public event Action<BlockEvent>? BlockBreak;

    internal bool HasBlockBreak => BlockBreak is not null;
    public event Action<BlockEvent>? BlockPlace;

    internal bool HasBlockPlace => BlockPlace is not null;
    public event Action<ItemUseEvent>? ItemUse;

    /// <summary>Right click in the air with an item in hand.</summary>
    public event Action<PlayerItemUseEvent>? PlayerItemUse;

    internal bool HasPlayerItemUse => PlayerItemUse is not null;

    internal bool HasItemUse => ItemUse is not null;
    public event Action<PlayerInteractBlockEvent>? PlayerInteractBlock;

    internal bool HasPlayerInteractBlock => PlayerInteractBlock is not null;
    public event Action<BlockBurnEvent>? BlockBurn;

    internal bool HasBlockBurn => BlockBurn is not null;
    public event Action<BlockSpreadEvent>? BlockSpread;

    internal bool HasBlockSpread => BlockSpread is not null;
    public event Action<BlockGrowEvent>? BlockGrow;

    internal bool HasBlockGrow => BlockGrow is not null;
    public event Action<FluidFlowEvent>? FluidFlow;

    internal bool HasFluidFlow => FluidFlow is not null;
    public event Action<FurnaceSmeltEvent>? FurnaceSmelt;

    internal bool HasFurnaceSmelt => FurnaceSmelt is not null;
    public event Action<FurnaceBurnEvent>? FurnaceBurn;

    internal bool HasFurnaceBurn => FurnaceBurn is not null;

    // --- Entities & mobs ---
    public event Action<EntityDamageEvent>? EntityDamage;

    internal bool HasEntityDamage => EntityDamage is not null;
    public event Action<EntitySpawnEvent>? EntitySpawn;

    internal bool HasEntitySpawn => EntitySpawn is not null;
    public event Action<EntityDespawnEvent>? EntityDespawn;

    internal bool HasEntityDespawn => EntityDespawn is not null;
    public event Action<EntityDeathEvent>? EntityDeath;

    internal bool HasEntityDeath => EntityDeath is not null;
    public event Action<EntityTargetEvent>? EntityTarget;

    internal bool HasEntityTarget => EntityTarget is not null;
    public event Action<PlayerInteractEntityEvent>? PlayerInteractEntity;

    internal bool HasPlayerInteractEntity => PlayerInteractEntity is not null;
    public event Action<ExplosionEvent>? Explosion;

    internal bool HasExplosion => Explosion is not null;

    // --- Server lifecycle ---
    /// <summary>Every game tick (20 per second), with the tick number.</summary>
    public event Action<long>? Tick;

    internal bool HasTick => Tick is not null;

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
    internal bool OnPlayerItemUse(PlayerItemUseEvent e) => Fire(PlayerItemUse, e);
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
    internal void Clear()
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
        PlayerItemUse = null;
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
