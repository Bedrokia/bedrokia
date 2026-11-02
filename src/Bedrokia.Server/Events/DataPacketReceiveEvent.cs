using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when an inbound Bedrock data packet is received from a client.
/// If cancelled, packet processing is aborted.
/// </summary>
public sealed class DataPacketReceiveEvent(BedrockConnection player, uint packetId, byte[] packet, int length = -1) : CancellableEvent, IPlayerEvent
{
    /// <summary>The connection that sent this packet.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>The Bedrock packet ID.</summary>
    public uint PacketId { get; } = packetId;

    /// <summary>The raw packet payload.</summary>
    public byte[] Packet { get; set; } = packet;

    /// <summary>The packet length in bytes.</summary>
    public int Length { get; } = length >= 0 ? length : packet.Length;
}
