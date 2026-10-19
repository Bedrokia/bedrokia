using System.Text.Json;

namespace Bedrokia.Server.Game.Ui.Forms;

/// <summary>
/// An element within a CustomForm.
/// </summary>
public interface IFormElement
{
    string ElementType { get; }
    void Serialize(Utf8JsonWriter writer);
    bool ValidateAndParse(JsonElement element, out object? parsedValue);
}
