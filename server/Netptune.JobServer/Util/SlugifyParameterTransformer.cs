
using Microsoft.AspNetCore.Routing;

namespace Netptune.JobServer.Util;

public class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        var route = value?.ToString();

        return route?.ToKebabCase();
    }
}
