using System.Text;
using Bedrokia.Protocol;
using Bedrokia.Server;
using Bedrokia.Server.Core;
using Bedrokia.Server.Game.Ui.Ddui;
using Bedrokia.Server.Game.Ui.Forms;
using Bedrokia.Server.Network;
using Xunit;

namespace Bedrokia.Tests;

public class FormsAndDduiTests
{
    private static BedrokiaServer CreateTestServer()
    {
        var tempWorlds = TestWorlds.NewDirectory();
        var config = new ServerConfig { WorldsDirectory = tempWorlds, Port = 0, MaxPlayers = 5 };
        var logger = new Logger(LogLevel.Warn, null);
        return new BedrokiaServer(config, logger);
    }

    [Fact]
    public void SimpleForm_serializes_valid_bedrock_json()
    {
        var form = new SimpleForm("Shop")
            .Content("Choose an item:")
            .Button("Diamond Sword", "textures/items/diamond_sword", FormButtonImageType.Path)
            .Button("Apple", "https://example.com/apple.png", FormButtonImageType.Url);

        var json = Encoding.UTF8.GetString(form.SerializeJson());
        Assert.Contains("\"type\":\"form\"", json);
        Assert.Contains("\"title\":\"Shop\"", json);
        Assert.Contains("\"content\":\"Choose an item:\"", json);
        Assert.Contains("\"buttons\":", json);
        Assert.Contains("\"text\":\"Diamond Sword\"", json);
        Assert.Contains("\"type\":\"path\"", json);
    }

    [Fact]
    public void SimpleForm_handles_valid_button_click()
    {
        var clicked = false;
        var form = new SimpleForm("Test")
            .Button("First")
            .Button("Second", onClick: _ => clicked = true);

        // Click on index 1 ("Second")
        var handled = form.HandleResponse(null!, "1");
        Assert.True(handled);
        Assert.True(clicked);
    }

    [Fact]
    public void SimpleForm_rejects_out_of_bounds_or_non_integer()
    {
        var form = new SimpleForm("Test").Button("Only One");

        Assert.False(form.HandleResponse(null!, "5")); // out of bounds
        Assert.False(form.HandleResponse(null!, "-1")); // negative
        Assert.False(form.HandleResponse(null!, "\"invalid\"")); // string
        Assert.False(form.HandleResponse(null!, "true")); // bool
    }

    [Fact]
    public void ModalForm_serializes_and_handles_boolean_response()
    {
        bool? result = null;
        var form = new ModalForm("Confirm", "Are you sure?")
            .ConfirmText("Yes!")
            .CancelText("No!")
            .OnSubmit((_, confirmed) => result = confirmed);

        var json = Encoding.UTF8.GetString(form.SerializeJson());
        Assert.Contains("\"type\":\"modal\"", json);
        Assert.Contains("\"button1\":\"Yes!\"", json);
        Assert.Contains("\"button2\":\"No!\"", json);

        Assert.True(form.HandleResponse(null!, "true"));
        Assert.True(result);

        Assert.True(form.HandleResponse(null!, "false"));
        Assert.False(result);

        Assert.False(form.HandleResponse(null!, "123")); // reject non-bool
        Assert.False(form.HandleResponse(null!, "\"true\"")); // reject string
    }

    [Fact]
    public void CustomForm_serializes_and_parses_elements()
    {
        CustomFormResult? formResult = null;
        var form = new CustomForm("Profile")
            .Label("Welcome to profile settings")
            .Input("Username", "Steve", "Alex", maxLength: 16)
            .Toggle("Notifications", defaultValue: true)
            .Slider("Volume", min: 0.0, max: 100.0, step: 5.0, defaultValue: 50.0)
            .StepSlider("Difficulty", ["Easy", "Normal", "Hard"], defaultIndex: 1)
            .Dropdown("Language", ["English", "Français", "Español"], defaultIndex: 0)
            .OnSubmit((_, r) => formResult = r);

        var json = Encoding.UTF8.GetString(form.SerializeJson());
        Assert.Contains("\"type\":\"custom_form\"", json);
        Assert.Contains("\"type\":\"label\"", json);
        Assert.Contains("\"type\":\"input\"", json);
        Assert.Contains("\"type\":\"toggle\"", json);
        Assert.Contains("\"type\":\"slider\"", json);
        Assert.Contains("\"type\":\"step_slider\"", json);
        Assert.Contains("\"type\":\"dropdown\"", json);

        // Valid client response: [null, "Gamer123", false, 45.0, 2, 1]
        var responseJson = "[null, \"Gamer123\", false, 45.0, 2, 1]";
        var handled = form.HandleResponse(null!, responseJson);
        Assert.True(handled);
        Assert.NotNull(formResult);

        Assert.Equal("Gamer123", formResult.GetString(1));
        Assert.False(formResult.GetBool(2));
        Assert.Equal(45.0, formResult.GetDouble(3));
        Assert.Equal(2, formResult.GetInt(4));
        Assert.Equal("Hard", formResult.GetSelectedOption(4));
        Assert.Equal(1, formResult.GetInt(5));
        Assert.Equal("Français", formResult.GetSelectedOption(5));
    }

    [Fact]
    public void CustomForm_rejects_invalid_elements_and_bounds()
    {
        var form = new CustomForm("Strict")
            .Input("Name", maxLength: 8)
            .Slider("Speed", min: 1.0, max: 10.0, step: 1.0)
            .Dropdown("Choice", ["A", "B"]);

        // Mismatched length (2 instead of 3)
        Assert.False(form.HandleResponse(null!, "[\"Hello\", 5.0]"));

        // Slider out of range (> 10.0)
        Assert.False(form.HandleResponse(null!, "[\"Hello\", 15.0, 0]"));

        // Dropdown out of range (>= 2)
        Assert.False(form.HandleResponse(null!, "[\"Hello\", 5.0, 5]"));

        // Input exceeding maxLength
        Assert.False(form.HandleResponse(null!, "[\"ThisIsWayTooLong\", 5.0, 0]"));
    }

    [Fact]
    public void CustomForm_cleans_illegal_control_characters()
    {
        CustomFormResult? formResult = null;
        var form = new CustomForm("InputTest")
            .Input("Message")
            .OnSubmit((_, r) => formResult = r);

        var responseJson = "[\"Hello\\u0000\\u0007World!\"]";
        Assert.True(form.HandleResponse(null!, responseJson));
        Assert.NotNull(formResult);
        Assert.Equal("HelloWorld!", formResult.GetString(0));
    }

    [Fact]
    public void DduiDataStore_enforces_client_writable_and_type_validation()
    {
        var store = new DduiDataStore("PlayerSettings")
            .DefineProperty("sound_enabled", typeof(bool), clientWritable: true)
            .DefineProperty("music_volume", typeof(double), clientWritable: true, min: 0.0, max: 1.0)
            .DefineProperty("player_role", typeof(string), clientWritable: false) // ReadOnly!
            .DefineProperty("theme", typeof(string), clientWritable: true, whitelist: ["light", "dark"]);

        // Client writable bool
        Assert.True(store.TrySetClientValue("sound_enabled", "true"));
        Assert.True(store.Get<bool>("sound_enabled"));

        // Client writable double with bounds
        Assert.True(store.TrySetClientValue("music_volume", "0.75"));
        Assert.Equal(0.75, store.Get<double>("music_volume"));

        // Out of bounds double (> 1.0)
        Assert.False(store.TrySetClientValue("music_volume", "1.5"));

        // ReadOnly property rejected
        Assert.False(store.TrySetClientValue("player_role", "admin"));

        // Whitelist validation
        Assert.True(store.TrySetClientValue("theme", "dark"));
        Assert.Equal("dark", store.Get<string>("theme"));
        Assert.False(store.TrySetClientValue("theme", "neon")); // rejected
    }

    [Fact]
    public void AntiDoS_validation_detects_oversized_payload_and_excessive_depth()
    {
        // Max 16 KB
        var hugePayload = new string('A', 17 * 1024);
        Assert.False(FormSecurity.ValidatePayloadSize(hugePayload));

        var validPayload = "{\"test\":123}";
        Assert.True(FormSecurity.ValidatePayloadSize(validPayload));

        // Depth <= 2
        var depthOk = "[1, 2, 3]";
        Assert.True(FormSecurity.ValidateJsonDepth(depthOk, maxDepth: 2));

        var depthExcessive = "[[[1]]]";
        Assert.False(FormSecurity.ValidateJsonDepth(depthExcessive, maxDepth: 2));
    }

    [Fact]
    public void DduiSession_rate_limits_flood_attacks()
    {
        var session = new DduiSession();

        // Up to 20 updates on the same tick are allowed
        for (var i = 0; i < DduiSession.MaxUpdatesPerTick; i++)
            Assert.True(session.CanAcceptUpdate(currentTick: 100));

        // 21st update in the same tick is blocked
        Assert.False(session.CanAcceptUpdate(currentTick: 100));

        // Next tick allows updates again
        Assert.True(session.CanAcceptUpdate(currentTick: 101));
    }

    private sealed class TestGameTransport : IGameTransport
    {
        public System.Net.IPEndPoint RemoteEndPoint { get; } = new(System.Net.IPAddress.Loopback, 19132);
        public System.Security.Cryptography.ECDsa? PeerPublicKey => null;
        public List<byte[]> Sent { get; } = [];
        public void Send(byte[] batch) => Sent.Add(batch);
        public void Disconnect(string reason) { }
    }

    [Fact]
    public void BedrockConnection_SendForm_and_AntiReplay()
    {
        var server = CreateTestServer();
        var transport = new TestGameTransport();
        var player = new BedrockConnection(server, transport, 1);

        var submitted = false;
        var form = new ModalForm("Confirm", "Are you sure?")
            .OnSubmit((_, result) => submitted = result);

        var formId = player.SendForm(form);
        Assert.Equal(1, player.ActiveFormCount);
        Assert.NotEmpty(transport.Sent);

        // First response packet from client
        var writer = new BedrockWriter();
        writer.VarUInt32(formId);
        writer.Bool(true); // hasResponse
        writer.String("true");
        writer.Bool(false); // hasCancel
        var packetData = writer.ToArray();

        var reader = new BedrockReader(packetData);
        player.OnModalFormResponse(ref reader);

        Assert.True(submitted);
        Assert.Equal(0, player.ActiveFormCount);

        // Anti-Replay: duplicate response packet arrives (e.g. injected or retransmitted)
        submitted = false;
        var duplicateReader = new BedrockReader(packetData);
        player.OnModalFormResponse(ref duplicateReader);

        // Nothing happened because form was already removed
        Assert.False(submitted);
        Assert.Equal(0, player.ActiveFormCount);
    }

    [Fact]
    public void BedrockConnection_bounded_queue_cancels_oldest_on_overflow()
    {
        var server = CreateTestServer();
        var transport = new TestGameTransport();
        var player = new BedrockConnection(server, transport, 1);

        FormCancelReason? firstCancel = null;
        var form1 = new SimpleForm("Form 1").Button("A").OnClose((_, r) => firstCancel = r);
        var form2 = new SimpleForm("Form 2").Button("B");
        var form3 = new SimpleForm("Form 3").Button("C");
        var form4 = new SimpleForm("Form 4").Button("D");

        player.SendForm(form1);
        player.SendForm(form2);
        player.SendForm(form3);
        Assert.Equal(3, player.ActiveFormCount);

        // 4th form exceeds MaxQueuedForms (3): form1 must be closed with UserBusy
        player.SendForm(form4);
        Assert.Equal(3, player.ActiveFormCount);
        Assert.Equal(FormCancelReason.UserBusy, firstCancel);
    }

    [Fact]
    public void BedrockConnection_OnClosed_cancels_all_forms_with_disconnected()
    {
        var server = CreateTestServer();
        var transport = new TestGameTransport();
        var player = new BedrockConnection(server, transport, 1);

        FormCancelReason? cancelReason = null;
        var form = new SimpleForm("Pending").Button("Ok").OnClose((_, r) => cancelReason = r);

        player.SendForm(form);
        player.DduiSession.OpenScreen("custom_hud");
        Assert.Equal(1, player.ActiveFormCount);
        Assert.NotEmpty(player.DduiSession.OpenScreens);

        player.OnClosed();

        Assert.Equal(0, player.ActiveFormCount);
        Assert.Equal(FormCancelReason.Disconnected, cancelReason);
        Assert.Empty(player.DduiSession.OpenScreens);
    }

    [Fact]
    public void UiPackets_serialization_roundtrip()
    {
        // ModalFormRequest packet
        var request = UiPackets.ModalFormRequest(42, "{\"title\":\"Test\"}");
        Assert.NotEmpty(request);

        // ModalFormResponse parsing
        var writer = new BedrockWriter();
        writer.VarUInt32(42);
        writer.Bool(true);
        writer.String("1");
        writer.Bool(false);

        var reader = new BedrockReader(writer.ToArray());
        var (formId, hasResponse, responseJson, hasCancel, cancelReason) = UiPackets.ReadModalFormResponse(ref reader);
        Assert.Equal(42u, formId);
        Assert.True(hasResponse);
        Assert.Equal("1", responseJson);
        Assert.False(hasCancel);

        // ServerboundDataStore parsing
        var dsWriter = new BedrockWriter();
        dsWriter.String("StoreA");
        dsWriter.String("Prop1");
        dsWriter.String("Val1");

        var dsReader = new BedrockReader(dsWriter.ToArray());
        var (storeName, prop, val) = UiPackets.ReadServerboundDataStore(ref dsReader);
        Assert.Equal("StoreA", storeName);
        Assert.Equal("Prop1", prop);
        Assert.Equal("Val1", val);
    }
}
