using System.Buffers;
using System.Text.Json;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Game.Ui.Forms;

/// <summary>
/// A ModalForm ("modal") with two choices (e.g. Yes/No, Accept/Decline) returning a strict boolean.
/// </summary>
public sealed class ModalForm(string title, string content = "") : IForm
{
    private Action<BedrockConnection, bool>? _onSubmit;
    private Action<BedrockConnection, FormCancelReason>? _onClose;

    public uint Id { get; set; }
    public string Title { get; set; } = title;
    public string ContentText { get; set; } = content;
    public string Button1Text { get; set; } = "gui.yes";
    public string Button2Text { get; set; } = "gui.no";
    public long SentTick { get; set; }

    public ModalForm Content(string content)
    {
        ContentText = content;
        return this;
    }

    public ModalForm ConfirmText(string text)
    {
        Button1Text = text;
        return this;
    }

    public ModalForm CancelText(string text)
    {
        Button2Text = text;
        return this;
    }

    public ModalForm OnSubmit(Action<BedrockConnection, bool> onSubmit)
    {
        _onSubmit = onSubmit;
        return this;
    }

    public ModalForm OnClose(Action<BedrockConnection, FormCancelReason> onClose)
    {
        _onClose = onClose;
        return this;
    }

    public byte[] SerializeJson()
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "modal");
            writer.WriteString("title", Title);
            writer.WriteString("content", ContentText ?? ""); // the client refuses a form without "content"
            writer.WriteString("button1", Button1Text);
            writer.WriteString("button2", Button2Text);
            writer.WriteEndObject();
        }
        return buffer.WrittenSpan.ToArray();
    }

    public bool HandleResponse(BedrockConnection player, string responseJson)
    {
        var trimmed = responseJson.Trim();
        if (string.Equals(trimmed, "true", StringComparison.Ordinal))
        {
            _onSubmit?.Invoke(player, true);
            return true;
        }

        if (string.Equals(trimmed, "false", StringComparison.Ordinal))
        {
            _onSubmit?.Invoke(player, false);
            return true;
        }

        return false;
    }

    public void HandleClose(BedrockConnection player, FormCancelReason reason) =>
        _onClose?.Invoke(player, reason);
}
