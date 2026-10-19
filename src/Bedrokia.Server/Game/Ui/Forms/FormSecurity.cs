using System.Text;
using System.Text.Json;

namespace Bedrokia.Server.Game.Ui.Forms;

/// <summary>
/// Security validations and Anti-DoS guards for incoming client form payloads (Dragonfly model).
/// </summary>
public static class FormSecurity
{
    public const int MaxPayloadBytes = 16 * 1024; // 16 KB
    public const int MaxJsonDepth = 2;
    public const int DefaultMaxStringLength = 256;

    /// <summary>
    /// Validates that the payload does not exceed the maximum allowed payload size.
    /// </summary>
    public static bool ValidatePayloadSize(string? payload, int maxBytes = MaxPayloadBytes)
    {
        if (payload is null)
            return true;
        return Encoding.UTF8.GetByteCount(payload) <= maxBytes;
    }

    /// <summary>
    /// Validates that JSON does not exceed the maximum allowed nesting depth (Dragonfly model, depth <= 2).
    /// </summary>
    public static bool ValidateJsonDepth(string json, int maxDepth = MaxJsonDepth)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            var reader = new Utf8JsonReader(bytes, new JsonReaderOptions { MaxDepth = maxDepth });
            while (reader.Read())
            {
            }
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Strips control characters (excluding standard whitespace) to prevent terminal and UI injection.
    /// </summary>
    public static string CleanString(string input, int maxLength = DefaultMaxStringLength)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sb = new StringBuilder(Math.Min(input.Length, maxLength));
        foreach (char c in input)
        {
            if (sb.Length >= maxLength)
                break;
            // Disallow non-whitespace control characters
            if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t')
                continue;
            sb.Append(c);
        }
        return sb.ToString();
    }
}
