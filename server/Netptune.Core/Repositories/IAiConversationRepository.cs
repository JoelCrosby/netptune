using Netptune.Core.Entities;
using Netptune.Core.Models.Ai;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Core.Repositories;

public interface IAiConversationRepository : IRepository<AiConversation, Guid>
{
    Task<List<AiConversationViewModel>> GetForUser(string userId, int workspaceId, CancellationToken cancellationToken = default);

    Task<AiConversation?> GetOwned(Guid conversationId, string userId, int workspaceId, CancellationToken cancellationToken = default);

    Task<AiConversation?> GetInWorkspace(Guid conversationId, int workspaceId, CancellationToken cancellationToken = default);

    Task<PagedResponse<AiWorkspaceConversationViewModel>> GetPageForWorkspace(int workspaceId, PageRequest request, CancellationToken cancellationToken = default);

    Task<AiTokenUsageViewModel> GetUsage(Guid conversationId, CancellationToken cancellationToken = default);

    Task<List<AiSpendSlice>> GetSpendSlices(int workspaceId, DateTime fromUtc, CancellationToken cancellationToken = default);

    Task<List<AiModelTokens>> GetModelTokens(int workspaceId, DateTime fromUtc, CancellationToken cancellationToken = default);

    Task<List<AiConversationCount>> GetConversationCounts(int workspaceId, DateTime fromUtc, CancellationToken cancellationToken = default);

    Task<List<AiMessage>> GetMessages(Guid conversationId, CancellationToken cancellationToken = default);

    Task<List<AiToolInvocation>> GetToolInvocations(Guid conversationId, CancellationToken cancellationToken = default);

    Task<int> GetNextSequence(Guid conversationId, CancellationToken cancellationToken = default);

    Task AddMessage(AiMessage message, CancellationToken cancellationToken = default);

    Task<int> RemoveMessagesFrom(Guid conversationId, int sequence, CancellationToken cancellationToken = default);

    Task AddMessageUsage(long messageId, AiUsage usage, CancellationToken cancellationToken = default);

    Task AddToolInvocations(IEnumerable<AiToolInvocation> invocations, CancellationToken cancellationToken = default);
}
