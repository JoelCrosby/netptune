namespace Netptune.Core.Models.ProjectTasks;

public sealed record BoardGroupTaskTarget(
    int Id,
    string Name,
    double MaxSortOrder,
    int WorkspaceId = 0);
