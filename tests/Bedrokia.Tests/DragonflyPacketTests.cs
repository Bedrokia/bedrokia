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
using System.Reflection;
using System.Security.Cryptography;
using Bedrokia.Protocol;
using Bedrokia.Server;
using Bedrokia.Server.Core;
using Bedrokia.Server.Network;

namespace Bedrokia.Tests;

public class DragonflyPacketTests
{
    private sealed class FakeTransport : IGameTransport
    {
        public IPEndPoint RemoteEndPoint { get; } = new(IPAddress.Loopback, 19132);
        public ECDsa? PeerPublicKey => null;
        public List<byte[]> Sent { get; } = [];
        public void Send(byte[] batch) => Sent.Add(batch);
        public void Disconnect(string reason) { }
    }

    [Fact]
    public void Packet_id_constants_match_dragonfly_protocol()
    {
        Assert.Equal(56u, PacketId.BlockActorData);
        Assert.Equal(34u, PacketId.BlockPickRequest);
        Assert.Equal(97u, PacketId.BookEdit);
        Assert.Equal(135u, PacketId.ClientCacheBlobStatus);
        Assert.Equal(77u, PacketId.CommandRequest);
        Assert.Equal(47u, PacketId.ContainerClose);
        Assert.Equal(138u, PacketId.Emote);
        Assert.Equal(33u, PacketId.Interact);
        Assert.Equal(30u, PacketId.InventoryTransaction);
        Assert.Equal(147u, PacketId.ItemStackRequest);
        Assert.Equal(125u, PacketId.LecternUpdate);
        Assert.Equal(31u, PacketId.MobEquipment);
        Assert.Equal(101u, PacketId.ModalFormResponse);
        Assert.Equal(98u, PacketId.NPCRequest);
        Assert.Equal(36u, PacketId.PlayerAction);
        Assert.Equal(144u, PacketId.PlayerAuthInput);
        Assert.Equal(93u, PacketId.PlayerSkin);
        Assert.Equal(184u, PacketId.RequestAbility);
        Assert.Equal(69u, PacketId.RequestChunkRadius);
        Assert.Equal(45u, PacketId.Respawn);
        Assert.Equal(175u, PacketId.SubChunkRequest);
        Assert.Equal(9u, PacketId.Text);
        Assert.Equal(312u, PacketId.ServerBoundLoadingScreen);
        Assert.Equal(315u, PacketId.ServerBoundDiagnostics);

        Assert.Equal(27u, PacketId.ActorEvent);
        Assert.Equal(55u, PacketId.AdventureSettings);
        Assert.Equal(44u, PacketId.Animate);
        Assert.Equal(141u, PacketId.AnvilDamage);
        Assert.Equal(74u, PacketId.BossEvent);
        Assert.Equal(152u, PacketId.EmoteList);
        Assert.Equal(163u, PacketId.FilterText);
        Assert.Equal(19u, PacketId.MovePlayer);
        Assert.Equal(351u, PacketId.SetPlayerFurnaceOptions);
        Assert.Equal(307u, PacketId.SetPlayerInventoryOptions);
    }

    [Fact]
    public void Handlers_table_contains_all_24_dragonfly_packets()
    {
        using var logger = new Logger(LogLevel.Error, null);
        var server = new BedrokiaServer(new ServerConfig { WorldsDirectory = TestWorlds.NewDirectory() }, logger);
        var conn = new BedrockConnection(server, new FakeTransport(), 1);

        var handlersField = typeof(BedrockConnection).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(handlersField);
        var handlers = handlersField.GetValue(conn) as System.Collections.IDictionary;
        Assert.NotNull(handlers);

        uint[] active24 = [
            PacketId.BlockActorData,
            PacketId.BlockPickRequest,
            PacketId.BookEdit,
            PacketId.ClientCacheBlobStatus,
            PacketId.CommandRequest,
            PacketId.ContainerClose,
            PacketId.Emote,
            PacketId.Interact,
            PacketId.InventoryTransaction,
            PacketId.ItemStackRequest,
            PacketId.LecternUpdate,
            PacketId.MobEquipment,
            PacketId.ModalFormResponse,
            PacketId.NPCRequest,
            PacketId.PlayerAction,
            PacketId.PlayerAuthInput,
            PacketId.PlayerSkin,
            PacketId.RequestAbility,
            PacketId.RequestChunkRadius,
            PacketId.Respawn,
            PacketId.SubChunkRequest,
            PacketId.Text,
            PacketId.ServerBoundLoadingScreen,
            PacketId.ServerBoundDiagnostics
        ];

        foreach (var id in active24)
        {
            Assert.True(handlers.Contains(id));
            Assert.NotNull(handlers[id]);
        }

        uint[] unhandledNil = [
            PacketId.ActorEvent,
            PacketId.AdventureSettings,
            PacketId.Animate,
            PacketId.AnvilDamage,
            PacketId.BossEvent,
            PacketId.EmoteList,
            PacketId.FilterText,
            PacketId.MovePlayer,
            PacketId.SetPlayerFurnaceOptions,
            PacketId.SetPlayerInventoryOptions
        ];

        foreach (var id in unhandledNil)
        {
            Assert.True(handlers.Contains(id));
            Assert.Null(handlers[id]);
        }
    }

    [Fact]
    public void In_game_dispatches_player_auth_input_movement()
    {
        using var logger = new Logger(LogLevel.Error, null);
        var server = new BedrokiaServer(new ServerConfig { WorldsDirectory = TestWorlds.NewDirectory() }, logger);
        var conn = new BedrockConnection(server, new FakeTransport(), 1);

        var stageField = typeof(BedrockConnection).GetField("_stage", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(stageField);
        stageField.SetValue(conn, Enum.Parse(stageField.FieldType, "InGame"));

        var handleMethod = typeof(BedrockConnection).GetMethod("Handle", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(handleMethod);

        var input = new BedrockWriter()
            .VarUInt32(PacketId.PlayerAuthInput)
            .Float32(10.5f)
            .Float32(20.0f)
            .Float32(100.0f)
            .Float32(64.0f)
            .Float32(200.0f)
            .ToArray();

        handleMethod.Invoke(conn, [input]);

        Assert.Equal(100.0f, conn.Position.X);
        Assert.Equal(64.0f, conn.Position.Y);
        Assert.Equal(200.0f, conn.Position.Z);
        Assert.Equal(10.5f, conn.Pitch);
        Assert.Equal(20.0f, conn.Yaw);
    }

    [Fact]
    public void In_game_ignores_move_player_and_animate_cleanly()
    {
        using var logger = new Logger(LogLevel.Error, null);
        var server = new BedrokiaServer(new ServerConfig { WorldsDirectory = TestWorlds.NewDirectory() }, logger);
        var conn = new BedrockConnection(server, new FakeTransport(), 1);

        var stageField = typeof(BedrockConnection).GetField("_stage", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(stageField);
        stageField.SetValue(conn, Enum.Parse(stageField.FieldType, "InGame"));

        var handleMethod = typeof(BedrockConnection).GetMethod("Handle", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(handleMethod);

        var movePlayer = new BedrockWriter().VarUInt32(PacketId.MovePlayer).VarUInt64(1).ToArray();
        var animate = new BedrockWriter().VarUInt32(PacketId.Animate).VarInt32(1).VarUInt64(1).ToArray();
        var adventure = new BedrockWriter().VarUInt32(PacketId.AdventureSettings).UInt32(0).ToArray();

        handleMethod.Invoke(conn, [movePlayer]);
        handleMethod.Invoke(conn, [animate]);
        handleMethod.Invoke(conn, [adventure]);
    }

    [Fact]
    public void Emote_packet_encodes_correctly()
    {
        var packet = Packets.Emote(42, "wave-guid", 1);
        var reader = new BedrockReader(packet);
        Assert.Equal(PacketId.Emote, reader.VarUInt32());
        Assert.Equal(42ul, reader.VarUInt64());
        Assert.Equal("wave-guid", reader.String());
        Assert.Equal((byte)1, reader.Byte());
    }
}
