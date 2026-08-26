using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Relations;
using Netptune.Core.Relations;
using Netptune.Core.Relationships;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.Relations;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Relations;

namespace Netptune.Services.Relations;

public sealed class TaskRelationLinker : ITaskRelationLinker
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;

    public TaskRelationLinker(
        INetptuneUnitOfWork unitOfWork,
        IActivityLogger activity,
        IEventPublisher eventPublisher)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
        EventPublisher = eventPublisher;
    }

    public async Task<TaskRelationPlan> Plan(TaskRelationPlanRequest request, CancellationToken cancellationToken = default)
    {
        var hasLinks = request.Links.Count > 0;

        if (!hasLinks)
        {
            return new TaskRelationPlan { WorkspaceId = request.WorkspaceId };
        }

        var requests = new List<RequestedRelation>();
        var seen = new HashSet<(int RelationTypeId, string RelatedSystemId)>();

        foreach (var link in request.Links)
        {
            var relatedSystemId = link.RelatedSystemId.Trim();

            if (relatedSystemId.Length == 0)
            {
                return TaskRelationPlan.Failed("Related task keys cannot be empty");
            }

            var isDuplicate = !seen.Add((link.RelationTypeId, relatedSystemId));

            if (isDuplicate)
            {
                return TaskRelationPlan.Failed($"{relatedSystemId} is linked more than once by the same relation.");
            }

            requests.Add(new RequestedRelation(link.RelationTypeId, relatedSystemId, link.TaskIsSource));
        }

        var relationTypes = await UnitOfWork.RelationTypes.GetAllInWorkspace(
            request.WorkspaceId,
            isReadonly: true,
            cancellationToken: cancellationToken);

        var relationTypesById = relationTypes.ToDictionary(relationType => relationType.Id);
        var systemIds = requests.Select(requested => requested.RelatedSystemId).Distinct().ToList();
        var relatedTasks = await UnitOfWork.Tasks.GetTaskViewModels(systemIds, request.WorkspaceKey, cancellationToken);

        var relatedTasksBySystemId = relatedTasks.ToDictionary(
            task => task.SystemId,
            StringComparer.OrdinalIgnoreCase);

        var planned = new List<PlannedTaskRelation>();

        foreach (var requested in requests)
        {
            if (!relationTypesById.TryGetValue(requested.RelationTypeId, out var relationType))
            {
                return TaskRelationPlan.Failed($"Relation type with Id {requested.RelationTypeId} not found");
            }

            if (!relatedTasksBySystemId.TryGetValue(requested.RelatedSystemId, out var relatedTask))
            {
                return TaskRelationPlan.Failed($"Task with key {requested.RelatedSystemId} not found");
            }

            planned.Add(new PlannedTaskRelation(relationType, relatedTask, requested.TaskIsSource));
        }

        var conflict = await FindConflict(planned, cancellationToken);

        if (conflict is not null)
        {
            return TaskRelationPlan.Failed(conflict);
        }

        return new TaskRelationPlan
        {
            WorkspaceId = request.WorkspaceId,
            Relations = planned,
        };
    }

    public async Task<List<LinkedTaskRelation>> Apply(TaskRelationPlan plan, int taskId, CancellationToken cancellationToken = default)
    {
        if (plan.Relations.Count == 0)
        {
            return [];
        }

        var task = await UnitOfWork.Tasks.GetTaskViewModel(taskId, cancellationToken);

        if (task is null)
        {
            return [];
        }

        var links = plan.Relations
            .Select(planned => new PendingRelation
            {
                Relation = BuildRelation(task.Id, plan.WorkspaceId, planned),
                Planned = planned,
            })
            .ToList();
        var relations = links.ConvertAll(link => link.Relation);

        await UnitOfWork.ProjectTaskRelations.AddRangeAsync(relations, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        foreach (var link in links)
        {
            LogForBothTasks(task, link);
        }

        return links.ConvertAll(link => new LinkedTaskRelation(link.Relation, link.Planned.RelationType.Category));
    }

    public async Task Publish(IReadOnlyCollection<LinkedTaskRelation> links, string actorUserId)
    {
        foreach (var link in links)
        {
            await EventPublisher.Dispatch(new TaskRelationChangedMessage
            {
                WorkspaceId = link.Relation.WorkspaceId,
                SourceTaskId = link.Relation.SourceTaskId,
                TargetTaskId = link.Relation.TargetTaskId,
                Category = link.Category,
                Change = TaskRelationChange.Added,
                ActorUserId = actorUserId,
            });
        }
    }

    // The task the links attach to is a fresh node with no links of its own, which leaves two ways a
    // batch can still be refused: a single-source type whose other end is already claimed, and a pair
    // of links that routes a chain of an acyclic type back on itself.
    private async Task<string?> FindConflict(List<PlannedTaskRelation> planned, CancellationToken cancellationToken)
    {
        foreach (var group in planned.GroupBy(relation => relation.RelationType.Id))
        {
            var relationType = group.First().RelationType;
            var outbound = group.Where(relation => relation.TaskIsSource).ToList();
            var inbound = group.Where(relation => !relation.TaskIsSource).ToList();
            var hasSingleSource = RelationTypeRules.HasSingleSource(relationType.Category);

            if (hasSingleSource)
            {
                var conflict = await FindSingleSourceConflict(relationType, outbound, inbound, cancellationToken);

                if (conflict is not null)
                {
                    return conflict;
                }
            }

            var isAcyclic = RelationTypeRules.IsAcyclic(relationType.Category);

            if (!isAcyclic)
            {
                continue;
            }

            var cycle = await FindCycleConflict(relationType, outbound, inbound, cancellationToken);

            if (cycle is not null)
            {
                return cycle;
            }
        }

        return null;
    }

    private async Task<string?> FindSingleSourceConflict(
        RelationType relationType,
        List<PlannedTaskRelation> outbound,
        List<PlannedTaskRelation> inbound,
        CancellationToken cancellationToken)
    {
        var hasCompetingSources = inbound.Count > 1;

        if (hasCompetingSources)
        {
            return $"A task can only have one \"{relationType.Name}\" link.";
        }

        var targetIds = outbound.ConvertAll(relation => relation.Task.Id);
        var claimedTargetIds = await UnitOfWork.ProjectTaskRelations.GetTargetsWithExistingSource(
            relationType.Id,
            targetIds,
            cancellationToken);

        if (claimedTargetIds.Count > 0)
        {
            return $"That task already has a \"{relationType.Name}\" link. A task can only have one.";
        }

        return null;
    }

    // Linking both ends chains every inbound source through the new task to every outbound target, so
    // one walk forward from all the targets answers the question for the whole batch: a cycle exists
    // if any inbound source sits on a path leading out of any outbound target. A task linked both ways
    // at once never reaches here — the same task and relation type twice is refused as a duplicate.
    private async Task<string?> FindCycleConflict(
        RelationType relationType,
        List<PlannedTaskRelation> outbound,
        List<PlannedTaskRelation> inbound,
        CancellationToken cancellationToken)
    {
        var sourceIds = inbound.Select(relation => relation.Task.Id).ToHashSet();
        var targetIds = outbound.ConvertAll(relation => relation.Task.Id);
        var linksBothEnds = sourceIds.Count > 0 && targetIds.Count > 0;

        if (!linksBothEnds)
        {
            return null;
        }

        var reachableTaskIds = await UnitOfWork.ProjectTaskRelations.GetReachableTaskIds(relationType.Id, targetIds, cancellationToken);
        var closesTheLoop = reachableTaskIds.Any(sourceIds.Contains);

        if (closesTheLoop)
        {
            return $"This would create a circular \"{relationType.Name}\" chain.";
        }

        return null;
    }

    private static ProjectTaskRelation BuildRelation(int taskId, int workspaceId, PlannedTaskRelation planned)
    {
        var relationType = planned.RelationType;
        var requested = planned.TaskIsSource
            ? (Source: taskId, Target: planned.Task.Id)
            : (Source: planned.Task.Id, Target: taskId);

        var (source, target) = RelationTypeRules.Orient(relationType.Category, requested.Source, requested.Target);

        return new ProjectTaskRelation
        {
            WorkspaceId = workspaceId,
            RelationTypeId = relationType.Id,
            SourceTaskId = source,
            TargetTaskId = target,
        };
    }

    private void LogForBothTasks(TaskViewModel task, PendingRelation link)
    {
        var relationType = link.Planned.RelationType;
        var relationId = link.Relation.Id;
        var taskIsSource = link.Relation.SourceTaskId == task.Id;
        var taskView = TaskRelationViewModel.BuildView(relationId, relationType, taskIsSource, link.Planned.Task);
        var otherTaskView = TaskRelationViewModel.BuildView(relationId, relationType, !taskIsSource, task);

        Log(task.Id, taskView);
        Log(link.Planned.Task.Id, otherTaskView);
    }

    private void Log(int taskId, TaskRelationViewModel view)
    {
        Activity.LogWith<TaskRelationActivityMeta>(options =>
        {
            options.EntityId = taskId;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.AddRelation;
            options.Meta = TaskRelationActivityMeta.From(view);
        });
    }

    private sealed record RequestedRelation(int RelationTypeId, string RelatedSystemId, bool TaskIsSource);

    private sealed record PendingRelation
    {
        public required ProjectTaskRelation Relation { get; init; }

        public required PlannedTaskRelation Planned { get; init; }
    }
}
