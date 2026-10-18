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

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bedrokia.Protocol;
using Bedrokia.Protocol.Login;
using Bedrokia.Server.Game;

namespace Bedrokia.Tests;

public class ProtocolTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(300, 2)]
    [InlineData(int.MinValue, 5)]
    public void Varint32_round_trips(int value, int expectedLength)
    {
        var bytes = new BedrockWriter().VarInt32(value).ToArray();
        Assert.Equal(expectedLength, bytes.Length);
        Assert.Equal(value, new BedrockReader(bytes).VarInt32());
    }

    [Fact]
    public void Batch_round_trips_with_compression_and_encryption()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var server = new BatchCodec();
        var client = new BatchCodec();
        foreach (var codec in new[] { server, client })
        {
            codec.EnableCompression(256);
            codec.EnableEncryption(key);
        }
        byte[] small = [0x09, 1, 2, 3];
        var large = new byte[5000]; // compressed

        for (var i = 0; i < 3; i++) // counters must stay in sync across batches
        {
            var decoded = client.Decode(server.Encode([small, large]));
            Assert.Equal(2, decoded.Count);
            Assert.Equal(small, decoded[0]);
            Assert.Equal(large, decoded[1]);
        }
    }

    [Fact]
    public void Tampered_encrypted_batch_is_rejected()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var server = new BatchCodec();
        var client = new BatchCodec();
        server.EnableEncryption(key);
        client.EnableEncryption(key);
        var batch = server.Encode([[1, 2, 3]]);
        batch[2] ^= 1;
        Assert.Throws<InvalidDataException>(() => client.Decode(batch));
    }

    [Fact]
    public void Uncompressed_batch_before_network_settings()
    {
        var batch = new BatchCodec().Encode([[0xC1, 0x01, 0, 0, 0x08, 0x91]]);
        Assert.Equal(new byte[] { 0xFE, 6, 0xC1, 0x01, 0, 0, 0x08, 0x91 }, batch);
    }

    [Fact]
    public void Block_hashes_are_stable_and_unique()
    {
        // FNV-1a of {name: "minecraft:air", states: {}}; grass, dirt and bedrock rendered correctly on a real 26.52 client.
        Assert.Equal(-604749536, (int)Blocks.Air.NetworkId);
        var ids = Enumerable.Range(0, Blocks.Count).Select(i => Blocks.ById((ushort)i).NetworkId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.Same(Blocks.OakLog, Blocks.ByKey("minecraft:oak_log[pillar_axis=y]"));
    }

    [Fact]
    public void Uuid_from_xuid_is_version_3()
    {
        var uuid = LoginRequest.UuidFromXuid("2535412345678901").ToString();
        Assert.Equal('3', uuid[14]);
        Assert.Contains(uuid[19], "89ab");
        Assert.Equal(uuid, LoginRequest.UuidFromXuid("2535412345678901").ToString());
    }

    // --- login verification ---

    internal static byte[] BuildLoginRequest(ECDsa clientKey, string token)
    {
        var chain = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { AuthenticationType = 0, Certificate = "{\"chain\":[]}", Token = token }));
        var x5u = Convert.ToBase64String(clientKey.ExportSubjectPublicKeyInfo());
        var clientData = Encoding.UTF8.GetBytes(Jwt.SignEs384(clientKey,
            new Dictionary<string, string> { ["alg"] = "ES384", ["x5u"] = x5u },
            new Dictionary<string, object> { ["ServerAddress"] = "127.0.0.1:19132", ["GameVersion"] = ProtocolInfo.GameVersion }));
        var writer = new BedrockWriter();
        writer.Int32(chain.Length).Bytes(chain).Int32(clientData.Length).Bytes(clientData);
        return writer.ToArray();
    }

    internal static string OfflineToken(ECDsa clientKey, string name) => Jwt.SignEs384(clientKey,
        new Dictionary<string, string> { ["alg"] = "ES384" },
        new Dictionary<string, object> { ["xname"] = name, ["xid"] = "", ["cpk"] = Convert.ToBase64String(clientKey.ExportSubjectPublicKeyInfo()) });

    private static (string Token, AuthKeys.Snapshot Keys) OnlineToken(ECDsa clientKey, Action<Dictionary<string, object>>? tweak = null)
    {
        using var rsa = RSA.Create(2048);
        var keys = new AuthKeys.Snapshot("https://auth.example/", new Dictionary<string, RSA> { ["k1"] = RSA.Create(rsa.ExportParameters(false)) });
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var claims = new Dictionary<string, object>
        {
            ["iss"] = "https://auth.example/",
            ["aud"] = AuthKeys.Audience,
            ["exp"] = now + 3600,
            ["nbf"] = now - 10,
            ["xid"] = "2535412345678901",
            ["xname"] = "RealPlayer",
            ["cpk"] = Convert.ToBase64String(clientKey.ExportSubjectPublicKeyInfo()),
        };
        tweak?.Invoke(claims);
        var signed = Jwt.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", kid = "k1" }))
            + "." + Jwt.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(claims));
        var signature = rsa.SignData(Encoding.ASCII.GetBytes(signed), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return (signed + "." + Jwt.Base64UrlEncode(signature), keys);
    }

    [Fact]
    public void Offline_login_reads_the_identity()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        var identity = LoginRequest.Verify(BuildLoginRequest(key, OfflineToken(key, "Steve")), null, DateTimeOffset.UtcNow);

        Assert.Equal("Steve", identity.Name);
        Assert.False(identity.Authenticated);
    }

    [Fact]
    public void Online_login_accepts_a_token_signed_by_the_service()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        var (token, keys) = OnlineToken(key);
        var identity = LoginRequest.Verify(BuildLoginRequest(key, token), keys, DateTimeOffset.UtcNow);

        Assert.True(identity.Authenticated);
        Assert.Equal("RealPlayer", identity.Name);
        Assert.Equal("2535412345678901", identity.Xuid);
        Assert.Equal(LoginRequest.UuidFromXuid("2535412345678901"), identity.Uuid);
    }

    [Fact]
    public void Online_login_rejects_self_signed_tokens()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        var (_, keys) = OnlineToken(key);
        Assert.Throws<LoginException>(() => LoginRequest.Verify(BuildLoginRequest(key, OfflineToken(key, "Hacker")), keys, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData("iss")]
    [InlineData("aud")]
    [InlineData("exp")]
    public void Online_login_rejects_bad_claims(string claim)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        var (token, keys) = OnlineToken(key, c => c[claim] = claim == "exp" ? DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds() : "https://evil.example/");
        Assert.Throws<LoginException>(() => LoginRequest.Verify(BuildLoginRequest(key, token), keys, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Client_data_signed_with_another_key_is_rejected()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        using var other = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        var request = BuildLoginRequest(other, OfflineToken(key, "Steve")); // token vouches for key, data signed by other
        Assert.Throws<LoginException>(() => LoginRequest.Verify(request, null, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F, 1 })]
    [InlineData(new byte[] { 2, 0, 0, 0, (byte)'{', (byte)'}' })]
    public void Malformed_login_requests_are_rejected(byte[] request)
    {
        Assert.Throws<LoginException>(() => LoginRequest.Verify(request, null, DateTimeOffset.UtcNow));
    }

}
