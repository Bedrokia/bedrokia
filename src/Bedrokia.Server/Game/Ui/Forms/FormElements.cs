using System.Text.Json;

namespace Bedrokia.Server.Game.Ui.Forms;

public sealed class LabelElement(string text) : IFormElement
{
    public string ElementType => "label";
    public string Text { get; } = text;

    public void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "label");
        writer.WriteString("text", Text);
        writer.WriteEndObject();
    }

    public bool ValidateAndParse(JsonElement element, out object? parsedValue)
    {
        parsedValue = null;
        return true;
    }
}

public sealed class HeaderElement(string text) : IFormElement
{
    public string ElementType => "header";
    public string Text { get; } = text;

    public void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "header");
        writer.WriteString("text", Text);
        writer.WriteEndObject();
    }

    public bool ValidateAndParse(JsonElement element, out object? parsedValue)
    {
        parsedValue = null;
        return true;
    }
}

public sealed class DividerElement : IFormElement
{
    public string ElementType => "divider";

    public void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "divider");
        writer.WriteEndObject();
    }

    public bool ValidateAndParse(JsonElement element, out object? parsedValue)
    {
        parsedValue = null;
        return true;
    }
}

public sealed class InputElement(string text, string placeholder = "", string defaultValue = "", string? tooltip = null, int maxLength = 256) : IFormElement
{
    public string ElementType => "input";
    public string Text { get; } = text;
    public string Placeholder { get; } = placeholder;
    public string DefaultValue { get; } = defaultValue;
    public string? Tooltip { get; } = tooltip;
    public int MaxLength { get; } = maxLength;

    public void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "input");
        writer.WriteString("text", Text);
        if (!string.IsNullOrEmpty(Placeholder))
            writer.WriteString("placeholder", Placeholder);
        if (!string.IsNullOrEmpty(DefaultValue))
            writer.WriteString("default", DefaultValue);
        if (!string.IsNullOrEmpty(Tooltip))
            writer.WriteString("tooltip", Tooltip);
        writer.WriteEndObject();
    }

    public bool ValidateAndParse(JsonElement element, out object? parsedValue)
    {
        parsedValue = null;
        if (element.ValueKind != JsonValueKind.String)
            return false;

        var raw = element.GetString() ?? string.Empty;
        if (raw.Length > MaxLength)
            return false;

        parsedValue = FormSecurity.CleanString(raw, MaxLength);
        return true;
    }
}

public sealed class ToggleElement(string text, bool defaultValue = false, string? tooltip = null) : IFormElement
{
    public string ElementType => "toggle";
    public string Text { get; } = text;
    public bool DefaultValue { get; } = defaultValue;
    public string? Tooltip { get; } = tooltip;

    public void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "toggle");
        writer.WriteString("text", Text);
        writer.WriteBoolean("default", DefaultValue);
        if (!string.IsNullOrEmpty(Tooltip))
            writer.WriteString("tooltip", Tooltip);
        writer.WriteEndObject();
    }

    public bool ValidateAndParse(JsonElement element, out object? parsedValue)
    {
        parsedValue = null;
        if (element.ValueKind != JsonValueKind.True && element.ValueKind != JsonValueKind.False)
            return false;

        parsedValue = element.GetBoolean();
        return true;
    }
}

public sealed class SliderElement(string text, double min, double max, double step = 1.0, double defaultValue = 0.0, string? tooltip = null) : IFormElement
{
    public string ElementType => "slider";
    public string Text { get; } = text;
    public double Min { get; } = min;
    public double Max { get; } = max;
    public double Step { get; } = step > 0 ? step : 1.0;
    public double DefaultValue { get; } = defaultValue;
    public string? Tooltip { get; } = tooltip;

    public void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "slider");
        writer.WriteString("text", Text);
        writer.WriteNumber("min", Min);
        writer.WriteNumber("max", Max);
        writer.WriteNumber("step", Step);
        writer.WriteNumber("default", DefaultValue);
        if (!string.IsNullOrEmpty(Tooltip))
            writer.WriteString("tooltip", Tooltip);
        writer.WriteEndObject();
    }

    public bool ValidateAndParse(JsonElement element, out object? parsedValue)
    {
        parsedValue = null;
        if (element.ValueKind != JsonValueKind.Number)
            return false;

        if (!element.TryGetDouble(out var val))
            return false;

        if (double.IsNaN(val) || double.IsInfinity(val))
            return false;

        if (val < Min - 0.0001 || val > Max + 0.0001)
            return false;

        parsedValue = Math.Clamp(val, Min, Max);
        return true;
    }
}

public sealed class StepSliderElement(string text, IReadOnlyList<string> options, int defaultIndex = 0, string? tooltip = null) : IFormElement
{
    public string ElementType => "step_slider";
    public string Text { get; } = text;
    public IReadOnlyList<string> Options { get; } = options;
    public int DefaultIndex { get; } = defaultIndex;
    public string? Tooltip { get; } = tooltip;

    public void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "step_slider");
        writer.WriteString("text", Text);
        writer.WriteStartArray("steps");
        foreach (var opt in Options)
            writer.WriteStringValue(opt);
        writer.WriteEndArray();
        writer.WriteNumber("default", DefaultIndex);
        if (!string.IsNullOrEmpty(Tooltip))
            writer.WriteString("tooltip", Tooltip);
        writer.WriteEndObject();
    }

    public bool ValidateAndParse(JsonElement element, out object? parsedValue)
    {
        parsedValue = null;
        if (element.ValueKind != JsonValueKind.Number)
            return false;

        if (!element.TryGetInt32(out var idx))
            return false;

        if (idx < 0 || idx >= Options.Count)
            return false;

        parsedValue = idx;
        return true;
    }
}

public sealed class DropdownElement(string text, IReadOnlyList<string> options, int defaultIndex = 0, string? tooltip = null) : IFormElement
{
    public string ElementType => "dropdown";
    public string Text { get; } = text;
    public IReadOnlyList<string> Options { get; } = options;
    public int DefaultIndex { get; } = defaultIndex;
    public string? Tooltip { get; } = tooltip;

    public void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "dropdown");
        writer.WriteString("text", Text);
        writer.WriteStartArray("options");
        foreach (var opt in Options)
            writer.WriteStringValue(opt);
        writer.WriteEndArray();
        writer.WriteNumber("default", DefaultIndex);
        if (!string.IsNullOrEmpty(Tooltip))
            writer.WriteString("tooltip", Tooltip);
        writer.WriteEndObject();
    }

    public bool ValidateAndParse(JsonElement element, out object? parsedValue)
    {
        parsedValue = null;
        if (element.ValueKind != JsonValueKind.Number)
            return false;

        if (!element.TryGetInt32(out var idx))
            return false;

        if (idx < 0 || idx >= Options.Count)
            return false;

        parsedValue = idx;
        return true;
    }
}
