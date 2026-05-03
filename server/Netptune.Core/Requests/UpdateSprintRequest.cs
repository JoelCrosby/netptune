using Netptune.Core.Enums;

namespace Netptune.Core.Requests;

public record UpdateSprintRequest
{
    public int Id { get; init; }

    public string? Name { get; init; }

    public string? Goal { get; init; }

    public DateTime? StartDate { get; init; }

    public DateTime? EndDate { get; init; }

    public SprintStatus? Status { get; init; }
}
