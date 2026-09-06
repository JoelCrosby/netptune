using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetWorkspaceAiConversationsQuery(PageRequest Request)
    : IRequest<ClientResponse<PagedResponse<AiWorkspaceConversationViewModel>>>;

public sealed class GetWorkspaceAiConversationsQueryHandler
    : IRequestHandler<GetWorkspaceAiConversationsQuery, ClientResponse<PagedResponse<AiWorkspaceConversationViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetWorkspaceAiConversationsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<AiWorkspaceConversationViewModel>>> Handle(
        GetWorkspaceAiConversationsQuery query,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var page = await UnitOfWork.AiConversations.GetPageForWorkspace(workspaceId, query.Request, cancellationToken);

        return ClientResponse<PagedResponse<AiWorkspaceConversationViewModel>>.Success(page);
    }
}
