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
using Netptune.Core.Services.Notifications;
using Netptune.Transfer.Services;
using Netptune.Transfer;
using Netptune.Transfer.Mapping;
using Netptune.Core.UnitOfWork;

namespace Netptune.JobServer.Handlers;

public sealed class ImportJobHandler : IRequestHandler<ImportCommitRequestedMessage>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IImportSessionRepository ImportSessions;
    private readonly IImportSourceStore Store;
    private readonly IImportApplier Applier;
    private readonly IEventRecordWriter EventRecords;
    private readonly INotificationDispatcher Notifications;
    private readonly IActorContext Actor;
    private readonly TransferOptions Options;
    private readonly ILogger<ImportJobHandler> Logger;

    public ImportJobHandler(
        INetptuneUnitOfWork unitOfWork,
        IImportSourceStore store,
        IImportApplier applier,
        IEventRecordWriter eventRecords,
        INotificationDispatcher notifications,
        IActorContext actor,
        IOptions<TransferOptions> options,
        ILogger<ImportJobHandler> logger,
        IImportSessionRepository importSessions)
    {
        UnitOfWork = unitOfWork;
        Store = store;
        Applier = applier;
        EventRecords = eventRecords;
        Notifications = notifications;
        Actor = actor;
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

        if (!ImportStages.CanRun(session.Stage))
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

            await Announce(
                session,
                EventKeys.ImportFailed,
                ActivityType.ImportFailed,
                new ImportCompletedPayload
                {
                    RecordType = session.TargetRecordType,
                    SourceKind = session.SourceKind.ToString(),
                    Error = exception.Message,
                    VendorProfile = VendorProfileOf(session),
                },
                request.UserId,
                cancellationToken);
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

        using var actor = Actor.Begin(new ActorIdentity(request.UserId, session.WorkspaceId, workspaceSlug));

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

        await Announce(
            session,
            EventKeys.ImportCompleted,
            ActivityType.ImportCompleted,
            new ImportCompletedPayload
            {
                RecordType = session.TargetRecordType,
                SourceKind = session.SourceKind.ToString(),
                Created = result.Created,
                Updated = result.Updated,
                Skipped = result.Skipped,
                Failed = result.Failed,
                VendorProfile = VendorProfileOf(session),
            },
            request.UserId,
            cancellationToken);

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

    private static string? VendorProfileOf(ImportSession session)
    {
        return session.VendorProfile == ImportVendorProfile.None ? null : session.VendorProfile.ToString();
    }

    private async Task Announce(
        ImportSession session,
        string eventKey,
        ActivityType activityType,
        ImportCompletedPayload payload,
        string userId,
        CancellationToken cancellationToken)
    {
        var record = await EventRecords.Append(new EventWriteRequest<ImportCompletedPayload>
        {
            WorkspaceId = session.WorkspaceId,
            EventKey = eventKey,
            SubjectType = EventEntityTypes.From(EntityType.Workspace),
            SubjectId = session.WorkspaceId.ToString(),
            ActorUserId = userId,
            Payload = payload,
        }, cancellationToken);

        await UnitOfWork.CompleteAsync(cancellationToken);

        await Notifications.Dispatch(new NotificationDispatchRequest
        {
            UserId = userId,
            ActorUserId = userId,
            EventRecordId = record.Id,
            WorkspaceId = session.WorkspaceId,
            EntityType = EntityType.Workspace,
            ActivityType = activityType,
        }, cancellationToken);
    }
}
