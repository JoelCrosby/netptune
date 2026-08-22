using Mediator;

using Microsoft.Extensions.Options;

using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer;
using Netptune.Transfer.Definitions;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Services;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record RunExportInlineRequest
{
    public required ExportDefinitionModel Definition { get; init; }
}

public sealed record RunExportInlineCommand(RunExportInlineRequest Request) : IRequest<ClientResponse<ExportRunResult>>;

public sealed class RunExportInlineCommandHandler : IRequestHandler<RunExportInlineCommand, ClientResponse<ExportRunResult>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IExportRunner Runner;
    private readonly IEventRecordWriter EventRecords;
    private readonly TransferOptions Options;

    public RunExportInlineCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IExportRunner runner,
        IEventRecordWriter eventRecords,
        IOptions<TransferOptions> options)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Runner = runner;
        EventRecords = eventRecords;
        Options = options.Value;
    }

    public async ValueTask<ClientResponse<ExportRunResult>> Handle(RunExportInlineCommand request, CancellationToken cancellationToken)
    {
        var definition = request.Request.Definition;
        var validation = ExportDefinitionValidator.Validate(definition);

        if (!validation.IsValid)
        {
            return ClientResponse<ExportRunResult>.Failed(string.Join(" ", validation.Errors));
        }

        if (definition.Format == ExportFormat.Archive)
        {
            return ClientResponse<ExportRunResult>.Failed("An archive export has to run as a job.");
        }

        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var runRequest = new ExportRunRequest
        {
            WorkspaceId = workspaceId,
            WorkspaceSlug = workspaceKey,
            Definition = definition,
            InlineRowLimit = Options.InlineRowLimit,
        };
        var preview = await Runner.Preview(runRequest, 0, cancellationToken);

        if (!preview.CanRunInline)
        {
            return ClientResponse<ExportRunResult>.Failed(
                $"This export produces about {preview.EstimatedRowCount} rows. Run it as a job instead.");
        }

        var result = await Runner.Run(runRequest, (_, _) => Task.CompletedTask, cancellationToken);

        await LogExportRequested(workspaceId, definition, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse<ExportRunResult>.Success(result);
    }

    private async Task LogExportRequested(int workspaceId, ExportDefinitionModel definition, CancellationToken cancellationToken)
    {
        var scope = definition.Filter?.BoardIdentifiers.FirstOrDefault();

        await EventRecords.Append(new EventWriteRequest<ExportRequestedPayload>
        {
            WorkspaceId = workspaceId,
            EventKey = EventKeys.ExportRequested,
            SubjectType = EventEntityTypes.From(EntityType.Workspace),
            SubjectId = workspaceId.ToString(),
            Payload = new ExportRequestedPayload
            {
                ExportType = definition.RecordType,
                Scope = scope,
            },
        }, cancellationToken);
    }
}
