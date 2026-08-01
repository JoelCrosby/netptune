using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Ai;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class AiConversationRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, AiConversation, Guid>(context, connectionFactory), IAiConversationRepository
{
    public Task<List<AiConversation>> GetForUser(
        string userId,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(conversation =>
                conversation.UserId == userId &&
                conversation.WorkspaceId == workspaceId &&
                !conversation.IsDeleted)
            .OrderByDescending(conversation => conversation.LastMessageAt)
            .ToListAsync(cancellationToken);
    }

    public Task<AiConversation?> GetOwned(
        Guid conversationId,
        string userId,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(conversation =>
                conversation.Id == conversationId &&
                conversation.UserId == userId &&
                conversation.WorkspaceId == workspaceId &&
                !conversation.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<AiConversation?> GetInWorkspace(
        Guid conversationId,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(conversation =>
                conversation.Id == conversationId &&
                conversation.WorkspaceId == workspaceId &&
                !conversation.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<AiWorkspaceConversationViewModel>> GetForWorkspace(
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .Where(conversation => conversation.WorkspaceId == workspaceId && !conversation.IsDeleted)
            .OrderByDescending(conversation => conversation.LastMessageAt)
            .Select(conversation => new AiWorkspaceConversationViewModel
            {
                Id = conversation.Id,
                Title = conversation.Title,
                UserId = conversation.UserId,
                UserDisplayName = string.IsNullOrEmpty(conversation.User.Firstname) && string.IsNullOrEmpty(conversation.User.Lastname)
                    ? conversation.User.UserName!
                    : conversation.User.Firstname + " " + conversation.User.Lastname,
                Provider = conversation.Provider,
                Model = conversation.Model,
                LastMessageAt = conversation.LastMessageAt,
                MessageCount = conversation.MessageCount,
            })
            .ToListAsync(cancellationToken);
    }

    public Task<List<AiMessage>> GetMessages(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return Context.AiMessages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderBy(message => message.Sequence)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextSequence(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var messages = Context.AiMessages.Where(message => message.ConversationId == conversationId);
        var hasMessages = await messages.AnyAsync(cancellationToken);

        if (!hasMessages)
        {
            return 1;
        }

        var highest = await messages.MaxAsync(message => message.Sequence, cancellationToken);

        return highest + 1;
    }

    public async Task AddMessage(AiMessage message, CancellationToken cancellationToken = default)
    {
        await Context.AiMessages.AddAsync(message, cancellationToken);
    }

    public async Task AddToolInvocations(
        IEnumerable<AiToolInvocation> invocations,
        CancellationToken cancellationToken = default)
    {
        await Context.AiToolInvocations.AddRangeAsync(invocations, cancellationToken);
    }
}
