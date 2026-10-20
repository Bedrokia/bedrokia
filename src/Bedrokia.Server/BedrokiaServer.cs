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

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Bedrokia.Protocol;
using Bedrokia.Protocol.Login;
using Bedrokia.Server.Commands;
using Bedrokia.Server.Core;
using Bedrokia.Server.Entities;
using Bedrokia.Server.Game;
using Bedrokia.Server.Network;
using Bedrokia.Server.Performance;
using Bedrokia.Server.Permissions;
using Bedrokia.Server.Plugins;
using Bedrokia.Transport.NetherNet;
using System.Threading.Channels;

namespace Bedrokia.Server;

/// <summary>
/// Server lifecycle and game loop. All game state (connections, players) is touched from the tick loop only;
/// network and console threads hand work over through <see cref="RunOnMainThread"/>.
/// </summary>
public sealed partial class BedrokiaServer
{
    public const int TicksPerSecond = 20;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly TimeSpan AuthKeysRefresh = TimeSpan.FromMinutes(30);

    private readonly Channel<InboundWorkItem> _mainThread = Channel.CreateUnbounded<InboundWorkItem>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });
    private readonly CancellationTokenSource _stop = new();
    private readonly CancellationTokenSource _networkStop = new();

    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly AuthKeys _authKeys = new(Http);
    private readonly List<BedrockConnection> _connections = [];
    private readonly List<BedrockConnection> _players = []; // logged in (spawning or in game)
    private long _nextEntityId;
    private NetherNetListener _netherNet = null!;

    /// <summary>Mobs of the overworld (spawning, AI, combat).</summary>
    public MobManager Mobs { get; }

    /// <summary>Non-player characters spawned by plugins.</summary>
    public NpcManager Npcs { get; }

    /// <summary>Thrown snowballs, eggs, ender pearls and splash potions.</summary>
    public ProjectileManager Projectiles { get; }

    /// <summary>Packs from resource_packs/, sent to players at login.</summary>
    public ResourcePacks ResourcePacks { get; private set; } = null!;

    /// <summary>Flowing water and lava of the overworld (each world has its own: <see cref="World.Fluids"/>).</summary>
    public Fluids Fluids => World.Fluids;

    /// <summary>Sand and gravel falling as entities in the overworld (see <see cref="World.FallingBlocks"/>).</summary>
    public FallingBlocks FallingBlocks => World.FallingBlocks;

    /// <summary>The Nether: worlds/&lt;level-name&gt;/nether/.</summary>
    public World Nether { get; private set; } = null!;

    /// <summary>Every open world (overworld, then the Nether).</summary>
    public IEnumerable<World> Worlds => new[] { World, Nether }.Where(w => w is not null); // none before RunAsync

    /// <summary>The open world of a dimension (0 overworld, 1 nether).</summary>
    public World WorldOf(int dimension) => dimension == 1 && Nether is not null ? Nether : World;

    // Each world simulates its own items on the ground, liquids and falling blocks.
    private void Attach(World world)
    {
        world.DroppedItems = new DroppedItems(world);
        world.Fluids = new Fluids(this, world);
        world.FallingBlocks = new FallingBlocks(this, world);
    }

    internal long NextEntityId() => Interlocked.Increment(ref _nextEntityId);

    public PlayerList Ops { get; } = new("ops.txt");
    public PlayerList Whitelist { get; } = new("white-list.txt");
    public PlayerList Bans { get; } = new("banned-players.txt");
    public PermissionManager Permissions { get; }

    /// <summary>Why <paramref name="name"/> may not join (banned, not whitelisted), or null.</summary>
    internal string? AdmissionRefusal(string name) =>
        Bans.Contains(name) ? "You are banned from this server."
        : Config.Whitelist && !Whitelist.Contains(name) && !Ops.Contains(name) ? "You are not white-listed on this server."
        : null;

    public BedrokiaServer(ServerConfig config, Logger logger, PermissionManager? permissions = null)
    {
        Config = config;
        Logger = logger;
        Permissions = permissions ?? new("permissions.json");
        if (Permissions.LoadError is { } permissionsError)
            logger.Error(permissionsError);
        Commands = new CommandManager(logger);
        Mobs = new MobManager(this);
        Npcs = new NpcManager(this);
        Projectiles = new ProjectileManager(this);
        Events = new ServerEvents(logger);
        Scheduler = new ServerScheduler(logger);
        Plugins = new PluginManager(this);
        RegisterBuiltinCommands();
    }

    /// <summary>Folder of the resource packs (relative to the working directory, like PocketMine-MP).</summary>
    public string ResourcePacksDirectory { get; init; } = "resource_packs";

    /// <summary>Folders of the plugin DLLs and of their data (plugin_data/&lt;name&gt;/).</summary>
    public string PluginsDirectory { get; init; } = "plugins";

    public string PluginDataDirectory { get; init; } = "plugin_data";

    public ServerEvents Events { get; }
    public ServerScheduler Scheduler { get; }
    public PluginManager Plugins { get; }

    public static string Version { get; } =
        typeof(BedrokiaServer).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

    public ServerConfig Config { get; }
    public Logger Logger { get; }
    public CommandManager Commands { get; }
    public long CurrentTick { get; private set; }
    private bool _ticking;
    private int _tickThread;

    /// <summary>True on the game thread while a tick runs (other threads see false: what they send is not buffered).</summary>
    public bool IsTicking => _ticking && Environment.CurrentManagedThreadId == _tickThread;

    /// <summary>The overworld; available once <see cref="Started"/> has completed.</summary>
    public World World { get; private set; } = null!;

    internal void InitWorldForTesting(World? world = null)
    {
        World = world ?? World.OpenOrCreate(Path.Combine(Config.WorldsDirectory, Config.LevelName), Config.Seed);
        World.GetChunkAsync(0, 0).GetAwaiter().GetResult();
        Attach(World);
    }

    /// <summary>Authorization service keys, or null when offline mode is on or they are not fetched yet.</summary>
    public AuthKeys.Snapshot? AuthKeys => Config.OnlineMode ? _authKeys.Current : null;

    /// <summary>Players that finished logging in, including those still loading the world.</summary>
    public IReadOnlyList<BedrockConnection> Players => _players;
    internal List<BedrockConnection> PlayersList => _players;
    internal void AddPlayer(BedrockConnection connection) => _players.Add(connection);

    /// <summary>Server performance and TPS tracker.</summary>
    public PerformanceTracker Performance { get; } = new();

    /// <summary>Subsystem profiling and timings manager (PocketMine-MP / Paper style).</summary>
    public TimingsManager Timings { get; } = new();

    /// <summary>Total number of entities active in the world (mobs, items, falling blocks, players).</summary>
    public int TotalEntitiesCount =>
        Mobs.All.Count + Worlds.Sum(w => (w.DroppedItems?.All.Count ?? 0) + (w.FallingBlocks?.Count ?? 0)) + _players.Count;

    private long _lastTickTimestamp;

    /// <summary>Completes once the network port is bound (faults if binding fails).</summary>
    public Task Started => _started.Task;

    /// <summary>NetherNet signaling address, available after <see cref="Started"/> when NetherNet is enabled.</summary>
    public IPEndPoint? NetherNetEndPoint => _netherNet?.LocalEndPoint;

    /// <summary>Runs until <see cref="Stop"/> is called.</summary>
    /// <exception cref="System.Net.Sockets.SocketException">The port cannot be bound.</exception>
    public async Task RunAsync()
    {
        Logger.Info($"Starting {Product.Name} {Version} (Bedrock {ProtocolInfo.GameVersion}, protocol {ProtocolInfo.Version})");
        try
        {
            Game.Import.BedrockWorldImport.ImportAll(Config.WorldsDirectory, Logger);
            ResourcePacks = ResourcePacks.Load(ResourcePacksDirectory, Logger);
            World = World.OpenOrCreate(Path.Combine(Config.WorldsDirectory, Config.LevelName), Config.Seed);
            Attach(World);
            var netherFolder = Path.Combine(World.Folder, "nether");
            if (!File.Exists(Path.Combine(netherFolder, "world.properties")))
            {
                Directory.CreateDirectory(netherFolder);
                File.WriteAllText(Path.Combine(netherFolder, "world.properties"), "generator=nether\n");
            }
            Nether = World.OpenOrCreate(netherFolder, World.Generator.Seed);
            Attach(Nether);
            Plugins.LoadAll(PluginsDirectory, PluginDataDirectory);
            Logger.Info($"World '{Config.LevelName}': seed {World.Generator.Seed}, spawn {World.Spawn}.");
            _netherNet = new NetherNetListener(new IPEndPoint(IPAddress.Any, Config.Port),
                    ServerIdentityKey.LoadOrCreate(Path.Combine("data", "nethernet-identity.pem")),
                    VerifyNetherNetToken, allowAnonymous: !Config.OnlineMode, Config.ParseWebRtcPorts())
            {
                // Pterodactyl gives 0.0.0.0 for an allocation on every address: not one players can reach.
                PublicAddress = IPAddress.TryParse(Config.PublicIp, out var publicIp) && !publicIp.Equals(IPAddress.Any) ? publicIp : null,
            };
        }
        catch (Exception e)
        {
            _started.TrySetException(e);
            throw;
        }

        _netherNet.SessionOpened += OnNetherNetSessionOpened;
        _netherNet.Trace += message => Logger.Debug(message);
        using (_netherNet)
        {
            var signaling = _netherNet.RunAsync(_networkStop.Token);
            var auth = Config.OnlineMode ? RefreshAuthKeysAsync(_stop.Token) : Task.CompletedTask;
            // Port 0 is the tests' in-memory server: no network calls there.
            var updates = Config.CheckUpdates && Config.Port != 0 ? UpdateChecker.RunAsync(Version, Logger, _stop.Token) : Task.CompletedTask;
            if (!Config.OnlineMode)
                Logger.Warn("OnlineMode is off: player identities are NOT verified. Anyone can join under any name.");
            var (firstUdp, lastUdp) = Config.WebRtcPortRange();
            Logger.Info($"Listening on port {_netherNet.LocalEndPoint.Port}: TCP (join) + UDP {firstUdp}-{lastUdp} (players). Type 'help' for commands.");
            _started.TrySetResult();

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1.0 / TicksPerSecond));
            try
            {
                while (await timer.WaitForNextTickAsync(_stop.Token))
                {
                    try
                    {
                        Tick();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Unhandled tick error: {ex}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }

            foreach (var connection in _connections.ToList())
                connection.Disconnect("Server closed.", DisconnectReason.Shutdown);
            Plugins.DisableAll();
            SaveWorld();
            await Task.Delay(600); // let NetherNet sessions flush their Disconnect packet
            _networkStop.Cancel();
            await signaling;
            await auth;
            await updates;
        }
        Logger.Info("Server stopped.");
    }

    /// <summary>Requests a clean shutdown. Safe to call from any thread, more than once.</summary>
    public void Stop()
    {
        if (!_stop.IsCancellationRequested)
            Logger.Info("Stopping server...");
        _stop.Cancel();
    }

    /// <summary>Runs <paramref name="action"/> on the game thread during the next tick. Safe to call from any thread.</summary>
    public void RunOnMainThread(Action action) => _mainThread.Writer.TryWrite(new InboundWorkItem(null, null, 0, action));

    internal void EnqueueInboundPacket(BedrockConnection connection, byte[] pooledPacket, int length) =>
        _mainThread.Writer.TryWrite(new InboundWorkItem(connection, pooledPacket, length, null));

    /// <summary>Queues a console line; it runs on the next tick. Safe to call from any thread.</summary>
    public void EnqueueCommand(string line) => RunOnMainThread(() => Commands.Dispatch(line));

    /// <summary>Dispatches a pre-encoded batch directly to target connections without redundant re-encoding.</summary>
    public void BroadcastPreEncoded(byte[] framedData, IEnumerable<BedrockConnection> targets, bool preferUnreliable = false)
    {
        foreach (var player in targets)
            player.SendRawBatch(framedData, preferUnreliable);
    }

    /// <summary>Sends <paramref name="packet"/> to every player in game and logs <paramref name="log"/>.</summary>
    public void Broadcast(byte[] packet, string log)
    {
        Logger.Info(log);
        Broadcast(packet);
    }

    public void BroadcastMessage(string message) => Broadcast(Packets.Text(TextType.Raw, message), StripFormatting(message));

    /// <summary>Sends <paramref name="packet"/> to every player in game (encoded once for all targets).</summary>
    public void Broadcast(byte[] packet)
    {
        var targets = _players.Where(p => p.IsInGame).ToList();
        if (targets.Count == 0)
            return;
        if (targets.Count == 1)
        {
            targets[0].Send(packet);
            return;
        }
        var preEncoded = targets[0].Codec.Encode(packet);
        foreach (var player in targets)
            player.SendRawBatch(preEncoded);
    }

    public void BroadcastExcept(BedrockConnection except, byte[] packet)
    {
        var targets = _players.Where(p => p.IsInGame && p != except).ToList();
        if (targets.Count == 0)
            return;
        if (targets.Count == 1)
        {
            targets[0].Send(packet);
            return;
        }
        var preEncoded = targets[0].Codec.Encode(packet);
        foreach (var player in targets)
            player.SendRawBatch(preEncoded);
    }

    /// <summary>Sends <paramref name="packet"/> to the players in <paramref name="world"/> only (blocks, entities, effects).</summary>
    public void BroadcastIn(World world, byte[] packet)
    {
        var targets = _players.Where(p => p.IsInGame && p.World == world).ToList();
        if (targets.Count == 0)
            return;
        if (targets.Count == 1)
        {
            targets[0].Send(packet);
            return;
        }
        var preEncoded = targets[0].Codec.Encode(packet);
        foreach (var player in targets)
            player.SendRawBatch(preEncoded);
    }

    public void BroadcastInExcept(World world, BedrockConnection except, byte[] packet)
    {
        var targets = _players.Where(p => p.IsInGame && p != except && p.World == world).ToList();
        if (targets.Count == 0)
            return;
        if (targets.Count == 1)
        {
            targets[0].Send(packet);
            return;
        }
        var preEncoded = targets[0].Codec.Encode(packet);
        foreach (var player in targets)
            player.SendRawBatch(preEncoded);
    }

    /// <summary>Sends <paramref name="packet"/> to viewers of chunk (chunkX, chunkZ), or world-wide if chunk not cached.</summary>
    public void BroadcastInChunk(World world, int chunkX, int chunkZ, byte[] packet)
    {
        var chunk = world.LoadedChunk(chunkX, chunkZ);
        if (chunk is not null)
        {
            if (chunk.Viewers.Count == 0)
                return;
            if (chunk.Viewers.Count == 1)
            {
                var single = chunk.Viewers.First();
                if (single.IsInGame)
                    single.Send(packet);
                return;
            }
            var preEncoded = chunk.Viewers.First().Codec.Encode(packet);
            foreach (var viewer in chunk.Viewers)
                if (viewer.IsInGame)
                    viewer.SendRawBatch(preEncoded);
            return;
        }
        BroadcastIn(world, packet);
    }


    // "§e" colour codes are for the game, not for the console.
    private static string StripFormatting(string text) => System.Text.RegularExpressions.Regex.Replace(text, "§.", "");

    private void SavePlayer(BedrockConnection player, bool leaving = false)
    {
        // Items left in the crafting grid or on the cursor go back to the inventory (dropped if full).
        if (leaving)
            foreach (var stack in player.Inventory.ReturnCraftingItems())
                DropItem(stack, player.Position.X, player.Position.Y - BedrockConnection.EyeHeight + 0.5f, player.Position.Z);
        try
        {
            player.Snapshot().Write(player.Info!.Uuid);
        }
        catch (IOException e)
        {
            Logger.Error($"Could not save {player.Name}: {e.Message}");
        }
    }

    private void SaveWorld()
    {
        try
        {
            var saved = Worlds.Sum(w => w.Save());
            foreach (var player in _players.Where(p => p.IsInGame))
                player.Snapshot().Write(player.Info!.Uuid);
            if (saved > 0)
                Logger.Debug($"Saved {saved} chunk(s).");
        }
        catch (IOException e)
        {
            Logger.Error($"Could not save the world: {e.Message}");
        }
    }

    /// <summary>
    /// Gravity blocks (sand, red sand, gravel, concrete powder) from (x, y, z) upwards fall onto the first
    /// supporting block below. Call after a block changes at or under (x, y, z). Default world: the overworld.
    /// </summary>
    public void ApplyGravity(int x, int y, int z, World? world = null)
    {
        world ??= World;
        for (var cy = y; cy < Chunk.MaxY; cy++)
        {
            var block = world.GetBlock(x, cy, z);
            if (block is null || !Falls(block))
                return;
            if (BlockPhysics.IsSolid(world.GetBlock(x, cy - 1, z)) || cy <= Chunk.MinY)
                return; // supported: nothing above it can fall either
            world.SetBlock(x, cy, z, Blocks.Air);
            BroadcastIn(world, Packets.UpdateBlock(x, cy, z, Blocks.Air.NetworkId));
            world.FallingBlocks.Spawn(x, cy, z, block); // the blocks above follow in the next loop turns
        }
    }

    private static bool Falls(BlockType block) =>
        block.Name is "minecraft:sand" or "minecraft:red_sand" or "minecraft:gravel" || block.Name.EndsWith("_concrete_powder")
        || Anvil.IsAnvilBlock(block.Name);

    /// <summary>Removes every item lying on the ground in every world (a "clear lag"); returns how many there were.</summary>
    public int ClearDroppedItems()
    {
        var count = 0;
        foreach (var world in Worlds)
            foreach (var item in world.DroppedItems?.All.ToList() ?? [])
            {
                world.DroppedItems!.Remove(item);
                BroadcastIn(world, Packets.RemoveActor(item.EntityId));
                count++;
            }
        return count;
    }

    /// <summary>Drops <paramref name="stack"/> at the given position (it falls to the ground). Default world: the overworld.</summary>
    public void DropItem(ItemStack stack, float x, float y, float z, (float X, float Y, float Z)? velocity = null,
        int pickupDelay = DroppedItems.PickupDelayTicks, World? world = null)
    {
        world ??= World;
        if (world?.DroppedItems is null)
            return;
        var item = world.DroppedItems.Spawn(Interlocked.Increment(ref _nextEntityId), stack, x, y, z, CurrentTick);
        if (velocity is { } v)
            (item.Vx, item.Vy, item.Vz) = v;
        item.PickupDelay = pickupDelay;
        item.SentPosition = (item.X, item.Y, item.Z);
        item.SentVelocity = (item.Vx, item.Vy, item.Vz);
        BroadcastIn(world, InventoryPackets.AddItemActor(item.EntityId, stack, item.X, item.Y + DroppedItems.NetworkOffset, item.Z, item.Vx, item.Vy, item.Vz));
    }

    internal void TickDroppedItems(World world)
    {
        var items = world.DroppedItems;
        if (items is null)
            return;
        items.Tick();
        // Only what changed, with the item's network offset (Dragonfly Movement.Send / ViewEntityMovement).
        foreach (var item in items.All)
        {
            var position = (item.X, item.Y, item.Z);
            var (sx, sy, sz) = item.SentPosition;
            if (float.IsNaN(sx) || MathF.Abs(position.X - sx) + MathF.Abs(position.Y - sy) + MathF.Abs(position.Z - sz) > 0.001f)
            {
                item.SentPosition = position;
                BroadcastIn(world, EntityPackets.MoveAbsolute(item.EntityId, item.X, item.Y + DroppedItems.NetworkOffset, item.Z, 0, 0, item.OnGround));
            }
            var velocity = (item.Vx, item.Vy, item.Vz);
            if (velocity != item.SentVelocity)
            {
                item.SentVelocity = velocity;
                BroadcastIn(world, EntityPackets.Motion(item.EntityId, item.Vx, item.Vy, item.Vz));
            }
        }

        foreach (var item in items.Expire(CurrentTick))
            BroadcastIn(world, Packets.RemoveActor(item.EntityId));

        var worldPlayers = _players.Where(p => p.IsInGame && p.World == world).ToList();
        if (worldPlayers.Count == 0 || items.All.Count == 0)
            return;

        var pickedUp = new HashSet<DroppedItem>();
        foreach (var player in worldPlayers)
        {
            var (pcx, pcz) = player.ChunkPosition;
            for (var dx = -1; dx <= 1; dx++)
            for (var dz = -1; dz <= 1; dz++)
            {
                var candidateItems = items.InChunk(pcx + dx, pcz + dz);
                if (candidateItems is null || candidateItems.Count == 0)
                    continue;

                foreach (var item in candidateItems.ToList())
                {
                    if (pickedUp.Contains(item) || CurrentTick - item.SpawnTick < item.PickupDelay)
                        continue; // just dropped: not yet (the player who threw it would take it back at once)
                    if (Math.Abs(player.Position.X - item.X) < 1.5 && Math.Abs(player.Position.Z - item.Z) < 1.5
                        && Math.Abs(player.Position.Y - 1.62 - item.Y) < 2)
                    {
                        var stack = item.Stack;
                        if (Events.HasPlayerPickupItem)
                        {
                            var ev = new PlayerPickupItemEvent(player, item);
                            if (!Events.OnPlayerPickupItem(ev))
                                continue;
                            stack = ev.Item; // a plugin may change what is picked up
                        }
                        var (packets, remaining) = player.Inventory.Add(stack);
                        if (remaining == stack.Count)
                            continue; // inventory full
                        player.Send([.. packets]);
                        items.Remove(item);
                        pickedUp.Add(item);
                        BroadcastIn(world, InventoryPackets.TakeItemActor(item.EntityId, player.EntityId));
                        BroadcastIn(world, Packets.RemoveActor(item.EntityId));
                        if (remaining > 0)
                            DropItem(stack.Split(remaining), item.X, item.Y, item.Z, world: world); // ponytail: leftover respawns as a new entity
                    }
                }
            }
        }
    }


    internal void Tick()
    {
        var tickStart = Stopwatch.GetTimestamp();
        double intervalMs = _lastTickTimestamp > 0 ? Stopwatch.GetElapsedTime(_lastTickTimestamp, tickStart).TotalMilliseconds : 50.0;
        _lastTickTimestamp = tickStart;
        var wasTicking = _ticking;
        _ticking = true;
        _tickThread = Environment.CurrentManagedThreadId;

        try
        {
            using (Timings.Track(Timings.FullTick))
            {
                CurrentTick++;
                if (World is not null && World.Settings.DaylightCycle)
                    World.Time++;

                using (Timings.Track(Timings.DroppedItems))
                    foreach (var world in Worlds)
                        TickDroppedItems(world);

                using (Timings.Track(Timings.Mobs))
                {
                    Mobs.Tick(CurrentTick);
                    Npcs.Tick(CurrentTick);
                    Projectiles.Tick();
                }

                using (Timings.Track(Timings.FallingBlocks))
                    foreach (var world in Worlds)
                        world.FallingBlocks.Tick();

                using (Timings.Track(Timings.Furnaces))
                    TickFurnaces();

                using (Timings.Track(Timings.Blocks))
                    TickBlocks();

                using (Timings.Track(Timings.Events))
                {
                    Scheduler.Tick(CurrentTick);
                    Events.OnTick(CurrentTick);
                }

                using (Timings.Track(Timings.Players))
                {
                    foreach (var player in _players.Where(p => p.IsInGame))
                        player.TickHealth();
                }

                if (World is not null && CurrentTick % (TicksPerSecond * 30) == 0)
                    Broadcast(Packets.SetTime((int)(World.Time % int.MaxValue)));

                if (World is not null && CurrentTick % (TicksPerSecond * Config.AutosaveInterval) == 0)
                {
                    using (Timings.Track(Timings.World))
                    {
                        SaveWorld();
                        foreach (var world in Worlds)
                            world.Unload((x, z) => _players.Any(p =>
                        {
                            if (p.World != world)
                                return false;
                            var (px, pz) = p.ChunkPosition;
                            var r = p.ViewRadius + 2;
                            return Math.Abs(px - x) <= r && Math.Abs(pz - z) <= r;
                        }));
                    }
                }

                using (Timings.Track(Timings.MainThread))
                {
                    var reader = _mainThread.Reader;
                    while (reader.TryRead(out var item))
                    {
                        if (item.ControlAction is { } action)
                        {
                            try
                            {
                                action();
                            }
                            catch (Exception e)
                            {
                                Logger.Error($"Unhandled error in tick {CurrentTick}: {e}");
                            }
                        }
                        else if (item.Connection is { } conn && item.PooledPacket is { } pooled)
                        {
                            try
                            {
                                conn.HandlePacket(pooled, item.Length);
                            }
                            catch (Exception e)
                            {
                                Logger.Error($"Packet handling error in tick {CurrentTick}: {e}");
                            }
                            finally
                            {
                                ArrayPool<byte>.Shared.Return(pooled);
                            }
                        }
                    }
                }

                FlushAllConnections();
            }
        }
        finally
        {
            _ticking = wasTicking;
        }

        Timings.OnTickEnd();
        var elapsedMs = Stopwatch.GetElapsedTime(tickStart).TotalMilliseconds;
        Performance.OnTick(elapsedMs, intervalMs);
    }

    private void FlushAllConnections()
    {
        for (var i = 0; i < _connections.Count; i++)
        {
            var conn = _connections[i];
            conn.FlushTickMovement();
            conn.FlushOutgoing();
        }
    }

    // Network thread: subscribe right away (no packet may be missed), then hand everything to the game thread.
    // Signaling thread.
    private void OnNetherNetSessionOpened(NetherNetSession session)
    {
        var connection = new BedrockConnection(this, new NetherNetTransport(session), Interlocked.Increment(ref _nextEntityId));
        RunOnMainThread(() => _connections.Add(connection));
        session.Connected += s => Logger.Debug($"{s.RemoteEndPoint} connected (NetherNet).");
        session.PacketReceived += (_, batch) =>
        {
            try
            {
                connection.OnBatchReceived(batch);
            }
            catch (Exception e)
            {
                Logger.Warn($"Packet batch decode error: {e.Message}");
            }
        };
        session.Closed += (_, reason) => RunOnMainThread(() => OnConnectionClosed(connection, reason));
    }

    // Signaling thread. Online: token signed by the authorization service; offline: unverified.
    private ECDsa VerifyNetherNetToken(string token)
    {
        var keys = AuthKeys;
        if (Config.OnlineMode && keys is null)
            throw new LoginException("The server cannot reach Xbox Live right now. Try again in a minute.");
        return LoginRequest.VerifyMultiplayerToken(token, keys, DateTimeOffset.UtcNow).ClientKey;
    }

    private void OnConnectionClosed(BedrockConnection connection, string reason)
    {
        if (!_connections.Remove(connection))
            return;
        var wasInGame = connection.IsInGame; // read before OnClosed() changes the stage
        connection.OnClosed();
        Mobs.Forget(connection);
        Npcs.Forget(connection);
        _players.Remove(connection);
        if (wasInGame)
        {
            connection.CloseContainer(notifyClient: false); // a plugin menu gets its OnClose
            SavePlayer(connection, leaving: true);
            Broadcast(Packets.RemoveActor(connection.EntityId));
            Broadcast(PlayerPackets.PlayerListRemove(connection.Info!.Uuid));
            var quitEv = new PlayerQuitEvent(connection, $"§e{connection.Name} left the game");
            Events.OnQuit(quitEv);
            if (!string.IsNullOrEmpty(quitEv.BroadcastMessage))
                BroadcastMessage(quitEv.BroadcastMessage);
        }
        Logger.Info($"{connection.Name} disconnected: {reason}.");
    }

    /// <summary>Called after a successful login. Kicks an older session of the same account.</summary>
    internal bool TryReserveSlot(BedrockConnection connection)
    {
        var identity = connection.Identity!;
        var existing = _players.FirstOrDefault(p =>
            (identity.Xuid.Length > 0 && p.Identity!.Xuid == identity.Xuid)
            || string.Equals(p.Identity!.Name, identity.Name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            // Save and freeze the old session now: the new one loads this save, and the old one must not
            // act or save again afterwards (that would duplicate or roll back items).
            if (existing.IsInGame)
            {
                SavePlayer(existing, leaving: true);
                Broadcast(Packets.RemoveActor(existing.EntityId));
                Broadcast(PlayerPackets.PlayerListRemove(existing.Info!.Uuid));
            }
            existing.Disconnect("Logged in from another location.");
            existing.OnClosed();
            _players.Remove(existing);
        }
        if (_players.Count >= Config.MaxPlayers)
            return false;
        _players.Add(connection);
        return true;
    }

    // The newcomer gets everyone (itself included) in its player list and sees the others; they see it.
    internal void OnPlayerJoined(BedrockConnection connection)
    {
        var others = _players.Where(p => p.IsInGame && p != connection).ToList();
        connection.Send(PlayerPackets.PlayerListAdd([.. others.Select(p => p.Info!), connection.Info!]));
        foreach (var other in others.Where(o => o.World == connection.World && !o.Vanished))
            connection.Send(other.AddPlayerPacket());

        var info = connection.Info!;
        BroadcastExcept(connection, PlayerPackets.PlayerListAdd([info]));
        BroadcastInExcept(connection.World, connection, connection.AddPlayerPacket());
        foreach (var item in connection.World.DroppedItems.All)
            connection.Send(InventoryPackets.AddItemActor(item.EntityId, item.Stack, item.X, item.Y, item.Z, item.Vx, item.Vy, item.Vz));
        var joinEv = new PlayerJoinEvent(connection, $"§e{connection.Name} joined the game");
        Events.OnJoin(joinEv);
        if (!string.IsNullOrEmpty(joinEv.BroadcastMessage))
            BroadcastMessage(joinEv.BroadcastMessage);
    }

    private async Task RefreshAuthKeysAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var delay = AuthKeysRefresh;
            try
            {
                var first = _authKeys.Current is null;
                await _authKeys.RefreshAsync(cancellationToken);
                if (first)
                    Logger.Info("Xbox Live authentication keys loaded.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception e) when (e is HttpRequestException or InvalidDataException or TaskCanceledException)
            {
                Logger.Warn($"Cannot load Xbox Live authentication keys ({e.Message}); retrying in 30 s. Players cannot log in until then.");
                delay = TimeSpan.FromSeconds(30);
            }
            try
            {
                await Task.Delay(delay, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    // Bedrock server-list format; field order is fixed by the client.
    private static bool TryParseGameMode(string value, out GameMode mode)
    {
        mode = value.ToLowerInvariant() switch
        {
            "0" or "s" or "survival" => GameMode.Survival,
            "1" or "c" or "creative" => GameMode.Creative,
            "2" or "a" or "adventure" => GameMode.Adventure,
            "3" or "6" or "sp" or "spectator" => GameMode.Spectator,
            _ => (GameMode)(-1),
        };
        return Enum.IsDefined(mode);
    }

    private BedrockConnection? FindPlayer(string name) =>
        _players.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    // Target players: a name, @a (everyone), @s / @p / @r (the sender in game).
    private List<BedrockConnection> Targets(CommandSender sender, string selector)
    {
        var targets = selector switch
        {
            "@a" or "@e" => _players.Where(p => p.IsInGame).ToList(),
            "@s" or "@p" or "@r" => sender.Player is { } self ? [self] : [],
            _ => FindPlayer(selector) is { } player ? [player] : [],
        };
        if (targets.Count == 0)
            sender.Reply($"§cNo player matches '{selector}'.");
        return targets;
    }

    // The console must name a player.
    private static BedrockConnection Self(CommandSender sender) => sender.Player ?? throw new CommandUsageException();

    private static int Int(string value, int min, int max) =>
        int.TryParse(value, out var n) && n >= min && n <= max ? n : throw new CommandUsageException();

    // "~" and "~5" are relative to the current coordinate.
    private static float Coordinate(string value, float current)
    {
        var relative = value.StartsWith('~');
        var text = relative ? value[1..] : value;
        if (text.Length == 0 && relative)
            return current;
        if (!float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number))
            throw new CommandUsageException();
        return relative ? current + number : number;
    }

    private static void Teleport(CommandSender sender, List<BedrockConnection> who, BedrockConnection destination)
    {
        var (x, y, z) = destination.Position;
        foreach (var target in who)
        {
            target.Teleport(x, y - BedrockConnection.EyeHeight, z);
            sender.Reply($"Teleported {target.Name} to {destination.Name}.");
        }
    }

    private void RegisterBuiltinCommands()
    {
        void Add(string name, string description, string usage, string permission, Action<CommandSender, string[]> run) =>
            Commands.Register(name, description, usage, permission, run);
        void Alias(string alias, string target, string usage, string permission) =>
            Commands.Register(alias, $"Alias of {target}.", usage, permission, (sender, args) => Commands.Dispatch(sender, target + " " + string.Join(' ', args)));

        Add("help", "Lists the available commands.", "", "bedrokia.command.help", (sender, _) =>
        {
            foreach (var command in Commands.All.Where(c => sender.HasPermission(c.Permission)))
                sender.Reply($"§6/{command.Name} {CommandManager.DisplayUsage(command.Usage)}§r - {command.Description}");
        });
        Add("version", "Shows the server and Bedrock versions.", "", "bedrokia.command.version", (sender, _) =>
            sender.Reply($"{Product.Name} {Version} - Bedrock {ProtocolInfo.GameVersion} (protocol {ProtocolInfo.Version})"));
        Add("plugin", "Lists the plugins, or shows one.", "[name]", "bedrokia.command.plugin", (sender, args) =>
        {
            if (args.Length > 0)
            {
                if (Plugins.Loaded.FirstOrDefault(l => l.Plugin.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase)) is not { } found)
                {
                    sender.Reply($"§cNo plugin named '{args[0]}'. Type plugin for the list.");
                    return;
                }
                var info = found.Plugin.Info;
                sender.Reply($"§a{info.Name}§r v{info.Version}" + (info.Author.Length > 0 ? $" by {info.Author}" : "") + $" - {found.Plugin.Description}");
                if (info.Commands.Count > 0)
                    sender.Reply($"§7Commands: {string.Join(", ", info.Commands.Keys.Select(c => "/" + c))}");
                if (info.Depend.Count > 0)
                    sender.Reply($"§7Depends on: {string.Join(", ", info.Depend)}");
                sender.Reply($"§7Loaded from {found.File} ({found.Kind}), data in {found.Plugin.DataFolder}");
                return;
            }
            sender.Reply($"Plugins ({Plugins.Loaded.Count}): " + (Plugins.Loaded.Count == 0 ? "none"
                : string.Join("§r, ", Plugins.Loaded.Select(l => $"§a{l.Plugin.Name} v{l.Plugin.Version}"))));
            foreach (var (file, _) in Plugins.Failed)
                sender.Reply($"§c{Path.GetFileName(file)}: failed to load (see the console).");
        });
        Alias("plugins", "plugin", "[name]", "bedrokia.command.plugin");
        Add("list", "Lists the players online.", "", "bedrokia.command.list", (sender, _) =>
            sender.Reply($"{_players.Count}/{Config.MaxPlayers} players: {string.Join(", ", _players.Select(p => p.Name))}"));
        Add("seed", "Shows the world seed.", "", "bedrokia.command.seed", (sender, _) => sender.Reply($"Seed: {World.Generator.Seed}"));
        Add("me", "Describes an action in the chat.", "<action...>", "bedrokia.command.me", (sender, args) =>
        {
            if (args.Length == 0)
                throw new CommandUsageException();
            BroadcastMessage($"* {sender.Name} {string.Join(' ', args)}");
        });
        Add("tell", "Sends a private message.", "<player:player> <message...>", "bedrokia.command.tell", (sender, args) =>
        {
            if (args.Length < 2)
                throw new CommandUsageException();
            var message = string.Join(' ', args[1..]);
            foreach (var target in Targets(sender, args[0]))
            {
                target.SendMessage($"§7{sender.Name} whispers to you: {message}");
                sender.Reply($"§7You whisper to {target.Name}: {message}");
            }
        });
        Alias("msg", "tell", "<player:player> <message...>", "bedrokia.command.tell");
        Alias("w", "tell", "<player:player> <message...>", "bedrokia.command.tell");
        Add("spawn", "Teleports you to the world spawn.", "", "bedrokia.command.spawn", (sender, _) =>
        {
            var (x, y, z) = World.Spawn;
            Self(sender).Teleport(x + 0.5f, y, z + 0.5f);
        });

        Add("tps", "Shows the server's current performance (TPS, tick duration, memory).", "", "bedrokia.command.tps", (sender, _) =>
        {
            var snap = Performance.GetSnapshot();
            sender.Reply("§6--- Server Performance ---");
            sender.Reply($"§7TPS: {PerformanceTracker.FormatTps(snap.CurrentTps)} §7({snap.TickUsage:F1}%) [1m: {PerformanceTracker.FormatTps(snap.AverageTps1Min)}§7, 5m: {PerformanceTracker.FormatTps(snap.AverageTps5Min)}§7]");
            sender.Reply($"§7Tick Time: {PerformanceTracker.FormatMspt(snap.AverageTickMs)} §7(current: {PerformanceTracker.FormatMspt(snap.CurrentTickMs)}§7, target: 50.0ms)");
            sender.Reply($"§7Memory: §a{PerformanceTracker.FormatBytes(snap.GcTotalMemory)} §7/ §e{PerformanceTracker.FormatBytes(snap.ProcessWorkingSet)} §7(GC / Process)");
            var worldName = Config.LevelName;
            var chunkCount = World?.LoadedChunkCount ?? 0;
            sender.Reply($"§7World '{worldName}': §f{chunkCount} §7chunks, §f{TotalEntitiesCount} §7entities, §f{_players.Count}/{Config.MaxPlayers} §7players");
        });

        Add("timings", "Profiles server performance and subsystem timings.", "[action:on|off|reset|report]", "bedrokia.command.timings", (sender, args) =>
        {
            if (args.Length == 0)
            {
                if (Timings.IsEnabled || Timings.SampledTicks > 0)
                {
                    foreach (var line in Timings.GenerateReport().Split('\n'))
                        sender.Reply(line.TrimEnd('\r'));
                }
                else
                {
                    sender.Reply("§7Timings are currently §cdisabled§7. Use §6/timings on§7 to start profiling.");
                }
                return;
            }

            var sub = args[0].ToLowerInvariant();
            switch (sub)
            {
                case "on" or "start" or "enable":
                    Timings.Start();
                    sender.Reply("§aTimings collection enabled. Run §6/timings report§a after a while to inspect results.");
                    break;

                case "off" or "stop" or "disable":
                    Timings.Stop();
                    sender.Reply("§cTimings collection stopped. Run §6/timings report§c to view collected data or §6/timings reset§c to clear.");
                    break;

                case "reset" or "clear":
                    Timings.Reset();
                    sender.Reply("§aTimings data has been reset.");
                    break;

                case "report" or "paste" or "view":
                    if (Timings.SampledTicks == 0)
                    {
                        sender.Reply("§cNo timings data collected yet. Run §6/timings on§c and let the server tick.");
                        return;
                    }
                    foreach (var line in Timings.GenerateReport().Split('\n'))
                        sender.Reply(line.TrimEnd('\r'));
                    break;

                default:
                    sender.Reply("§cUsage: /timings [on|off|reset|report]");
                    break;
            }
        });

        Add("stop", "Stops the server cleanly.", "", "bedrokia.command.stop", (_, _) => Stop());
        Add("save-all", "Saves the world and the players now.", "", "bedrokia.command.saveall", (sender, _) =>
        {
            SaveWorld();
            sender.Reply("World saved.");
        });
        Add("say", "Sends a message to every player.", "<message...>", "bedrokia.command.say", (sender, args) =>
        {
            if (args.Length == 0)
                throw new CommandUsageException();
            BroadcastMessage($"§d[{sender.Name}] {string.Join(' ', args)}");
        });
        Add("gamemode", "Changes a game mode (survival, creative, adventure, spectator).", "<mode:survival|creative|adventure|spectator> [player:player]", "bedrokia.command.gamemode", (sender, args) =>
        {
            if (args.Length is 0 or > 2 || !TryParseGameMode(args[0], out var mode))
                throw new CommandUsageException();
            foreach (var target in args.Length == 2 ? Targets(sender, args[1]) : [Self(sender)])
            {
                target.SetGameMode(mode);
                target.SendMessage($"Your game mode has been updated to {mode}.");
                sender.Reply($"Set {target.Name}'s game mode to {mode}.");
            }
        });
        Alias("gm", "gamemode", "<mode:survival|creative|adventure|spectator> [player:player]", "bedrokia.command.gamemode");
        Add("give", "Gives items to a player.", "<player:player> <item> [amount:int]", "bedrokia.command.give", (sender, args) =>
        {
            if (args.Length is < 2 or > 3)
                throw new CommandUsageException();
            var amount = args.Length == 3 ? Int(args[2], 1, 64 * 36) : 1;
            if (ItemStack.Of(args[1]) is not { } template)
            {
                sender.Reply($"§cUnknown item '{args[1]}'.");
                return;
            }
            foreach (var target in Targets(sender, args[0]))
            {
                var (packets, remaining) = target.Inventory.Add(template.Split(amount));
                target.Send([.. packets]);
                if (remaining > 0)
                    DropItem(template.Split(remaining), target.Position.X, target.Position.Y - 1, target.Position.Z);
                sender.Reply($"Gave {amount} {(template.Type.Name.StartsWith("minecraft:") ? template.Type.Name["minecraft:".Length..] : template.Type.Name)} to {target.Name}.");
            }
        });
        Add("clear", "Empties a player's inventory.", "[player:player]", "bedrokia.command.clear", (sender, args) =>
        {
            foreach (var target in args.Length > 0 ? Targets(sender, args[0]) : [Self(sender)])
            {
                target.Inventory.Main.Clear();
                target.Inventory.Armour.Clear();
                target.Inventory.Offhand.Clear();
                target.Send([.. target.Inventory.ContentPackets()]);
                sender.Reply($"Cleared the inventory of {target.Name}.");
            }
        });
        Add("tp", "Teleports a player to a player or to coordinates.", "<target:player> [destination:player] || <target:player> <position:pos>", "bedrokia.command.tp", (sender, args) =>
        {
            switch (args.Length)
            {
                case 1: // sender -> player
                    if (Targets(sender, args[0]).FirstOrDefault() is { } to)
                        Teleport(sender, [Self(sender)], to);
                    break;
                case 2: // player -> player
                    if (Targets(sender, args[1]).FirstOrDefault() is { } destination)
                        Teleport(sender, Targets(sender, args[0]), destination);
                    break;
                case 3 or 4: // [player] x y z
                    var xyz = args[^3..];
                    foreach (var target in args.Length == 4 ? Targets(sender, args[0]) : [Self(sender)])
                    {
                        var (x, y, z) = target.Position;
                        var (tx, ty, tz) = (Coordinate(xyz[0], x), Coordinate(xyz[1], y - BedrockConnection.EyeHeight), Coordinate(xyz[2], z));
                        target.Teleport(tx, ty, tz);
                        sender.Reply($"Teleported {target.Name} to {tx:0.##}, {ty:0.##}, {tz:0.##}.");
                    }
                    break;
                default:
                    throw new CommandUsageException();
            }
        });
        Alias("teleport", "tp", "<target:player> [destination:player] || <target:player> <position:pos>", "bedrokia.command.tp");
        Add("time", "Changes the time of day.", "<action:set|add> <value>", "bedrokia.command.time", (sender, args) =>
        {
            if (args.Length != 2)
                throw new CommandUsageException();
            var value = args[1].ToLowerInvariant() switch
            {
                "day" => 1000,
                "noon" => 6000,
                "sunset" => 12000,
                "night" => 13000,
                "midnight" => 18000,
                "sunrise" => 23000,
                var n => Int(n, 0, int.MaxValue),
            };
            World.Time = args[0].ToLowerInvariant() switch
            {
                "set" => World.Time - World.Time % 24000 + value,
                "add" => World.Time + value,
                _ => throw new CommandUsageException(),
            };
            Broadcast(Packets.SetTime((int)(World.Time % int.MaxValue)));
            sender.Reply($"Time is now {World.Time % 24000}.");
        });
        Add("setworldspawn", "Sets the world spawn to your position.", "", "bedrokia.command.setworldspawn", (sender, _) =>
        {
            var (x, y, z) = Self(sender).Position;
            World.Spawn = ((int)Math.Floor(x), (int)Math.Floor(y - BedrockConnection.EyeHeight), (int)Math.Floor(z));
            sender.Reply($"World spawn set to {World.Spawn}.");
        });

        Add("op", "Makes a player a server operator.", "<player:player>", "bedrokia.command.op", (sender, args) =>
        {
            if (args.Length != 1)
                throw new CommandUsageException();
            sender.Reply(Ops.Add(args[0]) ? $"Made {args[0]} a server operator." : $"{args[0]} is already an operator.");
            if (FindPlayer(args[0]) is { } player)
            {
                player.SendCommands();
                player.SendMessage("§eYou are now a server operator.");
            }
        });
        Add("deop", "Removes operator status from a player.", "<player:player>", "bedrokia.command.deop", (sender, args) =>
        {
            if (args.Length != 1)
                throw new CommandUsageException();
            sender.Reply(Ops.Remove(args[0]) ? $"{args[0]} is no longer an operator." : $"{args[0]} is not an operator.");
            FindPlayer(args[0])?.SendCommands();
        });
        Add("whitelist", "Manages the whitelist.", "<action:on|off|add|remove|list> [player:player]", "bedrokia.command.whitelist", (sender, args) =>
        {
            switch (args.Length > 0 ? args[0].ToLowerInvariant() : "")
            {
                case "on" or "off":
                    Config.Whitelist = args[0].Equals("on", StringComparison.OrdinalIgnoreCase);
                    Config.Save();
                    sender.Reply($"Whitelist {(Config.Whitelist ? "enabled" : "disabled")}.");
                    break;
                case "add" when args.Length == 2:
                    sender.Reply(Whitelist.Add(args[1]) ? $"Added {args[1]} to the whitelist." : $"{args[1]} is already whitelisted.");
                    break;
                case "remove" when args.Length == 2:
                    sender.Reply(Whitelist.Remove(args[1]) ? $"Removed {args[1]} from the whitelist." : $"{args[1]} is not whitelisted.");
                    if (Config.Whitelist && !Ops.Contains(args[1]))
                        FindPlayer(args[1])?.Disconnect("You are not white-listed on this server.");
                    break;
                case "list":
                    sender.Reply($"Whitelist ({(Config.Whitelist ? "on" : "off")}): {string.Join(", ", Whitelist.Names)}");
                    break;
                default:
                    throw new CommandUsageException();
            }
        });
        Add("kick", "Disconnects a player.", "<player:player> [reason...]", "bedrokia.command.kick", (sender, args) =>
        {
            if (args.Length == 0)
                throw new CommandUsageException();
            foreach (var target in Targets(sender, args[0]))
            {
                target.Disconnect(args.Length > 1 ? string.Join(' ', args[1..]) : "Kicked by an operator.");
                sender.Reply($"Kicked {target.Name}.");
            }
        });
        Add("ban", "Bans a player by name.", "<player:player> [reason...]", "bedrokia.command.ban", (sender, args) =>
        {
            if (args.Length == 0)
                throw new CommandUsageException();
            Bans.Add(args[0]);
            FindPlayer(args[0])?.Disconnect(args.Length > 1 ? string.Join(' ', args[1..]) : "You are banned from this server.");
            sender.Reply($"Banned {args[0]}.");
        });
        Add("pardon", "Unbans a player.", "<player>", "bedrokia.command.pardon", (sender, args) =>
        {
            if (args.Length != 1)
                throw new CommandUsageException();
            sender.Reply(Bans.Remove(args[0]) ? $"Unbanned {args[0]}." : $"{args[0]} is not banned.");
        });
        Alias("unban", "pardon", "<player>", "bedrokia.command.pardon");
        Add("summon", "Spawns a mob at your position.", "<mob>", "bedrokia.command.summon", (sender, args) =>
        {
            if (args.Length != 1)
                throw new CommandUsageException();
            
            var mobName = args[0].ToLowerInvariant().Replace("minecraft:", "");
            if (!MobKind.ById.TryGetValue(mobName, out var kind))
            {
                sender.Reply($"§cUnknown mob '{args[0]}'. Available mobs: {string.Join(", ", MobKind.ById.Keys)}");
                return;
            }
            
            var (x, y, z) = Self(sender).Position;
            Mobs.Spawn(kind, x, y - BedrockConnection.EyeHeight, z);
            sender.Reply($"Summoned {kind.Id["minecraft:".Length..]}.");
        });
        Add("enchant", "Enchants the held item.", "<enchantment> [level:int]", "bedrokia.command.enchant", (sender, args) =>
        {
            if (args.Length is < 1 or > 2)
                throw new CommandUsageException();
            
            var player = Self(sender);
            var held = player.Inventory.Held;
            if (held is null)
            {
                sender.Reply("§cYou must be holding an item to enchant.");
                return;
            }
            
            var enchant = Enchanting.ByIdOrName(args[0]);
            if (enchant is null)
            {
                sender.Reply($"§cUnknown enchantment '{args[0]}'. Available: {string.Join(", ", Enchanting.All.Select(e => e.Name))}");
                return;
            }
            
            var level = args.Length == 2 ? Int(args[1], 1, enchant.MaxLevel) : 1;
            
            var itemName = held.Type.Name.StartsWith("minecraft:") ? held.Type.Name["minecraft:".Length..] : held.Type.Name;
            if (itemName is not ("book" or "enchanted_book") && !enchant.Fits(itemName))
            {
                sender.Reply($"§c{enchant.Name} cannot be applied to {itemName}.");
                return;
            }
            
            var enchanted = held.AddEnchantment(enchant.Id, level);
            var slot = player.Inventory.SelectedSlot;
            player.Inventory.Main[slot] = enchanted;
            player.Send([.. player.Inventory.ContentPackets()]);
            
            sender.Reply($"Enchanted {itemName} with {enchant.Name} {level}.");
        });
        Add("xp", "Gives experience: a number of points, or levels with an L suffix (e.g. 30L).", "<amount> [player:player]", "bedrokia.command.xp", (sender, args) =>
        {
            if (args.Length is 0 or > 2)
                throw new CommandUsageException();
            var levels = args[0].EndsWith('L') || args[0].EndsWith('l');
            var amount = Int(levels ? args[0][..^1] : args[0], 1, 100_000);
            foreach (var target in args.Length == 2 ? Targets(sender, args[1]) : [Self(sender)])
            {
                if (levels)
                    target.SetExperienceLevel(target.ExperienceLevel + amount);
                else
                    target.AddExperience(amount);
                sender.Reply($"Gave {amount}{(levels ? " levels" : " experience")} to {target.Name}.");
            }
        });
        Add("banlist", "Lists banned players.", "", "bedrokia.command.banlist", (sender, _) => sender.Reply($"Banned: {string.Join(", ", Bans.Names)}"));

        Add("perm", "Manages permissions and groups.", "<target:user|group> [args...]", "bedrokia.command.permission", (sender, args) =>
        {
            if (args.Length < 2)
                throw new CommandUsageException();

            switch (args[0].ToLowerInvariant())
            {
                case "user":
                {
                    if (args.Length < 3)
                        throw new CommandUsageException();
                    var target = args[1];
                    var action = args[2].ToLowerInvariant();
                    switch (action)
                    {
                        case "info":
                        {
                            var group = Permissions.GetPlayerGroup(target);
                            var prefix = Permissions.GetPlayerPrefix(target);
                            var effective = Permissions.GetEffectivePermissions(target);
                            sender.Reply($"§6--- Permissions for {target} ---");
                            sender.Reply($"§7Group: §e{group}§7 (prefix: '{prefix}')");
                            sender.Reply($"§7Effective permissions ({effective.Count}): " + (effective.Count == 0 ? "none" : string.Join("§r, §a", effective)));
                            break;
                        }
                        case "setgroup" when args.Length == 4:
                        {
                            var group = args[3];
                            Permissions.SetPlayerGroup(target, group);
                            sender.Reply($"Set group of {target} to {group}.");
                            FindPlayer(target)?.SendCommands();
                            break;
                        }
                        case "add" when args.Length == 4:
                        {
                            var perm = args[3];
                            Permissions.AddPlayerPermission(target, perm);
                            sender.Reply($"Added permission '{perm}' to {target}.");
                            FindPlayer(target)?.SendCommands();
                            break;
                        }
                        case "remove" when args.Length == 4:
                        {
                            var perm = args[3];
                            Permissions.RemovePlayerPermission(target, perm);
                            sender.Reply($"Removed permission '{perm}' from {target}.");
                            FindPlayer(target)?.SendCommands();
                            break;
                        }
                        default:
                            throw new CommandUsageException();
                    }
                    break;
                }
                case "group":
                {
                    if (args.Length < 3)
                        throw new CommandUsageException();
                    var groupName = args[1];
                    var action = args[2].ToLowerInvariant();
                    switch (action)
                    {
                        case "list":
                        {
                            if (Permissions.Groups.TryGetValue(groupName, out var grp))
                            {
                                sender.Reply($"§6--- Group: {grp.Name} ---");
                                sender.Reply($"§7Prefix: '§r{grp.Prefix}§7', Priority: {grp.Priority}, Parent: {(grp.Parent ?? "none")}");
                                sender.Reply($"§7Permissions ({grp.Permissions.Count}): " + (grp.Permissions.Count == 0 ? "none" : string.Join("§r, §a", grp.Permissions)));
                            }
                            else
                            {
                                sender.Reply($"§cGroup '{groupName}' does not exist.");
                            }
                            break;
                        }
                        case "add" when args.Length == 4:
                        {
                            var perm = args[3];
                            Permissions.AddGroupPermission(groupName, perm);
                            sender.Reply($"Added permission '{perm}' to group {groupName}.");
                            foreach (var p in _players)
                            {
                                if (string.Equals(Permissions.GetPlayerGroup(p.Name), groupName, StringComparison.OrdinalIgnoreCase))
                                    p.SendCommands();
                            }
                            break;
                        }
                        case "remove" when args.Length == 4:
                        {
                            var perm = args[3];
                            Permissions.RemoveGroupPermission(groupName, perm);
                            sender.Reply($"Removed permission '{perm}' from group {groupName}.");
                            foreach (var p in _players)
                            {
                                if (string.Equals(Permissions.GetPlayerGroup(p.Name), groupName, StringComparison.OrdinalIgnoreCase))
                                    p.SendCommands();
                            }
                            break;
                        }
                        case "setprefix" when args.Length >= 4:
                        {
                            var rawPrefix = string.Join(' ', args[3..]).Trim('"');
                            var prefix = rawPrefix.Length > 0 && !rawPrefix.EndsWith(' ') ? rawPrefix + " " : rawPrefix;
                            var existing = Permissions.Groups.TryGetValue(groupName, out var g) ? g : null;
                            Permissions.CreateGroup(groupName, prefix, existing?.Priority ?? 0, existing?.Parent);
                            sender.Reply($"Set prefix of group {groupName} to '{prefix}'.");
                            break;
                        }
                        case "setparent" when args.Length == 4:
                        {
                            var parent = args[3];
                            var existing = Permissions.Groups.TryGetValue(groupName, out var g) ? g : null;
                            Permissions.CreateGroup(groupName, existing?.Prefix ?? "", existing?.Priority ?? 0, parent);
                            sender.Reply($"Set parent of group {groupName} to '{parent}'.");
                            foreach (var p in _players)
                                p.SendCommands();
                            break;
                        }
                        default:
                            throw new CommandUsageException();
                    }
                    break;
                }
                default:
                    throw new CommandUsageException();
            }
        });
        Alias("permission", "perm", "<target:user|group> [args...]", "bedrokia.command.permission");
    }
}
