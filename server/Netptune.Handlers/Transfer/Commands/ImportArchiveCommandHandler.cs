using Mediator;

using Netptune.Core.Models.Search;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
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
    private readonly IEventPublisher EventPublisher;

    public ImportArchiveCommandHandler(
        IArchiveImporter importer,
        IIdentityService identity,
        IEventPublisher eventPublisher)
    {
        Importer = importer;
        Identity = identity;
        EventPublisher = eventPublisher;
    }

    public async ValueTask<ClientResponse<ArchiveImportResultViewModel>> Handle(
        ImportArchiveCommand request,
        CancellationToken cancellationToken)
    {
        var built = await request.Request.Resolve(Identity);

        try
        {
            var result = await Importer.Import(built, cancellationToken);

            await IndexPages(result);

            return ClientResponse<ArchiveImportResultViewModel>.Success(ArchiveImportResultViewModel.From(result));
        }
        catch (ArchiveSchemaException exception)
        {
            return ClientResponse<ArchiveImportResultViewModel>.Failed(exception.Message);
        }
    }

    private async Task IndexPages(ArchiveImportResult result)
    {
        if (result.PageIds.Count == 0)
        {
            return;
        }

        await EventPublisher.IndexPages(result.PageIds, result.WorkspaceSlug);
    }
}
