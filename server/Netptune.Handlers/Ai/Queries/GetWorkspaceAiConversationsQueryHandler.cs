using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetWorkspaceAiConversationsQuery : IRequest<List<AiWorkspaceConversationViewModel>>;

public sealed class GetWorkspaceAiConversationsQueryHandler
    : IRequestHandler<GetWorkspaceAiConversationsQuery, List<AiWorkspaceConversationViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetWorkspaceAiConversationsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<AiWorkspaceConversationViewModel>> Handle(
        GetWorkspaceAiConversationsQuery query,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();

        return await UnitOfWork.AiConversations.GetForWorkspace(workspaceId, cancellationToken);
    }
}
