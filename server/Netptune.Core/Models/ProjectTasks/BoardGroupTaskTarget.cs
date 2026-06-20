using Netptune.Core.Enums;

namespace Netptune.Core.Models.ProjectTasks;

public sealed record BoardGroupTaskTarget(
    int Id,
    string Name,
    BoardGroupType Type,
    double MaxSortOrder,
    int WorkspaceId = 0);
