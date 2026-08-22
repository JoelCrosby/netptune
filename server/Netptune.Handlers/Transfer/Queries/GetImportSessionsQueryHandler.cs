using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Transfer.Repositories;
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
