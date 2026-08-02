using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Models.Ai;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Ai;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;

namespace Netptune.Repositories;

public class AiConversationRepository(DataContext context, IDbConnectionFactory connectionFactory)
    : Repository<DataContext, AiConversation, Guid>(context, connectionFactory), IAiConversationRepository
{
    public async Task<List<AiConversationViewModel>> GetForUser(
        string userId,
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        var conversations = await Entities
            .AsNoTracking()
            .Where(conversation =>
                conversation.UserId == userId &&
                conversation.WorkspaceId == workspaceId &&
                !conversation.IsDeleted)
            .OrderByDescending(conversation => conversation.LastMessageAt)
            .Select(conversation => new AiConversationViewModel
            {
                Id = conversation.Id,
                Title = conversation.Title,
                Provider = conversation.Provider,
                Model = conversation.Model,
                LastMessageAt = conversation.LastMessageAt,
                MessageCount = conversation.MessageCount,
                Usage = new AiTokenUsageViewModel
                {
                    InputTokens = conversation.Messages.Sum(message => message.InputTokens),
                    OutputTokens = conversation.Messages.Sum(message => message.OutputTokens),
                    CacheReadTokens = conversation.Messages.Sum(message => message.CacheReadTokens),
                    CacheCreationTokens = conversation.Messages.Sum(message => message.CacheCreationTokens),
                },
            })
            .ToListAsync(cancellationToken);

        return conversations
            .Select(conversation => conversation with { Usage = conversation.Usage.WithCost(conversation.Model) })
            .ToList();
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

    public async Task<AiTokenUsageViewModel> GetUsage(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var messages = Context.AiMessages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId);

        var usage = await messages
            .GroupBy(message => message.ConversationId)
            .Select(group => new AiTokenUsageViewModel
            {
                InputTokens = group.Sum(message => message.InputTokens),
                OutputTokens = group.Sum(message => message.OutputTokens),
                CacheReadTokens = group.Sum(message => message.CacheReadTokens),
                CacheCreationTokens = group.Sum(message => message.CacheCreationTokens),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return usage ?? new AiTokenUsageViewModel();
    }

    public async Task<List<AiWorkspaceConversationViewModel>> GetForWorkspace(
        int workspaceId,
        CancellationToken cancellationToken = default)
    {
        var conversations = await Entities
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
                Usage = new AiTokenUsageViewModel
                {
                    InputTokens = conversation.Messages.Sum(message => message.InputTokens),
                    OutputTokens = conversation.Messages.Sum(message => message.OutputTokens),
                    CacheReadTokens = conversation.Messages.Sum(message => message.CacheReadTokens),
                    CacheCreationTokens = conversation.Messages.Sum(message => message.CacheCreationTokens),
                },
            })
            .ToListAsync(cancellationToken);

        return conversations
            .Select(conversation => conversation with { Usage = conversation.Usage.WithCost(conversation.Model) })
            .ToList();
    }

    public Task<List<AiMessage>> GetMessages(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return Context.AiMessages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderBy(message => message.Sequence)
            .ToListAsync(cancellationToken);
    }

    public Task<List<AiToolInvocation>> GetToolInvocations(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        return Context.AiToolInvocations
            .AsNoTracking()
            .Where(invocation => invocation.ConversationId == conversationId)
            .OrderBy(invocation => invocation.Id)
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

    public async Task<int> RemoveMessagesFrom(
        Guid conversationId,
        int sequence,
        CancellationToken cancellationToken = default)
    {
        var doomed = Context.AiMessages
            .Where(message => message.ConversationId == conversationId)
            .Where(message => message.Sequence >= sequence);

        var messageIds = await doomed.Select(message => message.Id).ToListAsync(cancellationToken);

        if (messageIds.Count == 0)
        {
            return 0;
        }

        await Context.AiToolInvocations
            .Where(invocation => messageIds.Contains(invocation.MessageId))
            .ExecuteDeleteAsync(cancellationToken);

        return await doomed.ExecuteDeleteAsync(cancellationToken);
    }

    public Task AddMessageUsage(long messageId, AiUsage usage, CancellationToken cancellationToken = default)
    {
        return Context.AiMessages
            .Where(message => message.Id == messageId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.InputTokens, message => message.InputTokens + usage.InputTokens)
                    .SetProperty(message => message.OutputTokens, message => message.OutputTokens + usage.OutputTokens)
                    .SetProperty(message => message.CacheReadTokens, message => message.CacheReadTokens + usage.CacheReadTokens)
                    .SetProperty(message => message.CacheCreationTokens, message => message.CacheCreationTokens + usage.CacheCreationTokens),
                cancellationToken);
    }

    public async Task AddToolInvocations(
        IEnumerable<AiToolInvocation> invocations,
        CancellationToken cancellationToken = default)
    {
        await Context.AiToolInvocations.AddRangeAsync(invocations, cancellationToken);
    }
}
