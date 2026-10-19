using Bedrokia.Server.Network;

namespace Bedrokia.Server.Game.Ui.Forms;

/// <summary>
/// Common contract for all Bedrock server forms.
/// </summary>
public interface IForm
{
    /// <summary>Unique incrementing form identifier assigned by the server.</summary>
    uint Id { get; set; }

    /// <summary>Title displayed at the top of the form.</summary>
    string Title { get; }

    /// <summary>Server tick when this form was dispatched.</summary>
    long SentTick { get; set; }

    /// <summary>Serializes the form into UTF-8 JSON bytes using Utf8JsonWriter without reflection.</summary>
    byte[] SerializeJson();

    /// <summary>
    /// Validates and parses the client JSON response.
    /// Returns true if validation succeeded and the submission was handled; false if invalid.
    /// </summary>
    bool HandleResponse(BedrockConnection player, string responseJson);

    /// <summary>
    /// Invoked when the form is closed, cancelled, timed out or rejected.
    /// </summary>
    void HandleClose(BedrockConnection player, FormCancelReason reason);
}
