using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Core.Repositories;

public interface IAiConversationRepository : IRepository<AiConversation, Guid>
{
    Task<List<AiConversationViewModel>> GetForUser(string userId, int workspaceId, CancellationToken cancellationToken = default);

    Task<AiConversation?> GetOwned(Guid conversationId, string userId, int workspaceId, CancellationToken cancellationToken = default);

    Task<AiConversation?> GetInWorkspace(Guid conversationId, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<AiWorkspaceConversationViewModel>> GetForWorkspace(int workspaceId, CancellationToken cancellationToken = default);

    Task<List<AiMessage>> GetMessages(Guid conversationId, CancellationToken cancellationToken = default);

    Task<int> GetNextSequence(Guid conversationId, CancellationToken cancellationToken = default);

    Task AddMessage(AiMessage message, CancellationToken cancellationToken = default);

    Task AddToolInvocations(IEnumerable<AiToolInvocation> invocations, CancellationToken cancellationToken = default);
}
