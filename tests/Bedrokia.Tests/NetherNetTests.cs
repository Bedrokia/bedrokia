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
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bedrokia.Protocol.Login;
using Bedrokia.Transport.NetherNet;
using SIPSorcery.Net;

namespace Bedrokia.Tests;

/// <summary>A SIPSorcery peer plays the Minecraft client: two data channels, SDP offer over HTTP, identity assertion.</summary>
public sealed class NetherNetTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    private static readonly HttpClient Http = new();

    private readonly ECDsa _serverKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
    private readonly CancellationTokenSource _stop = new();
    private NetherNetListener? _listener;

    public void Dispose()
    {
        _stop.Cancel();
        _listener?.Dispose();
        _serverKey.Dispose();
    }

    private NetherNetListener Start(bool allowAnonymous)
    {
        _listener = new NetherNetListener(new IPEndPoint(IPAddress.Loopback, 0), _serverKey,
            token => LoginRequest.VerifyMultiplayerToken(token, null, DateTimeOffset.UtcNow).ClientKey,
            allowAnonymous, null);
        _ = _listener.RunAsync(_stop.Token);
        return _listener;
    }

    /// <summary>Client side of NetherNet: returns the peer and its reliable channel once both channels are open.</summary>
    internal static async Task<(RTCPeerConnection Peer, RTCDataChannel Reliable, HttpResponseMessage Response)> ConnectAsync(
        IPEndPoint signaling, ECDsa? identityKey)
    {
        var peer = new RTCPeerConnection(new RTCConfiguration());
        var reliable = await peer.createDataChannel(NetherNetSession.ReliableLabel, new RTCDataChannelInit { ordered = true });
        await peer.createDataChannel(NetherNetSession.UnreliableLabel, new RTCDataChannelInit { ordered = false, maxRetransmits = 0 });
        var offer = peer.createOffer();
        await peer.setLocalDescription(offer);

        var sdp = offer.sdp;
        if (identityKey is not null)
            sdp = AddClientIdentity(sdp, identityKey);

        var response = await Http.PostAsync($"http://{signaling}/v1/join/12345",
            new StringContent(sdp, new MediaTypeHeaderValue("application/sdp")));
        if (response.IsSuccessStatusCode)
        {
            var answer = await response.Content.ReadAsStringAsync();
            var result = peer.setRemoteDescription(new RTCSessionDescriptionInit { type = RTCSdpType.answer, sdp = answer });
            Assert.Equal(SetDescriptionResultEnum.OK, result);
            var open = new TaskCompletionSource();
            reliable.onopen += () => open.TrySetResult();
            if (reliable.readyState == RTCDataChannelState.open)
                open.TrySetResult();
            await open.Task.WaitAsync(Timeout);
        }
        return (peer, reliable, response);
    }

    private static string AddClientIdentity(string offer, ECDsa key)
    {
        var token = ProtocolTests.OfflineToken(key, "NetherPlayer");
        var fingerprint = offer.Split('\n').First(l => l.StartsWith("a=fingerprint:")).TrimEnd('\r')["a=fingerprint:".Length..].Split(' ');
        var assertion = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["fingerprints"] = Jwt.SignDetachedEs384(key, SdpIdentity.FingerprintPayload(fingerprint[0], fingerprint[1])),
            ["token"] = token,
        });
        var identity = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            assertion,
            idp = new { domain = "https://authorization.franchise.minecraft-services.net/", protocol = "default" },
        })));
        var media = offer.IndexOf("\r\nm=", StringComparison.Ordinal) + 2;
        return offer[..media] + "a=identity:" + identity + "\r\n" + offer[media..];
    }

    [Fact]
    public async Task Status_endpoint_returns_json_and_tls_hello_is_closed()
    {
        var listener = Start(allowAnonymous: true);

        // Same as Bedrock Dedicated Server 1.26.52: 200 with an empty body.
        using var status = await Http.GetAsync($"http://{listener.LocalEndPoint}/v1/join");
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        Assert.Empty(await status.Content.ReadAsByteArrayAsync());
        await Assert.ThrowsAnyAsync<HttpRequestException>(() => Http.GetStringAsync($"https://{listener.LocalEndPoint}/v1/join"));
    }

    [Fact]
    public async Task Peer_with_identity_connects_and_exchanges_segmented_messages()
    {
        var listener = Start(allowAnonymous: false);
        var connected = new TaskCompletionSource<NetherNetSession>();
        var received = new TaskCompletionSource<byte[]>();
        listener.SessionOpened += session =>
        {
            session.Connected += s => connected.TrySetResult(s);
            session.PacketReceived += (_, data) => received.TrySetResult(data);
        };
        using var clientKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);

        var (peer, reliable, response) = await ConnectAsync(listener.LocalEndPoint, clientKey);
        Assert.True(response.IsSuccessStatusCode);
        var answer = await response.Content.ReadAsStringAsync();
        Assert.Contains("a=identity:", answer);

        var session = await connected.Task.WaitAsync(Timeout);
        Assert.Equal(clientKey.ExportSubjectPublicKeyInfo(), session.PeerPublicKey!.ExportSubjectPublicKeyInfo());

        // Client -> server: a 25 000-byte message in three segments (2, 1, 0 remaining).
        var big = RandomNumberGenerator.GetBytes(25_000);
        for (var i = 0; i < 3; i++)
            reliable.send([(byte)(2 - i), .. big.AsSpan(i * 10_000, Math.Min(10_000, big.Length - i * 10_000))]);
        Assert.Equal(big, await received.Task.WaitAsync(Timeout));

        // Server -> client: segmented by the session, reassembled here.
        var echo = new TaskCompletionSource<byte[]>();
        var parts = new List<byte>();
        reliable.onmessage += (_, _, data) =>
        {
            parts.AddRange(data.Skip(1));
            if (data[0] == 0)
                echo.TrySetResult([.. parts]);
        };
        session.Send(big);
        Assert.Equal(big, await echo.Task.WaitAsync(Timeout));

        peer.close();
    }

    [Fact]
    public async Task Anonymous_peer_is_refused_when_identity_is_required()
    {
        var listener = Start(allowAnonymous: false);
        var (peer, _, response) = await ConnectAsync(listener.LocalEndPoint, null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        peer.close();
    }

    [Fact]
    public void Server_identity_signs_the_answer_fingerprint()
    {
        const string answer = "v=0\r\no=- 1 2 IN IP4 127.0.0.1\r\ns=-\r\nt=0 0\r\nm=application 9 UDP/DTLS/SCTP webrtc-datachannel\r\na=fingerprint:sha-256 AB:CD\r\n";
        var signed = SdpIdentity.SignAnswer(answer, _serverKey);

        using var key = SdpIdentity.VerifyOffer(signed, token =>
        {
            var jwt = Jwt.Parse(token);
            Assert.Equal("ES384", jwt.HeaderString("alg"));
            return LoginRequest.ParsePublicKey(jwt.Payload.GetProperty("cpk"));
        });
        Assert.Equal(_serverKey.ExportSubjectPublicKeyInfo(), key!.ExportSubjectPublicKeyInfo());
        Assert.True(signed.IndexOf("a=identity:", StringComparison.Ordinal) < signed.IndexOf("m=", StringComparison.Ordinal));
    }
}
