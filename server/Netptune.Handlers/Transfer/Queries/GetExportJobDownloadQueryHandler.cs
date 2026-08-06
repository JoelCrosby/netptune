using Netptune.Transfer.Repositories;
using Netptune.Transfer.Enums;
using Mediator;

using Microsoft.Extensions.Options;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Storage;
using Netptune.Transfer;

namespace Netptune.Handlers.Transfer.Queries;

public sealed record GetExportJobDownloadQuery(Guid PublicId) : IRequest<ClientResponse<Uri>>;

public sealed class GetExportJobDownloadQueryHandler : IRequestHandler<GetExportJobDownloadQuery, ClientResponse<Uri>>
{
    private readonly IExportJobRepository ExportJobs;
    private readonly IIdentityService Identity;
    private readonly IStorageService Storage;
    private readonly TransferOptions Options;

    public GetExportJobDownloadQueryHandler(
        IIdentityService identity,
        IStorageService storage,
        IOptions<TransferOptions> options,
        IExportJobRepository exportJobs)
    {
        Identity = identity;
        Storage = storage;
        Options = options.Value;
        ExportJobs = exportJobs;
    }

    public async ValueTask<ClientResponse<Uri>> Handle(GetExportJobDownloadQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var job = await ExportJobs.GetByPublicId(request.PublicId, workspaceId, true, cancellationToken);

        if (job is null)
        {
            return ClientResponse<Uri>.NotFound;
        }

        var hasArtefact = ExportJobStatuses.HasArtefact(job.Status) && job.StorageKey is not null && job.FileName is not null;

        if (!hasArtefact)
        {
            return ClientResponse<Uri>.Failed($"This export is {job.Status} and has nothing to download.");
        }

        if (job.ExpiresAt <= DateTime.UtcNow)
        {
            return ClientResponse<Uri>.Failed("This export has expired.");
        }

        var readOptions = new StorageReadOptions
        {
            Key = job.StorageKey!,
            FileName = job.FileName!,
            Disposition = StorageDisposition.Attachment,
            Lifetime = TimeSpan.FromMinutes(Options.DownloadUriLifetimeMinutes),
        };
        var uri = await Storage.GetReadUriAsync(readOptions, cancellationToken);

        if (uri is null)
        {
            return ClientResponse<Uri>.NotFound;
        }

        return ClientResponse<Uri>.Success(uri);
    }
}
