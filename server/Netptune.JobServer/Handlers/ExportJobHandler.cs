using Netptune.Transfer.Repositories;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Entities;
using System.Text.Json;

using Mediator;

using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Transfer.Messages;
using Netptune.Core.Services;
using Netptune.Core.Services.Notifications;
using Netptune.Transfer.Services;
using Netptune.Core.Storage;
using Netptune.Transfer.Definitions;
using Netptune.Core.UnitOfWork;

namespace Netptune.JobServer.Handlers;

public sealed class ExportJobHandler : IRequestHandler<ExportJobRequestedMessage>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IExportJobRepository ExportJobs;
    private readonly IExportRunner Runner;
    private readonly IStorageService Storage;
    private readonly ITransferJobNotifier Notifier;
    private readonly IEventRecordWriter EventRecords;
    private readonly INotificationDispatcher Notifications;
    private readonly IActorContext Actor;
    private readonly ILogger<ExportJobHandler> Logger;

    public ExportJobHandler(
        INetptuneUnitOfWork unitOfWork,
        IExportRunner runner,
        IStorageService storage,
        ITransferJobNotifier notifier,
        IEventRecordWriter eventRecords,
        INotificationDispatcher notifications,
        IActorContext actor,
        ILogger<ExportJobHandler> logger,
        IExportJobRepository exportJobs)
    {
        UnitOfWork = unitOfWork;
        Runner = runner;
        Storage = storage;
        Notifier = notifier;
        EventRecords = eventRecords;
        Notifications = notifications;
        Actor = actor;
        Logger = logger;
        ExportJobs = exportJobs;
    }

    public async ValueTask<Unit> Handle(ExportJobRequestedMessage request, CancellationToken cancellationToken)
    {
        var job = await ExportJobs.GetForProcessing(request.ExportJobId, cancellationToken);

        if (job is null)
        {
            Logger.LogWarning("[Export] job {JobId} no longer exists", request.ExportJobId);

            return default;
        }

        if (!ExportJobStatuses.CanRun(job.Status))
        {
            Logger.LogInformation("[Export] job {PublicId} is {Status} and will not be run", job.PublicId, job.Status);

            return default;
        }

        var workspaceSlug = job.Workspace?.Slug;

        if (workspaceSlug is null)
        {
            await Fail(job, null, request.UserId, "The workspace could not be resolved.", cancellationToken);

            return default;
        }

        using var actor = Actor.Begin(new ActorIdentity(request.UserId, job.WorkspaceId, workspaceSlug));

        try
        {
            await Run(job, workspaceSlug, request.UserId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ExportJobCancelledException)
        {
            // The API already wrote Cancelled. Nothing to save — the tracked job still says Running and
            // flushing it here would undo the cancellation.
            Logger.LogInformation("[Export] job {PublicId} was cancelled while running", job.PublicId);
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "[Export] job {PublicId} failed", job.PublicId);

            await Fail(job, workspaceSlug, request.UserId, exception.Message, cancellationToken);
        }

        return default;
    }

    private async Task ThrowIfCancelled(ExportJob job, CancellationToken cancellationToken)
    {
        var status = await ExportJobs.GetStatus(job.Id, cancellationToken);

        if (status == ExportJobStatus.Cancelled)
        {
            throw new ExportJobCancelledException();
        }
    }

    private async Task Run(ExportJob job, string workspaceSlug, string userId, CancellationToken cancellationToken)
    {
        job.Status = ExportJobStatus.Running;
        job.StartedAt = DateTime.UtcNow;
        job.ProgressPercent = 0;
        job.ProgressMessage = "Starting";

        await Save(job, workspaceSlug, cancellationToken);

        var definition = job.Definition.Deserialize<ExportDefinitionModel>(JsonOptions.Default)
            ?? throw new InvalidOperationException("The export definition could not be read.");

        var runRequest = new ExportRunRequest
        {
            WorkspaceId = job.WorkspaceId,
            WorkspaceSlug = workspaceSlug,
            Definition = definition,
        };

        async Task ReportProgress(ExportRunProgress update, CancellationToken token)
        {
            await ThrowIfCancelled(job, token);

            job.ProgressPercent = update.Percent;
            job.ProgressMessage = update.Message;

            await Save(job, workspaceSlug, token);
        }

        var result = await Runner.Run(runRequest, ReportProgress, cancellationToken);

        await using var content = result.Content;

        // Last look before the artefact costs the workspace anything. A cancellation that lands after
        // the final progress report would otherwise be overwritten by the Succeeded write below.
        await ThrowIfCancelled(job, cancellationToken);

        var sizeBytes = content.Length;
        var reserved = await UnitOfWork.Workspaces.TryReserveStorage(job.WorkspaceId, sizeBytes, cancellationToken);

        if (!reserved)
        {
            await Fail(job, workspaceSlug, userId, $"Workspace storage limit exceeded ({sizeBytes} bytes requested).", cancellationToken);

            return;
        }

        var storageKey = $"{PathConstants.ExportPath(workspaceSlug)}{job.PublicId:N}/{result.FileName}";

        // The quota is already charged, so anything that goes wrong from here has to give it back —
        // including a thrown upload, which would otherwise leak the reservation for good.
        try
        {
            var uploaded = await Upload(content, storageKey, result, cancellationToken);

            if (!uploaded)
            {
                throw new InvalidOperationException("The export artefact could not be uploaded.");
            }
        }
        catch
        {
            await UnitOfWork.Workspaces.ReleaseStorage(job.WorkspaceId, sizeBytes, CancellationToken.None);

            throw;
        }

        job.Status = ExportJobStatus.Succeeded;
        job.StorageKey = storageKey;
        job.FileName = result.FileName;
        job.ContentType = result.ContentType;
        job.RowCount = result.RowCount;
        job.SizeBytes = sizeBytes;
        job.ProgressPercent = 100;
        job.ProgressMessage = "Complete";
        job.CompletedAt = DateTime.UtcNow;

        await Save(job, workspaceSlug, cancellationToken);

        await Announce(
            job,
            EventKeys.ExportCompleted,
            ActivityType.ExportCompleted,
            new ExportCompletedPayload
            {
                Format = job.Format.ToString(),
                FileName = job.FileName,
                RowCount = job.RowCount,
                SizeBytes = job.SizeBytes,
            },
            userId,
            cancellationToken);

        Logger.LogInformation("[Export] job {PublicId} produced {RowCount} rows", job.PublicId, result.RowCount);
    }

    private async Task<bool> Upload(Stream content, string storageKey, ExportRunResult result, CancellationToken cancellationToken)
    {
        content.Seek(0, SeekOrigin.Begin);

        var uploadOptions = new StorageUploadOptions
        {
            Name = result.FileName,
            Key = storageKey,
            ContentType = result.ContentType,
            Access = StorageAccess.Private,
        };
        var response = await Storage.UploadFileAsync(content, uploadOptions, cancellationToken);

        return response.IsSuccess;
    }

    private async Task Fail(ExportJob job, string? workspaceSlug, string userId, string error, CancellationToken cancellationToken)
    {
        job.Status = ExportJobStatus.Failed;
        job.Error = error;
        job.ProgressMessage = "Failed";
        job.CompletedAt = DateTime.UtcNow;

        await Save(job, workspaceSlug, cancellationToken);

        await Announce(
            job,
            EventKeys.ExportFailed,
            ActivityType.ExportFailed,
            new ExportCompletedPayload
            {
                Format = job.Format.ToString(),
                Error = error,
            },
            userId,
            cancellationToken);
    }

    private async Task Announce(
        ExportJob job,
        string eventKey,
        ActivityType activityType,
        ExportCompletedPayload payload,
        string userId,
        CancellationToken cancellationToken)
    {
        var record = await EventRecords.Append(new EventWriteRequest<ExportCompletedPayload>
        {
            WorkspaceId = job.WorkspaceId,
            EventKey = eventKey,
            SubjectType = EventEntityTypes.From(EntityType.Workspace),
            SubjectId = job.WorkspaceId.ToString(),
            ActorUserId = userId,
            Payload = payload,
        }, cancellationToken);

        await UnitOfWork.CompleteAsync(cancellationToken);

        await Notifications.Dispatch(new NotificationDispatchRequest
        {
            UserId = userId,
            ActorUserId = userId,
            EventRecordId = record.Id,
            WorkspaceId = job.WorkspaceId,
            EntityType = EntityType.Workspace,
            ActivityType = activityType,
        }, cancellationToken);
    }

    private async Task Save(ExportJob job, string? workspaceSlug, CancellationToken cancellationToken)
    {
        await UnitOfWork.CompleteAsync(cancellationToken);

        if (workspaceSlug is null)
        {
            return;
        }

        var progressEvent = new ExportJobProgressEvent
        {
            PublicId = job.PublicId,
            Status = job.Status,
            ProgressPercent = job.ProgressPercent,
            ProgressMessage = job.ProgressMessage,
            Error = job.Error,
        };

        await Notifier.PublishExportAsync(workspaceSlug, progressEvent, cancellationToken);
    }
}

// Unwinds a run whose job was cancelled from the API. Distinct from OperationCanceledException, which
// means the job server itself is shutting down and the job should be picked up again.
public sealed class ExportJobCancelledException : Exception
{
    public ExportJobCancelledException() : base("The export job was cancelled.")
    {
    }
}
