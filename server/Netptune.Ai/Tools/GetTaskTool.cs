using System.Text.Json;
using System.Text.Json.Nodes;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Comments.Queries;
using Netptune.Handlers.Relations.Queries;
using Netptune.Handlers.SourceControl.Queries;
using Netptune.Handlers.Storage.Queries;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class GetTaskTool : IAiTool
{
    private const string IncludeComments = "comments";
    private const string IncludeRelations = "relations";
    private const string IncludeFiles = "files";
    private const string IncludeCode = "code";
    private const int MaxCommentCharacters = 4_000;

    private static readonly string[] KnownIncludes = [IncludeComments, IncludeRelations, IncludeFiles, IncludeCode];

    private readonly IMediator Mediator;
    private readonly IIdentityService Identity;

    public GetTaskTool(IMediator mediator, IIdentityService identity)
    {
        Mediator = mediator;
        Identity = identity;
    }

    public string Name => "get_task";

    public string Description =>
        "Read one task in full by its systemId: description, assignees, tags, dates, estimate and flags. "
        + "Add include to read its 50 most recent comments (newest first), the tasks it is linked to and by which relation, "
        + "its attached files, or the branches, commits and pull requests linked to it from source control. "
        + "File contents cannot be read — only what is attached; read linked code with the repository tools. "
        + "descriptionFormat is a format block for the description. "
        + AiTextFormatJson.Description;

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "systemId": { "type": "string", "description": "The task system id, such as NPT-42." },
          "include": {
            "type": "array",
            "items": { "type": "string", "enum": ["comments", "relations", "files", "code"] },
            "description": "Extra detail to read alongside the task."
          }
        }
        """,
        "systemId");

    public IReadOnlySet<string> GetRequiredPermissions(JsonElement arguments)
    {
        var includes = ReadIncludes(arguments);
        var required = new HashSet<string>(RequiredPermissions, StringComparer.Ordinal);
        var includesComments = includes.Contains(IncludeComments);

        if (includesComments)
        {
            required.Add(NetptunePermissions.Comments.Read);
        }

        var includesFiles = includes.Contains(IncludeFiles);

        if (includesFiles)
        {
            required.Add(NetptunePermissions.Files.Read);
        }

        var includesCode = includes.Contains(IncludeCode);

        if (includesCode)
        {
            required.Add(NetptunePermissions.SourceControl.Read);
        }

        return required;
    }

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var systemId = AiToolSchema.GetString(arguments, "systemId")?.Trim();
        var hasSystemId = !string.IsNullOrWhiteSpace(systemId);

        if (!hasSystemId)
        {
            return AiToolExecution.Failed("A systemId is required.");
        }

        var includes = ReadIncludes(arguments);
        var unknownInclude = includes.FirstOrDefault(include => !KnownIncludes.Contains(include));

        if (unknownInclude is not null)
        {
            return AiToolExecution.Failed(
                $"“{unknownInclude}” cannot be included. Use comments, relations, files or code.");
        }

        var task = await Mediator.Send(new GetTaskDetailQuery(systemId!), cancellationToken);

        if (task is null)
        {
            return AiToolExecution.Failed($"Task {systemId} was not found in this workspace.");
        }

        var descriptionFormat = AiTextFormatReader.Read(task.Description ?? string.Empty);
        var detail = JsonSerializer.SerializeToNode(new
        {
            taskId = task.Id,
            systemId = task.SystemId,
            name = task.Name,
            description = task.Description,
            descriptionFormat = AiTextFormatJson.From(descriptionFormat),
            status = task.StatusName,
            statusId = task.StatusId,
            statusCategory = task.StatusCategory.ToString(),
            assignees = task.Assignees.Select(assignee => new { userId = assignee.Id, assignee.DisplayName }),
            tags = task.Tags,
            priority = task.Priority?.ToString(),
            estimateType = task.EstimateType?.ToString(),
            estimateValue = task.EstimateValue,
            startDate = task.StartDate,
            dueDate = task.DueDate,
            projectId = task.ProjectId,
            projectName = task.ProjectName,
            sprintId = task.SprintId,
            sprintName = task.SprintName,
            placements = task.Placements.Select(placement => new
            {
                boardId = placement.BoardId,
                board = placement.BoardName,
                boardGroupId = placement.BoardGroupId,
                column = placement.BoardGroupName,
            }),
            hasComments = task.HasComments,
            flags = task.Flags.Select(flag => new { id = flag.Id, name = flag.Name, description = flag.Description }),
        })!.AsObject();

        var includesRelations = includes.Contains(IncludeRelations);

        if (includesRelations)
        {
            detail["relations"] = await ReadRelations(systemId!, cancellationToken);
        }

        var includesFiles = includes.Contains(IncludeFiles);

        if (includesFiles)
        {
            detail["files"] = await ReadFiles(systemId!, cancellationToken);
        }

        var includesCode = includes.Contains(IncludeCode);

        if (includesCode)
        {
            detail["code"] = await ReadCodeLinks(systemId!, cancellationToken);
        }

        var includesComments = includes.Contains(IncludeComments);
        var commentsOmitted = 0;

        // Comments go last and take whatever room the rest of the task leaves, so a long thread cannot push
        // the relations and files out of the result.
        if (includesComments)
        {
            var budget = AiResultBudget.Characters - detail.ToJsonString(AiToolJson.Options).Length;
            var comments = await ReadComments(systemId!, budget, cancellationToken);

            detail["comments"] = comments.Rows;
            commentsOmitted = comments.Omitted;

            if (commentsOmitted > 0)
            {
                detail["commentsOmitted"] = commentsOmitted;
            }
        }

        return AiToolExecution.Success(detail.ToJsonString(), commentsOmitted > 0);
    }

    private static List<string> ReadIncludes(JsonElement arguments)
    {
        return AiToolSchema.GetStringArray(arguments, "include")
            .Select(include => include.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private async Task<AiBudgetedRows> ReadComments(string systemId, int budget, CancellationToken cancellationToken)
    {
        var comments = await Mediator.Send(new GetCommentsForTaskQuery(systemId), cancellationToken);
        var currentUserId = Identity.GetCurrentUserId();
        var summaries = (comments ?? []).Select(comment => new
        {
            id = comment.Id,
            author = comment.UserDisplayName,
            isYours = comment.UserId == currentUserId,
            body = ShortenComment(comment.Body),
            createdAt = comment.CreatedAt,
        });

        return AiResultBudget.Fit(summaries, budget: budget);
    }

    private static string? ShortenComment(string? body)
    {
        var isLong = body is not null && body.Length > MaxCommentCharacters;

        return isLong ? $"{body![..MaxCommentCharacters]}\n… (comment shortened)" : body;
    }

    private async Task<JsonNode?> ReadRelations(string systemId, CancellationToken cancellationToken)
    {
        var relations = await Mediator.Send(new GetTaskRelationsQuery(systemId), cancellationToken);
        var summaries = (relations ?? []).Select(relation => new
        {
            id = relation.Id,
            label = relation.Label,
            relationTypeId = relation.RelationTypeId,
            relationType = relation.RelationTypeName,
            relatedTaskId = relation.RelatedTask.Id,
            relatedSystemId = relation.RelatedTask.SystemId,
            relatedName = relation.RelatedTask.Name,
        });

        return JsonSerializer.SerializeToNode(summaries);
    }

    private async Task<JsonNode?> ReadFiles(string systemId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetTaskFilesQuery(systemId), cancellationToken);
        var hasFiles = result.IsSuccess && result.Payload is not null;
        var files = hasFiles ? result.Payload! : [];
        var summaries = files.Select(file => new
        {
            id = file.Id,
            name = file.OriginalName,
            contentType = file.ContentType,
            sizeBytes = file.SizeBytes,
            uploadedBy = file.UploadedByDisplayName,
            uploadedAt = file.CreatedAt,
        });

        return JsonSerializer.SerializeToNode(summaries);
    }

    private async Task<JsonNode?> ReadCodeLinks(string systemId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetTaskCodeLinksQuery(systemId), cancellationToken);
        var hasLinks = result.IsSuccess && result.Payload is not null;
        var links = hasLinks ? result.Payload! : [];
        var summaries = links
            .Where(link => !link.IsRemoved)
            .Select(link => new
            {
                kind = link.Kind.ToString(),
                repository = link.RepoFullName,
                @ref = link.ExternalRef,
                title = link.Title,
                pullRequestState = link.PullRequestState?.ToString(),
                checks = link.Checks?.State.ToString(),
                author = link.Author?.DisplayName ?? link.AuthorLogin,
                occurredAt = link.OccurredAt,
                url = link.WebUrl,
            });

        return JsonSerializer.SerializeToNode(summaries);
    }
}
