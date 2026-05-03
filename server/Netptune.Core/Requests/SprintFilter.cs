using Netptune.Core.Enums;

namespace Netptune.Core.Requests;

public record SprintFilter
{
    public int? ProjectId { get; init; }

    public SprintStatus? Status { get; init; }
}
