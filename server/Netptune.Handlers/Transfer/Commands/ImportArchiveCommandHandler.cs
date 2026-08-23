using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Transfer.Archive;
using Netptune.Transfer.Services;
using Netptune.Transfer.ViewModels;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record ImportArchiveCommand(ImportArchiveRequest Request)
    : IRequest<ClientResponse<ArchiveImportResultViewModel>>;

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
        var built = await request.Request.Resolve(Identity);

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
