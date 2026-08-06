using Mediator;
using Netptune.Core.Services;
using Netptune.Transfer.Repositories;
using Netptune.Transfer.ViewModels;

namespace Netptune.Handlers.Transfer.Queries;

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
