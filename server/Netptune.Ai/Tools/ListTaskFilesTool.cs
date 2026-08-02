using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Storage.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListTaskFilesTool : IAiTool
{
    private readonly IMediator Mediator;

    public ListTaskFilesTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_task_files";

    public string Description =>
        "List the files attached to one task, with their name, type, size and who uploaded them. "
        + "File contents cannot be read — this reports what is attached, not what is inside.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.Tasks.Read,
        NetptunePermissions.Files.Read,
    };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "systemId": { "type": "string", "description": "The task's system id, for example NPT-42." }
        }
        """,
        "systemId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var systemId = AiToolSchema.GetString(arguments, "systemId")?.Trim();
        var hasSystemId = !string.IsNullOrWhiteSpace(systemId);

        if (!hasSystemId)
        {
            return AiToolExecution.Failed("A systemId is required.");
        }

        var result = await Mediator.Send(new GetTaskFilesQuery(systemId!), cancellationToken);

        if (!result.IsSuccess || result.Payload is null)
        {
            return AiToolExecution.Failed(
                result.Message ?? $"Task {systemId} was not found in this workspace.");
        }

        var files = result.Payload.Select(file => new
        {
            id = file.Id,
            name = file.OriginalName,
            contentType = file.ContentType,
            sizeBytes = file.SizeBytes,
            uploadedBy = file.UploadedByDisplayName,
            uploadedAt = file.CreatedAt,
        });

        return AiToolExecution.Success(JsonSerializer.Serialize(files));
    }
}
