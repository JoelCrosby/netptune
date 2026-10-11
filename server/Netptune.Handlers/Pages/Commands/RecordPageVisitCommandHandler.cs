using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Pages.Commands;

public sealed record RecordPageVisitCommand(int Id) : IRequest<ClientResponse>;

public sealed class RecordPageVisitCommandHandler : IRequestHandler<RecordPageVisitCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public RecordPageVisitCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(RecordPageVisitCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var page = await UnitOfWork.Pages.GetLiveInWorkspace(request.Id, workspaceId, true, cancellationToken);

        if (page is null)
        {
            return ClientResponse.NotFound;
        }

        await UnitOfWork.PageVisits.Record(page.Id, userId, cancellationToken);

        return ClientResponse.Success;
    }
}
