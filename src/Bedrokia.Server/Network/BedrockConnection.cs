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
using Bedrokia.Server.Commands;
using Bedrokia.Protocol.Login;
using Bedrokia.Server.Core;
using Bedrokia.Server.Game;
using Bedrokia.Server.Game.Ui.Forms;

namespace Bedrokia.Server.Network;

/// <summary>
/// Bedrock login sequence and in-game packet handling for one client.
/// Game-thread only: every method is called from the tick loop, so no locking is needed here.
/// </summary>
public sealed partial class BedrockConnection
{
    private enum Stage { NetworkSettings, Login, Handshake, ResourcePacks, ChunkRadius, Spawning, InGame, Closed }

    private const ushort CompressionThreshold = 256;
    public const float EyeHeight = 1.62f;
    private const double MaxReach = 12;

    private readonly BedrokiaServer _server;
    private readonly IGameTransport _transport;
    private readonly BatchCodec _codec;
    private readonly HashSet<long> _sentChunks = [];
    private readonly HashSet<long> _requestedChunks = [];
    private Stage _stage = Stage.NetworkSettings;
    private int _viewRadius;
    private (int X, int Z) _centerChunk = (int.MinValue, int.MinValue);
    private bool _hasPendingMovement;
    private float _pendingPitch, _pendingYaw, _pendingX, _pendingY, _pendingZ;
    private readonly List<byte[]> _pendingOutgoing = [];

    internal BedrockConnection(BedrokiaServer server, IGameTransport transport, long entityId)
    {
        _server = server;
        _transport = transport;
        _codec = new BatchCodec(withHeader: false); // NetherNet: no 0xFE header, DTLS encrypts the connection
        EntityId = entityId;
        Inventory.Player = this;
        Inventory.Rejected = reason => _server.Logger.Debug($"{Name}: {reason}");
        Inventory.Accepted = summary => _server.Logger.Debug($"{Name}: {summary}");
        RegisterHandlers();
    }

    public BedrokiaServer Server => _server;

    public PlayerIdentity? Identity { get; internal set; }
    public PlayerInfo? Info { get; internal set; }
    public string Name => Identity?.Name ?? _transport.RemoteEndPoint.ToString();
    public long EntityId { get; }
    public bool IsInGame { get => _stage == Stage.InGame; internal set => _stage = value ? Stage.InGame : Stage.Closed; }
    public System.Net.IPEndPoint RemoteEndPoint => _transport.RemoteEndPoint;

    /// <summary>Eye position as reported by the client.</summary>
    public (float X, float Y, float Z) Position { get; internal set; }
    public float Pitch { get; internal set; }
    public float Yaw { get; internal set; }

    /// <summary>Chunk the player stands in.</summary>
    public (int X, int Z) ChunkPosition => ((int)Math.Floor(Position.X) >> 4, (int)Math.Floor(Position.Z) >> 4);

    public int ViewRadius => _viewRadius;

    public void SendMessage(string message) => Send(Packets.Text(TextType.Raw, message));

    private const uint PacketViolationWarningId = 156;

    // The client says which packet it could not read before it disconnects: the only trace of such a bug.
    private void OnPacketViolationWarning(ref BedrockReader reader)
    {
        var type = reader.VarInt32();
        var severity = reader.VarInt32();
        var packet = reader.VarInt32();
        var context = reader.Remaining > 0 ? reader.String() : "";
        _server.Logger.Warn($"{Name}: the client rejected packet {packet} (violation {type}, severity {severity}): {context}");
    }

    public bool IsOp => _server.Ops.Contains(Name);

    public string Prefix => _server.Permissions.GetPlayerPrefix(Name);

    public bool HasPermission(string? permission) => _server.Permissions.HasPermission(Name, permission, IsOp);

    public CommandSender CommandSender => new(Name, IsOp, SendMessage, this);

    /// <summary>Sends the command list this player may use (call again after op/deop or perm change).</summary>
    public void SendCommands() =>
        Send(new BedrockWriter().VarUInt32(CommandManager.SetCommandsEnabledId).Bool(true).ToArray(),
            _server.Commands.AvailableCommandsPacket(CommandSender));

    /// <summary>Moves the player (feet position) and loads the chunks around the destination.</summary>
    public void Teleport(float x, float y, float z)
    {
        Position = (x, y + EyeHeight, z);
        var packet = Packets.MovePlayer(EntityId, x, y + EyeHeight, z, 0, 0, 0, teleport: true, onGround: false);
        Send(packet);
        _server.BroadcastInExcept(World, this, packet);
        UpdateChunks(ChunkPosition.X, ChunkPosition.Z);
    }

    /// <summary>Shows <paramref name="message"/> on the client's disconnection screen and closes the connection.</summary>
    public void Disconnect(string message, int reason = DisconnectReason.Kicked)
    {
        if (_stage == Stage.Closed)
            return;
        if (_codec.CompressionEnabled) // the client cannot read game packets before NetworkSettings
            Send(Packets.Disconnect(reason, message));
        FlushOutgoing();
        _transport.Disconnect(message);
    }

    internal void OnBatchReceived(byte[] batch)
    {
        if (_stage == Stage.Closed)
            return;
        try
        {
            _codec.DecodePackets(batch, (rented, len) => _server.EnqueueInboundPacket(this, rented, len));
        }
        catch (InvalidDataException e)
        {
            _server.Logger.Warn($"{Name}: protocol error: {e.Message}");
            _server.RunOnMainThread(() => Disconnect("Protocol error.", DisconnectReason.Unknown));
        }
    }

    internal void HandleBatch(byte[] batch)
    {
        if (_stage == Stage.Closed)
            return;
        try
        {
            foreach (var packet in _codec.Decode(batch))
            {
                if (_stage == Stage.Closed)
                    break;
                HandlePacket(packet, packet.Length);
            }
        }
        catch (InvalidDataException e)
        {
            _server.Logger.Warn($"{Name}: protocol error: {e.Message}");
            Disconnect("Protocol error.", DisconnectReason.Unknown);
        }
        catch (LoginException e)
        {
            _server.Logger.Info($"{_transport.RemoteEndPoint}: login refused: {e.Message}");
            Disconnect(e.Message, DisconnectReason.NotAuthenticated);
        }
    }

    internal void OnClosed()
    {
        _stage = Stage.Closed;
        foreach (var key in _sentChunks)
        {
            var (cx, cz) = World.UnpackChunkKey(key);
            if (World.LoadedChunk(cx, cz) is { } chunk)
                chunk.Viewers.Remove(this);
        }
        _sentChunks.Clear();
        _requestedChunks.Clear();
        _pendingOutgoing.Clear();
        CloseAllForms(FormCancelReason.Disconnected);
        DduiSession.CloseAll();
    }

    internal void HandlePacket(byte[] packet, int length)
    {
        if (_stage == Stage.Closed)
            return;
        try
        {
            var span = packet.AsSpan(0, length);
            var reader = new BedrockReader(span);
            var id = reader.VarUInt32() & 0x3FF; // low 10 bits; the rest are sub-client ids
        switch (_stage, id)
        {
            case (Stage.NetworkSettings, PacketId.RequestNetworkSettings):
                OnRequestNetworkSettings(reader.Int32BE());
                break;
            case (Stage.Login, PacketId.Login):
            {
                reader.Int32BE(); // protocol, already checked
                OnLogin(reader.ByteArray());
                break;
            }
            case (Stage.Handshake, PacketId.ClientToServerHandshake):
                CompleteLogin();
                break;
            case (Stage.ResourcePacks, PacketId.ResourcePackClientResponse):
                OnResourcePackResponse(ref reader);
                break;
            case (Stage.ResourcePacks, ResourcePacks.ChunkRequestId):
            {
                var packId = reader.String();
                var index = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(reader.Bytes(4));
                if (_server.ResourcePacks.ById(packId) is { } pack && ResourcePacks.ChunkPacket(pack, index) is { } chunk)
                    Send(chunk);
                break;
            }
            case (_, PacketId.RequestChunkRadius) when _stage >= Stage.ChunkRadius && _stage < Stage.InGame:
                OnRequestChunkRadius(reader.VarInt32());
                break;
            case (Stage.Spawning, PacketId.SetLocalPlayerAsInitialised):
                _stage = Stage.InGame;
                _server.OnPlayerJoined(this);
                break;
            case (Stage.InGame, _):
                HandleInGamePacket(id, ref reader);
                break;
            case (Stage.NetworkSettings or Stage.Login or Stage.Handshake, _):
                throw new InvalidDataException($"Unexpected packet {id} during login.");
            default:
                // ponytail: other in-game packets are ignored until their feature exists; logged once each to spot gaps.
                if (_ignoredPackets.Add(id))
                    _server.Logger.Debug($"{Name}: ignored packet id {id}");
                break;
        }
        }
        catch (InvalidDataException e)
        {
            _server.Logger.Warn($"{Name}: protocol error: {e.Message}");
            Disconnect("Protocol error.", DisconnectReason.Unknown);
        }
        catch (LoginException e)
        {
            _server.Logger.Info($"{_transport.RemoteEndPoint}: login refused: {e.Message}");
            Disconnect(e.Message, DisconnectReason.NotAuthenticated);
        }
    }

    private void Handle(byte[] packet) => HandlePacket(packet, packet.Length);

    private delegate void PacketHandler(ref BedrockReader reader);

    private readonly Dictionary<uint, PacketHandler?> _handlers = [];

    private void RegisterHandlers()
    {
        _handlers[PacketViolationWarningId] = OnPacketViolationWarning;
        _handlers[PacketId.ActorEvent] = null;
        _handlers[PacketId.AdventureSettings] = null;
        _handlers[PacketId.Animate] = null;
        _handlers[PacketId.AnvilDamage] = null;
        _handlers[PacketId.BlockActorData] = OnBlockActorData;
        _handlers[PacketId.BlockPickRequest] = OnBlockPickRequest;
        _handlers[PacketId.BookEdit] = OnBookEdit;
        _handlers[PacketId.BossEvent] = null;
        _handlers[PacketId.ClientCacheBlobStatus] = OnClientCacheBlobStatus;
        _handlers[PacketId.CommandRequest] = OnCommandRequest;
        _handlers[PacketId.ContainerClose] = OnContainerClose;
        _handlers[PacketId.Emote] = OnEmote;
        _handlers[PacketId.EmoteList] = null;
        _handlers[PacketId.FilterText] = null;
        _handlers[PacketId.Interact] = OnInteract;
        _handlers[PacketId.InventoryTransaction] = OnInventoryTransactionPacket;
        _handlers[PacketId.ItemStackRequest] = OnItemStackRequest;
        _handlers[PacketId.LecternUpdate] = OnLecternUpdate;
        _handlers[PacketId.MobEquipment] = OnMobEquipment;
        _handlers[PacketId.ModalFormResponse] = OnModalFormResponse;
        _handlers[PacketId.ServerSettingsRequest] = OnServerSettingsRequest;
        _handlers[PacketId.ServerboundDataStore] = OnServerboundDataStore;
        _handlers[PacketId.MovePlayer] = null;
        _handlers[PacketId.NPCRequest] = OnNPCRequest;
        _handlers[PacketId.PlayerAction] = OnPlayerAction;
        _handlers[PacketId.PlayerAuthInput] = OnPlayerAuthInput;
        _handlers[PacketId.PlayerSkin] = OnPlayerSkin;
        _handlers[PacketId.RequestAbility] = OnRequestAbility;
        _handlers[PacketId.RequestChunkRadius] = OnRequestChunkRadiusPacket;
        _handlers[PacketId.Respawn] = OnRespawnPacket;
        _handlers[PacketId.SetPlayerFurnaceOptions] = null;
        _handlers[PacketId.SetPlayerInventoryOptions] = null;
        _handlers[PacketId.SubChunkRequest] = OnSubChunkRequest;
        _handlers[PacketId.Text] = OnTextPacket;
        _handlers[PacketId.ServerBoundLoadingScreen] = OnServerBoundLoadingScreen;
        _handlers[PacketId.ServerBoundDiagnostics] = OnServerBoundDiagnostics;
    }

    internal void HandleInGamePacket(uint id, ref BedrockReader reader)
    {
        if (_handlers.TryGetValue(id, out var handler))
        {
            handler?.Invoke(ref reader);
        }
        else
        {
            if (_ignoredPackets.Add(id))
                _server.Logger.Debug($"{Name}: ignored packet id {id}");
        }
    }

    private void OnBlockActorData(ref BedrockReader reader)
    {
        if (reader.Remaining == 0)
            return;
        reader.VarInt32();
        reader.VarUInt32();
        reader.VarInt32();
        _ = reader.Bytes(reader.Remaining);
    }

    private void OnBlockPickRequest(ref BedrockReader reader)
    {
        if (reader.Remaining == 0)
            return;
        OnBlockPick(reader.VarInt32(), reader.VarInt32(), reader.VarInt32());
    }

    private void OnBookEdit(ref BedrockReader reader)
    {
        _ = reader.Bytes(reader.Remaining);
    }

    private void OnClientCacheBlobStatus(ref BedrockReader reader)
    {
        _ = reader.Bytes(reader.Remaining);
    }

    private void OnCommandRequest(ref BedrockReader reader)
    {
        _server.Commands.Dispatch(CommandSender, reader.String());
    }

    private void OnContainerClose(ref BedrockReader reader)
    {
        var window = reader.Byte();
        var type = reader.Byte();
        _inventoryOpen = false;
        CloseContainer(notifyClient: false);
        foreach (var stack in Inventory.ReturnCraftingItems())
            _server.DropItem(stack, Position.X, Position.Y - EyeHeight + 0.5f, Position.Z, world: World);
        Send(InventoryPackets.ContainerClose(window, window == InventoryPackets.WindowCraftingTable
            ? InventoryPackets.ContainerTypeWorkbench : type, serverInitiated: false));
        Send([.. Inventory.ContentPackets()]);
    }

    private void OnEmote(ref BedrockReader reader)
    {
        if (reader.Remaining == 0)
            return;
        reader.VarUInt64(); // claimed runtime id: ignored, a player can only emote as itself
        var emoteId = reader.String();
        var flags = reader.Remaining > 0 ? reader.Byte() : (byte)0;
        // Dragonfly session/handler_emote.go: one emote per second, so a client cannot flood every other player.
        if (emoteId.Length > 64 || _server.CurrentTick - _lastEmoteTick < BedrokiaServer.TicksPerSecond)
            return;
        _lastEmoteTick = _server.CurrentTick;
        _server.BroadcastInExcept(World, this, Packets.Emote((ulong)EntityId, emoteId, flags));
    }

    private long _lastEmoteTick = long.MinValue / 2;

    private void OnInteract(ref BedrockReader reader)
    {
        if (reader.Byte() == InventoryPackets.InteractOpenInventory && !_inventoryOpen)
        {
            _inventoryOpen = true;
            Send(InventoryPackets.ContainerOpen(0, InventoryPackets.ContainerTypeInventory,
                (int)Position.X, (int)Position.Y, (int)Position.Z));
        }
    }

    private void OnInventoryTransactionPacket(ref BedrockReader reader)
    {
        OnInventoryTransaction(ref reader);
    }

    private void OnItemStackRequest(ref BedrockReader reader)
    {
        Send([.. Inventory.HandleRequests(ref reader, GameMode, DropFromHand)]);
        RefreshEnchantOptions();
        SyncOpenContainer();
    }

    private void OnLecternUpdate(ref BedrockReader reader)
    {
        _ = reader.Bytes(reader.Remaining);
    }

    private void OnMobEquipment(ref BedrockReader reader)
    {
        reader.VarUInt64();
        SkipItemInstance(ref reader);
        reader.Byte();
        var hotbar = reader.Byte();
        if (reader.Byte() == InventoryPackets.WindowInventory && hotbar < 9)
        {
            if (hotbar != Inventory.SelectedSlot)
            {
                var oldSlot = Inventory.SelectedSlot;
                if (_server.Events.HasPlayerHeldSlotChange)
                {
                    var ev = new PlayerHeldSlotChangeEvent(this, oldSlot, hotbar);
                    if (!_server.Events.OnPlayerHeldSlotChange(ev))
                    {
                        Send(InventoryPackets.MobEquipment(EntityId, Inventory.Held, oldSlot, oldSlot, InventoryPackets.WindowInventory));
                        return;
                    }
                    hotbar = (byte)ev.NewSlot;
                }
                Inventory.SelectedSlot = hotbar;
            }
        }
    }



    private void OnNPCRequest(ref BedrockReader reader)
    {
        _ = reader.Bytes(reader.Remaining);
    }

    private void OnPlayerAction(ref BedrockReader reader)
    {
        if (reader.Remaining == 0)
            return;
        reader.VarUInt64();
        var action = reader.VarInt32();
        var x = reader.Remaining > 0 ? reader.VarInt32() : 0;
        var y = reader.Remaining > 0 ? reader.VarUInt32() : 0;
        var z = reader.Remaining > 0 ? reader.VarInt32() : 0;
        if (reader.Remaining >= 3)
        {
            reader.VarInt32();
            reader.VarUInt32();
            reader.VarInt32();
        }
        var face = reader.Remaining > 0 ? reader.VarInt32() : 0;
        OnBlockAction(action, x, (int)y, z, face);
    }

    private void OnPlayerSkin(ref BedrockReader reader)
    {
        _ = reader.Bytes(reader.Remaining);
    }

    private void OnRequestAbility(ref BedrockReader reader)
    {
        if (reader.Remaining == 0)
            return;
        reader.VarInt32();
        _ = reader.Bytes(reader.Remaining);
    }

    private void OnRequestChunkRadiusPacket(ref BedrockReader reader)
    {
        OnRequestChunkRadius(reader.VarInt32());
    }

    private void OnRespawnPacket(ref BedrockReader reader)
    {
        OnRespawnRequest(ref reader);
    }

    private void OnSubChunkRequest(ref BedrockReader reader)
    {
        _ = reader.Bytes(reader.Remaining);
    }

    private void OnTextPacket(ref BedrockReader reader)
    {
        OnText(ref reader);
    }

    private void OnServerBoundLoadingScreen(ref BedrockReader reader)
    {
        _ = reader.Bytes(reader.Remaining);
    }

    private void OnServerBoundDiagnostics(ref BedrockReader reader)
    {
        _ = reader.Bytes(reader.Remaining);
    }

    private void OnRequestNetworkSettings(int protocol)
    {
        if (protocol != ProtocolInfo.Version)
        {
            _server.Logger.Info($"{_transport.RemoteEndPoint}: refused protocol {protocol} (supported: {ProtocolInfo.Version}).");
            Send(Packets.PlayStatus(protocol < ProtocolInfo.Version ? PlayStatus.LoginFailedClient : PlayStatus.LoginFailedServer));
            _transport.Disconnect("incompatible version");
            return;
        }
        Send(Packets.NetworkSettings(CompressionThreshold));
        _codec.EnableCompression(CompressionThreshold);
        _stage = Stage.Login;
    }

    private void OnLogin(ReadOnlySpan<byte> request)
    {
        var keys = _server.AuthKeys;
        if (_server.Config.OnlineMode && keys is null)
            throw new LoginException("The server cannot reach Xbox Live right now. Try again in a minute.");

        var identity = LoginRequest.Verify(request, keys, DateTimeOffset.UtcNow);
        // NetherNet has no game-layer encryption: tie the login to the key the WebRTC identity proved,
        // otherwise a captured Login packet could be replayed.
        if (_transport.PeerPublicKey is { } peerKey
            && !peerKey.ExportSubjectPublicKeyInfo().AsSpan().SequenceEqual(identity.ClientKey.ExportSubjectPublicKeyInfo()))
            throw new LoginException("Login does not match the connection identity.");
        if (_transport.PeerPublicKey is null && _server.Config.OnlineMode)
            throw new LoginException("An Xbox Live identity is required to join this server.");

        Identity = identity;
        byte[] skin;
        try
        {
            skin = PlayerPackets.Skin(identity.ClientData);
        }
        catch (InvalidDataException e)
        {
            throw new LoginException($"Your skin could not be read ({e.Message}).");
        }
        var platform = identity.ClientData.TryGetProperty("DeviceOS", out var os) && os.TryGetInt32(out var value) ? value : 0;
        Info = new PlayerInfo(identity.Uuid, EntityId, identity.Name, identity.Xuid, platform, skin);

        if (_server.AdmissionRefusal(identity.Name) is { } refusal)
        {
            _server.Logger.Info($"{identity.Name} was refused: {refusal}");
            Disconnect(refusal);
            return;
        }
        if (!_server.TryReserveSlot(this))
        {
            Send(Packets.PlayStatus(PlayStatus.LoginFailedServerFull));
            _transport.Disconnect("server full");
            return;
        }
        _server.Logger.Info($"{Identity.Name} ({(Identity.Authenticated ? "xuid " + Identity.Xuid : "not authenticated")}) is logging in from {_transport.RemoteEndPoint} (NetherNet).");

        CompleteLogin(); // DTLS already encrypts the connection: no game-layer encryption handshake
    }

    private void CompleteLogin()
    {
        if (_server.Events.HasPlayerLogin)
        {
            var loginEv = new PlayerLoginEvent(this);
            if (!_server.Events.OnPlayerLogin(loginEv))
            {
                Disconnect(loginEv.KickReason);
                return;
            }
        }
        _stage = Stage.ResourcePacks;
        Send(Packets.PlayStatus(PlayStatus.LoginSuccess), _server.ResourcePacks.InfoPacket(_server.Config.ForceResourcePacks));
    }

    // Response (0 refused, 1 send packs, 2 all downloaded, 3 completed), its name, and for 1 the packs to send.
    private void OnResourcePackResponse(ref BedrockReader reader)
    {
        var response = reader.VarUInt32();
        if (reader.Remaining > 0)
            reader.String();
        var required = _server.Config.ForceResourcePacks;
        switch (response)
        {
            case 0: // refused
                if (required && _server.ResourcePacks.All.Count > 0)
                    Disconnect("This server requires its resource packs.");
                else
                    Send(_server.ResourcePacks.StackPacket(required));
                break;
            case 1: // send packs: one data info each, then the client asks for the chunks
            {
                var count = reader.Remaining > 0 ? reader.VarUInt32() : 0;
                var sent = 0;
                for (var i = 0; i < count; i++)
                    if (_server.ResourcePacks.ById(reader.String()) is { } pack)
                    {
                        Send(ResourcePacks.DataInfoPacket(pack));
                        sent++;
                    }
                if (sent == 0)
                    Send(_server.ResourcePacks.StackPacket(required));
                break;
            }
            case 2: // all downloaded (or nothing to download)
                Send(_server.ResourcePacks.StackPacket(required));
                break;
            case 3: // stack finished
            {
                _stage = Stage.ChunkRadius;
                var saved = PlayerData.Read(Identity!.Uuid);
                if (saved is not null)
                    _world = _server.WorldOf(saved.Dimension);
                var world = World;
                var spawn = _server.World.Spawn;
                Position = (spawn.X + 0.5f, spawn.Y + EyeHeight, spawn.Z + 0.5f);
                GameMode = _server.Config.GameMode;
                if (saved is not null)
                {
                    Position = (saved.X, saved.Y + EyeHeight, saved.Z); // saved at the feet, Position is the eyes
                    if (!_server.Config.ForceGameMode)
                        GameMode = saved.GameMode;
                    Inventory.SelectedSlot = Math.Clamp(saved.SelectedSlot, 0, 8);
                    PlayerData.Load(Inventory.Main, saved.Main);
                    PlayerData.Load(Inventory.Armour, saved.Armour);
                    PlayerData.Load(Inventory.Offhand, saved.Offhand);
                    if (saved.EnderChest is { } ender)
                        PlayerData.Load(Inventory.EnderChest, ender);
                    ExperienceLevel = Math.Max(0, saved.Level);
                    ExperienceProgress = Math.Clamp(saved.Progress, 0, 1);
                }
                Send(
                    Packets.JigsawStructureData(),
                    Packets.VoxelShapes(),
                    Packets.StartGame(new Packets.StartGameData(
                        EntityId, (int)GameMode,
                        Position.X, Position.Y, Position.Z,
                        spawn.X, spawn.Y, spawn.Z,
                        _server.Config.LevelName, _server.World.Time, (int)_server.Config.Difficulty, world.Settings.DaylightCycle, world.Dimension,
                        Game.Custom.Blocks.CustomBlockRegistry.GetDefinitions())),
                    Items.RegistryPacket,
                    Biome.DefinitionListPacket);
                break;
            }
            default:
                throw new InvalidDataException($"Unknown resource pack response {response}.");
        }
    }

    private void OnRequestChunkRadius(int requested)
    {
        _viewRadius = Math.Clamp(requested, 2, _server.Config.ViewDistance);
        Send(Packets.ChunkRadiusUpdated(_viewRadius));
        _centerChunk = (int.MinValue, int.MinValue);
        UpdateChunks(ChunkPosition.X, ChunkPosition.Z);
    }

    // Pitch, yaw, position (eyes), move vector, head yaw, ... (only the start is read).
    internal void OnPlayerAuthInput(ref BedrockReader reader)
    {
        var pitch = reader.Float32();
        var yaw = reader.Float32();
        var x = reader.Float32();
        var y = reader.Float32();
        var z = reader.Float32();
        if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(z) || !float.IsFinite(pitch) || !float.IsFinite(yaw))
            throw new InvalidDataException("Invalid position.");

        // Just after a dimension change the client still reports positions of the old dimension: ignore them.
        if (_portalCooldown > 0 && Math.Abs(x - Position.X) + Math.Abs(y - Position.Y) + Math.Abs(z - Position.Z) > 8)
            return;
        if (Frozen && Math.Abs(x - Position.X) + Math.Abs(z - Position.Z) + Math.Abs(y - Position.Y) > 0.5)
        {
            Teleport(Position.X, Position.Y - EyeHeight, Position.Z);
            return;
        }
        var moved = Math.Abs(x - Position.X) + Math.Abs(y - Position.Y) + Math.Abs(z - Position.Z) > 0.001
                    || Math.Abs(pitch - Pitch) + Math.Abs(yaw - Yaw) > 0.1;
        TrackFall(Position.Y, y); // every packet, so that landing is seen even when the player then stands still

        if (_server.IsTicking)
        {
            _hasPendingMovement = true;
            _pendingPitch = pitch;
            _pendingYaw = yaw;
            _pendingX = x;
            _pendingY = y;
            _pendingZ = z;
        }
        else
        {
            ApplyMovement(pitch, yaw, x, y, z);
        }

        if (reader.Remaining == 0)
            return; // short form (tests)

        // Rest of PlayerAuthInput (gophertunnel packet/player_auth_input.go, MIT): this is where a modern client
        // reports block breaking, item use and some inventory requests.
        reader.Bytes(12); // move vector, head yaw
        var flags = reader.VarUInt32();
        var sneaking = false;
        for (var i = 0; i < flags; i++)
            sneaking |= reader.VarInt32() == InputFlagSneaking;
        if (sneaking != Sneaking)
        {
            if (_server.Events.HasPlayerToggleAction)
            {
                var toggleEv = new PlayerToggleActionEvent(this, ToggleActionType.Sneak, sneaking);
                if (!_server.Events.OnPlayerToggleAction(toggleEv))
                    sneaking = Sneaking;
            }
            Sneaking = sneaking;
        }
        reader.VarUInt32(); // input mode
        reader.VarUInt32(); // play mode
        reader.VarInt32(); // interaction model
        reader.Bytes(8); // interact pitch, yaw
        reader.VarUInt64(); // tick
        reader.Bytes(12); // delta
        if (reader.Bool())
        {
            ReadLegacyItemSlots(ref reader);
            SkipInventoryActions(ref reader);
            OnUseItem(ref reader);
        }
        if (reader.Bool())
            Send([.. Inventory.HandleRequest(ref reader, GameMode, DropFromHand)]);
            RefreshEnchantOptions();
            SyncOpenContainer();
        if (reader.Bool())
        {
            var count = reader.VarUInt32();
            for (var i = 0; i < count; i++)
                OnBlockAction(reader.VarInt32(), reader.VarInt32(), reader.VarInt32(), reader.VarInt32(), reader.VarInt32());
        }
    }

    // Thrown from the eyes in the look direction, like vanilla (Dragonfly player.Drop).
    private void DropFromHand(ItemStack stack)
    {
        var yaw = Yaw * MathF.PI / 180;
        var pitch = Pitch * MathF.PI / 180;
        var velocity = (-MathF.Sin(yaw) * MathF.Cos(pitch) * 0.3f, -MathF.Sin(pitch) * 0.3f + 0.1f, MathF.Cos(yaw) * MathF.Cos(pitch) * 0.3f);
        _server.DropItem(stack, Position.X, Position.Y - 0.3f, Position.Z, velocity, pickupDelay: 40, world: World);
    }

    // Player action ids (gophertunnel protocol/player.go).
    private const int InputFlagSneaking = 8; // gophertunnel packet/player_auth_input.go

    /// <summary>Holding sneak: right clicks place against containers and doors instead of using them.</summary>
    public bool Sneaking { get; private set; }

    private const int StartBreak = 0, AbortBreak = 1, StopBreak = 2, CreativeDestroyBlock = 13, PredictDestroyBlock = 26,
        ContinueDestroyBlock = 27;
    private const int LevelEventStartCracking = 3600, LevelEventStopCracking = 3601, LevelEventDestroyParticles = 2001;
    private (int X, int Y, int Z)? _breaking;

    // Block cracking is driven by the server: tell everyone how long the block takes to break.
    private void OnBlockAction(int action, int x, int y, int z, int face)
    {
        _ = face;
        switch (action)
        {
            case StartBreak or ContinueDestroyBlock:
            {
                _breaking = (x, y, z);
                if (GameMode == GameMode.Creative || World.GetBlock(x, y, z) is not { IsAir: false } block)
                    return;
                var ticks = DropRules.BreakTicks(block, Inventory.Held);
                if (ticks > 0)
                    _server.BroadcastIn(World, Packets.LevelEvent(LevelEventStartCracking, x, y, z, 65535 / ticks));
                break;
            }
            case AbortBreak:
                _breaking = null;
                _server.BroadcastIn(World, Packets.LevelEvent(LevelEventStopCracking, x, y, z, 0));
                break;
            case PredictDestroyBlock or StopBreak or CreativeDestroyBlock:
            {
                var target = action == StopBreak && _breaking is { } breaking ? breaking : (x, y, z);
                _breaking = null;
                _server.BroadcastIn(World, Packets.LevelEvent(LevelEventStopCracking, target.Item1, target.Item2, target.Item3, 0));
                BreakBlock(target.Item1, target.Item2, target.Item3);
                break;
            }
        }
    }

    // LegacyRequestId and the optional legacy set-item-slots list.
    private static void ReadLegacyItemSlots(ref BedrockReader reader)
    {
        reader.VarInt32();
        if (!reader.Bool())
            return;
        var count = reader.VarUInt32();
        for (var i = 0; i < count; i++)
        {
            reader.Byte();
            reader.ByteArray();
        }
    }

    // InventoryAction: source type, optional window id, optional source flags, slot, old item, new item.
    private static void SkipInventoryActions(ref BedrockReader reader)
    {
        var count = reader.VarUInt32();
        for (var i = 0; i < count; i++)
        {
            reader.VarUInt32();
            if (reader.Bool())
                reader.Byte();
            if (reader.Bool())
                reader.VarUInt32();
            reader.VarUInt32();
            SkipItemInstance(ref reader);
            SkipItemInstance(ref reader);
        }
    }

    // LegacyRequestId, [legacy slots], type, actions, then the use-item data.
    private void OnInventoryTransaction(ref BedrockReader reader)
    {
        ReadLegacyItemSlots(ref reader);
        const uint useItem = 2, useItemOnEntity = 3;
        var type = reader.VarUInt32();
        if (type == 0) // normal: how the client drops items with the drop key (Q)
        {
            OnDropTransaction(ref reader);
            return;
        }
        if (type == 4) // release item: stopped eating, or a drawn bow, crossbow or trident is let go
        {
            _eatingSince = -1;
            OnReleaseItem();
        }
        if (type is not (useItem or useItemOnEntity))
            return; // ponytail: normal, mismatch and release transactions are not used
        SkipInventoryActions(ref reader);
        if (type == useItem)
            OnUseItem(ref reader);
        else
            OnUseItemOnEntity(ref reader);
    }

    // UseItemTransactionData: action, trigger, block position, face, hotbar slot, hand, held item, ...
    private void OnUseItem(ref BedrockReader reader)
    {
        const int clickBlock = 0, clickAir = 1, breakBlock = 2;
        var action = reader.VarInt32();
        reader.Byte(); // trigger type
        var bx = reader.VarInt32();
        var by = reader.VarInt32();
        var bz = reader.VarInt32();
        var face = reader.Byte();
        reader.VarInt32(); // hotbar slot
        reader.Byte(); // hand
        // Held item: id, count, meta, [stack net id], block runtime id, user data.
        var heldId = BinaryPrimitivesReadInt16(ref reader);
        reader.Bytes(2);
        reader.VarUInt32();
        if (reader.Bool())
            reader.VarInt32();
        var heldBlock = Blocks.ByNetworkId(reader.VarUInt32());
        reader.ByteArray();
        var clickY = 0f;
        if (reader.Remaining > 0) // the short form used by tests stops after the held item
        {
            clickY = System.Buffers.Binary.BinaryPrimitives.ReadSingleLittleEndian(reader.Bytes(24)[16..]); // player position, clicked position (x, y, z)
            reader.VarUInt32(); // clicked block runtime id
            reader.Bytes(2); // client prediction, cooldown state
        }

        if (action == clickAir)
        {
            if (Inventory.Held is { } inHand && _server.Events.HasPlayerItemUse
                && !_server.Events.OnPlayerItemUse(new PlayerItemUseEvent(this, inHand)))
            {
                Send(InventoryPackets.InventorySlot(InventoryPackets.WindowInventory, Inventory.SelectedSlot, Inventory.Held));
                return;
            }
            if (UseHeldInAir())
                return;
            if (Entities.ProjectileManager.IsThrowable(Inventory.Held))
            {
                var held = Inventory.Held!;
                if (_server.Projectiles.Launch(this, held.Type.Name, potion: held.Type.Name == Entities.ProjectileManager.SplashPotion ? held.Meta : (short)0) is not null
                    && GameMode != GameMode.Creative)
                    Send(Inventory.ConsumeHeld());
                else
                    Send(InventoryPackets.InventorySlot(InventoryPackets.WindowInventory, Inventory.SelectedSlot, Inventory.Held));
                return;
            }
            if (!TryEquipHeld()) // right click with armour in hand: the client already shows it worn
                StartEating();
            return;
        }
        if (action == breakBlock)
        {
            BreakBlock(bx, by, bz);
            return;
        }
        // Using the clicked block (doors, containers, tables...) unless sneaking, then using the held item on it.
        if (action == clickBlock && World.GetBlock(bx, by, bz) is { } clicked)
        {
            if (!Sneaking && UseBlock(clicked, bx, by, bz, face))
                return;
            if (Inventory.Held is { } used && !_server.Events.OnItemUse(new ItemUseEvent(this, used, bx, by, bz, clicked, face)))
            {
                Send(InventoryPackets.InventorySlot(InventoryPackets.WindowInventory, Inventory.SelectedSlot, used));
                return;
            }
            if (Inventory.Held is { } tool && UseItemOn(tool, clicked, bx, by, bz, face))
                return;
        }
        if (action == clickBlock && Inventory.Held is { } egg && egg.Type.Name.EndsWith("_spawn_egg"))
        {
            UseSpawnEgg(egg, bx, by, bz, face);
            return;
        }
        heldBlock ??= PlacedBlock(Inventory.Held); // beds, doors, signs... are items that place a block of their own
        if (action != clickBlock || heldId == 0 || heldBlock is null || heldBlock.IsAir)
            return; // not placing a block: interaction only
        // Clicking a replaceable block (tall grass, a thin snow layer...) puts the new block in its place, as the client
        // predicts it: placing next to it left a second, ghost block on the client.
        var (px, py, pz) = World.GetBlock(bx, by, bz) is { IsAir: false } target && IsReplaceable(target) ? (bx, by, bz) : face switch
        {
            0 => (bx, by - 1, bz),
            1 => (bx, by + 1, bz),
            2 => (bx, by, bz - 1),
            3 => (bx, by, bz + 1),
            4 => (bx - 1, by, bz),
            5 => (bx + 1, by, bz),
            _ => throw new InvalidDataException($"Invalid block face {face}."),
        };
        PlaceBlock(px, py, pz, heldBlock, face, clickY - MathF.Floor(clickY));
    }

    private static short BinaryPrimitivesReadInt16(ref BedrockReader reader) =>
        System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(reader.Bytes(2));

    // ItemInstance: id, count, meta, [stack network id], block runtime id, length-prefixed user data.
    private static void SkipItemInstance(ref BedrockReader reader)
    {
        reader.Bytes(4); // id, count
        reader.VarUInt32();
        if (reader.Bool())
            reader.VarInt32();
        reader.VarUInt32();
        reader.ByteArray();
    }

    /// <summary>Items, armour and off-hand of this player (server-authoritative).</summary>
    public PlayerInventory Inventory { get; } = new();

    private bool _inventoryOpen;
    private readonly HashSet<uint> _ignoredPackets = [];

    /// <summary>What to save for this player.</summary>
    public PlayerData Snapshot() => new(Position.X, Position.Y - EyeHeight, Position.Z, GameMode, Inventory.SelectedSlot,
        PlayerData.Save(Inventory.Main), PlayerData.Save(Inventory.Armour), PlayerData.Save(Inventory.Offhand), ExperienceLevel, ExperienceProgress, PlayerData.Save(Inventory.EnderChest), World.Dimension);

    // Blocks a placed block replaces (Dragonfly block/*.go ReplaceableBy).
    private static readonly HashSet<string> Replaceable =
    [
        "minecraft:air", "minecraft:water", "minecraft:flowing_water", "minecraft:lava", "minecraft:flowing_lava",
        "minecraft:short_grass", "minecraft:tall_grass", "minecraft:fern", "minecraft:large_fern", "minecraft:deadbush",
        "minecraft:seagrass", "minecraft:vine", "minecraft:glow_lichen", "minecraft:fire", "minecraft:snow_layer",
    ];

    // A snow layer is only replaced while it is one layer thick (Dragonfly block/snow.go).
    private static bool IsReplaceable(BlockType? block) => block is not null && Replaceable.Contains(block.Name)
        && (block.Name != "minecraft:snow_layer" || Convert.ToInt32(block.States.GetValueOrDefault("height", 0)) == 0);

    /// <summary>The player's current game mode (changed with the gamemode command).</summary>
    public GameMode GameMode { get; private set; }

    public void SetGameMode(GameMode mode)
    {
        var previous = GameMode;
        GameMode = mode;
        Send(Packets.SetPlayerGameType((int)mode));
        if (_canFly || _god || mode == GameMode.Spectator || previous == GameMode.Spectator)
            SendAbilities(); // flying / god mode / spectator flight and no-clip
    }

    // ponytail: the block state is the one the client sent (no server-side orientation rules yet).
    private void PlaceBlock(int x, int y, int z, BlockType block, int face = 1, float clickY = 0)
    {
        var world = World;
        var current = world.GetBlock(x, y, z);
        var dx = x + 0.5 - Position.X;
        var dy = y + 0.5 - (Position.Y - EyeHeight / 2);
        var dz = z + 0.5 - Position.Z;
        var held = Inventory.Held;
        var survival = GameMode != GameMode.Creative;
        if (current is null || SpawnProtected(x, z) || !IsReplaceable(current) || dx * dx + dy * dy + dz * dz > MaxReach * MaxReach
            || y is < Chunk.MinY or > Chunk.MaxY
            || survival && PlacedBlock(held)?.Name != block.Name
            || !_server.Events.OnPlace(new BlockEvent(this, x, y, z, block))) // survival: the block must really be in hand
        {
            if (current is not null)
                Send(Packets.UpdateBlock(x, y, z, current.NetworkId));
            if (survival)
                Send(InventoryPackets.InventorySlot(InventoryPackets.WindowInventory, Inventory.SelectedSlot, held));
            return;
        }
        var parts = BlockPlacement.Place(block, x, y, z, face, clickY, Yaw, Pitch,
            (fx, fy, fz) => IsReplaceable(world.GetBlock(fx, fy, fz)) && !SpawnProtected(fx, fz));
        // Like Dragonfly, a solid block cannot go where a player stands (no room for the door top either).
        if (parts.Count == 0 || parts.Any(p => Entities.BlockPhysics.IsSolid(p.Block) && OccupiedByPlayer(p.X, p.Y, p.Z)))
        {
            Send(Packets.UpdateBlock(x, y, z, current.NetworkId)); // undo the client's prediction
            if (survival)
                Send(InventoryPackets.InventorySlot(InventoryPackets.WindowInventory, Inventory.SelectedSlot, held));
            return;
        }
        foreach (var (px, py, pz, part) in parts)
        {
            world.SetBlock(px, py, pz, part);
            _server.BroadcastIn(World, Packets.UpdateBlock(px, py, pz, part.NetworkId));
            if (BlockEntity.For(part) is { } entity)
            {
                if (entity.Id == "Bed" && held is not null)
                    entity.Colour = (byte)held.Meta;
                world.SetBlockEntity(px, py, pz, entity);
                _server.BroadcastIn(World, BlockEntity.DataPacket(px, py, pz, entity));
            }
        }
        _server.ApplyGravity(x, y, z, World);
        World.Fluids.Wake(x, y, z);
        foreach (var (px, py, pz, _) in parts)
            _server.UpdateShapes(px, py, pz, World);
        if (survival)
            Send(Inventory.ConsumeHeld());
    }

    // True when a player's box (0.6 wide, 1.8 high from the feet) overlaps the block at (x, y, z).
    // ponytail: mobs are not checked yet; they get pushed out by their own physics.
    private bool OccupiedByPlayer(int x, int y, int z) => _server.Players.Any(p =>
    {
        if (!p.IsInGame)
            return false;
        var (px, eyes, pz) = p.Position;
        var feet = eyes - EyeHeight;
        return px + 0.3 > x && px - 0.3 < x + 1 && pz + 0.3 > z && pz - 0.3 < z + 1 && feet + 1.8 > y && feet < y + 1;
    });

    private void BreakBlock(int x, int y, int z)
    {
        var world = World;
        var current = world.GetBlock(x, y, z);
        var dx = x + 0.5 - Position.X;
        var dy = y + 0.5 - (Position.Y - EyeHeight / 2);
        var dz = z + 0.5 - Position.Z;
        var allowed = current is not null && !current.IsAir && dx * dx + dy * dy + dz * dz <= MaxReach * MaxReach
                      && (!current.Unbreakable || GameMode == GameMode.Creative) && !SpawnProtected(x, z);
        // Like PocketMine's BlockBreakEvent, plugins see (and may change) the drops and the experience.
        var breakEvent = new BlockEvent(this, x, y, z, current!);
        if (allowed && GameMode == GameMode.Survival)
            (breakEvent.Drops, breakEvent.Xp) = BreakDrops(current!);
        allowed = allowed && _server.Events.OnBreak(breakEvent);
        if (!allowed)
        {
            // Undo the client's prediction.
            if (current is not null)
                Send(Packets.UpdateBlock(x, y, z, current.NetworkId));
            return;
        }
        // Containers spill their contents (in every game mode, like vanilla) and close for their viewers.
        if (world.GetBlockEntity(x, y, z)?.Items is { } contents)
        {
            _server.CloseContainerFor((x, y, z), World);
            for (var slot = 0; slot < contents.Size; slot++)
                if (contents[slot] is { } stack)
                {
                    _server.DropItem(stack, x + 0.5f, y + 0.5f, z + 0.5f, world: World);
                    contents[slot] = null;
                }
        }
        world.SetBlock(x, y, z, Blocks.Air);
        AddExhaustion(0.005f);
        World.Fluids.Wake(x, y, z);
        _server.BroadcastIn(World, Packets.UpdateBlock(x, y, z, Blocks.Air.NetworkId));
        // Doors, beds and tall plants: the other half goes too (without a second drop).
        if (BlockPlacement.OtherHalf(current!, x, y, z) is { } other && world.GetBlock(other.X, other.Y, other.Z)?.Name == current!.Name)
        {
            world.SetBlock(other.X, other.Y, other.Z, Blocks.Air);
            _server.BroadcastIn(World, Packets.UpdateBlock(other.X, other.Y, other.Z, Blocks.Air.NetworkId));
        }
        _server.ApplyGravity(x, y + 1, z, World);
        _server.UpdateShapes(x, y, z, World);
        if (current!.Name is "minecraft:obsidian" or "minecraft:portal")
            BreakPortal(x, y, z);
        // The breaker's client shows its own particles; the others need the event.
        _server.BroadcastInExcept(World, this, Packets.LevelEvent(LevelEventDestroyParticles, x, y, z, unchecked((int)current!.NetworkId)));
        if (GameMode == GameMode.Creative) // a creative client shows neither particles nor sound for an instant break
            Send(Packets.LevelEvent(LevelEventDestroyParticles, x, y, z, unchecked((int)current.NetworkId)),
                new BedrockWriter().VarUInt32(123).String("break").Vec3(x + 0.5f, y + 0.5f, z + 0.5f).VarInt32(unchecked((int)current.NetworkId))
                    .String("").Bool(false).Bool(false).Int64(-1).Bool(false).ToArray());
        if (GameMode != GameMode.Survival)
            return;
        // Tools wear out on blocks that are not broken instantly (swords twice as fast).
        if (DropRules.HardnessOf(current) > 0)
            WearHeld(Inventory.Held?.Type.Name.EndsWith("_sword") == true ? 2 : 1);
        if (breakEvent.Xp > 0)
            AddExperience(breakEvent.Xp);
        foreach (var drop in breakEvent.Drops)
            _server.DropItem(drop, x + 0.5f, y + 0.25f, z + 0.5f, world: World);
    }

    // What breaking this block with the held item gives: items and experience.
    private (List<ItemStack> Drops, int Xp) BreakDrops(BlockType current)
    {
        var held = Inventory.Held;
        var drops = DropRules.Drops(current, held, Random.Shared);
        var xp = 0;
        if (Enchanting.Level(held, "silk_touch") > 0 && (drops.Count > 0 || current.Name.Contains("glass") || current.Name.Contains("ice")) && ItemStack.Of(current.Name) is { } itself)
            drops = [itself]; // ponytail: blocks with no item form (crops, beds...) keep their normal drops
        else
        {
            if (drops.Count > 0)
                xp = BlockExperience(current.Name);
            var fortune = Enchanting.Level(held, "fortune");
            if (fortune > 0 && current.Name.EndsWith("_ore"))
            {
                var bonus = Math.Max(0, Random.Shared.Next(fortune + 2) - 1); // vanilla ore fortune: x(bonus + 1)
                foreach (var drop in drops)
                    drop.SetCount(drop.Count * (bonus + 1));
            }
        }
        return (drops, xp);
    }

    internal void FlushTickMovement()
    {
        if (!_hasPendingMovement || _stage == Stage.Closed)
            return;
        _hasPendingMovement = false;
        ApplyMovement(_pendingPitch, _pendingYaw, _pendingX, _pendingY, _pendingZ);
    }

    private void ApplyMovement(float pitch, float yaw, float x, float y, float z)
    {
        if (_portalCooldown > 0 && Math.Abs(x - Position.X) + Math.Abs(y - Position.Y) + Math.Abs(z - Position.Z) > 8)
            return;
        if (Frozen && Math.Abs(x - Position.X) + Math.Abs(z - Position.Z) + Math.Abs(y - Position.Y) > 0.5)
        {
            Teleport(Position.X, Position.Y - EyeHeight, Position.Z);
            return;
        }
        var moved = Math.Abs(x - Position.X) + Math.Abs(y - Position.Y) + Math.Abs(z - Position.Z) > 0.001
                    || Math.Abs(pitch - Pitch) + Math.Abs(yaw - Yaw) > 0.1;
        if (moved)
        {
            var from = new System.Numerics.Vector3(Position.X, Position.Y, Position.Z);
            var to = new System.Numerics.Vector3(x, y, z);
            var isBlockChange = (int)Math.Floor(from.X) != (int)Math.Floor(to.X)
                || (int)Math.Floor(from.Y) != (int)Math.Floor(to.Y)
                || (int)Math.Floor(from.Z) != (int)Math.Floor(to.Z);

            if (_server.Events.HasPlayerMove)
            {
                var ev = new PlayerMoveEvent(this, from, to, yaw, pitch, isBlockChange);
                if (!_server.Events.OnMove(ev))
                {
                    Send(Packets.MovePlayer(EntityId, from.X, from.Y, from.Z, Pitch, Yaw, Yaw, teleport: true, onGround: true));
                    return;
                }
                if (ev.To != to)
                {
                    x = ev.To.X;
                    y = ev.To.Y;
                    z = ev.To.Z;
                    Send(Packets.MovePlayer(EntityId, x, y, z, pitch, yaw, yaw, teleport: true, onGround: true));
                }
            }

            Position = (x, y, z);
            Pitch = pitch;
            Yaw = yaw;
            UpdateChunks(ChunkPosition.X, ChunkPosition.Z);
            _server.BroadcastInExcept(World, this, Packets.MovePlayer(EntityId, x, y, z, pitch, yaw, yaw, teleport: false, onGround: true));
        }
    }

    internal void UpdateChunks(int centerX, int centerZ)
    {
        if (_viewRadius == 0 || _centerChunk == (centerX, centerZ))
            return;
        _centerChunk = (centerX, centerZ);
        var keep = (_viewRadius + 1) * (_viewRadius + 1);

        List<long>? toRemove = null;
        foreach (var key in _sentChunks)
        {
            var (cx, cz) = World.UnpackChunkKey(key);
            if (Distance2(cx - centerX, cz - centerZ) > keep)
            {
                toRemove ??= [];
                toRemove.Add(key);
                if (World.LoadedChunk(cx, cz) is { } chunk)
                    chunk.Viewers.Remove(this);
            }
        }
        if (toRemove is not null)
        {
            for (var i = 0; i < toRemove.Count; i++)
                _sentChunks.Remove(toRemove[i]);
        }

        List<long>? reqToRemove = null;
        foreach (var key in _requestedChunks)
        {
            var (cx, cz) = World.UnpackChunkKey(key);
            if (Distance2(cx - centerX, cz - centerZ) > keep)
            {
                reqToRemove ??= [];
                reqToRemove.Add(key);
            }
        }
        if (reqToRemove is not null)
        {
            for (var i = 0; i < reqToRemove.Count; i++)
                _requestedChunks.Remove(reqToRemove[i]);
        }

        Send(Packets.NetworkChunkPublisherUpdate((int)Position.X, (int)Position.Y, (int)Position.Z, (uint)_viewRadius * 16));
        var wanted = new List<(int X, int Z, int Dist2)>();
        for (var dx = -_viewRadius; dx <= _viewRadius; dx++)
        for (var dz = -_viewRadius; dz <= _viewRadius; dz++)
        {
            var d2 = Distance2(dx, dz);
            if (d2 <= _viewRadius * _viewRadius)
                wanted.Add((centerX + dx, centerZ + dz, d2));
        }
        wanted.Sort((a, b) => a.Dist2.CompareTo(b.Dist2));

        foreach (var item in wanted)
        {
            var key = World.ChunkKey(item.X, item.Z);
            if (_sentChunks.Contains(key) || !_requestedChunks.Add(key))
                continue;
            var world = World;
            var task = world.GetChunkAsync(item.X, item.Z);
            task.ContinueWith(t => _server.RunOnMainThread(() => OnChunkReady(t, world)), TaskScheduler.Default);
        }
        TrySpawn();
    }

    private void OnChunkReady(Task<Chunk> task, World world)
    {
        if (_stage == Stage.Closed || world != World)
            return;
        if (!task.IsCompletedSuccessfully)
        {
            _server.Logger.Error($"Chunk generation failed: {task.Exception?.GetBaseException()}");
            return;
        }
        var chunk = task.Result;
        var key = World.ChunkKey(chunk.X, chunk.Z);
        if (!_requestedChunks.Remove(key))
            return;
        _sentChunks.Add(key);
        chunk.Viewers.Add(this);
        var (count, payload) = chunk.NetworkPayload(world.Dimension == 1);
        Send(Packets.LevelChunk(chunk.X, chunk.Z, count, payload, world.Dimension));
        TrySpawn();
    }

    private void TrySpawn()
    {
        if (_stage != Stage.ChunkRadius)
            return;
        var (cx, cz) = ChunkPosition;
        for (var dx = -1; dx <= 1; dx++)
        for (var dz = -1; dz <= 1; dz++)
            if (!_sentChunks.Contains(World.ChunkKey(cx + dx, cz + dz)))
                return;
        _stage = Stage.Spawning;
        Send(Packets.PlayStatus(PlayStatus.PlayerSpawn), Items.CreativeContentPacket, Recipes.CraftingDataPacket, Recipes.TrimDataPacket, Packets.SetTime((int)_server.World.Time),
            PlayerPackets.SetPlayerData(EntityId, Name), PlayerPackets.PlayerAttributes(EntityId, Health, Food, Saturation, ExperienceLevel, ExperienceProgress));
        Send([.. Inventory.ContentPackets()]);
        SendCommands();
    }

    private static int Distance2(int dx, int dz) => dx * dx + dz * dz;

    // bool needsTranslation, u8 category, u8 type, [source], message, xuid, platform id, optional filtered.
    private void OnText(ref BedrockReader reader)
    {
        reader.Bool();
        reader.Byte();
        if ((TextType)reader.Byte() != TextType.Chat)
            return;
        reader.String(); // claimed source name: ignored, the server knows who is talking
        var message = reader.String().Trim();
        if (message.Length is 0 or > 512)
            return;
        var chat = new PlayerChatEvent(this, message);
        if (_server.Events.OnChat(chat))
        {
            var senderName = Prefix.Length > 0 ? $"{Prefix}{Name}§r" : Name;
            _server.Broadcast(Packets.Text(TextType.Chat, chat.Message, senderName), $"<{senderName}> {chat.Message}");
        }
    }

    internal BatchCodec Codec => _codec;

    internal void SendRawBatch(byte[] framedBatch, bool preferUnreliable = false)
    {
        if (_stage == Stage.Closed)
            return;
        FlushOutgoing();
        _transport.Send(framedBatch, preferUnreliable);
    }

    internal void FlushOutgoing()
    {
        // The buffer belongs to the tick thread; outside a tick it is already empty.
        if (!_server.IsTicking || _pendingOutgoing.Count == 0 || _stage == Stage.Closed)
            return;

        try
        {
            var batch = _codec.EncodeBatch(_pendingOutgoing);
            if (batch.Length > 0)
                _transport.Send(batch);
        }
        finally
        {
            _pendingOutgoing.Clear();
        }
    }

    internal void Send(byte[] packet)
    {
        if (_stage == Stage.Closed)
            return;

        if (_stage == Stage.InGame && _server.IsTicking)
        {
            _pendingOutgoing.Add(packet);
            return;
        }

        _transport.Send(_codec.Encode(packet));
    }

    internal void Send(params byte[][] packets)
    {
        if (_stage == Stage.Closed || packets.Length == 0)
            return;

        if (_stage == Stage.InGame && _server.IsTicking)
        {
            _pendingOutgoing.AddRange(packets);
            return;
        }

        if (packets.Length == 1)
            _transport.Send(_codec.Encode(packets[0]));
        else
            _transport.Send(_codec.Encode(packets));
    }
}

