using Netptune.Core.Enums;

namespace Netptune.Core.Models.Sprints;

public sealed record SprintTaskAssignmentTarget(
    int Id,
    SprintStatus Status,
    int WorkspaceId,
    int ProjectId);
