using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Storage;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Storage.Queries;

public sealed record GetWorkspaceFileContentQuery : IRequest<ClientResponse<Uri>>
{
    public required string ContentId { get; init; }

    public string? Disposition { get; init; }

    public bool CanReadTasks { get; init; }
}

public sealed class GetWorkspaceFileContentQueryHandler : IRequestHandler<GetWorkspaceFileContentQuery, ClientResponse<Uri>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IStorageService Storage;

    public GetWorkspaceFileContentQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IStorageService storage)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Storage = storage;
    }

    public async ValueTask<ClientResponse<Uri>> Handle(GetWorkspaceFileContentQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var entity = await UnitOfWork.WorkspaceFiles.GetByContentId(request.ContentId, workspaceId, isReadonly: true, cancellationToken);

        if (entity is null || entity.Status != WorkspaceFileStatus.Ready)
        {
            return ClientResponse<Uri>.NotFound;
        }

        var isTaskFile = await UnitOfWork.TaskFiles.ExistsByWorkspaceFileId(entity.Id, cancellationToken);

        if (isTaskFile && !request.CanReadTasks)
        {
            return ClientResponse<Uri>.Forbidden;
        }

        var requestedInline = string.Equals(request.Disposition, "inline", StringComparison.OrdinalIgnoreCase);
        var safeInline = requestedInline && (entity.ContentType.StartsWith("image/") || entity.ContentType == "application/pdf");
        var readOptions = new StorageReadOptions
        {
            Key = entity.StorageKey,
            FileName = entity.OriginalName,
            Disposition = safeInline ? StorageDisposition.Inline : StorageDisposition.Attachment,
            Lifetime = TimeSpan.FromMinutes(5),
        };

        var uri = await Storage.GetReadUriAsync(readOptions, cancellationToken);

        return uri is null ? ClientResponse<Uri>.NotFound : ClientResponse<Uri>.Success(uri);
    }
}
