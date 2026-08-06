using Netptune.Transfer.Repositories;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Entities;
using System.Text.Json;

using Mediator;

using Microsoft.Extensions.Options;

using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Transfer.Messages;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Transfer;
using Netptune.Transfer.Export;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.ViewModels;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record CreateExportJobRequest
{
    public required ExportDefinitionModel Definition { get; init; }

    public string? Name { get; init; }
}

public sealed record CreateExportJobCommand(CreateExportJobRequest Request) : IRequest<ClientResponse<ExportJobViewModel>>;

public sealed class CreateExportJobCommandHandler : IRequestHandler<CreateExportJobCommand, ClientResponse<ExportJobViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IArchiveRepository Archives;
    private readonly IExportJobRepository ExportJobs;
    private readonly IIdentityService Identity;
    private readonly IEventPublisher EventPublisher;
    private readonly IEventRecordWriter EventRecords;
    private readonly TransferOptions Options;

    public CreateExportJobCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IEventPublisher eventPublisher,
        IEventRecordWriter eventRecords,
        IOptions<TransferOptions> options,
        IArchiveRepository archives,
        IExportJobRepository exportJobs)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        EventPublisher = eventPublisher;
        EventRecords = eventRecords;
        Options = options.Value;
        Archives = archives;
        ExportJobs = exportJobs;
    }

    public async ValueTask<ClientResponse<ExportJobViewModel>> Handle(CreateExportJobCommand request, CancellationToken cancellationToken)
    {
        var definition = request.Request.Definition;
        var validation = ExportDefinitionValidator.Validate(definition);

        if (!validation.IsValid)
        {
            return ClientResponse<ExportJobViewModel>.Failed(string.Join(" ", validation.Errors));
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var unfinished = await ExportJobs.CountUnfinished(workspaceId, cancellationToken);

        if (unfinished >= Options.MaxConcurrentJobsPerWorkspace)
        {
            return ClientResponse<ExportJobViewModel>.Failed(
                $"This workspace already has {unfinished} export jobs in flight. Wait for one to finish and try again.");
        }

        var quotaError = await CheckArchiveQuota(definition, workspaceId, cancellationToken);

        if (quotaError is not null)
        {
            return ClientResponse<ExportJobViewModel>.Failed(quotaError);
        }

        var userId = Identity.GetCurrentUserId();
        var job = await ExportJobs.AddAsync(new ExportJob
        {
            WorkspaceId = workspaceId,
            Status = ExportJobStatus.Pending,
            RecordType = definition.RecordType,
            Format = definition.Format,
            Definition = JsonSerializer.SerializeToDocument(definition, JsonOptions.Default),
            RequestedBy = userId,
            Name = request.Request.Name,
            ExpiresAt = DateTime.UtcNow.AddDays(Options.ExportArtefactRetentionDays),
            CreatedByUserId = userId,
            OwnerId = userId,
        }, cancellationToken);

        await LogExportRequested(workspaceId, definition, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        await EventPublisher.Dispatch(new ExportJobRequestedMessage
        {
            WorkspaceId = workspaceId,
            ExportJobId = job.Id,
            UserId = userId,
        });

        var viewModel = await ExportJobs.GetViewModel(job.PublicId, workspaceId, cancellationToken);

        if (viewModel is null)
        {
            return ClientResponse<ExportJobViewModel>.Failed("The export job could not be read back.");
        }

        return ClientResponse<ExportJobViewModel>.Success(viewModel);
    }

    private async Task<string?> CheckArchiveQuota(ExportDefinitionModel definition, int workspaceId, CancellationToken cancellationToken)
    {
        var needsFileBudget = definition.Format == ExportFormat.Archive && definition.Options.IncludeFiles;

        if (!needsFileBudget)
        {
            return null;
        }

        var usage = await UnitOfWork.Workspaces.GetStorageUsage(workspaceId, cancellationToken);

        if (usage is null)
        {
            return null;
        }

        var fileBytes = await Archives.GetFileBytes(workspaceId, cancellationToken);

        if (fileBytes <= usage.AvailableBytes)
        {
            return null;
        }

        return $"This archive would add about {fileBytes} bytes of files but only {usage.AvailableBytes} bytes of workspace storage remain. Export it without files, or free some space.";
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
