using Netptune.Core.Colors;

namespace Netptune.Core.Meta;

public class BoardMeta
{
    private string? _color;

    public string? Color
    {
        get => _color;
        set => _color = NamedColors.Normalize(value);
    }
}
