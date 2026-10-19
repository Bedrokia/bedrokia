namespace Bedrokia.Server.Network;

/// <summary>
/// A single item of inbound work processed on the main server thread without heap-allocating delegate closures.
/// </summary>
/// <param name="Connection">Target connection for an inbound packet, or null for general control actions.</param>
/// <param name="PooledPacket">Packet data rented from ArrayPool, or null for control actions.</param>
/// <param name="Length">Valid length of the pooled packet buffer.</param>
/// <param name="ControlAction">Non-packet delegate to execute (commands, tasks, connection events), or null.</param>
public readonly record struct InboundWorkItem(BedrockConnection? Connection, byte[]? PooledPacket, int Length, Action? ControlAction);
