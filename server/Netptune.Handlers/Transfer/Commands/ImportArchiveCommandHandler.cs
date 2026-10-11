using Mediator;

using Netptune.Core.Models.Search;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Pages;
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
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IEventPublisher EventPublisher;

    public ImportArchiveCommandHandler(
        IArchiveImporter importer,
        IIdentityService identity,
        INetptuneUnitOfWork unitOfWork,
        IEventPublisher eventPublisher)
    {
        Importer = importer;
        Identity = identity;
        UnitOfWork = unitOfWork;
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

            await FinishPages(result, cancellationToken);

            return ClientResponse<ArchiveImportResultViewModel>.Success(ArchiveImportResultViewModel.From(result));
        }
        catch (ArchiveSchemaException exception)
        {
            return ClientResponse<ArchiveImportResultViewModel>.Failed(exception.Message);
        }
    }

    // Links and the search index are derived from page bodies, so they are built once the import has landed
    // rather than carried in the archive.
    private async Task FinishPages(ArchiveImportResult result, CancellationToken cancellationToken)
    {
        if (result.Pages.Count == 0)
        {
            return;
        }

        var sources = result.Pages.Select(page => new PageLinkSource(page.Id, page.Body)).ToList();
        var scope = new PageLinkScope(result.WorkspaceId, result.WorkspaceSlug);
        var pageIds = result.Pages.Select(page => page.Id).ToList();

        await PageLinkRebuilder.BuildForNewPages(UnitOfWork, sources, scope, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);
        await EventPublisher.IndexPages(pageIds, result.WorkspaceSlug);
    }
}
