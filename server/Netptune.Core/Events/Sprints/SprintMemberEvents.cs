using Netptune.Core.Entities;
using Netptune.Core.Enums;

namespace Netptune.Core.Events.Sprints;

public sealed record SprintScope(int WorkspaceId, int SprintId, int ProjectId);

public sealed record SprintMember
{
    public required int TaskId { get; init; }

    public int? StatusId { get; init; }

    public string? StatusCategory { get; init; }

    public string? EstimateType { get; init; }

    public decimal? EstimateValue { get; init; }
}

public static class SprintMemberChanges
{
    public const string Added = "added";

    public const string Committed = "committed";

    public const string Removed = "removed";
}

public static class SprintMemberEvents
{
    // Both references are load-bearing for reporting: velocity reads the member, and the project
    // scope is what lets a report filter sprint membership without walking back through the task.
    public static EventWriteRequest<ScopeMemberChangedPayload> Changed(
        SprintScope scope,
        SprintMember member,
        string change,
        DateTime? occurredAt = null)
    {
        return new EventWriteRequest<ScopeMemberChangedPayload>
        {
            WorkspaceId = scope.WorkspaceId,
            EventKey = EventKeys.ScopeMemberChanged,
            SubjectType = EventEntityTypes.From(EntityType.Sprint),
            SubjectId = scope.SprintId.ToString(),
            OccurredAt = occurredAt,
            Payload = new ScopeMemberChangedPayload
            {
                Change = change,
                MemberType = EventEntityTypes.From(EntityType.Task),
                MemberId = member.TaskId.ToString(),
                EstimateType = member.EstimateType,
                EstimateValue = member.EstimateValue,
                StatusId = member.StatusId,
                StatusCategory = member.StatusCategory,
            },
            References =
            [
                new EventReferenceInput
                {
                    Role = EventReferenceRoles.Member,
                    EntityType = EventEntityTypes.From(EntityType.Task),
                    EntityId = member.TaskId.ToString(),
                },
                new EventReferenceInput
                {
                    Role = EventReferenceRoles.Scope,
                    EntityType = EventEntityTypes.From(EntityType.Project),
                    EntityId = scope.ProjectId.ToString(),
                },
            ],
        };
    }
}
