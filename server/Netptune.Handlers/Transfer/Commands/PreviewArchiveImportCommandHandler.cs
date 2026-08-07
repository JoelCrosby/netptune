using Mediator;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Transfer.Archive;
using Netptune.Transfer.Services;
using Netptune.Transfer.ViewModels;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record PreviewArchiveImportCommand(ImportArchiveRequest Request)
    : IRequest<ClientResponse<ArchiveImportPreviewViewModel>>;

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
            return ClientResponse<ArchiveImportPreviewViewModel>.Failed(exception.Message);
        }
    }
}
