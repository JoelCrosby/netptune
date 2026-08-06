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
using Netptune.Core.Services;
using Netptune.Transfer.Services;
using Netptune.Transfer;
using Netptune.Transfer.Import;
using Netptune.Core.UnitOfWork;

namespace Netptune.JobServer.Handlers;

public sealed class ImportJobHandler : IRequestHandler<ImportCommitRequestedMessage>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IImportSessionRepository ImportSessions;
    private readonly IImportSourceStore Store;
    private readonly IImportApplier Applier;
    private readonly IEventRecordWriter EventRecords;
    private readonly TransferOptions Options;
    private readonly ILogger<ImportJobHandler> Logger;

    public ImportJobHandler(
        INetptuneUnitOfWork unitOfWork,
        IImportSourceStore store,
        IImportApplier applier,
        IEventRecordWriter eventRecords,
        IOptions<TransferOptions> options,
        ILogger<ImportJobHandler> logger,
        IImportSessionRepository importSessions)
    {
        UnitOfWork = unitOfWork;
        Store = store;
        Applier = applier;
        EventRecords = eventRecords;
        Options = options.Value;
        Logger = logger;
        ImportSessions = importSessions;
    }

    public async ValueTask<Unit> Handle(ImportCommitRequestedMessage request, CancellationToken cancellationToken)
    {
        var session = await ImportSessions.GetForProcessing(request.ImportSessionId, cancellationToken);

        if (session is null)
        {
            Logger.LogWarning("[Import] session {SessionId} no longer exists", request.ImportSessionId);

            return default;
        }

        if (session.Stage != ImportStage.Committing)
        {
            Logger.LogInformation("[Import] session {PublicId} is {Stage} and will not be committed", session.PublicId, session.Stage);

            return default;
        }

        try
        {
            await Commit(session, request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "[Import] session {PublicId} failed", session.PublicId);

            session.Stage = ImportStage.Failed;
            session.Error = exception.Message;
            session.ProgressMessage = "Failed";

            await UnitOfWork.CompleteAsync(cancellationToken);
        }

        return default;
    }

    private async Task Commit(ImportSession session, ImportCommitRequestedMessage request, CancellationToken cancellationToken)
    {
        var workspaceSlug = session.Workspace?.Slug
            ?? throw new InvalidOperationException("The workspace could not be resolved.");
        var mapping = session.Mapping?.Deserialize<ImportMappingModel>(JsonOptions.Default)
            ?? throw new InvalidOperationException("The mapping could not be read.");
        var profile = session.SourceProfile?.Deserialize<ImportSourceProfile>(JsonOptions.Default);

        await using var source = await Store.Open(session, cancellationToken);

        var applyRequest = new ImportApplyRequest
        {
            WorkspaceId = session.WorkspaceId,
            WorkspaceSlug = workspaceSlug,
            UserId = request.UserId,
            Session = session,
            Mapping = mapping,
            Source = source,
            ColumnNames = profile?.Columns.Select(column => column.Name).ToList() ?? [],
            ReadOptions = new ImportReadOptions
            {
                Delimiter = profile?.Delimiter,
                HasHeaderRow = profile?.HasHeaderRow ?? true,
            },
            SkipFailingRows = request.SkipFailingRows,
            EstimatedRowCount = profile?.EstimatedRowCount,
            MaxRows = Options.MaxRowsPerImport,
        };
        var result = await Applier.Commit(applyRequest, ReportProgress, cancellationToken);

        session.Stage = ImportStage.Committed;
        session.ProgressPercent = 100;
        session.ProgressMessage = "Complete";
        session.CommittedAt = DateTime.UtcNow;
        session.Result = JsonSerializer.SerializeToDocument(result, JsonOptions.Default);

        await WriteCompletedEvent(session, result, request.UserId, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        Logger.LogInformation(
            "[Import] session {PublicId} created {Created} and updated {Updated} records",
            session.PublicId,
            result.Created,
            result.Updated);

        async Task ReportProgress(ImportProgress progress, CancellationToken token)
        {
            session.ProgressPercent = progress.Percent;
            session.ProgressMessage = progress.Message;

            await UnitOfWork.CompleteAsync(token);
        }
    }

    private async Task WriteCompletedEvent(
        ImportSession session,
        ImportCommitResult result,
        string userId,
        CancellationToken cancellationToken)
    {
        await EventRecords.Append(new EventWriteRequest<ImportCompletedPayload>
        {
            WorkspaceId = session.WorkspaceId,
            EventKey = EventKeys.ImportCompleted,
            SubjectType = EventEntityTypes.From(EntityType.Workspace),
            SubjectId = session.WorkspaceId.ToString(),
            // The job server has no request to resolve an identity from, so the actor is named here.
            ActorUserId = userId,
            Payload = new ImportCompletedPayload
            {
                RecordType = session.TargetRecordType,
                SourceKind = session.SourceKind.ToString(),
                Created = result.Created,
                Updated = result.Updated,
                Skipped = result.Skipped,
                Failed = result.Failed,
                VendorProfile = session.VendorProfile == ImportVendorProfile.None ? null : session.VendorProfile.ToString(),
            },
        }, cancellationToken);
    }
}
