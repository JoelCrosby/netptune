using Netptune.Core.Colors;

namespace Netptune.Core.Meta;

public class ProjectMeta
{
    private string? _color;

    public string? Color
    {
        get => _color;
        set => _color = NamedColors.Normalize(value);
    }
}
