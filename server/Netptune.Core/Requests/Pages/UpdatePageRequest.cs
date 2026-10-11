using Netptune.Core.Enums;

namespace Netptune.Core.Requests.Pages;

public sealed record UpdatePageRequest
{
    public PageStatus? Status { get; init; }

    public List<int>? TagIds { get; init; }
}
