using Netptune.Core.Enums;

namespace Netptune.Api.Requests;

public sealed record PublicUpdatePageRequest
{
    public PageStatus? Status { get; init; }

    public List<string?>? Tags { get; init; }
}
