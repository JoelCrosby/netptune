using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Storage.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListWorkspaceFilesTool : IAiTool
{
    private const int DefaultPageSize = 25;

    private readonly IMediator Mediator;

    public ListWorkspaceFilesTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_workspace_files";

    public string Description =>
        "Search files uploaded anywhere in the workspace, newest first, with the task each one is attached to. "
        + "File contents cannot be read — this reports what exists, not what is inside.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Files.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "query": { "type": "string", "description": "Optional file name fragment to search for." },
          "uploadedByUserId": { "type": "string", "description": "Restrict to files uploaded by one member." },
          "from": { "type": "string", "description": "Only files uploaded on or after this ISO date." },
          "to": { "type": "string", "description": "Only files uploaded on or before this ISO date." },
          "take": { "type": "integer", "description": "How many files to return. Defaults to 25." }
        }
        """);

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var take = AiToolSchema.GetInt(arguments, "take") ?? DefaultPageSize;
        var filter = new WorkspaceFileFilter
        {
            Query = AiToolSchema.GetString(arguments, "query"),
            UploadedByUserId = AiToolSchema.GetString(arguments, "uploadedByUserId"),
            CreatedFrom = AiToolSchema.GetDate(arguments, "from"),
            CreatedTo = AiToolSchema.GetDate(arguments, "to"),
            Page = 1,
            PageSize = Math.Clamp(take, 1, 100),
        };

        var result = await Mediator.Send(new GetWorkspaceFilesQuery(filter), cancellationToken);

        if (!result.IsSuccess || result.Payload is null)
        {
            return AiToolExecution.Failed(result.Message ?? "Files could not be read.");
        }

        var files = result.Payload.Items.Select(file => new
        {
            id = file.Id,
            name = file.OriginalName,
            contentType = file.ContentType,
            sizeBytes = file.SizeBytes,
            uploadedBy = file.UploadedByDisplayName,
            uploadedAt = file.CreatedAt,
            taskSystemId = file.TaskSystemId,
            taskName = file.TaskName,
        });

        var summary = new
        {
            totalCount = result.Payload.TotalCount,
            files,
        };

        return AiToolExecution.Success(JsonSerializer.Serialize(summary));
    }
}
