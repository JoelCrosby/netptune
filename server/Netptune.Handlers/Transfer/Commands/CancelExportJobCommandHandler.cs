using Netptune.Transfer.Repositories;
using Netptune.Transfer.Enums;
using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Transfer.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.ViewModels;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record CancelExportJobCommand(Guid PublicId) : IRequest<ClientResponse<ExportJobViewModel>>;

public sealed class CancelExportJobCommandHandler : IRequestHandler<CancelExportJobCommand, ClientResponse<ExportJobViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IExportJobRepository ExportJobs;
    private readonly IIdentityService Identity;
    private readonly ITransferJobNotifier Notifier;

    public CancelExportJobCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        ITransferJobNotifier notifier,
        IExportJobRepository exportJobs)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Notifier = notifier;
        ExportJobs = exportJobs;
    }

    public async ValueTask<ClientResponse<ExportJobViewModel>> Handle(CancelExportJobCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var job = await ExportJobs.GetByPublicId(request.PublicId, workspaceId, cancellationToken: cancellationToken);

        if (job is null)
        {
            return ClientResponse<ExportJobViewModel>.NotFound;
        }

        var isCancellable = ExportJobStatuses.CanCancel(job.Status);

        if (!isCancellable)
        {
            return ClientResponse<ExportJobViewModel>.Failed($"An export that is {job.Status} cannot be cancelled.");
        }

        job.Status = ExportJobStatus.Cancelled;
        job.CompletedAt = DateTime.UtcNow;
        job.ProgressMessage = "Cancelled";
        job.ModifiedByUserId = Identity.GetCurrentUserId();

        await UnitOfWork.CompleteAsync(cancellationToken);

        await Notifier.PublishExportAsync(workspaceKey, new ExportJobProgressEvent
        {
            PublicId = job.PublicId,
            Status = job.Status,
            ProgressPercent = job.ProgressPercent,
            ProgressMessage = job.ProgressMessage,
        }, cancellationToken);

        var viewModel = await ExportJobs.GetViewModel(job.PublicId, workspaceId, cancellationToken);

        if (viewModel is null)
        {
            return ClientResponse<ExportJobViewModel>.NotFound;
        }

        return ClientResponse<ExportJobViewModel>.Success(viewModel);
    }
}
