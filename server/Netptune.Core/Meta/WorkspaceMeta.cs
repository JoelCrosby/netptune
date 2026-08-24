using Netptune.Core.Colors;
using Netptune.Core.Utilities;

namespace Netptune.Core.Meta;

public class WorkspaceMeta
{
    private string? _color;
    private string _timeZone = TimeZones.Default;

    public string? Color
    {
        get => _color;
        set => _color = NamedColors.Normalize(value);
    }

    public string TimeZone
    {
        get => _timeZone;
        set => _timeZone = TimeZones.IsValid(value) ? value : TimeZones.Default;
    }

    public string? LogoFileId { get; set; }
}
