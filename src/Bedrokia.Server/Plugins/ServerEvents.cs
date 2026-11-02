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

    // --- A. Player connection & movement ---
    public event Action<PlayerLoginEvent>? PlayerLogin;
    internal bool HasPlayerLogin => PlayerLogin is not null;

    public event Action<PlayerJoinEvent>? PlayerJoin;
    internal bool HasPlayerJoin => PlayerJoin is not null;

    public event Action<PlayerQuitEvent>? PlayerQuit;
    internal bool HasPlayerQuit => PlayerQuit is not null;

    public event Action<PlayerMoveEvent>? PlayerMove;
    internal bool HasPlayerMove => PlayerMove is not null;

    public event Action<PlayerWarpEvent>? PlayerWarp;
    internal bool HasPlayerWarp => PlayerWarp is not null;

    public event Action<PlayerCommandEvent>? PlayerCommand;
    internal bool HasPlayerCommand => PlayerCommand is not null;

    public event Action<PlayerToggleActionEvent>? PlayerToggleAction;
    internal bool HasPlayerToggleAction => PlayerToggleAction is not null;

    public event Action<PlayerChatEvent>? PlayerChat;
    internal bool HasPlayerChat => PlayerChat is not null;

    /// <summary>Fired when a player swings and misses.</summary>
    public event Action<PlayerMissSwingEvent>? PlayerMissSwing;
    internal bool HasPlayerMissSwing => PlayerMissSwing is not null;

    /// <summary>Fired when a player toggles flight.</summary>
    public event Action<PlayerToggleFlightEvent>? PlayerToggleFlight;
    internal bool HasPlayerToggleFlight => PlayerToggleFlight is not null;

    /// <summary>Fired when a player toggles crawl.</summary>
    public event Action<PlayerToggleCrawlEvent>? PlayerToggleCrawl;
    internal bool HasPlayerToggleCrawl => PlayerToggleCrawl is not null;

    /// <summary>Fired when a player changes world/dimension.</summary>
    public event Action<PlayerWorldChangeEvent>? PlayerWorldChange;
    internal bool HasPlayerWorldChange => PlayerWorldChange is not null;

    /// <summary>Fired when a player crosses into a new chunk.</summary>
    public event Action<PlayerChunkEnterEvent>? PlayerChunkEnter;
    internal bool HasPlayerChunkEnter => PlayerChunkEnter is not null;

    /// <summary>Fired when a player responds to resource packs.</summary>
    public event Action<ResourcePackClientResponseEvent>? ResourcePackClientResponse;
    internal bool HasResourcePackClientResponse => ResourcePackClientResponse is not null;

    /// <summary>Fired when a player's AFK state changes.</summary>
    public event Action<PlayerAfkEvent>? PlayerAfk;
    internal bool HasPlayerAfk => PlayerAfk is not null;

    /// <summary>Fired before player login completes.</summary>
    public event Action<PlayerPreLoginEvent>? PlayerPreLogin;
    internal bool HasPlayerPreLogin => PlayerPreLogin is not null;

    /// <summary>Fired when a player is kicked.</summary>
    public event Action<PlayerKickEvent>? PlayerKick;
    internal bool HasPlayerKick => PlayerKick is not null;

    /// <summary>Fired when a player's view distance changes.</summary>
    public event Action<PlayerViewDistanceChangeEvent>? PlayerViewDistanceChange;
    internal bool HasPlayerViewDistanceChange => PlayerViewDistanceChange is not null;

    /// <summary>Fired when a player's game mode changes.</summary>
    public event Action<PlayerGameModeChangeEvent>? PlayerGameModeChange;
    internal bool HasPlayerGameModeChange => PlayerGameModeChange is not null;

    /// <summary>Fired when a player changes skin.</summary>
    public event Action<PlayerChangeSkinEvent>? PlayerChangeSkin;
    internal bool HasPlayerChangeSkin => PlayerChangeSkin is not null;

    /// <summary>Fired when a player performs an emote.</summary>
    public event Action<PlayerEmoteEvent>? PlayerEmote;
    internal bool HasPlayerEmote => PlayerEmote is not null;

    /// <summary>Fired when a player is transferred to another server.</summary>
    public event Action<PlayerTransferEvent>? PlayerTransfer;
    internal bool HasPlayerTransfer => PlayerTransfer is not null;

    /// <summary>Fired when a player submits or closes a modal form.</summary>
    public event Action<PlayerFormResponseEvent>? PlayerFormResponse;
    internal bool HasPlayerFormResponse => PlayerFormResponse is not null;

    /// <summary>Fired when a player changes input mode.</summary>
    public event Action<PlayerInputModeChangeEvent>? PlayerInputModeChange;
    internal bool HasPlayerInputModeChange => PlayerInputModeChange is not null;

    /// <summary>Fired when an inbound data packet is received.</summary>
    public event Action<DataPacketReceiveEvent>? DataPacketReceive;
    internal bool HasDataPacketReceive => DataPacketReceive is not null;

    /// <summary>Fired before an outbound data packet is sent.</summary>
    public event Action<DataPacketSendEvent>? DataPacketSend;
    internal bool HasDataPacketSend => DataPacketSend is not null;

    // --- B. Player combat, health, effects & equipment ---
    public event Action<PlayerDamageEvent>? PlayerDamage;
    internal bool HasPlayerDamage => PlayerDamage is not null;

    public event Action<PlayerDeathEvent>? PlayerDeath;
    internal bool HasPlayerDeath => PlayerDeath is not null;

    public event Action<PlayerRespawnEvent>? PlayerRespawn;
    internal bool HasPlayerRespawn => PlayerRespawn is not null;

    public event Action<PlayerHealEvent>? PlayerHeal;
    internal bool HasPlayerHeal => PlayerHeal is not null;

    public event Action<PlayerCombustEvent>? PlayerCombust;
    internal bool HasPlayerCombust => PlayerCombust is not null;

    public event Action<PlayerFoodChangeEvent>? PlayerFoodChange;
    internal bool HasPlayerFoodChange => PlayerFoodChange is not null;

    public event Action<PlayerHungerChangeEvent>? PlayerHungerChange;
    internal bool HasPlayerHungerChange => PlayerHungerChange is not null;

    public event Action<PlayerExperienceChangeEvent>? PlayerExperienceChange;
    internal bool HasPlayerExperienceChange => PlayerExperienceChange is not null;

    public event Action<PlayerExperiencePickupEvent>? PlayerExperiencePickup;
    internal bool HasPlayerExperiencePickup => PlayerExperiencePickup is not null;

    public event Action<PlayerEffectChangeEvent>? PlayerEffectChange;
    internal bool HasPlayerEffectChange => PlayerEffectChange is not null;

    public event Action<PlayerConsumeItemEvent>? PlayerConsumeItem;
    internal bool HasPlayerConsumeItem => PlayerConsumeItem is not null;

    public event Action<PlayerDropItemEvent>? PlayerDropItem;
    internal bool HasPlayerDropItem => PlayerDropItem is not null;

    public event Action<PlayerPickupItemEvent>? PlayerPickupItem;
    internal bool HasPlayerPickupItem => PlayerPickupItem is not null;

    public event Action<PlayerHeldSlotChangeEvent>? PlayerHeldSlotChange;
    internal bool HasPlayerHeldSlotChange => PlayerHeldSlotChange is not null;

    public event Action<OffhandChangeEvent>? OffhandChange;
    internal bool HasOffhandChange => OffhandChange is not null;

    public event Action<ItemDamageEvent>? ItemDamage;
    internal bool HasItemDamage => ItemDamage is not null;

    public event Action<ItemBreakEvent>? ItemBreak;
    internal bool HasItemBreak => ItemBreak is not null;

    public event Action<ItemMendEvent>? ItemMend;
    internal bool HasItemMend => ItemMend is not null;

    public event Action<ShieldBlockEvent>? ShieldBlock;
    internal bool HasShieldBlock => ShieldBlock is not null;

    public event Action<TotemUseEvent>? TotemUse;
    internal bool HasTotemUse => TotemUse is not null;

    public event Action<PlayerMountEvent>? PlayerMount;
    internal bool HasPlayerMount => PlayerMount is not null;

    public event Action<VehicleMoveEvent>? VehicleMove;
    internal bool HasVehicleMove => VehicleMove is not null;

    public event Action<CrossbowLoadEvent>? CrossbowLoad;
    internal bool HasCrossbowLoad => CrossbowLoad is not null;

    public event Action<TridentRiptideEvent>? TridentRiptide;
    internal bool HasTridentRiptide => TridentRiptide is not null;

    public event Action<PlayerFishEvent>? PlayerFish;
    internal bool HasPlayerFish => PlayerFish is not null;

    public event Action<HornBlowEvent>? HornBlow;
    internal bool HasHornBlow => HornBlow is not null;

    public event Action<BookEditEvent>? BookEdit;
    internal bool HasBookEdit => BookEdit is not null;

    /// <summary>Fired when a player attacks an entity.</summary>
    public event Action<PlayerAttackEvent>? PlayerAttack;
    internal bool HasPlayerAttack => PlayerAttack is not null;

    /// <summary>Fired when a player falls and takes fall damage.</summary>
    public event Action<PlayerFallEvent>? PlayerFall;
    internal bool HasPlayerFall => PlayerFall is not null;

    /// <summary>Fired when player exhaustion increases.</summary>
    public event Action<PlayerExhaustEvent>? PlayerExhaust;
    internal bool HasPlayerExhaust => PlayerExhaust is not null;

    /// <summary>Fired when a player changes armor.</summary>
    public event Action<PlayerArmorChangeEvent>? PlayerArmorChange;
    internal bool HasPlayerArmorChange => PlayerArmorChange is not null;

    /// <summary>Fired when a player enters a bed.</summary>
    public event Action<PlayerBedEnterEvent>? PlayerBedEnter;
    internal bool HasPlayerBedEnter => PlayerBedEnter is not null;

    /// <summary>Fired when a player leaves a bed.</summary>
    public event Action<PlayerBedLeaveEvent>? PlayerBedLeave;
    internal bool HasPlayerBedLeave => PlayerBedLeave is not null;

    /// <summary>Fired when night is skipped via sleeping.</summary>
    public event Action<PlayerSleepSkipNightEvent>? PlayerSleepSkipNight;
    internal bool HasPlayerSleepSkipNight => PlayerSleepSkipNight is not null;

    /// <summary>Fired when a player fills a bucket.</summary>
    public event Action<PlayerBucketFillEvent>? PlayerBucketFill;
    internal bool HasPlayerBucketFill => PlayerBucketFill is not null;

    /// <summary>Fired when a player empties a bucket.</summary>
    public event Action<PlayerBucketEmptyEvent>? PlayerBucketEmpty;
    internal bool HasPlayerBucketEmpty => PlayerBucketEmpty is not null;

    /// <summary>Fired when a player changes dimensions.</summary>
    public event Action<PlayerDimensionChangeEvent>? PlayerDimensionChange;
    internal bool HasPlayerDimensionChange => PlayerDimensionChange is not null;

    // --- C. Blocks & Block behaviours ---
    public event Action<BlockEvent>? BlockBreak;
    internal bool HasBlockBreak => BlockBreak is not null;

    public event Action<BlockEvent>? BlockPlace;
    internal bool HasBlockPlace => BlockPlace is not null;

    public event Action<ItemUseEvent>? ItemUse;
    internal bool HasItemUse => ItemUse is not null;

    /// <summary>Right click in the air with an item in hand.</summary>
    public event Action<PlayerItemUseEvent>? PlayerItemUse;
    internal bool HasPlayerItemUse => PlayerItemUse is not null;

    public event Action<PlayerInteractBlockEvent>? PlayerInteractBlock;
    internal bool HasPlayerInteractBlock => PlayerInteractBlock is not null;

    public event Action<SignChangeEvent>? SignChange;
    internal bool HasSignChange => SignChange is not null;

    public event Action<BlockUseEvent>? BlockUse;
    internal bool HasBlockUse => BlockUse is not null;

    public event Action<BlockGrowEvent>? BlockGrow;
    internal bool HasBlockGrow => BlockGrow is not null;

    public event Action<TreeGrowEvent>? TreeGrow;
    internal bool HasTreeGrow => TreeGrow is not null;

    public event Action<BlockFallEvent>? BlockFall;
    internal bool HasBlockFall => BlockFall is not null;

    public event Action<BlockLandEvent>? BlockLand;
    internal bool HasBlockLand => BlockLand is not null;

    public event Action<BlockFormEvent>? BlockForm;
    internal bool HasBlockForm => BlockForm is not null;

    public event Action<BlockIgniteEvent>? BlockIgnite;
    internal bool HasBlockIgnite => BlockIgnite is not null;

    public event Action<BlockBurnEvent>? BlockBurn;
    internal bool HasBlockBurn => BlockBurn is not null;

    public event Action<BlockSpreadEvent>? BlockSpread;
    internal bool HasBlockSpread => BlockSpread is not null;

    public event Action<TntPrimeEvent>? TntPrime;
    internal bool HasTntPrime => TntPrime is not null;

    public event Action<NotePlayEvent>? NotePlay;
    internal bool HasNotePlay => NotePlay is not null;

    public event Action<JukeboxChangeEvent>? JukeboxChange;
    internal bool HasJukeboxChange => JukeboxChange is not null;

    public event Action<LecternChangeEvent>? LecternChange;
    internal bool HasLecternChange => LecternChange is not null;

    public event Action<LecternPageEvent>? LecternPage;
    internal bool HasLecternPage => LecternPage is not null;

    public event Action<ItemCookEvent>? ItemCook;
    internal bool HasItemCook => ItemCook is not null;

    public event Action<FuelBurnEvent>? FuelBurn;
    internal bool HasFuelBurn => FuelBurn is not null;

    public event Action<FurnaceSmeltEvent>? FurnaceSmelt;
    internal bool HasFurnaceSmelt => FurnaceSmelt is not null;

    public event Action<FurnaceBurnEvent>? FurnaceBurn;
    internal bool HasFurnaceBurn => FurnaceBurn is not null;

    public event Action<BrewEvent>? Brew;
    internal bool HasBrew => Brew is not null;

    public event Action<BeaconEffectEvent>? BeaconEffect;
    internal bool HasBeaconEffect => BeaconEffect is not null;

    public event Action<LiquidFlowEvent>? LiquidFlow;
    internal bool HasLiquidFlow => LiquidFlow is not null;

    public event Action<FluidFlowEvent>? FluidFlow;
    internal bool HasFluidFlow => FluidFlow is not null;

    public event Action<SculkShriekEvent>? SculkShriek;
    internal bool HasSculkShriek => SculkShriek is not null;

    /// <summary>Fired when a stonecutter is used.</summary>
    public event Action<StonecutterUseEvent>? StonecutterUse;
    internal bool HasStonecutterUse => StonecutterUse is not null;

    /// <summary>Fired when leaves decay naturally.</summary>
    public event Action<LeavesDecayEvent>? LeavesDecay;
    internal bool HasLeavesDecay => LeavesDecay is not null;

    /// <summary>Fired when farmland moisture changes.</summary>
    public event Action<MoistureChangeEvent>? MoistureChange;
    internal bool HasMoistureChange => MoistureChange is not null;

    // --- D. World & Weather ---
    public event Action<ExplosionEvent>? Explosion;
    internal bool HasExplosion => Explosion is not null;

    public event Action<ExplosionBlocksEvent>? ExplosionBlocks;
    internal bool HasExplosionBlocks => ExplosionBlocks is not null;

    public event Action<LightningStrikeEvent>? LightningStrike;
    internal bool HasLightningStrike => LightningStrike is not null;

    public event Action<RainChangeEvent>? RainChange;
    internal bool HasRainChange => RainChange is not null;

    public event Action<ThunderChangeEvent>? ThunderChange;
    internal bool HasThunderChange => ThunderChange is not null;

    public event Action<WeatherEffectEvent>? WeatherEffect;
    internal bool HasWeatherEffect => WeatherEffect is not null;

    public event Action<TickRateChangeEvent>? TickRateChange;
    internal bool HasTickRateChange => TickRateChange is not null;

    /// <summary>Fired when a world is loaded.</summary>
    public event Action<WorldLoadEvent>? WorldLoad;
    internal bool HasWorldLoad => WorldLoad is not null;

    /// <summary>Fired when a world is saved.</summary>
    public event Action<WorldSaveEvent>? WorldSave;
    internal bool HasWorldSave => WorldSave is not null;

    /// <summary>Fired when world spawn point changes.</summary>
    public event Action<SpawnChangeEvent>? SpawnChange;
    internal bool HasSpawnChange => SpawnChange is not null;

    /// <summary>Fired when a chunk is loaded.</summary>
    public event Action<ChunkLoadEvent>? ChunkLoad;
    internal bool HasChunkLoad => ChunkLoad is not null;

    /// <summary>Fired when a chunk is populated.</summary>
    public event Action<ChunkPopulateEvent>? ChunkPopulate;
    internal bool HasChunkPopulate => ChunkPopulate is not null;

    /// <summary>Fired when world time changes.</summary>
    public event Action<TimeChangeEvent>? TimeChange;
    internal bool HasTimeChange => TimeChange is not null;

    // --- E. Inventory, Containers & Crafting Stations ---
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

    public event Action<CraftItemEvent>? CraftItem;
    internal bool HasCraftItem => CraftItem is not null;

    public event Action<PlayerCraftItemEvent>? PlayerCraftItem;
    internal bool HasPlayerCraftItem => PlayerCraftItem is not null;

    public event Action<EnchantItemEvent>? EnchantItem;
    internal bool HasEnchantItem => EnchantItem is not null;

    public event Action<PlayerEnchantItemEvent>? PlayerEnchantItem;
    internal bool HasPlayerEnchantItem => PlayerEnchantItem is not null;

    public event Action<AnvilUseEvent>? AnvilUse;
    internal bool HasAnvilUse => AnvilUse is not null;

    public event Action<GrindstoneUseEvent>? GrindstoneUse;
    internal bool HasGrindstoneUse => GrindstoneUse is not null;

    public event Action<SmithingUseEvent>? SmithingUse;
    internal bool HasSmithingUse => SmithingUse is not null;

    public event Action<BeaconChangeEvent>? BeaconChange;
    internal bool HasBeaconChange => BeaconChange is not null;

    // --- F. Entities, Mobs, Projectiles & Villagers ---
    public event Action<EntitySpawnEvent>? EntitySpawn;
    internal bool HasEntitySpawn => EntitySpawn is not null;

    public event Action<EntityDespawnEvent>? EntityDespawn;
    internal bool HasEntityDespawn => EntityDespawn is not null;

    public event Action<EntityDamageEvent>? EntityDamage;
    internal bool HasEntityDamage => EntityDamage is not null;

    public event Action<EntityDeathEvent>? EntityDeath;
    internal bool HasEntityDeath => EntityDeath is not null;

    public event Action<EntityPlaceEvent>? EntityPlace;
    internal bool HasEntityPlace => EntityPlace is not null;

    public event Action<EntityTouchEvent>? EntityTouch;
    internal bool HasEntityTouch => EntityTouch is not null;

    public event Action<EntityCombustEvent>? EntityCombust;
    internal bool HasEntityCombust => EntityCombust is not null;

    public event Action<EntityKilledEvent>? EntityKilled;
    internal bool HasEntityKilled => EntityKilled is not null;

    public event Action<ArmorStandChangeEvent>? ArmorStandChange;
    internal bool HasArmorStandChange => ArmorStandChange is not null;

    public event Action<MobTargetEvent>? MobTarget;
    internal bool HasMobTarget => MobTarget is not null;

    public event Action<EntityTargetEvent>? EntityTarget;
    internal bool HasEntityTarget => EntityTarget is not null;

    public event Action<MobAttackEvent>? MobAttack;
    internal bool HasMobAttack => MobAttack is not null;

    public event Action<MobBreedEvent>? MobBreed;
    internal bool HasMobBreed => MobBreed is not null;

    public event Action<MobInteractEvent>? MobInteract;
    internal bool HasMobInteract => MobInteract is not null;

    /// <summary>A player hit or used an NPC (<see cref="BedrokiaServer.Npcs"/>).</summary>
    public event Action<NpcInteractEvent>? NpcInteract;

    public event Action<PlayerInteractEntityEvent>? PlayerInteractEntity;
    internal bool HasPlayerInteractEntity => PlayerInteractEntity is not null;

    public event Action<MobTameEvent>? MobTame;
    internal bool HasMobTame => MobTame is not null;

    public event Action<MobLeashEvent>? MobLeash;
    internal bool HasMobLeash => MobLeash is not null;

    public event Action<ProjectileLaunchEvent>? ProjectileLaunch;
    internal bool HasProjectileLaunch => ProjectileLaunch is not null;

    public event Action<ProjectileHitEvent>? ProjectileHit;
    internal bool HasProjectileHit => ProjectileHit is not null;

    public event Action<ProjectilePickupEvent>? ProjectilePickup;
    internal bool HasProjectilePickup => ProjectilePickup is not null;

    public event Action<PotionSplashEvent>? PotionSplash;
    internal bool HasPotionSplash => PotionSplash is not null;

    public event Action<VillagerProfessionEvent>? VillagerProfession;
    internal bool HasVillagerProfession => VillagerProfession is not null;

    public event Action<VillagerTradeEvent>? VillagerTrade;
    internal bool HasVillagerTrade => VillagerTrade is not null;

    public event Action<VillagerLevelUpEvent>? VillagerLevelUp;
    internal bool HasVillagerLevelUp => VillagerLevelUp is not null;

    /// <summary>Fired when a vehicle is created.</summary>
    public event Action<VehicleCreateEvent>? VehicleCreate;
    internal bool HasVehicleCreate => VehicleCreate is not null;

    /// <summary>Fired when a vehicle takes damage.</summary>
    public event Action<VehicleDamageEvent>? VehicleDamage;
    internal bool HasVehicleDamage => VehicleDamage is not null;

    /// <summary>Fired when a vehicle is destroyed.</summary>
    public event Action<VehicleDestroyEvent>? VehicleDestroy;
    internal bool HasVehicleDestroy => VehicleDestroy is not null;

    /// <summary>Fired when an entity enters a vehicle.</summary>
    public event Action<VehicleEnterEvent>? VehicleEnter;
    internal bool HasVehicleEnter => VehicleEnter is not null;

    /// <summary>Fired when an entity exits a vehicle.</summary>
    public event Action<VehicleExitEvent>? VehicleExit;
    internal bool HasVehicleExit => VehicleExit is not null;

    /// <summary>Fired when a sheep is sheared.</summary>
    public event Action<SheepShearEvent>? SheepShear;
    internal bool HasSheepShear => SheepShear is not null;

    /// <summary>Fired when a sheep regrows its wool.</summary>
    public event Action<SheepRegrowWoolEvent>? SheepRegrowWool;
    internal bool HasSheepRegrowWool => SheepRegrowWool is not null;

    /// <summary>Fired when a firework explodes.</summary>
    public event Action<FireworkExplodeEvent>? FireworkExplode;
    internal bool HasFireworkExplode => FireworkExplode is not null;

    // --- G. Redstone, Portals & Dragon ---
    public event Action<RedstonePowerEvent>? RedstonePower;
    internal bool HasRedstonePower => RedstonePower is not null;

    public event Action<PressurePlateEvent>? PressurePlate;
    internal bool HasPressurePlate => PressurePlate is not null;

    public event Action<PistonMoveEvent>? PistonMove;
    internal bool HasPistonMove => PistonMove is not null;

    public event Action<PistonPushEvent>? PistonPush;
    internal bool HasPistonPush => PistonPush is not null;

    public event Action<BlockDispenseEvent>? BlockDispense;
    internal bool HasBlockDispense => BlockDispense is not null;

    public event Action<DispenserEquipEvent>? DispenserEquip;
    internal bool HasDispenserEquip => DispenserEquip is not null;

    public event Action<ItemTransferEvent>? ItemTransfer;
    internal bool HasItemTransfer => ItemTransfer is not null;

    public event Action<HopperPickupEvent>? HopperPickup;
    internal bool HasHopperPickup => HopperPickup is not null;

    public event Action<MinecartActivateEvent>? MinecartActivate;
    internal bool HasMinecartActivate => MinecartActivate is not null;

    public event Action<MinecartTransferEvent>? MinecartTransfer;
    internal bool HasMinecartTransfer => MinecartTransfer is not null;

    public event Action<MinecartPickupEvent>? MinecartPickup;
    internal bool HasMinecartPickup => MinecartPickup is not null;

    public event Action<TripwireEvent>? Tripwire;
    internal bool HasTripwire => Tripwire is not null;

    public event Action<DetectorRailEvent>? DetectorRail;
    internal bool HasDetectorRail => DetectorRail is not null;

    public event Action<SculkVibrationEvent>? SculkVibration;
    internal bool HasSculkVibration => SculkVibration is not null;

    public event Action<CrafterCraftEvent>? CrafterCraft;
    internal bool HasCrafterCraft => CrafterCraft is not null;

    public event Action<PortalCreateEvent>? PortalCreate;
    internal bool HasPortalCreate => PortalCreate is not null;

    public event Action<PlayerPortalEvent>? PlayerPortal;
    internal bool HasPlayerPortal => PlayerPortal is not null;

    public event Action<EntityPortalEvent>? EntityPortal;
    internal bool HasEntityPortal => EntityPortal is not null;

    public event Action<EndGatewayEvent>? EndGateway;
    internal bool HasEndGateway => EndGateway is not null;

    public event Action<DragonSpawnEvent>? DragonSpawn;
    internal bool HasDragonSpawn => DragonSpawn is not null;

    public event Action<DragonPhaseEvent>? DragonPhase;
    internal bool HasDragonPhase => DragonPhase is not null;

    public event Action<DragonDeathEvent>? DragonDeath;
    internal bool HasDragonDeath => DragonDeath is not null;

    // --- Server lifecycle ---
    /// <summary>Every game tick (20 per second), with the tick number.</summary>
    public event Action<long>? Tick;
    internal bool HasTick => Tick is not null;

    /// <summary>Fired when the server finishes starting.</summary>
    public event Action<ServerStartedEvent>? ServerStarted;
    internal bool HasServerStarted => ServerStarted is not null;

    /// <summary>Fired when the server is stopping.</summary>
    public event Action<ServerStopEvent>? ServerStop;
    internal bool HasServerStop => ServerStop is not null;

    /// <summary>Fired when a plugin is enabled.</summary>
    public event Action<PluginEnableEvent>? PluginEnable;
    internal bool HasPluginEnable => PluginEnable is not null;

    /// <summary>Fired when a plugin is disabled.</summary>
    public event Action<PluginDisableEvent>? PluginDisable;
    internal bool HasPluginDisable => PluginDisable is not null;

    /// <summary>Fired when a console command is executed.</summary>
    public event Action<ConsoleCommandEvent>? ConsoleCommand;
    internal bool HasConsoleCommand => ConsoleCommand is not null;

    // --- Dispatchers ---
    internal bool OnPlayerLogin(PlayerLoginEvent e) => Fire(PlayerLogin, e);
    internal void OnJoin(PlayerJoinEvent e) => Fire(PlayerJoin, e);
    internal void OnQuit(PlayerQuitEvent e) => Fire(PlayerQuit, e);
    internal bool OnMove(PlayerMoveEvent e) => Fire(PlayerMove, e);
    internal bool OnPlayerWarp(PlayerWarpEvent e) => Fire(PlayerWarp, e);
    internal bool OnPlayerCommand(PlayerCommandEvent e) => Fire(PlayerCommand, e);
    internal bool OnPlayerToggleAction(PlayerToggleActionEvent e) => Fire(PlayerToggleAction, e);
    internal bool OnChat(PlayerChatEvent e) => Fire(PlayerChat, e);

    internal bool OnPlayerDamage(PlayerDamageEvent e) => Fire(PlayerDamage, e);
    internal void OnPlayerDeath(PlayerDeathEvent e) => Fire(PlayerDeath, e);
    internal void OnPlayerRespawn(PlayerRespawnEvent e) => Fire(PlayerRespawn, e);
    internal bool OnPlayerHeal(PlayerHealEvent e) => Fire(PlayerHeal, e);
    internal bool OnPlayerCombust(PlayerCombustEvent e) => Fire(PlayerCombust, e);
    internal bool OnPlayerFoodChange(PlayerFoodChangeEvent e) => Fire(PlayerFoodChange, e);
    internal bool OnPlayerHungerChange(PlayerHungerChangeEvent e) => Fire(PlayerHungerChange, e);
    internal bool OnPlayerExperienceChange(PlayerExperienceChangeEvent e) => Fire(PlayerExperienceChange, e);
    internal bool OnPlayerExperiencePickup(PlayerExperiencePickupEvent e) => Fire(PlayerExperiencePickup, e);
    internal bool OnPlayerEffectChange(PlayerEffectChangeEvent e) => Fire(PlayerEffectChange, e);
    internal bool OnPlayerConsumeItem(PlayerConsumeItemEvent e) => Fire(PlayerConsumeItem, e);
    internal bool OnPlayerDropItem(PlayerDropItemEvent e) => Fire(PlayerDropItem, e);
    internal bool OnPlayerPickupItem(PlayerPickupItemEvent e) => Fire(PlayerPickupItem, e);
    internal bool OnPlayerHeldSlotChange(PlayerHeldSlotChangeEvent e) => Fire(PlayerHeldSlotChange, e);
    internal bool OnOffhandChange(OffhandChangeEvent e) => Fire(OffhandChange, e);
    internal bool OnItemDamage(ItemDamageEvent e) => Fire(ItemDamage, e);
    internal void OnItemBreak(ItemBreakEvent e) => Fire(ItemBreak, e);
    internal bool OnItemMend(ItemMendEvent e) => Fire(ItemMend, e);
    internal bool OnShieldBlock(ShieldBlockEvent e) => Fire(ShieldBlock, e);
    internal bool OnTotemUse(TotemUseEvent e) => Fire(TotemUse, e);
    internal bool OnPlayerMount(PlayerMountEvent e) => Fire(PlayerMount, e);
    internal bool OnVehicleMove(VehicleMoveEvent e) => Fire(VehicleMove, e);
    internal bool OnCrossbowLoad(CrossbowLoadEvent e) => Fire(CrossbowLoad, e);
    internal bool OnTridentRiptide(TridentRiptideEvent e) => Fire(TridentRiptide, e);
    internal bool OnPlayerFish(PlayerFishEvent e) => Fire(PlayerFish, e);
    internal bool OnHornBlow(HornBlowEvent e) => Fire(HornBlow, e);
    internal bool OnBookEdit(BookEditEvent e) => Fire(BookEdit, e);

    internal bool OnBreak(BlockEvent e) => Fire(BlockBreak, e);
    internal bool OnPlace(BlockEvent e) => Fire(BlockPlace, e);
    internal bool OnItemUse(ItemUseEvent e) => Fire(ItemUse, e);
    internal bool OnPlayerItemUse(PlayerItemUseEvent e) => Fire(PlayerItemUse, e);
    internal bool OnPlayerInteractBlock(PlayerInteractBlockEvent e) => Fire(PlayerInteractBlock, e);
    internal bool OnSignChange(SignChangeEvent e) => Fire(SignChange, e);
    internal bool OnBlockUse(BlockUseEvent e) => Fire(BlockUse, e);
    internal bool OnBlockGrow(BlockGrowEvent e) => Fire(BlockGrow, e);
    internal bool OnTreeGrow(TreeGrowEvent e) => Fire(TreeGrow, e);
    internal bool OnBlockFall(BlockFallEvent e) => Fire(BlockFall, e);
    internal bool OnBlockLand(BlockLandEvent e) => Fire(BlockLand, e);
    internal bool OnBlockForm(BlockFormEvent e) => Fire(BlockForm, e);
    internal bool OnBlockIgnite(BlockIgniteEvent e) => Fire(BlockIgnite, e);
    internal bool OnBlockBurn(BlockBurnEvent e) => Fire(BlockBurn, e);
    internal bool OnBlockSpread(BlockSpreadEvent e) => Fire(BlockSpread, e);
    internal bool OnTntPrime(TntPrimeEvent e) => Fire(TntPrime, e);
    internal bool OnNotePlay(NotePlayEvent e) => Fire(NotePlay, e);
    internal bool OnJukeboxChange(JukeboxChangeEvent e) => Fire(JukeboxChange, e);
    internal bool OnLecternChange(LecternChangeEvent e) => Fire(LecternChange, e);
    internal bool OnLecternPage(LecternPageEvent e) => Fire(LecternPage, e);
    internal bool OnItemCook(ItemCookEvent e) => Fire(ItemCook, e);
    internal bool OnFuelBurn(FuelBurnEvent e) => Fire(FuelBurn, e);
    internal bool OnFurnaceSmelt(FurnaceSmeltEvent e) => Fire(FurnaceSmelt, e);
    internal bool OnFurnaceBurn(FurnaceBurnEvent e) => Fire(FurnaceBurn, e);
    internal bool OnBrew(BrewEvent e) => Fire(Brew, e);
    internal bool OnBeaconEffect(BeaconEffectEvent e) => Fire(BeaconEffect, e);
    internal bool OnLiquidFlow(LiquidFlowEvent e) => Fire(LiquidFlow, e);
    internal bool OnFluidFlow(FluidFlowEvent e) => Fire(FluidFlow, e);
    internal bool OnSculkShriek(SculkShriekEvent e) => Fire(SculkShriek, e);

    internal bool OnExplosion(ExplosionEvent e) => Fire(Explosion, e);
    internal bool OnExplosionBlocks(ExplosionBlocksEvent e) => Fire(ExplosionBlocks, e);
    internal bool OnLightningStrike(LightningStrikeEvent e) => Fire(LightningStrike, e);
    internal bool OnRainChange(RainChangeEvent e) => Fire(RainChange, e);
    internal bool OnThunderChange(ThunderChangeEvent e) => Fire(ThunderChange, e);
    internal bool OnWeatherEffect(WeatherEffectEvent e) => Fire(WeatherEffect, e);
    internal bool OnTickRateChange(TickRateChangeEvent e) => Fire(TickRateChange, e);

    internal bool OnInventoryTransaction(InventoryTransactionEvent e) => Fire(InventoryTransaction, e);
    internal bool OnContainerOpen(ContainerOpenEvent e) => Fire(ContainerOpen, e);
    internal void OnContainerClose(ContainerCloseEvent e) => Fire(ContainerClose, e);
    internal bool OnCraftItem(CraftItemEvent e) => Fire(CraftItem, e);
    internal bool OnPlayerCraftItem(PlayerCraftItemEvent e) => Fire(PlayerCraftItem, e);
    internal bool OnEnchantItem(EnchantItemEvent e) => Fire(EnchantItem, e);
    internal bool OnPlayerEnchantItem(PlayerEnchantItemEvent e) => Fire(PlayerEnchantItem, e);
    internal bool OnAnvilUse(AnvilUseEvent e) => Fire(AnvilUse, e);
    internal bool OnGrindstoneUse(GrindstoneUseEvent e) => Fire(GrindstoneUse, e);
    internal bool OnSmithingUse(SmithingUseEvent e) => Fire(SmithingUse, e);
    internal bool OnBeaconChange(BeaconChangeEvent e) => Fire(BeaconChange, e);

    internal bool OnEntitySpawn(EntitySpawnEvent e) => Fire(EntitySpawn, e);
    internal void OnEntityDespawn(EntityDespawnEvent e) => Fire(EntityDespawn, e);
    internal bool OnEntityDamage(EntityDamageEvent e) => Fire(EntityDamage, e);
    internal void OnEntityDeath(EntityDeathEvent e) => Fire(EntityDeath, e);
    internal bool OnEntityPlace(EntityPlaceEvent e) => Fire(EntityPlace, e);
    internal bool OnEntityTouch(EntityTouchEvent e) => Fire(EntityTouch, e);
    internal bool OnEntityCombust(EntityCombustEvent e) => Fire(EntityCombust, e);
    internal bool OnEntityKilled(EntityKilledEvent e) => Fire(EntityKilled, e);
    internal bool OnArmorStandChange(ArmorStandChangeEvent e) => Fire(ArmorStandChange, e);
    internal bool OnMobTarget(MobTargetEvent e) => Fire(MobTarget, e);
    internal bool OnEntityTarget(EntityTargetEvent e) => Fire(EntityTarget, e);
    internal bool OnMobAttack(MobAttackEvent e) => Fire(MobAttack, e);
    internal bool OnMobBreed(MobBreedEvent e) => Fire(MobBreed, e);
    internal bool OnMobInteract(MobInteractEvent e) => Fire(MobInteract, e);
    internal bool OnPlayerInteractEntity(PlayerInteractEntityEvent e) => Fire(PlayerInteractEntity, e);
    internal bool OnNpcInteract(NpcInteractEvent e) => Fire(NpcInteract, e);
    internal bool OnMobTame(MobTameEvent e) => Fire(MobTame, e);
    internal bool OnMobLeash(MobLeashEvent e) => Fire(MobLeash, e);
    internal bool OnProjectileLaunch(ProjectileLaunchEvent e) => Fire(ProjectileLaunch, e);
    internal bool OnProjectileHit(ProjectileHitEvent e) => Fire(ProjectileHit, e);
    internal bool OnProjectilePickup(ProjectilePickupEvent e) => Fire(ProjectilePickup, e);
    internal bool OnPotionSplash(PotionSplashEvent e) => Fire(PotionSplash, e);
    internal bool OnVillagerProfession(VillagerProfessionEvent e) => Fire(VillagerProfession, e);
    internal bool OnVillagerTrade(VillagerTradeEvent e) => Fire(VillagerTrade, e);
    internal bool OnVillagerLevelUp(VillagerLevelUpEvent e) => Fire(VillagerLevelUp, e);

    internal bool OnRedstonePower(RedstonePowerEvent e) => Fire(RedstonePower, e);
    internal bool OnPressurePlate(PressurePlateEvent e) => Fire(PressurePlate, e);
    internal bool OnPistonMove(PistonMoveEvent e) => Fire(PistonMove, e);
    internal bool OnPistonPush(PistonPushEvent e) => Fire(PistonPush, e);
    internal bool OnBlockDispense(BlockDispenseEvent e) => Fire(BlockDispense, e);
    internal bool OnDispenserEquip(DispenserEquipEvent e) => Fire(DispenserEquip, e);
    internal bool OnItemTransfer(ItemTransferEvent e) => Fire(ItemTransfer, e);
    internal bool OnHopperPickup(HopperPickupEvent e) => Fire(HopperPickup, e);
    internal bool OnMinecartActivate(MinecartActivateEvent e) => Fire(MinecartActivate, e);
    internal bool OnMinecartTransfer(MinecartTransferEvent e) => Fire(MinecartTransfer, e);
    internal bool OnMinecartPickup(MinecartPickupEvent e) => Fire(MinecartPickup, e);
    internal bool OnTripwire(TripwireEvent e) => Fire(Tripwire, e);
    internal bool OnDetectorRail(DetectorRailEvent e) => Fire(DetectorRail, e);
    internal bool OnSculkVibration(SculkVibrationEvent e) => Fire(SculkVibration, e);
    internal bool OnCrafterCraft(CrafterCraftEvent e) => Fire(CrafterCraft, e);
    internal bool OnPortalCreate(PortalCreateEvent e) => Fire(PortalCreate, e);
    internal bool OnPlayerPortal(PlayerPortalEvent e) => Fire(PlayerPortal, e);
    internal bool OnEntityPortal(EntityPortalEvent e) => Fire(EntityPortal, e);
    internal bool OnEndGateway(EndGatewayEvent e) => Fire(EndGateway, e);
    internal bool OnDragonSpawn(DragonSpawnEvent e) => Fire(DragonSpawn, e);
    internal bool OnDragonPhase(DragonPhaseEvent e) => Fire(DragonPhase, e);
    internal void OnDragonDeath(DragonDeathEvent e) => Fire(DragonDeath, e);

    internal bool OnPlayerMissSwing(PlayerMissSwingEvent e) => Fire(PlayerMissSwing, e);
    internal bool OnPlayerToggleFlight(PlayerToggleFlightEvent e) => Fire(PlayerToggleFlight, e);
    internal bool OnPlayerToggleCrawl(PlayerToggleCrawlEvent e) => Fire(PlayerToggleCrawl, e);
    internal bool OnPlayerAttack(PlayerAttackEvent e) => Fire(PlayerAttack, e);
    internal bool OnPlayerFall(PlayerFallEvent e) => Fire(PlayerFall, e);
    internal void OnPlayerWorldChange(PlayerWorldChangeEvent e) => Fire(PlayerWorldChange, e);
    internal void OnPlayerChunkEnter(PlayerChunkEnterEvent e) => Fire(PlayerChunkEnter, e);
    internal void OnResourcePackClientResponse(ResourcePackClientResponseEvent e) => Fire(ResourcePackClientResponse, e);
    internal bool OnPlayerAfk(PlayerAfkEvent e) => Fire(PlayerAfk, e);
    internal bool OnPlayerPreLogin(PlayerPreLoginEvent e) => Fire(PlayerPreLogin, e);
    internal bool OnPlayerKick(PlayerKickEvent e) => Fire(PlayerKick, e);
    internal bool OnPlayerBedEnter(PlayerBedEnterEvent e) => Fire(PlayerBedEnter, e);
    internal void OnPlayerBedLeave(PlayerBedLeaveEvent e) => Fire(PlayerBedLeave, e);
    internal void OnPlayerSleepSkipNight(PlayerSleepSkipNightEvent e) => Fire(PlayerSleepSkipNight, e);
    internal void OnPlayerViewDistanceChange(PlayerViewDistanceChangeEvent e) => Fire(PlayerViewDistanceChange, e);
    internal bool OnPlayerGameModeChange(PlayerGameModeChangeEvent e) => Fire(PlayerGameModeChange, e);
    internal bool OnPlayerChangeSkin(PlayerChangeSkinEvent e) => Fire(PlayerChangeSkin, e);
    internal bool OnPlayerEmote(PlayerEmoteEvent e) => Fire(PlayerEmote, e);
    internal bool OnPlayerTransfer(PlayerTransferEvent e) => Fire(PlayerTransfer, e);
    internal bool OnPlayerFormResponse(PlayerFormResponseEvent e) => Fire(PlayerFormResponse, e);
    internal void OnPlayerInputModeChange(PlayerInputModeChangeEvent e) => Fire(PlayerInputModeChange, e);
    internal bool OnPlayerExhaust(PlayerExhaustEvent e) => Fire(PlayerExhaust, e);
    internal bool OnPlayerArmorChange(PlayerArmorChangeEvent e) => Fire(PlayerArmorChange, e);
    internal bool OnPlayerBucketFill(PlayerBucketFillEvent e) => Fire(PlayerBucketFill, e);
    internal bool OnPlayerBucketEmpty(PlayerBucketEmptyEvent e) => Fire(PlayerBucketEmpty, e);
    internal bool OnPlayerDimensionChange(PlayerDimensionChangeEvent e) => Fire(PlayerDimensionChange, e);
    internal void OnServerStarted(ServerStartedEvent e) => Fire(ServerStarted, e);
    internal void OnServerStop(ServerStopEvent e) => Fire(ServerStop, e);
    internal void OnPluginEnable(PluginEnableEvent e) => Fire(PluginEnable, e);
    internal void OnPluginDisable(PluginDisableEvent e) => Fire(PluginDisable, e);
    internal bool OnConsoleCommand(ConsoleCommandEvent e) => Fire(ConsoleCommand, e);
    internal bool OnDataPacketReceive(DataPacketReceiveEvent e) => Fire(DataPacketReceive, e);
    internal bool OnDataPacketSend(DataPacketSendEvent e) => Fire(DataPacketSend, e);
    internal void OnWorldLoad(WorldLoadEvent e) => Fire(WorldLoad, e);
    internal void OnWorldSave(WorldSaveEvent e) => Fire(WorldSave, e);
    internal bool OnSpawnChange(SpawnChangeEvent e) => Fire(SpawnChange, e);
    internal void OnChunkLoad(ChunkLoadEvent e) => Fire(ChunkLoad, e);
    internal void OnChunkPopulate(ChunkPopulateEvent e) => Fire(ChunkPopulate, e);
    internal bool OnTimeChange(TimeChangeEvent e) => Fire(TimeChange, e);
    internal bool OnStonecutterUse(StonecutterUseEvent e) => Fire(StonecutterUse, e);
    internal bool OnLeavesDecay(LeavesDecayEvent e) => Fire(LeavesDecay, e);
    internal bool OnMoistureChange(MoistureChangeEvent e) => Fire(MoistureChange, e);
    internal void OnVehicleCreate(VehicleCreateEvent e) => Fire(VehicleCreate, e);
    internal bool OnVehicleDamage(VehicleDamageEvent e) => Fire(VehicleDamage, e);
    internal void OnVehicleDestroy(VehicleDestroyEvent e) => Fire(VehicleDestroy, e);
    internal bool OnVehicleEnter(VehicleEnterEvent e) => Fire(VehicleEnter, e);
    internal bool OnVehicleExit(VehicleExitEvent e) => Fire(VehicleExit, e);
    internal bool OnSheepShear(SheepShearEvent e) => Fire(SheepShear, e);
    internal bool OnSheepRegrowWool(SheepRegrowWoolEvent e) => Fire(SheepRegrowWool, e);
    internal bool OnFireworkExplode(FireworkExplodeEvent e) => Fire(FireworkExplode, e);

    /// <summary>Registers a listener for missed swings.</summary>
    public void on_player_swing_miss(Action<PlayerMissSwingEvent> handler) => PlayerMissSwing += handler;

    /// <summary>Registers a listener for toggling flight.</summary>
    public void on_player_fly_toggle(Action<PlayerToggleFlightEvent> handler) => PlayerToggleFlight += handler;

    /// <summary>Registers a listener for crawling toggles.</summary>
    public void on_player_crawl(Action<PlayerToggleCrawlEvent> handler) => PlayerToggleCrawl += handler;

    /// <summary>Registers a listener for player attacks.</summary>
    public void on_player_attack(Action<PlayerAttackEvent> handler) => PlayerAttack += handler;

    /// <summary>Registers a listener for player falls.</summary>
    public void on_player_fall(Action<PlayerFallEvent> handler) => PlayerFall += handler;

    /// <summary>Registers a listener for world changes.</summary>
    public void on_player_world_change(Action<PlayerWorldChangeEvent> handler) => PlayerWorldChange += handler;

    /// <summary>Registers a listener for chunk transitions.</summary>
    public void on_player_chunk_enter(Action<PlayerChunkEnterEvent> handler) => PlayerChunkEnter += handler;

    /// <summary>Registers a listener for resource pack responses.</summary>
    public void on_resource_pack_status(Action<ResourcePackClientResponseEvent> handler) => ResourcePackClientResponse += handler;

    /// <summary>Registers a listener for AFK status changes.</summary>
    public void on_player_afk(Action<PlayerAfkEvent> handler) => PlayerAfk += handler;

    internal void OnTick(long tick) => Fire(Tick, tick);

    private readonly HashSet<Delegate> _reported = [];

    /// <summary>Resets all registered event listeners and clears reported crashes.</summary>
    public void Clear()
    {
        // Player connection & movement
        PlayerLogin = null;
        PlayerJoin = null;
        PlayerQuit = null;
        PlayerMove = null;
        PlayerWarp = null;
        PlayerCommand = null;
        PlayerToggleAction = null;
        PlayerChat = null;

        // Player combat, health, effects & equipment
        PlayerDamage = null;
        PlayerDeath = null;
        PlayerRespawn = null;
        PlayerHeal = null;
        PlayerCombust = null;
        PlayerFoodChange = null;
        PlayerHungerChange = null;
        PlayerExperienceChange = null;
        PlayerExperiencePickup = null;
        PlayerEffectChange = null;
        PlayerConsumeItem = null;
        PlayerDropItem = null;
        PlayerPickupItem = null;
        PlayerHeldSlotChange = null;
        OffhandChange = null;
        ItemDamage = null;
        ItemBreak = null;
        ItemMend = null;
        ShieldBlock = null;
        TotemUse = null;
        PlayerMount = null;
        VehicleMove = null;
        CrossbowLoad = null;
        TridentRiptide = null;
        PlayerFish = null;
        HornBlow = null;
        BookEdit = null;

        // Blocks & Block behaviours
        BlockBreak = null;
        BlockPlace = null;
        ItemUse = null;
        PlayerItemUse = null;
        PlayerInteractBlock = null;
        SignChange = null;
        BlockUse = null;
        BlockGrow = null;
        TreeGrow = null;
        BlockFall = null;
        BlockLand = null;
        BlockForm = null;
        BlockIgnite = null;
        BlockBurn = null;
        BlockSpread = null;
        TntPrime = null;
        NotePlay = null;
        JukeboxChange = null;
        LecternChange = null;
        LecternPage = null;
        ItemCook = null;
        FuelBurn = null;
        FurnaceSmelt = null;
        FurnaceBurn = null;
        Brew = null;
        BeaconEffect = null;
        LiquidFlow = null;
        FluidFlow = null;
        SculkShriek = null;

        // World & Weather
        Explosion = null;
        ExplosionBlocks = null;
        LightningStrike = null;
        RainChange = null;
        ThunderChange = null;
        WeatherEffect = null;
        TickRateChange = null;

        // Inventory, Containers & Crafting Stations
        InventoryTransaction = null;
        ContainerOpen = null;
        ContainerClose = null;
        CraftItem = null;
        PlayerCraftItem = null;
        EnchantItem = null;
        PlayerEnchantItem = null;
        AnvilUse = null;
        GrindstoneUse = null;
        SmithingUse = null;
        BeaconChange = null;

        // Entities, Mobs, Projectiles & Villagers
        EntitySpawn = null;
        EntityDespawn = null;
        EntityDamage = null;
        EntityDeath = null;
        EntityPlace = null;
        EntityTouch = null;
        EntityCombust = null;
        EntityKilled = null;
        ArmorStandChange = null;
        MobTarget = null;
        EntityTarget = null;
        MobAttack = null;
        MobBreed = null;
        MobInteract = null;
        PlayerInteractEntity = null;
        NpcInteract = null;
        MobTame = null;
        MobLeash = null;
        ProjectileLaunch = null;
        ProjectileHit = null;
        ProjectilePickup = null;
        PotionSplash = null;
        VillagerProfession = null;
        VillagerTrade = null;
        VillagerLevelUp = null;

        // Redstone, Portals & Dragon
        RedstonePower = null;
        PressurePlate = null;
        PistonMove = null;
        PistonPush = null;
        BlockDispense = null;
        DispenserEquip = null;
        ItemTransfer = null;
        HopperPickup = null;
        MinecartActivate = null;
        MinecartTransfer = null;
        MinecartPickup = null;
        Tripwire = null;
        DetectorRail = null;
        SculkVibration = null;
        CrafterCraft = null;
        PortalCreate = null;
        PlayerPortal = null;
        EntityPortal = null;
        EndGateway = null;
        DragonSpawn = null;
        DragonPhase = null;
        DragonDeath = null;

        PlayerMissSwing = null;
        PlayerToggleFlight = null;
        PlayerToggleCrawl = null;
        PlayerWorldChange = null;
        PlayerChunkEnter = null;
        ResourcePackClientResponse = null;
        PlayerAfk = null;
        PlayerPreLogin = null;
        PlayerKick = null;
        PlayerViewDistanceChange = null;
        PlayerGameModeChange = null;
        PlayerChangeSkin = null;
        PlayerEmote = null;
        PlayerTransfer = null;
        PlayerFormResponse = null;
        PlayerInputModeChange = null;
        DataPacketReceive = null;
        DataPacketSend = null;
        PlayerAttack = null;
        PlayerFall = null;
        PlayerExhaust = null;
        PlayerArmorChange = null;
        PlayerBedEnter = null;
        PlayerBedLeave = null;
        PlayerSleepSkipNight = null;
        PlayerBucketFill = null;
        PlayerBucketEmpty = null;
        PlayerDimensionChange = null;
        StonecutterUse = null;
        LeavesDecay = null;
        MoistureChange = null;
        WorldLoad = null;
        WorldSave = null;
        SpawnChange = null;
        ChunkLoad = null;
        ChunkPopulate = null;
        TimeChange = null;
        VehicleCreate = null;
        VehicleDamage = null;
        VehicleDestroy = null;
        VehicleEnter = null;
        VehicleExit = null;
        SheepShear = null;
        SheepRegrowWool = null;
        FireworkExplode = null;
        ServerStarted = null;
        ServerStop = null;
        PluginEnable = null;
        PluginDisable = null;
        ConsoleCommand = null;
        Tick = null;
        _reported.Clear();
    }

    /// <summary>While /timings runs: the time of each plugin's handlers, per plugin and event.</summary>
    internal Performance.TimingsManager? Timings { get; set; }

    private readonly System.Collections.Concurrent.ConcurrentDictionary<(System.Reflection.Assembly, string), Performance.TimingRecord?> _pluginRecords = new();

    // The plugin a handler belongs to (null for the server's own handlers).
    private Performance.TimingRecord? RecordOf(Delegate handler, string name)
    {
        var assembly = handler.Method.DeclaringType?.Assembly ?? typeof(ServerEvents).Assembly;
        if (_pluginRecords.TryGetValue((assembly, name), out var record))
            return record;
        return _pluginRecords.GetOrAdd((assembly, name), key => key.Item1 == typeof(ServerEvents).Assembly ? null
            : Timings!.GetOrCreate($"{key.Item1.GetName().Name}: {key.Item2}", "Plugin:" + key.Item1.GetName().Name));
    }

    // Returns false when a handler cancelled the event.
    private bool Fire<T>(Action<T>? handlers, T e, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(handlers))] string name = "")
    {
        if (handlers is null)
            return true;
        var timed = Timings is { IsEnabled: true };
        if (handlers.HasSingleTarget && !timed)
        {
            Invoke(handlers, e, name); // the common case, without the invocation list array
            return e is not CancellableEvent { Cancelled: true };
        }
        foreach (var handler in handlers.GetInvocationList().Cast<Action<T>>())
        {
            using (timed ? Timings!.Track(RecordOf(handler, name)) : default)
                Invoke(handler, e, name);
        }
        return e is not CancellableEvent { Cancelled: true };
    }

    private void Invoke<T>(Action<T> handler, T e, string name)
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
}
