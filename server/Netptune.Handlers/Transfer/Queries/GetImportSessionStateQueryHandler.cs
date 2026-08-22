using System.Text.Json;

using Mediator;

using Netptune.Core.Encoding;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Transfer.Mapping;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.ViewModels;

namespace Netptune.Handlers.Transfer.Queries;

public sealed record GetImportSessionStateQuery(Guid PublicId) : IRequest<ClientResponse<ImportSessionStateViewModel>>;

public sealed class GetImportSessionStateQueryHandler : IRequestHandler<GetImportSessionStateQuery, ClientResponse<ImportSessionStateViewModel>>
{
    private readonly IIdentityService Identity;
    private readonly IImportSessionRepository ImportSessions;

    public GetImportSessionStateQueryHandler(IIdentityService identity, IImportSessionRepository importSessions)
    {
        Identity = identity;
        ImportSessions = importSessions;
    }

    public async ValueTask<ClientResponse<ImportSessionStateViewModel>> Handle(GetImportSessionStateQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var session = await ImportSessions.GetByPublicId(request.PublicId, workspaceId, true, cancellationToken);

        if (session is null)
        {
            return ClientResponse<ImportSessionStateViewModel>.NotFound;
        }

        var viewModel = await ImportSessions.GetViewModel(request.PublicId, workspaceId, cancellationToken);

        if (viewModel is null)
        {
            return ClientResponse<ImportSessionStateViewModel>.NotFound;
        }

        var state = new ImportSessionStateViewModel
        {
            Session = viewModel,
            SourceProfile = session.SourceProfile?.Deserialize<ImportSourceProfile>(JsonOptions.Default),
            Mapping = session.Mapping?.Deserialize<ImportMappingModel>(JsonOptions.Default),
            PreviewResult = session.PreviewResult?.Deserialize<ImportPreviewResult>(JsonOptions.Default),
        };

        return ClientResponse<ImportSessionStateViewModel>.Success(state);
    }
}
