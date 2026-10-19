using System.Collections.Concurrent;
using System.Globalization;
using Bedrokia.Server.Game.Ui.Forms;

namespace Bedrokia.Server.Game.Ui.Ddui;

public sealed record DataStorePropertyDefinition(
    string Name,
    Type PropertyType,
    bool ClientWritable,
    double? MinValue = null,
    double? MaxValue = null,
    int? MaxLength = null,
    HashSet<string>? Whitelist = null
);

/// <summary>
/// Server-authoritative data store for Data-Driven UI (DDUI).
/// Validates and synchronizes reactive state with Bedrock clients.
/// </summary>
public sealed class DduiDataStore(string name)
{
    private readonly ConcurrentDictionary<string, DataStorePropertyDefinition> _schema = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, object> _values = new(StringComparer.OrdinalIgnoreCase);

    public string Name { get; } = name;
    public IReadOnlyDictionary<string, object> Values => _values;

    public DduiDataStore DefineProperty(
        string propertyName,
        Type propertyType,
        bool clientWritable = false,
        double? min = null,
        double? max = null,
        int? maxLength = null,
        IEnumerable<string>? whitelist = null)
    {
        var wl = whitelist is not null ? new HashSet<string>(whitelist, StringComparer.OrdinalIgnoreCase) : null;
        _schema[propertyName] = new DataStorePropertyDefinition(propertyName, propertyType, clientWritable, min, max, maxLength, wl);
        return this;
    }

    public void Set(string propertyName, object value) =>
        _values[propertyName] = value;

    public T? Get<T>(string propertyName)
    {
        if (_values.TryGetValue(propertyName, out var val) && val is T typed)
            return typed;
        return default;
    }

    /// <summary>
    /// Attempts to update a property from a client-sent packet.
    /// Strictly verifies schema, ClientWritable flag, types, ranges and whitelist values.
    /// </summary>
    public bool TrySetClientValue(string propertyName, string rawValue)
    {
        if (!_schema.TryGetValue(propertyName, out var def))
            return false;

        if (!def.ClientWritable)
            return false;

        if (def.PropertyType == typeof(bool))
        {
            if (string.Equals(rawValue, "true", StringComparison.OrdinalIgnoreCase))
            {
                _values[propertyName] = true;
                return true;
            }
            if (string.Equals(rawValue, "false", StringComparison.OrdinalIgnoreCase))
            {
                _values[propertyName] = false;
                return true;
            }
            return false;
        }

        if (def.PropertyType == typeof(double) || def.PropertyType == typeof(float))
        {
            if (!double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
                return false;

            if (double.IsNaN(num) || double.IsInfinity(num))
                return false;

            if (def.MinValue.HasValue && num < def.MinValue.Value)
                return false;

            if (def.MaxValue.HasValue && num > def.MaxValue.Value)
                return false;

            _values[propertyName] = num;
            return true;
        }

        if (def.PropertyType == typeof(string))
        {
            if (def.MaxLength.HasValue && rawValue.Length > def.MaxLength.Value)
                return false;

            var cleaned = FormSecurity.CleanString(rawValue, def.MaxLength ?? FormSecurity.DefaultMaxStringLength);
            if (def.Whitelist is not null && !def.Whitelist.Contains(cleaned))
                return false;

            _values[propertyName] = cleaned;
            return true;
        }

        return false;
    }
}
