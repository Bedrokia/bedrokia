using Bedrokia.Server.Network;

namespace Bedrokia.Server.Game.Ui.Forms;

public enum FormButtonImageType
{
    Path,
    Url
}

public sealed record FormButtonImage(string Data, FormButtonImageType Type = FormButtonImageType.Path);

/// <summary>
/// A clickable button in a SimpleForm.
/// </summary>
public sealed class FormButton(string text, FormButtonImage? image = null, Action<BedrockConnection>? onClick = null)
{
    public string Text { get; set; } = text;
    public FormButtonImage? Image { get; set; } = image;
    public Action<BedrockConnection>? OnClick { get; set; } = onClick;
}
