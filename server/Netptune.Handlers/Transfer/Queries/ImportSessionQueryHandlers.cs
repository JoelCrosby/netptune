using Netptune.Transfer.Repositories;
using Netptune.Transfer.Enums;
using System.Text.Json;

using Mediator;

using Microsoft.Extensions.Options;

using Netptune.Core.Encoding;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Transfer.Services;
using Netptune.Transfer;
using Netptune.Transfer.Import;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.ViewModels;

namespace Netptune.Handlers.Transfer.Queries;

public sealed record GetImportSessionsQuery(PageRequest Page) : IRequest<ClientResponse<PagedResponse<ImportSessionViewModel>>>;

public sealed class GetImportSessionsQueryHandler : IRequestHandler<GetImportSessionsQuery, ClientResponse<PagedResponse<ImportSessionViewModel>>>
{
    private readonly IImportSessionRepository ImportSessions;
    private readonly IIdentityService Identity;

    public GetImportSessionsQueryHandler(IIdentityService identity, IImportSessionRepository importSessions)
    {
        Identity = identity;
        ImportSessions = importSessions;
    }

    public async ValueTask<ClientResponse<PagedResponse<ImportSessionViewModel>>> Handle(GetImportSessionsQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var sessions = await ImportSessions.GetSessions(workspaceId, request.Page, cancellationToken);

        return ClientResponse<PagedResponse<ImportSessionViewModel>>.Success(sessions);
    }
}

public sealed record GetImportSessionQuery(Guid PublicId) : IRequest<ImportSessionViewModel?>;

public sealed class GetImportSessionQueryHandler : IRequestHandler<GetImportSessionQuery, ImportSessionViewModel?>
{
    private readonly IIdentityService Identity;
    private readonly IImportSessionRepository ImportSessions;

    public GetImportSessionQueryHandler(IIdentityService identity, IImportSessionRepository importSessions)
    {
        Identity = identity;
        ImportSessions = importSessions;
    }

    public async ValueTask<ImportSessionViewModel?> Handle(GetImportSessionQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();

        return await ImportSessions.GetViewModel(request.PublicId, workspaceId, cancellationToken);
    }
}

public sealed record PreviewImportSessionQuery(Guid PublicId) : IRequest<ClientResponse<ImportPreviewResult>>;

public sealed class PreviewImportSessionQueryHandler : IRequestHandler<PreviewImportSessionQuery, ClientResponse<ImportPreviewResult>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IImportSourceStore Store;
    private readonly IImportApplier Applier;
    private readonly TransferOptions Options;
    private readonly IImportSessionRepository ImportSessions;

    public PreviewImportSessionQueryHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IImportSourceStore store,
        IImportApplier applier,
        IOptions<TransferOptions> options,
        IImportSessionRepository importSessions)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Store = store;
        Applier = applier;
        Options = options.Value;
        ImportSessions = importSessions;
    }

    public async ValueTask<ClientResponse<ImportPreviewResult>> Handle(PreviewImportSessionQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var session = await ImportSessions.GetByPublicId(request.PublicId, workspaceId, cancellationToken: cancellationToken);

        if (session is null)
        {
            return ClientResponse<ImportPreviewResult>.NotFound;
        }

        var mapping = session.Mapping?.Deserialize<ImportMappingModel>(JsonOptions.Default);

        if (mapping is null)
        {
            return ClientResponse<ImportPreviewResult>.Failed("Map the file before previewing it.");
        }

        var profile = session.SourceProfile?.Deserialize<ImportSourceProfile>(JsonOptions.Default);

        await using var source = await Store.Open(session, cancellationToken);

        var applyRequest = new ImportApplyRequest
        {
            WorkspaceId = workspaceId,
            WorkspaceSlug = workspaceKey,
            UserId = Identity.GetCurrentUserId(),
            Session = session,
            Mapping = mapping,
            Source = source,
            ColumnNames = profile?.Columns.Select(column => column.Name).ToList() ?? [],
            ReadOptions = new ImportReadOptions
            {
                Delimiter = profile?.Delimiter,
                HasHeaderRow = profile?.HasHeaderRow ?? true,
            },
            PreviewRowCap = Options.PreviewRowCap,
        };
        var preview = await Applier.Preview(applyRequest, cancellationToken);

        session.PreviewResult = JsonSerializer.SerializeToDocument(preview, JsonOptions.Default);
        session.Stage = ImportStage.Previewed;

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse<ImportPreviewResult>.Success(preview);
    }
}
