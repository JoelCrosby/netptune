using System.Runtime.InteropServices;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Relations;
using Netptune.Core.Relationships;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.Entities;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.Services;

namespace Netptune.Import;

// Links are checked in row order, so when two links compete (a second parent, the edge that closes a
// cycle) the earlier one is kept and the later one is skipped rather than failing the import.
public sealed class ImportRelationWriter
{
    private const int ChunkSize = 500;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IImportSessionRepository ImportSessions;

    public ImportRelationWriter(INetptuneUnitOfWork unitOfWork, IImportSessionRepository importSessions)
    {
        UnitOfWork = unitOfWork;
        ImportSessions = importSessions;
    }

    public async Task<int> Write(
        IReadOnlyList<ImportLinks> pending,
        Func<string, int?> resolveTaskId,
        ImportApplyRequest request,
        CancellationToken cancellationToken)
    {
        if (pending.Count == 0)
        {
            return 0;
        }

        var relationTypes = await UnitOfWork.RelationTypes.GetAllInWorkspace(
            request.WorkspaceId,
            cancellationToken: cancellationToken);
        var systemTypes = FindSystemTypes(relationTypes);
        var candidates = Resolve(pending, systemTypes, resolveTaskId);
        var accepted = new List<ProjectTaskRelation>();

        foreach (var group in candidates.GroupBy(candidate => candidate.RelationType.Id))
        {
            var relationType = group.First().RelationType;
            var typeCandidates = group.ToList();
            var graph = await LoadGraph(relationType, typeCandidates, cancellationToken);
            var acceptedCandidates = typeCandidates.Where(graph.TryAdd);
            var relations = acceptedCandidates.Select(candidate => BuildRelation(candidate, request.WorkspaceId));

            accepted.AddRange(relations);
        }

        await Save(accepted, request, cancellationToken);

        return accepted.Count;
    }

    private static Dictionary<RelationCategory, RelationType> FindSystemTypes(IReadOnlyList<RelationType> relationTypes)
    {
        return relationTypes
            .Where(relationType => relationType.IsSystem)
            .GroupBy(relationType => relationType.Category)
            .ToDictionary(group => group.Key, group => group.MinBy(relationType => relationType.SortOrder)!);
    }

    private static List<CandidateRelation> Resolve(
        IReadOnlyList<ImportLinks> pending,
        Dictionary<RelationCategory, RelationType> systemTypes,
        Func<string, int?> resolveTaskId)
    {
        var candidates = new List<CandidateRelation>();

        foreach (var task in pending)
        {
            foreach (var link in task.Links)
            {
                var relationType = systemTypes.GetValueOrDefault(link.Category);
                var otherTaskId = resolveTaskId(link.Reference);

                if (relationType is null || otherTaskId is null || otherTaskId == task.TaskId)
                {
                    continue;
                }

                var (sourceTaskId, targetTaskId) = link.TaskIsSource
                    ? (task.TaskId, otherTaskId.Value)
                    : (otherTaskId.Value, task.TaskId);
                var oriented = RelationTypeRules.Orient(link.Category, sourceTaskId, targetTaskId);

                candidates.Add(new CandidateRelation(relationType, oriented.Source, oriented.Target));
            }
        }

        return candidates;
    }

    private async Task<RelationGraph> LoadGraph(
        RelationType relationType,
        List<CandidateRelation> candidates,
        CancellationToken cancellationToken)
    {
        var relations = UnitOfWork.ProjectTaskRelations;
        var sourceIds = candidates.Select(candidate => candidate.SourceTaskId).Distinct().ToList();
        var targetIds = candidates.Select(candidate => candidate.TargetTaskId).Distinct().ToList();
        var involvedIds = sourceIds.Union(targetIds).ToList();
        var isAcyclic = RelationTypeRules.IsAcyclic(relationType.Category);
        var hasSingleSource = RelationTypeRules.HasSingleSource(relationType.Category);

        // An acyclic type loads every existing edge reachable from the import's tasks, so any path the
        // cycle check walks from one of them, through old edges or new, stays inside the loaded graph.
        var existingEdges = isAcyclic
            ? await relations.GetReachableEdges(relationType.Id, involvedIds, cancellationToken)
            : await relations.GetEdgesFrom(relationType.Id, sourceIds, cancellationToken);
        var claimedTargets = hasSingleSource
            ? await relations.GetTargetsWithExistingSource(relationType.Id, targetIds, cancellationToken)
            : new List<int>();

        return new RelationGraph(relationType.Category, existingEdges, claimedTargets);
    }

    private static ProjectTaskRelation BuildRelation(CandidateRelation candidate, int workspaceId)
    {
        return new ProjectTaskRelation
        {
            WorkspaceId = workspaceId,
            RelationTypeId = candidate.RelationType.Id,
            SourceTaskId = candidate.SourceTaskId,
            TargetTaskId = candidate.TargetTaskId,
        };
    }

    private async Task Save(
        List<ProjectTaskRelation> relations,
        ImportApplyRequest request,
        CancellationToken cancellationToken)
    {
        foreach (var chunk in relations.Chunk(ChunkSize))
        {
            await UnitOfWork.ProjectTaskRelations.AddRangeAsync(chunk, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);

            var entries = chunk.Select(relation => new ImportSessionEntry
            {
                SessionId = request.Session.Id,
                EntityType = ImportEntryTypes.TaskRelation,
                EntityId = relation.Id,
                Operation = ImportEntryOperation.Created,
            });

            await ImportSessions.AddEntries(entries, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        }
    }

    private sealed record CandidateRelation(RelationType RelationType, int SourceTaskId, int TargetTaskId);

    private sealed class RelationGraph
    {
        private readonly bool IsAcyclic;
        private readonly bool HasSingleSource;
        private readonly HashSet<(int Source, int Target)> Edges = [];
        private readonly HashSet<int> ClaimedTargets;
        private readonly Dictionary<int, List<int>> Outgoing = [];

        public RelationGraph(
            RelationCategory category,
            IEnumerable<(int SourceTaskId, int TargetTaskId)> existingEdges,
            IEnumerable<int> claimedTargets)
        {
            IsAcyclic = RelationTypeRules.IsAcyclic(category);
            HasSingleSource = RelationTypeRules.HasSingleSource(category);
            ClaimedTargets = claimedTargets.ToHashSet();

            foreach (var (source, target) in existingEdges)
            {
                Link(source, target);
            }
        }

        public bool TryAdd(CandidateRelation candidate)
        {
            var source = candidate.SourceTaskId;
            var target = candidate.TargetTaskId;
            var isDuplicate = Edges.Contains((source, target));

            if (isDuplicate)
            {
                return false;
            }

            var targetHasSource = HasSingleSource && ClaimedTargets.Contains(target);

            if (targetHasSource)
            {
                return false;
            }

            var closesCycle = IsAcyclic && Reaches(target, source);

            if (closesCycle)
            {
                return false;
            }

            Link(source, target);
            ClaimedTargets.Add(target);

            return true;
        }

        private void Link(int source, int target)
        {
            var isNew = Edges.Add((source, target));

            if (!isNew)
            {
                return;
            }

            ref var targets = ref CollectionsMarshal.GetValueRefOrAddDefault(Outgoing, source, out _);

            targets ??= [];
            targets.Add(target);
        }

        private bool Reaches(int from, int to)
        {
            var visited = new HashSet<int> { from };
            var stack = new Stack<int>();

            stack.Push(from);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (current == to)
                {
                    return true;
                }

                var targets = Outgoing.GetValueOrDefault(current);

                if (targets is null)
                {
                    continue;
                }

                foreach (var next in targets)
                {
                    var isUnvisited = visited.Add(next);

                    if (isUnvisited)
                    {
                        stack.Push(next);
                    }
                }
            }

            return false;
        }
    }
}
