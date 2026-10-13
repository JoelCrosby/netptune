namespace Netptune.Core.ViewModels.ProjectTasks;

public sealed record TaskTextViewModel
{
    public int Id { get; init; }

    public required string SystemId { get; init; }

    public required string Name { get; init; }

    public int? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public string Description { get; init; } = string.Empty;
}
