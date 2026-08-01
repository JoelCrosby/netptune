using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetAiConversationsQuery : IRequest<List<AiConversationViewModel>>;

public sealed class GetAiConversationsQueryHandler
    : IRequestHandler<GetAiConversationsQuery, List<AiConversationViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetAiConversationsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<AiConversationViewModel>> Handle(
        GetAiConversationsQuery query,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();
        var conversations = await UnitOfWork.AiConversations.GetForUser(userId, workspaceId, cancellationToken);

        return conversations.Select(ToViewModel).ToList();
    }

    public static AiConversationViewModel ToViewModel(AiConversation conversation)
    {
        return new AiConversationViewModel
        {
            Id = conversation.Id,
            Title = conversation.Title,
            Provider = conversation.Provider,
            Model = conversation.Model,
            LastMessageAt = conversation.LastMessageAt,
            MessageCount = conversation.MessageCount,
        };
    }
}
