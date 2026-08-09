using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetAiWebDocumentQuery(Guid Id) : IRequest<AiWebDocument?>;

public sealed class GetAiWebDocumentQueryHandler : IRequestHandler<GetAiWebDocumentQuery, AiWebDocument?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAiWebDocumentQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<AiWebDocument?> Handle(GetAiWebDocumentQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null)
        {
            return null;
        }

        var document = await UnitOfWork.AiWebDocuments.GetInWorkspace(request.Id, workspaceId.Value, cancellationToken);

        return document;
    }
}
