using System.Buffers;
using System.Text.Json;
using Bedrokia.Server.Network;

namespace Bedrokia.Server.Game.Ui.Forms;

/// <summary>
/// A CustomForm ("custom_form") allowing interactive inputs, toggles, sliders, dropdowns and labels.
/// </summary>
public sealed class CustomForm(string title) : IForm
{
    private readonly List<IFormElement> _elements = [];
    private Action<BedrockConnection, CustomFormResult>? _onSubmit;
    private Action<BedrockConnection, FormCancelReason>? _onClose;

    public uint Id { get; set; }
    public string Title { get; set; } = title;
    public long SentTick { get; set; }
    public IReadOnlyList<IFormElement> Elements => _elements;

    public CustomForm Label(string text)
    {
        _elements.Add(new LabelElement(text));
        return this;
    }

    public CustomForm Header(string text)
    {
        _elements.Add(new HeaderElement(text));
        return this;
    }

    public CustomForm Divider()
    {
        _elements.Add(new DividerElement());
        return this;
    }

    public CustomForm Input(string text, string placeholder = "", string defaultValue = "", string? tooltip = null, int maxLength = 256)
    {
        _elements.Add(new InputElement(text, placeholder, defaultValue, tooltip, maxLength));
        return this;
    }

    public CustomForm Toggle(string text, bool defaultValue = false, string? tooltip = null)
    {
        _elements.Add(new ToggleElement(text, defaultValue, tooltip));
        return this;
    }

    public CustomForm Slider(string text, double min, double max, double step = 1.0, double defaultValue = 0.0, string? tooltip = null)
    {
        _elements.Add(new SliderElement(text, min, max, step, defaultValue, tooltip));
        return this;
    }

    public CustomForm StepSlider(string text, IReadOnlyList<string> options, int defaultIndex = 0, string? tooltip = null)
    {
        _elements.Add(new StepSliderElement(text, options, defaultIndex, tooltip));
        return this;
    }

    public CustomForm Dropdown(string text, IReadOnlyList<string> options, int defaultIndex = 0, string? tooltip = null)
    {
        _elements.Add(new DropdownElement(text, options, defaultIndex, tooltip));
        return this;
    }

    public CustomForm OnSubmit(Action<BedrockConnection, CustomFormResult> onSubmit)
    {
        _onSubmit = onSubmit;
        return this;
    }

    public CustomForm OnClose(Action<BedrockConnection, FormCancelReason> onClose)
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
            writer.WriteString("type", "custom_form");
            writer.WriteString("title", Title);
            writer.WriteStartArray("content");
            foreach (var elem in _elements)
                elem.Serialize(writer);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return buffer.WrittenSpan.ToArray();
    }

    public bool HandleResponse(BedrockConnection player, string responseJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson, new JsonDocumentOptions { MaxDepth = FormSecurity.MaxJsonDepth });
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return false;

            if (doc.RootElement.GetArrayLength() != _elements.Count)
                return false;

            var values = new object?[_elements.Count];
            var index = 0;
            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                if (!_elements[index].ValidateAndParse(elem, out var parsed))
                    return false;

                values[index] = parsed;
                index++;
            }

            var result = new CustomFormResult(_elements, values);
            _onSubmit?.Invoke(player, result);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public void HandleClose(BedrockConnection player, FormCancelReason reason) =>
        _onClose?.Invoke(player, reason);
}
