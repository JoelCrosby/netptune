using Netptune.Core.Enums;
using Netptune.Core.UnitOfWork;
using Netptune.Query.Model;
using Netptune.Query.Schema;
using Netptune.Query.Validation;

namespace Netptune.Query.Tasks;

public sealed class TaskReferenceValidator
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public TaskReferenceValidator(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public async Task<QueryValidationResult> Validate(
        QueryGroup? group,
        QueryWorkspaceScope scope,
        CancellationToken cancellationToken)
    {
        if (group is null)
        {
            return QueryValidationResult.Valid;
        }

        var conditions = new List<ConditionReference>();

        Collect(group, "query", conditions);

        var errors = new List<QueryValidationError>();

        foreach (var source in conditions.Select(condition => condition.OptionSource).Distinct())
        {
            var known = await GetKnownValues(source, scope, cancellationToken);

            if (known is null)
            {
                continue;
            }

            var referencing = conditions.Where(condition => condition.OptionSource == source);

            foreach (var reference in referencing)
            {
                AddMissingValueErrors(reference, known, errors);
            }
        }

        return new QueryValidationResult { Errors = errors };
    }

    private static void AddMissingValueErrors(
        ConditionReference reference,
        HashSet<string> known,
        List<QueryValidationError> errors)
    {
        foreach (var value in reference.Condition.Values)
        {
            var normalized = Normalize(reference.OptionSource, value);
            var isKnown = known.Contains(normalized);

            if (isKnown)
            {
                continue;
            }

            errors.Add(new QueryValidationError
            {
                Path = reference.Path,
                Field = reference.Condition.Field,
                Message = $"'{value}' no longer exists in this workspace.",
            });
        }
    }

    private async Task<HashSet<string>?> GetKnownValues(
        string optionSource,
        QueryWorkspaceScope scope,
        CancellationToken cancellationToken)
    {
        switch (optionSource)
        {
            case QueryOptionSources.Statuses:
                {
                    var statuses = await UnitOfWork.Statuses.GetViewModelsForWorkspace(scope.WorkspaceId, EntityType.Task, cancellationToken);

                    return statuses.Select(status => status.Id.ToString()).ToHashSet();
                }

            case QueryOptionSources.Projects:
                {
                    var projects = await UnitOfWork.Projects.GetAllProjectViewModels(scope.WorkspaceKey, cancellationToken);

                    return projects.Select(project => project.Id.ToString()).ToHashSet();
                }

            case QueryOptionSources.Sprints:
                {
                    var sprints = await UnitOfWork.Sprints.GetAllSprintViewModels(scope.WorkspaceKey, cancellationToken);

                    return sprints.Select(sprint => sprint.Id.ToString()).ToHashSet();
                }

            case QueryOptionSources.Boards:
                {
                    var projectBoards = await UnitOfWork.Boards.GetBoardViewModels(scope.WorkspaceKey, cancellationToken);
                    var boards = projectBoards.SelectMany(project => project.Boards);

                    return boards.Select(board => board.Id.ToString()).ToHashSet();
                }

            case QueryOptionSources.Members:
                {
                    var userIds = await UnitOfWork.WorkspaceUsers.GetWorkspaceUserIds(scope.WorkspaceId, cancellationToken);

                    return userIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
                }

            case QueryOptionSources.Tags:
                {
                    var tags = await UnitOfWork.Tags.GetViewModelsForWorkspace(scope.WorkspaceId, cancellationToken);

                    return tags.Select(tag => tag.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                }

            case QueryOptionSources.RelationTypes:
                {
                    var relationTypes = await UnitOfWork.RelationTypes.GetViewModelsForWorkspace(scope.WorkspaceId, cancellationToken);

                    return relationTypes.Select(relationType => relationType.Id.ToString()).ToHashSet();
                }

            default:
                return null;
        }
    }

    private static string Normalize(string optionSource, string value)
    {
        if (optionSource != QueryOptionSources.RelationTypes)
        {
            return value.Trim();
        }

        var reference = TaskRelationReference.Parse(value);

        return reference is null ? value.Trim() : reference.RelationTypeId.ToString();
    }

    private static void Collect(QueryGroup group, string path, List<ConditionReference> conditions)
    {
        for (var index = 0; index < group.Conditions.Count; index++)
        {
            var condition = group.Conditions[index];
            var field = TaskFieldCatalog.Instance.Find(condition.Field);
            var isEntityReference = field?.OptionSource is not null && field.EnumType is null;

            if (isEntityReference)
            {
                conditions.Add(new ConditionReference(condition, field!.OptionSource!, $"{path}.conditions[{index}]"));
            }
        }

        for (var index = 0; index < group.Groups.Count; index++)
        {
            Collect(group.Groups[index], $"{path}.groups[{index}]", conditions);
        }
    }

    private sealed record ConditionReference(QueryCondition Condition, string OptionSource, string Path);
}
