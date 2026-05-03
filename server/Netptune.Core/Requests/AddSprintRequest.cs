namespace Netptune.Core.Requests;

public record AddSprintRequest
{
    public string Name { get; init; } = null!;

    public string? Goal { get; init; }

    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public int ProjectId { get; init; }
}
