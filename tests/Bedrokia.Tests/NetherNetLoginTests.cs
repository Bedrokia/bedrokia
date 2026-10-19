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

using System.Net;
using System.Security.Cryptography;
using System.Threading.Channels;
using Bedrokia.Protocol;
using Bedrokia.Server;
using Bedrokia.Server.Core;
using Bedrokia.Transport.NetherNet;
using SIPSorcery.Net;

namespace Bedrokia.Tests;

/// <summary>End-to-end over NetherNet: WebRTC with identity, then the Bedrock login without game-layer encryption.</summary>
public sealed class NetherNetLoginTests : IAsyncDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private readonly Logger _logger = new(LogLevel.Error, null);
    private readonly BedrokiaServer _server;
    private readonly Task _run;

    public NetherNetLoginTests()
    {
        _server = new BedrokiaServer(new ServerConfig { WorldsDirectory = TestWorlds.NewDirectory(), Seed = 42, Port = 0, OnlineMode = false, ViewDistance = 2 }, _logger);
        _run = _server.RunAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _server.Stop();
        await _run.WaitAsync(Timeout);
        _logger.Dispose();
    }

    private sealed class Client(RTCPeerConnection peer, RTCDataChannel reliable)
    {
        private readonly Channel<byte[]> _batches = Channel.CreateUnbounded<byte[]>();
        private readonly Queue<byte[]> _packets = new();
        private readonly List<byte> _partial = [];

        public BatchCodec Codec { get; } = new(withHeader: false);
        public RTCPeerConnection Peer => peer;

        public void Listen() => reliable.onmessage += (_, _, data) =>
        {
            _partial.AddRange(data.Skip(1));
            if (data[0] != 0)
                return;
            _batches.Writer.TryWrite([.. _partial]);
            _partial.Clear();
        };

        public void Send(uint id, Action<BedrockWriter> body)
        {
            var writer = new BedrockWriter().VarUInt32(id);
            body(writer);
            reliable.send([0, .. Codec.Encode([writer.ToArray()])]);
        }

        public async Task<byte[]> Expect(uint id)
        {
            while (true)
            {
                while (_packets.Count == 0)
                    foreach (var p in Codec.Decode(await _batches.Reader.ReadAsync().AsTask().WaitAsync(Timeout)))
                        _packets.Enqueue(p);
                var packet = _packets.Dequeue();
                if ((new BedrockReader(packet).VarUInt32() & 0x3FF) == id)
                    return packet;
            }
        }

        /// <summary>The block sent for (x, y, z), skipping the updates of other blocks (grass spreading, leaves...).</summary>
        public async Task<uint> ExpectBlockAt(int x, int y, int z)
        {
            while (true)
            {
                var reader = new BedrockReader(await Expect(PacketId.UpdateBlock));
                reader.VarUInt32();
                if ((reader.VarInt32(), reader.VarInt32(), reader.VarInt32()) == (x, y, z))
                    return reader.VarUInt32();
            }
        }
    }

    private async Task<Client> ConnectAsync(ECDsa identityKey)
    {
        await _server.Started.WaitAsync(Timeout);
        var signaling = new IPEndPoint(IPAddress.Loopback, _server.NetherNetEndPoint!.Port);
        var (peer, reliable, response) = await NetherNetTests.ConnectAsync(signaling, identityKey);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var client = new Client(peer, reliable);
        client.Listen();
        return client;
    }

    /// <summary>Full join up to SetLocalPlayerAsInitialised; returns the client and its entity id.</summary>
    private async Task<(Client Client, ulong EntityId)> JoinAsync(string name)
    {
        var key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        var client = await ConnectAsync(key);
        client.Send(PacketId.RequestNetworkSettings, w => w.Int32BE(ProtocolInfo.Version));
        await client.Expect(PacketId.NetworkSettings);
        client.Codec.EnableCompression(256);
        var request = ProtocolTests.BuildLoginRequest(key, ProtocolTests.OfflineToken(key, name));
        client.Send(PacketId.Login, w => w.Int32BE(ProtocolInfo.Version).ByteArray(request));
        await client.Expect(PacketId.PlayStatus);
        client.Send(PacketId.ResourcePackClientResponse, w => w.VarUInt32(2).String("downloadingfinished"));
        await client.Expect(PacketId.ResourcePackStack);
        client.Send(PacketId.ResourcePackClientResponse, w => w.VarUInt32(3).String("resourcepackstackfinished"));
        var startGame = new BedrockReader(await client.Expect(PacketId.StartGame));
        startGame.VarUInt32();
        startGame.VarUInt64();
        var entityId = startGame.VarUInt64();
        client.Send(PacketId.RequestChunkRadius, w => w.VarInt32(2).Byte(2));
        await client.Expect(PacketId.PlayStatus); // spawn
        client.Send(PacketId.SetLocalPlayerAsInitialised, w => w.VarUInt64(entityId));
        return (client, entityId);
    }

    [Fact]
    public async Task A_summoned_mob_is_sent_to_the_player()
    {
        var (alice, _) = await JoinAsync("Alice");
        await Task.Delay(300); // let the server mark Alice in game
        _server.EnqueueCommand("op Alice");
        _server.RunOnMainThread(() =>
        {
            var p = _server.Players[0].Position;
            _server.Mobs.Spawn(Bedrokia.Server.Entities.MobKind.Cow, p.X + 2, p.Y - 1.62, p.Z);
        });
        var add = new BedrockReader(await alice.Expect(Bedrokia.Server.Entities.EntityPackets.AddActorId));
        add.VarUInt32();
        add.VarInt64();
        add.VarUInt64();
        Assert.Equal("minecraft:cow", add.String());
    }

    [Fact]
    public async Task Players_see_each_other_move_break_blocks_and_leave()
    {
        var (alice, aliceId) = await JoinAsync("Alice");
        await alice.Expect(PacketId.PlayerList);
        var (bob, _) = await JoinAsync("Bob");

        // Bob is told about Alice.
        var add = new BedrockReader(await bob.Expect(PacketId.AddPlayer));
        add.VarUInt32();
        add.Bytes(16); // uuid
        Assert.Equal("Alice", add.String());
        Assert.Equal(aliceId, add.VarUInt64());

        // Alice moves: Bob gets MovePlayer.
        var spawn = _server.World.Spawn;
        alice.Send(PacketId.PlayerAuthInput, w => w.Float32(0).Float32(90).Vec3(spawn.X + 2.5f, spawn.Y + 1.62f, spawn.Z + 0.5f));
        var move = new BedrockReader(await bob.Expect(PacketId.MovePlayer));
        move.VarUInt32();
        Assert.Equal(aliceId, move.VarUInt64());
        Assert.Equal(spawn.X + 2.5f, move.Float32());

        // Survival: Alice breaks a dirt block under her feet. Everyone sees it go, the dirt item drops on the ground.
        var dirt = Bedrokia.Server.Game.Blocks.Dirt;
        var onMain = new TaskCompletionSource();
        _server.RunOnMainThread(() => { _server.World.SetBlock(spawn.X + 2, spawn.Y - 1, spawn.Z, dirt); onMain.SetResult(); });
        await onMain.Task.WaitAsync(Timeout);
        alice.Send(PacketId.InventoryTransaction, w => w.VarInt32(0).Bool(false).VarUInt32(2).VarUInt32(0)
            .VarInt32(2).Byte(0).BlockPos(spawn.X + 2, spawn.Y - 1, spawn.Z)
            .Byte(1).VarInt32(0).Byte(0).Int16(0).UInt16(0).VarUInt32(0).Bool(false).VarUInt32(0).VarUInt32(0)); // face, slot, hand, empty hand
        Assert.Equal(Bedrokia.Server.Game.Blocks.Air.NetworkId, await bob.ExpectBlockAt(spawn.X + 2, spawn.Y - 1, spawn.Z));
        await bob.Expect(Bedrokia.Server.Network.InventoryPackets.AddItemActorId);

        // Alice stands on it: she picks it up into hotbar slot 0.
        var pickup = new BedrockReader(await alice.Expect(Bedrokia.Server.Network.InventoryPackets.InventorySlotId));
        pickup.VarUInt32();
        Assert.Equal(0u, pickup.VarUInt32()); // window: inventory
        Assert.Equal(0u, pickup.VarUInt32()); // slot 0
        await bob.Expect(Bedrokia.Server.Network.InventoryPackets.TakeItemActorId);

        // Snow fell in the hole. She puts the dirt back by clicking the thin snow layer (face up): like the client
        // predicts it, the dirt replaces the snow instead of going on top of it. Her slot empties.
        var snowSet = new TaskCompletionSource();
        _server.RunOnMainThread(() =>
        {
            _server.World.SetBlock(spawn.X + 2, spawn.Y - 1, spawn.Z, Bedrokia.Server.Game.Blocks.ByName("snow_layer")!);
            snowSet.SetResult();
        });
        await snowSet.Task.WaitAsync(Timeout);
        var dirtItem = Bedrokia.Server.Game.Items.ByName("dirt")!;
        alice.Send(PacketId.InventoryTransaction, w => w.VarInt32(0).Bool(false).VarUInt32(2).VarUInt32(0)
            .VarInt32(0).Byte(0).BlockPos(spawn.X + 2, spawn.Y - 1, spawn.Z)
            .Byte(1).VarInt32(0).Byte(0) // face up, hotbar slot, hand
            .Int16(dirtItem.RuntimeId).UInt16(1).VarUInt32(0).Bool(false).VarUInt32(dirt.NetworkId).VarUInt32(0));
        Assert.Equal(dirt.NetworkId, await bob.ExpectBlockAt(spawn.X + 2, spawn.Y - 1, spawn.Z));
        var emptied = new BedrockReader(await alice.Expect(Bedrokia.Server.Network.InventoryPackets.InventorySlotId));
        emptied.VarUInt32();
        emptied.VarUInt32();
        emptied.VarUInt32();
        emptied.Bool();
        emptied.Bool();
        Assert.Equal(0, emptied.Bytes(2)[0]); // air

        // In creative, she tries to put dirt where she stands (on top of the dirt): refused, her client is corrected.
        _server.EnqueueCommand("gamemode creative Alice");
        await alice.Expect(PacketId.SetPlayerGameType);
        alice.Send(PacketId.InventoryTransaction, w => w.VarInt32(0).Bool(false).VarUInt32(2).VarUInt32(0)
            .VarInt32(0).Byte(0).BlockPos(spawn.X + 2, spawn.Y - 1, spawn.Z)
            .Byte(1).VarInt32(0).Byte(0)
            .Int16(dirtItem.RuntimeId).UInt16(1).VarUInt32(0).Bool(false).VarUInt32(dirt.NetworkId).VarUInt32(0));
        Assert.NotEqual(dirt.NetworkId, await alice.ExpectBlockAt(spawn.X + 2, spawn.Y, spawn.Z)); // the block that really is there (air or grass)

        // Alice is kicked: Bob removes her.
        _server.EnqueueCommand("kick Alice");
        var remove = new BedrockReader(await bob.Expect(PacketId.RemoveActor));
        remove.VarUInt32();
        Assert.Equal((long)aliceId, remove.VarInt64());
        bob.Peer.close();
    }

    [Fact]
    public async Task Client_joins_over_nethernet()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        var client = await ConnectAsync(key);

        client.Send(PacketId.RequestNetworkSettings, w => w.Int32BE(ProtocolInfo.Version));
        await client.Expect(PacketId.NetworkSettings);
        client.Codec.EnableCompression(256);

        var request = ProtocolTests.BuildLoginRequest(key, ProtocolTests.OfflineToken(key, "NetherPlayer"));
        client.Send(PacketId.Login, w => w.Int32BE(ProtocolInfo.Version).ByteArray(request));
        var status = await client.Expect(PacketId.PlayStatus); // no encryption handshake on NetherNet
        var reader = new BedrockReader(status);
        reader.VarUInt32();
        Assert.Equal(PlayStatus.LoginSuccess, reader.Int32BE());

        client.Send(PacketId.ResourcePackClientResponse, w => w.VarUInt32(2).String("downloadingfinished"));
        await client.Expect(PacketId.ResourcePackStack);
        client.Send(PacketId.ResourcePackClientResponse, w => w.VarUInt32(3).String("resourcepackstackfinished"));
        await client.Expect(PacketId.StartGame);
        client.Send(PacketId.RequestChunkRadius, w => w.VarInt32(2).Byte(2));
        await client.Expect(PacketId.LevelChunk);
        await client.Expect(PacketId.PlayStatus); // spawn

        Assert.Single(_server.Players);
        client.Peer.close();
    }

    [Fact]
    public async Task Login_with_another_key_than_the_webrtc_identity_is_refused()
    {
        using var identityKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using var replayedKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        var client = await ConnectAsync(identityKey);

        client.Send(PacketId.RequestNetworkSettings, w => w.Int32BE(ProtocolInfo.Version));
        await client.Expect(PacketId.NetworkSettings);
        client.Codec.EnableCompression(256);
        var request = ProtocolTests.BuildLoginRequest(replayedKey, ProtocolTests.OfflineToken(replayedKey, "Replayer"));
        client.Send(PacketId.Login, w => w.Int32BE(ProtocolInfo.Version).ByteArray(request));

        var disconnect = await client.Expect(PacketId.Disconnect);
        Assert.Contains("identity", System.Text.Encoding.UTF8.GetString(disconnect));
        Assert.Empty(_server.Players);
        client.Peer.close();
    }
}
