using System.Text.Json;

namespace Netptune.Core.SourceControl;

public static class SourceControlCapabilityNames
{
    public static List<string> From(SourceControlCapabilities capabilities)
    {
        return Enum.GetValues<SourceControlCapabilities>()
            .Where(capability => capability != SourceControlCapabilities.None && capabilities.HasFlag(capability))
            .Select(capability => JsonNamingPolicy.CamelCase.ConvertName(capability.ToString()))
            .ToList();
    }
}
