using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Localization;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Relations.Commands;
using Netptune.Handlers.Tags.Commands;
using Netptune.Handlers.Tasks.Commands;

namespace Netptune.Ai.Execution.Reviews;

public sealed class TaskReviewSuggestions
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IMediator Mediator;

    public TaskReviewSuggestions(INetptuneUnitOfWork unitOfWork, IMediator mediator)
    {
        UnitOfWork = unitOfWork;
        Mediator = mediator;
    }

    public static IReadOnlyList<string> PermissionsFor(AiReviewApplyRequest request)
    {
        var kind = AiReviewSuggestionKinds.Read(request.Suggestion);

        return kind switch
        {
            AiReviewSuggestionKinds.Split => [NetptunePermissions.Tasks.Create, NetptunePermissions.Tasks.Update],
            AiReviewSuggestionKinds.Link => [NetptunePermissions.Tasks.Update],
            AiReviewSuggestionKinds.Metadata => MetadataPermissions(request),
            _ => [NetptunePermissions.Tasks.Update],
        };
    }

    public async Task<AiReviewApplication> Apply(
        AiReviewApplyRequest request,
        TaskViewModel task,
        CancellationToken cancellationToken)
    {
        var kind = AiReviewSuggestionKinds.Read(request.Suggestion);

        return kind switch
        {
            AiReviewSuggestionKinds.Split => await ApplySplit(request, task, cancellationToken),
            AiReviewSuggestionKinds.Link => await ApplyLink(request, task, cancellationToken),
            AiReviewSuggestionKinds.Metadata => await ApplyMetadata(request, task, cancellationToken),
            _ => AiReviewApplication.Failed(AppText.AiReviewNothingToAccept),
        };
    }

    private static IReadOnlyList<string> MetadataPermissions(AiReviewApplyRequest request)
    {
        var suggestion = request.Suggestion.Deserialize<AiReviewMetadataSuggestion>(JsonOptions.Default);
        var chosenTags = ChosenTags(request, suggestion);
        var suggestedExisting = (suggestion?.Tags ?? [])
            .Where(tag => !tag.IsNew)
            .Select(tag => tag.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var permissions = new List<string> { NetptunePermissions.Tasks.Update };
        var addsTags = chosenTags.Count > 0;
        var createsTags = chosenTags.Any(tag => !suggestedExisting.Contains(tag));

        if (addsTags)
        {
            permissions.Add(NetptunePermissions.Tags.Assign);
        }

        if (createsTags)
        {
            permissions.Add(NetptunePermissions.Tags.Create);
        }

        return permissions;
    }

    private static List<string> ChosenTags(AiReviewApplyRequest request, AiReviewMetadataSuggestion? suggestion)
    {
        var suggestedNames = (suggestion?.Tags ?? []).Select(tag => tag.Name);
        var chosen = request.Decision.Tags ?? suggestedNames.ToList();

        return chosen
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<AiReviewApplication> ApplySplit(
        AiReviewApplyRequest request,
        TaskViewModel task,
        CancellationToken cancellationToken)
    {
        var suggestion = request.Suggestion.Deserialize<AiReviewSplitSuggestion>(JsonOptions.Default);
        var tasks = (request.Decision.SplitTasks ?? suggestion?.Tasks ?? [])
            .Where(split => !string.IsNullOrWhiteSpace(split.Name))
            .Take(AiReviewValidator.MaxSplitTasks)
            .ToList();

        if (tasks.Count == 0)
        {
            return AiReviewApplication.Failed(AppText.AiReviewNothingToAccept);
        }

        var hasInvalidTask = tasks.Any(split => !IsValidSplitTask(split));

        if (hasInvalidTask)
        {
            var message = Localized.Format(
                AppText.AiReviewSplitTaskTooLong,
                AiReviewValidator.MaxSplitNameLength,
                AiReviewValidator.MaxSplitDescriptionLength);

            return AiReviewApplication.Failed(message);
        }

        var hierarchy = await FirstRelationType(request.WorkspaceId, RelationCategory.Hierarchy, cancellationToken);
        var createdIds = new List<int>();

        foreach (var split in tasks)
        {
            var relations = hierarchy is null
                ? null
                : new List<AddTaskRelationRequest>
                {
                    new() { RelatedSystemId = task.SystemId, RelationTypeId = hierarchy.Value, TaskIsSource = false },
                };

            var create = new AddProjectTaskRequest
            {
                Name = split.Name.Trim(),
                Description = split.Description ?? string.Empty,
                ProjectId = task.ProjectId,
                Relations = relations,
            };

            var created = await Mediator.Send(new CreateTaskCommand(create), cancellationToken);

            if (!created.IsSuccess)
            {
                return AiReviewApplication.Failed(created.Message);
            }

            createdIds.Add(created.Payload!.Id);
        }

        var appliedChange = new
        {
            kind = AiReviewSuggestionKinds.Split,
            createdTaskIds = createdIds,
            linkedToParent = hierarchy is not null,
        };

        return new AiReviewApplication
        {
            IsSuccess = true,
            AppliedChange = JsonSerializer.SerializeToDocument(appliedChange, JsonOptions.Default),
            Changes = createdIds.Select(id => AppliedEntity("taskCreated", EntityType.Task, id)).ToList(),
            Created = createdIds.Select(id => new AiReviewCreatedEntity(EntityType.Task, id)).ToList(),
        };
    }

    private static bool IsValidSplitTask(AiReviewSplitTask split)
    {
        var isNameWithinLimit = split.Name.Trim().Length <= AiReviewValidator.MaxSplitNameLength;
        var descriptionLength = split.Description?.Length ?? 0;
        var isDescriptionWithinLimit = descriptionLength <= AiReviewValidator.MaxSplitDescriptionLength;

        return isNameWithinLimit && isDescriptionWithinLimit;
    }

    private async Task<AiReviewApplication> ApplyLink(
        AiReviewApplyRequest request,
        TaskViewModel task,
        CancellationToken cancellationToken)
    {
        var suggestion = request.Suggestion.Deserialize<AiReviewLinkSuggestion>(JsonOptions.Default);

        if (suggestion is null)
        {
            return AiReviewApplication.Failed(AppText.AiReviewNothingToAccept);
        }

        var relationTypeId = await FirstRelationType(request.WorkspaceId, suggestion.Category, cancellationToken);

        if (relationTypeId is null)
        {
            return AiReviewApplication.Failed(AppText.AiReviewNoRelationType);
        }

        var link = new CreateTaskRelationRequest
        {
            SourceSystemId = task.SystemId,
            TargetSystemId = suggestion.SystemId,
            RelationTypeId = relationTypeId.Value,
        };

        var created = await Mediator.Send(new CreateTaskRelationCommand(link), cancellationToken);

        if (!created.IsSuccess)
        {
            return AiReviewApplication.Failed(created.Message);
        }

        var relationId = created.Payload!.Id;
        var appliedChange = new { kind = AiReviewSuggestionKinds.Link, relationId, relatedTaskId = suggestion.TaskId };

        return new AiReviewApplication
        {
            IsSuccess = true,
            AppliedChange = JsonSerializer.SerializeToDocument(appliedChange, JsonOptions.Default),
            Changes = [AppliedEntity("relationCreated", EntityType.Task, suggestion.TaskId)],
        };
    }

    private async Task<AiReviewApplication> ApplyMetadata(
        AiReviewApplyRequest request,
        TaskViewModel task,
        CancellationToken cancellationToken)
    {
        var suggestion = request.Suggestion.Deserialize<AiReviewMetadataSuggestion>(JsonOptions.Default);
        var chosenTags = ChosenTags(request, suggestion);
        var tags = await EnsureTags(request.WorkspaceId, chosenTags, cancellationToken);

        if (!tags.IsSuccess)
        {
            return AiReviewApplication.Failed(tags.Message);
        }

        var estimate = request.Decision.ApplyEstimate ? suggestion?.EstimateValue : null;
        var priority = request.Decision.ApplyPriority ? suggestion?.Priority : null;
        var addedTags = tags.Names.Where(name => !task.Tags.Contains(name, StringComparer.Ordinal)).ToList();
        var hasTagChange = addedTags.Count > 0;
        var hasChange = hasTagChange || estimate is not null || priority is not null;

        if (!hasChange)
        {
            return AiReviewApplication.Failed(AppText.AiReviewNothingToAccept);
        }

        var update = new UpdateProjectTaskRequest
        {
            Id = task.Id,
            Tags = hasTagChange ? [.. task.Tags, .. addedTags] : null,
            EstimateValue = estimate,
            EstimateType = estimate is null ? null : task.EstimateType ?? EstimateType.StoryPoints,
            Priority = priority,
        };

        var updated = await Mediator.Send(new UpdateTaskCommand(update), cancellationToken);

        if (!updated.IsSuccess)
        {
            return AiReviewApplication.Failed(updated.Message);
        }

        var appliedChange = new
        {
            kind = AiReviewSuggestionKinds.Metadata,
            addedTags,
            createdTagIds = tags.CreatedIds,
            estimateBefore = task.EstimateValue,
            estimateAfter = estimate,
            priorityBefore = task.Priority,
            priorityAfter = priority,
        };

        var createdTags = tags.CreatedIds.Select(id => new AiReviewCreatedEntity(EntityType.Tag, id)).ToList();
        var tagChanges = tags.CreatedIds.Select(id => AppliedEntity("tagCreated", EntityType.Tag, id));

        return new AiReviewApplication
        {
            IsSuccess = true,
            AppliedChange = JsonSerializer.SerializeToDocument(appliedChange, JsonOptions.Default),
            Changes = [AppliedEntity("taskUpdated", EntityType.Task, task.Id), .. tagChanges],
            Created = createdTags,
        };
    }

    private async Task<TagResolution> EnsureTags(
        int workspaceId,
        List<string> names,
        CancellationToken cancellationToken)
    {
        if (names.Count == 0)
        {
            return new TagResolution { IsSuccess = true };
        }

        var workspaceTags = await UnitOfWork.Tags.GetTagsInWorkspace(workspaceId, true, cancellationToken);
        var existingByName = workspaceTags
            .GroupBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Name, StringComparer.OrdinalIgnoreCase);
        var existingNames = names.Where(existingByName.ContainsKey).Select(name => existingByName[name]).ToList();
        var newNames = names.Where(name => !existingByName.ContainsKey(name)).ToList();
        var hasTooManyNewTags = newNames.Count > AiReviewValidator.MaxNewTags;

        if (hasTooManyNewTags)
        {
            var message = Localized.Format(AppText.AiReviewTooManyNewTags, AiReviewValidator.MaxNewTags);

            return new TagResolution { IsSuccess = false, Message = message };
        }

        var resolvedNames = new List<string>(existingNames);
        var createdIds = new List<int>();

        foreach (var name in newNames)
        {
            var createTag = new CreateTagCommand(new AddTagRequest { Tag = name });
            var created = await Mediator.Send(createTag, cancellationToken);

            if (created.IsSuccess)
            {
                resolvedNames.Add(created.Payload!.Name);
                createdIds.Add(created.Payload.Id);

                continue;
            }

            var createdMeanwhile = await UnitOfWork.Tags.GetTagsByValueInWorkspace(
                workspaceId,
                [name],
                true,
                cancellationToken);

            if (createdMeanwhile.Count == 0)
            {
                return new TagResolution { IsSuccess = false, Message = created.Message };
            }

            resolvedNames.Add(createdMeanwhile[0].Name);
        }

        return new TagResolution
        {
            IsSuccess = true,
            Names = resolvedNames,
            CreatedIds = createdIds,
        };
    }

    private async Task<int?> FirstRelationType(
        int workspaceId,
        RelationCategory category,
        CancellationToken cancellationToken)
    {
        var relationTypes = await UnitOfWork.RelationTypes.GetViewModelsForWorkspace(workspaceId, cancellationToken);
        var match = relationTypes
            .Where(relationType => relationType.Category == category)
            .OrderBy(relationType => relationType.SortOrder)
            .FirstOrDefault();

        return match?.Id;
    }

    private static AssistantReviewAppliedEntity AppliedEntity(string kind, EntityType entityType, int entityId)
    {
        return new AssistantReviewAppliedEntity
        {
            Kind = kind,
            EntityType = EventEntityTypes.From(entityType),
            EntityId = entityId,
        };
    }

    private sealed record TagResolution
    {
        public bool IsSuccess { get; init; }

        public string? Message { get; init; }

        public List<string> Names { get; init; } = [];

        public List<int> CreatedIds { get; init; } = [];
    }
}
