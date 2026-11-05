using Bedrokia.Server.Network;

namespace Bedrokia.Server.Events;

/// <summary>
/// Fired when an outbound Bedrock data packet is about to be sent to a client.
/// If cancelled, the packet is not sent.
/// </summary>
public sealed class DataPacketSendEvent(BedrockConnection player, uint packetId, byte[] packet) : CancellableEvent, IPlayerEvent
{
    /// <summary>The recipient connection.</summary>
    public BedrockConnection Player { get; } = player;

    /// <summary>Uniform player accessor.</summary>
    public BedrockConnection player() => Player;

    /// <summary>The Bedrock packet ID.</summary>
    public uint PacketId { get; } = packetId;

    /// <summary>The raw packet bytes to send.</summary>
    public byte[] Packet { get; set; } = packet;
}
