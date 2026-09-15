using System.Text.Json;
using System.Text.Json.Nodes;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Comments.Queries;
using Netptune.Handlers.Relations.Queries;
using Netptune.Handlers.Storage.Queries;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class GetTaskTool : IAiTool
{
    private const string IncludeComments = "comments";
    private const string IncludeRelations = "relations";
    private const string IncludeFiles = "files";

    private static readonly string[] KnownIncludes = [IncludeComments, IncludeRelations, IncludeFiles];

    private readonly IMediator Mediator;

    public GetTaskTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "get_task";

    public string Description =>
        "Read one task in full by its systemId: description, assignees, tags, dates, estimate and flags. "
        + "Add include to read its comments (oldest first), the tasks it is linked to and by which relation, "
        + "or its attached files. File contents cannot be read — only what is attached.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "systemId": { "type": "string", "description": "The task system id, such as NPT-42." },
          "include": {
            "type": "array",
            "items": { "type": "string", "enum": ["comments", "relations", "files"] },
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
                $"“{unknownInclude}” cannot be included. Use comments, relations or files.");
        }

        var task = await Mediator.Send(new GetTaskDetailQuery(systemId!), cancellationToken);

        if (task is null)
        {
            return AiToolExecution.Failed($"Task {systemId} was not found in this workspace.");
        }

        var detail = JsonSerializer.SerializeToNode(new
        {
            id = task.Id,
            systemId = task.SystemId,
            name = task.Name,
            description = task.Description,
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
            boardGroupId = task.BoardGroupId,
            hasComments = task.HasComments,
            flags = task.Flags.Select(flag => new { id = flag.Id, name = flag.Name, description = flag.Description }),
        })!.AsObject();

        var includesComments = includes.Contains(IncludeComments);

        if (includesComments)
        {
            detail["comments"] = await ReadComments(systemId!, cancellationToken);
        }

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

        return AiToolExecution.Success(detail.ToJsonString());
    }

    private static List<string> ReadIncludes(JsonElement arguments)
    {
        return AiToolSchema.GetStringArray(arguments, "include")
            .Select(include => include.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private async Task<JsonNode?> ReadComments(string systemId, CancellationToken cancellationToken)
    {
        var comments = await Mediator.Send(new GetCommentsForTaskQuery(systemId), cancellationToken);
        var summaries = (comments ?? []).Select(comment => new
        {
            id = comment.Id,
            author = comment.UserDisplayName,
            body = comment.Body,
            createdAt = comment.CreatedAt,
        });

        return JsonSerializer.SerializeToNode(summaries);
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
}
