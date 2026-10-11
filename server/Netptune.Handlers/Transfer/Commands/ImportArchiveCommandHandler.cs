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
    private const int IndexBatchSize = 1000;

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

            await IndexImported(result);

            return ClientResponse<ArchiveImportResultViewModel>.Success(ArchiveImportResultViewModel.From(result));
        }
        catch (ArchiveSchemaException exception)
        {
            return ClientResponse<ArchiveImportResultViewModel>.Failed(exception.Message);
        }
    }

    private async Task IndexImported(ArchiveImportResult result)
    {
        var slug = result.WorkspaceSlug;

        await IndexInBatches(result.TaskIds, ids => EventPublisher.IndexTasks(ids, slug));
        await IndexInBatches(result.ProjectIds, ids => EventPublisher.IndexProjects(ids, slug));
        await IndexInBatches(result.BoardIds, ids => EventPublisher.IndexBoards(ids, slug));
        await IndexInBatches(result.SprintIds, ids => EventPublisher.IndexSprints(ids, slug));
        await IndexInBatches(result.PageIds, ids => EventPublisher.IndexPages(ids, slug));
    }

    private static Task IndexInBatches(IReadOnlyList<int> ids, Func<IReadOnlyList<int>, Task> index)
    {
        var batches = ids.Chunk(IndexBatchSize).Select(batch => index(batch));

        return Task.WhenAll(batches);
    }
}
