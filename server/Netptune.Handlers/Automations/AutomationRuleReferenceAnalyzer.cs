using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Handlers.Automations;

public static class AutomationRuleReferenceAnalyzer
{
    public static async Task<Dictionary<int, List<AutomationRuleWarning>>> Analyze(
        INetptuneUnitOfWork unitOfWork,
        IReadOnlyCollection<AutomationRuleViewModel> rules,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        if (rules.Count == 0)
        {
            return [];
        }

        var references = CollectReferences(rules);
        var existing = await ResolveExistingReferences(unitOfWork, references, workspaceId, cancellationToken);

        return rules.ToDictionary(rule => rule.Id, rule => BuildWarnings(rule, existing));
    }

    private static AutomationRuleReferences CollectReferences(IReadOnlyCollection<AutomationRuleViewModel> rules)
    {
        var references = new AutomationRuleReferences();

        foreach (var rule in rules)
        {
            references.Add(rule);
        }

        return references;
    }

    private static async Task<AutomationRuleReferences> ResolveExistingReferences(
        INetptuneUnitOfWork unitOfWork,
        AutomationRuleReferences references,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        var projectIds = await unitOfWork.Projects.GetExistingIds([.. references.ProjectIds], workspaceId, cancellationToken);
        var boardIds = await unitOfWork.Boards.GetExistingIds([.. references.BoardIds], workspaceId, cancellationToken);
        var sprintIds = await unitOfWork.Sprints.GetExistingIds([.. references.SprintIds], workspaceId, cancellationToken);
        var statusIds = await unitOfWork.Statuses.GetExistingIds([.. references.StatusIds], workspaceId, cancellationToken);
        var boardGroupIds = await unitOfWork.BoardGroups.GetExistingIds([.. references.BoardGroupIds], workspaceId, cancellationToken);
        var relationTypeIds = await unitOfWork.RelationTypes.GetExistingIds([.. references.RelationTypeIds], workspaceId, cancellationToken);
        var taskIds = await unitOfWork.Tasks.GetValidTaskIdsInWorkspace(references.TaskIds, workspaceId, cancellationToken);
        var tagNames = await unitOfWork.Tags.GetExistingNames([.. references.TagNames], workspaceId, cancellationToken);
        var users = await unitOfWork.Users.IsUserInWorkspaceRange(references.UserIds, workspaceId, cancellationToken);

        return new AutomationRuleReferences
        {
            ProjectIds = [.. projectIds],
            BoardIds = [.. boardIds],
            SprintIds = [.. sprintIds],
            StatusIds = [.. statusIds],
            BoardGroupIds = [.. boardGroupIds],
            RelationTypeIds = [.. relationTypeIds],
            TaskIds = [.. taskIds],
            TagNames = [.. tagNames],
            UserIds = [.. users.Select(user => user.Id)],
        };
    }

    private static List<AutomationRuleWarning> BuildWarnings(
        AutomationRuleViewModel rule,
        AutomationRuleReferences existing)
    {
        var warnings = new List<AutomationRuleWarning>();

        AddScopeWarnings(rule, existing, warnings);
        AddConditionWarnings(rule, existing, warnings);

        foreach (var action in rule.Actions)
        {
            AddActionWarnings(action, existing, warnings);
        }

        return warnings;
    }

    private static void AddScopeWarnings(
        AutomationRuleViewModel rule,
        AutomationRuleReferences existing,
        List<AutomationRuleWarning> warnings)
    {
        var projectIsMissing = rule.ProjectId.HasValue && !existing.ProjectIds.Contains(rule.ProjectId.Value);

        if (projectIsMissing)
        {
            warnings.Add(Warning(AutomationWarningCode.MissingProject, "This rule is scoped to a project that no longer exists."));
        }

        var boardIsMissing = rule.BoardId.HasValue && !existing.BoardIds.Contains(rule.BoardId.Value);

        if (boardIsMissing)
        {
            warnings.Add(Warning(AutomationWarningCode.MissingBoard, "This rule is scoped to a board that no longer exists."));
        }

        var sprintIsMissing = rule.SprintId.HasValue && !existing.SprintIds.Contains(rule.SprintId.Value);

        if (sprintIsMissing)
        {
            warnings.Add(Warning(AutomationWarningCode.MissingSprint, "This rule is scoped to a sprint that no longer exists."));
        }

        var executionUserIsMissing = rule.ExecutionUserId is not null && !existing.UserIds.Contains(rule.ExecutionUserId);

        if (executionUserIsMissing)
        {
            warnings.Add(Warning(
                AutomationWarningCode.MissingExecutionUser,
                "The account this rule runs as is no longer a member of this workspace."));
        }
    }

    private static void AddConditionWarnings(
        AutomationRuleViewModel rule,
        AutomationRuleReferences existing,
        List<AutomationRuleWarning> warnings)
    {
        var conditionGroup = rule.Trigger.ConditionGroup;

        if (conditionGroup is null)
        {
            return;
        }

        var conditions = FlattenConditions(conditionGroup);

        foreach (var condition in conditions)
        {
            AddConditionWarning(condition, existing, warnings);
        }
    }

    private static void AddConditionWarning(
        AutomationFieldCondition condition,
        AutomationRuleReferences existing,
        List<AutomationRuleWarning> warnings)
    {
        var value = condition.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var referencesMissingStatus = condition.Field == TaskChangeField.Status
            && int.TryParse(value, out var statusId)
            && !existing.StatusIds.Contains(statusId);

        if (referencesMissingStatus)
        {
            warnings.Add(Warning(AutomationWarningCode.MissingStatus, "A condition checks a status that no longer exists."));
        }

        var referencesMissingSprint = condition.Field == TaskChangeField.Sprint
            && int.TryParse(value, out var sprintId)
            && !existing.SprintIds.Contains(sprintId);

        if (referencesMissingSprint)
        {
            warnings.Add(Warning(AutomationWarningCode.MissingSprint, "A condition checks a sprint that no longer exists."));
        }

        var referencesMissingTag = condition.Field == TaskChangeField.Tags && !existing.TagNames.Contains(value);

        if (referencesMissingTag)
        {
            warnings.Add(Warning(AutomationWarningCode.MissingTag, $"A condition checks the tag \"{value}\", which no longer exists."));
        }

        var checksUserField = condition.Field is TaskChangeField.Owner or TaskChangeField.Assignees;
        var referencesMissingUser = checksUserField && !existing.UserIds.Contains(value);

        if (referencesMissingUser)
        {
            warnings.Add(Warning(AutomationWarningCode.MissingUser, "A condition checks a member who is no longer in this workspace."));
        }
    }

    private static void AddActionWarnings(
        AutomationActionViewModel action,
        AutomationRuleReferences existing,
        List<AutomationRuleWarning> warnings)
    {
        var statusIsMissing = action.StatusId.HasValue && !existing.StatusIds.Contains(action.StatusId.Value);

        if (statusIsMissing)
        {
            warnings.Add(Warning(AutomationWarningCode.MissingStatus, "This action sets a status that no longer exists.", action.Id));
        }

        var sprintIsMissing = action.SprintId.HasValue && !existing.SprintIds.Contains(action.SprintId.Value);

        if (sprintIsMissing)
        {
            warnings.Add(Warning(AutomationWarningCode.MissingSprint, "This action sets a sprint that no longer exists.", action.Id));
        }

        var boardGroupIsMissing = action.BoardGroupId.HasValue && !existing.BoardGroupIds.Contains(action.BoardGroupId.Value);

        if (boardGroupIsMissing)
        {
            warnings.Add(Warning(AutomationWarningCode.MissingBoardGroup, "This action moves tasks to a board group that no longer exists.", action.Id));
        }

        AddRelationTypeWarnings(action, existing, warnings);
        AddUserWarnings(action, existing, warnings);
        AddTagWarnings(action, existing, warnings);

        var relatedTaskIsMissing = action.RelatedTaskId.HasValue && !existing.TaskIds.Contains(action.RelatedTaskId.Value);

        if (relatedTaskIsMissing)
        {
            warnings.Add(Warning(AutomationWarningCode.MissingTask, "This action links to a task that no longer exists.", action.Id));
        }
    }

    private static void AddRelationTypeWarnings(
        AutomationActionViewModel action,
        AutomationRuleReferences existing,
        List<AutomationRuleWarning> warnings)
    {
        var relationTypeIds = new[] { action.RelationTypeId, action.LinkRelationTypeId }
            .Where(relationTypeId => relationTypeId.HasValue)
            .Select(relationTypeId => relationTypeId!.Value)
            .Distinct();

        var missingRelationTypes = relationTypeIds.Where(relationTypeId => !existing.RelationTypeIds.Contains(relationTypeId));

        foreach (var _ in missingRelationTypes)
        {
            warnings.Add(Warning(AutomationWarningCode.MissingRelationType, "This action uses a relation type that no longer exists.", action.Id));
        }
    }

    private static void AddUserWarnings(
        AutomationActionViewModel action,
        AutomationRuleReferences existing,
        List<AutomationRuleWarning> warnings)
    {
        var userIds = new List<string>();

        if (action.OwnerId is not null)
        {
            userIds.Add(action.OwnerId);
        }

        userIds.AddRange(action.AssigneeIds ?? []);
        userIds.AddRange(action.RecipientUserIds);

        var missingUserIds = userIds
            .Distinct(StringComparer.Ordinal)
            .Where(userId => !existing.UserIds.Contains(userId))
            .ToList();

        foreach (var _ in missingUserIds)
        {
            warnings.Add(Warning(
                AutomationWarningCode.MissingUser,
                "This action references a member who is no longer in this workspace.",
                action.Id));
        }
    }

    private static void AddTagWarnings(
        AutomationActionViewModel action,
        AutomationRuleReferences existing,
        List<AutomationRuleWarning> warnings)
    {
        var missingTags = action.RemoveTags
            .Concat(action.AddTags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(tag => !existing.TagNames.Contains(tag))
            .ToList();

        foreach (var tag in missingTags)
        {
            warnings.Add(Warning(
                AutomationWarningCode.MissingTag,
                $"This action uses the tag \"{tag}\", which no longer exists.",
                action.Id));
        }
    }

    private static List<AutomationFieldCondition> FlattenConditions(AutomationConditionGroup group)
    {
        var nestedConditions = group.Groups.SelectMany(FlattenConditions);

        return group.Conditions.Concat(nestedConditions).ToList();
    }

    private static AutomationRuleWarning Warning(AutomationWarningCode code, string message, int? actionId = null)
    {
        return new AutomationRuleWarning
        {
            Code = code,
            Message = message,
            ActionId = actionId,
        };
    }
}
