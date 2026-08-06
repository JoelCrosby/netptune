using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Transfer.Services;
using Netptune.Transfer.Archive;
using Netptune.Transfer.ViewModels;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record ImportArchiveRequest
{
    public required Stream Archive { get; init; }

    public ArchiveImportMode Mode { get; init; }

    // Slug for the workspace created by ArchiveImportMode.Clone.
    public string? TargetSlug { get; init; }

    public bool InviteUnmatchedMembers { get; init; }
}

public sealed record PreviewArchiveImportCommand(ImportArchiveRequest Request)
    : IRequest<ClientResponse<ArchiveImportPreviewViewModel>>;

public sealed record ImportArchiveCommand(ImportArchiveRequest Request)
    : IRequest<ClientResponse<ArchiveImportResultViewModel>>;

public sealed class PreviewArchiveImportCommandHandler
    : IRequestHandler<PreviewArchiveImportCommand, ClientResponse<ArchiveImportPreviewViewModel>>
{
    private readonly IArchiveImporter Importer;
    private readonly IIdentityService Identity;

    public PreviewArchiveImportCommandHandler(IArchiveImporter importer, IIdentityService identity)
    {
        Importer = importer;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<ArchiveImportPreviewViewModel>> Handle(
        PreviewArchiveImportCommand request,
        CancellationToken cancellationToken)
    {
        var built = await ArchiveImportRequestFactory.Build(request.Request, Identity);

        try
        {
            var preview = await Importer.Preview(built, cancellationToken);

            return ClientResponse<ArchiveImportPreviewViewModel>.Success(ArchiveImportPreviewViewModel.From(preview));
        }
        catch (ArchiveSchemaException exception)
        {
            // A malformed or incompatible archive is the caller's problem, not a server fault.
            return ClientResponse<ArchiveImportPreviewViewModel>.Failed(exception.Message);
        }
    }
}

public sealed class ImportArchiveCommandHandler
    : IRequestHandler<ImportArchiveCommand, ClientResponse<ArchiveImportResultViewModel>>
{
    private readonly IArchiveImporter Importer;
    private readonly IIdentityService Identity;

    public ImportArchiveCommandHandler(IArchiveImporter importer, IIdentityService identity)
    {
        Importer = importer;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<ArchiveImportResultViewModel>> Handle(
        ImportArchiveCommand request,
        CancellationToken cancellationToken)
    {
        var built = await ArchiveImportRequestFactory.Build(request.Request, Identity);

        try
        {
            var result = await Importer.Import(built, cancellationToken);

            return ClientResponse<ArchiveImportResultViewModel>.Success(ArchiveImportResultViewModel.From(result));
        }
        catch (ArchiveSchemaException exception)
        {
            return ClientResponse<ArchiveImportResultViewModel>.Failed(exception.Message);
        }
    }
}

internal static class ArchiveImportRequestFactory
{
    public static async Task<ArchiveImportRequest> Build(ImportArchiveRequest request, IIdentityService identity)
    {
        return new ArchiveImportRequest
        {
            Archive = request.Archive,
            UserId = identity.GetCurrentUserId(),
            Mode = request.Mode,
            WorkspaceId = await identity.GetWorkspaceId(),
            TargetSlug = request.TargetSlug,
            InviteUnmatchedMembers = request.InviteUnmatchedMembers,
        };
    }
}
