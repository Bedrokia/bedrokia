namespace Bedrokia.Server.Game.Ui.Forms;

/// <summary>
/// Strongly-typed, validated result of a CustomForm submission.
/// </summary>
public sealed class CustomFormResult(IReadOnlyList<IFormElement> elements, object?[] values)
{
    private readonly IReadOnlyList<IFormElement> _elements = elements;
    private readonly object?[] _values = values;

    public int Count => _values.Length;

    public string GetString(int index)
    {
        if (index >= 0 && index < _values.Length && _values[index] is string s)
            return s;
        return string.Empty;
    }

    public bool GetBool(int index)
    {
        if (index >= 0 && index < _values.Length && _values[index] is bool b)
            return b;
        return false;
    }

    public double GetDouble(int index)
    {
        if (index >= 0 && index < _values.Length)
        {
            if (_values[index] is double d) return d;
            if (_values[index] is int i) return i;
            if (_values[index] is float f) return f;
        }
        return 0.0;
    }

    public float GetFloat(int index) => (float)GetDouble(index);

    public int GetInt(int index)
    {
        if (index >= 0 && index < _values.Length)
        {
            if (_values[index] is int i) return i;
            if (_values[index] is double d) return (int)Math.Round(d);
        }
        return 0;
    }

    public string? GetSelectedOption(int index)
    {
        int selectedIndex = GetInt(index);
        if (index >= 0 && index < _elements.Count)
        {
            if (_elements[index] is DropdownElement dd && selectedIndex >= 0 && selectedIndex < dd.Options.Count)
                return dd.Options[selectedIndex];
            if (_elements[index] is StepSliderElement ss && selectedIndex >= 0 && selectedIndex < ss.Options.Count)
                return ss.Options[selectedIndex];
        }
        return null;
    }

    public T? Get<T>(int index)
    {
        if (index >= 0 && index < _values.Length && _values[index] is T typed)
            return typed;
        return default;
    }
}
