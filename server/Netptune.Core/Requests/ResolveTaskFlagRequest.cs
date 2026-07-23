using Netptune.Core.Enums;

namespace Netptune.Core.Requests;

public sealed record ResolveTaskFlagRequest
{
    public FlagResolutionType Resolution { get; init; }
}
