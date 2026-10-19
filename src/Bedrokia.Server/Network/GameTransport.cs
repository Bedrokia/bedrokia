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
using Bedrokia.Transport.NetherNet;

namespace Bedrokia.Server.Network;

/// <summary>What a Bedrock connection needs from its transport (NetherNet; an interface so tests can fake it).</summary>
public interface IGameTransport
{
    IPEndPoint RemoteEndPoint { get; }

    /// <summary>Key the transport proved the client owns (NetherNet identity), or null.</summary>
    ECDsa? PeerPublicKey { get; }

    void Send(byte[] batch);
    void Send(byte[] batch, bool preferUnreliable) => Send(batch);
    void Disconnect(string reason);
}

internal sealed class NetherNetTransport(NetherNetSession session) : IGameTransport
{
    public IPEndPoint RemoteEndPoint => session.RemoteEndPoint;
    public ECDsa? PeerPublicKey => session.PeerPublicKey;
    public void Send(byte[] batch) => session.Send(batch, false);
    public void Send(byte[] batch, bool preferUnreliable) => session.Send(batch, preferUnreliable);
    public void Disconnect(string reason) => session.Disconnect(reason);
}

