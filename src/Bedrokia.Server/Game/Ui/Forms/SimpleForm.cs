using System.Buffers;
using System.Text.Json;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Game.Ui.Forms;

/// <summary>
/// A SimpleForm ("form") containing a title, optional content/body text, and a list of buttons.
/// </summary>
public sealed class SimpleForm(string title, string content = "") : IForm
{
    private readonly List<FormButton> _buttons = [];
    private Action<BedrockConnection, int, FormButton>? _onSubmit;
    private Action<BedrockConnection, FormCancelReason>? _onClose;

    public uint Id { get; set; }
    public string Title { get; set; } = title;
    public string ContentText { get; set; } = content;
    public long SentTick { get; set; }
    public IReadOnlyList<FormButton> Buttons => _buttons;

    public SimpleForm Content(string content)
    {
        ContentText = content;
        return this;
    }

    public SimpleForm Button(string text, Action<BedrockConnection>? onClick = null)
    {
        _buttons.Add(new FormButton(text, null, onClick));
        return this;
    }

    public SimpleForm Button(string text, string imageData, FormButtonImageType imageType = FormButtonImageType.Path, Action<BedrockConnection>? onClick = null)
    {
        _buttons.Add(new FormButton(text, new FormButtonImage(imageData, imageType), onClick));
        return this;
    }

    public SimpleForm OnSubmit(Action<BedrockConnection, int, FormButton> onSubmit)
    {
        _onSubmit = onSubmit;
        return this;
    }

    public SimpleForm OnClose(Action<BedrockConnection, FormCancelReason> onClose)
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
            writer.WriteString("type", "form");
            writer.WriteString("title", Title);
            writer.WriteString("content", ContentText ?? ""); // the client refuses a form without "content"

            writer.WriteStartArray("buttons");
            foreach (var btn in _buttons)
            {
                writer.WriteStartObject();
                writer.WriteString("text", btn.Text);
                if (btn.Image is { } img)
                {
                    writer.WriteStartObject("image");
                    writer.WriteString("type", img.Type == FormButtonImageType.Path ? "path" : "url");
                    writer.WriteString("data", img.Data);
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return buffer.WrittenSpan.ToArray();
    }

    public bool HandleResponse(BedrockConnection player, string responseJson)
    {
        var trimmed = responseJson.Trim();
        if (!int.TryParse(trimmed, out int buttonIndex))
            return false;

        if (buttonIndex < 0 || buttonIndex >= _buttons.Count)
            return false;

        var button = _buttons[buttonIndex];
        button.OnClick?.Invoke(player);
        _onSubmit?.Invoke(player, buttonIndex, button);
        return true;
    }

    public void HandleClose(BedrockConnection player, FormCancelReason reason) =>
        _onClose?.Invoke(player, reason);
}
