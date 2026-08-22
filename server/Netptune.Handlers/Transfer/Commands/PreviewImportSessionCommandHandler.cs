using System.Text.Json;

using Mediator;

using Microsoft.Extensions.Options;

using Netptune.Core.Encoding;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Mapping;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.Services;

namespace Netptune.Handlers.Transfer.Commands;

public sealed record PreviewImportSessionCommand(Guid PublicId) : IRequest<ClientResponse<ImportPreviewResult>>;

public sealed class PreviewImportSessionCommandHandler : IRequestHandler<PreviewImportSessionCommand, ClientResponse<ImportPreviewResult>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IImportSourceStore Store;
    private readonly IImportApplier Applier;
    private readonly TransferOptions Options;
    private readonly IImportSessionRepository ImportSessions;

    public PreviewImportSessionCommandHandler(
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

    public async ValueTask<ClientResponse<ImportPreviewResult>> Handle(PreviewImportSessionCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var session = await ImportSessions.GetByPublicId(request.PublicId, workspaceId, cancellationToken: cancellationToken);

        if (session is null)
        {
            return ClientResponse<ImportPreviewResult>.NotFound;
        }

        var isPreviewable = ImportStages.CanPreview(session.Stage);

        if (!isPreviewable)
        {
            return ClientResponse<ImportPreviewResult>.Failed(
                $"An import that is {session.Stage} cannot be previewed.");
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
