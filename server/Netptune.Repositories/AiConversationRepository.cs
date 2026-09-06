using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Models.Ai;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
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

    public Task<List<AiSpendSlice>> GetSpendSlices(
        int workspaceId,
        DateTime fromUtc,
        CancellationToken cancellationToken = default)
    {
        return BilledMessages(workspaceId, fromUtc)
            .GroupBy(message => new
            {
                message.Conversation.UserId,
                DisplayName = string.IsNullOrEmpty(message.Conversation.User.Firstname) && string.IsNullOrEmpty(message.Conversation.User.Lastname)
                    ? message.Conversation.User.UserName!
                    : message.Conversation.User.Firstname + " " + message.Conversation.User.Lastname,
                message.Model,
                Day = message.CreatedAt.Date,
            })
            .Select(group => new AiSpendSlice
            {
                UserId = group.Key.UserId,
                UserDisplayName = group.Key.DisplayName,
                Model = group.Key.Model,
                Day = group.Key.Day,
                InputTokens = group.Sum(message => message.InputTokens),
                OutputTokens = group.Sum(message => message.OutputTokens),
                CacheReadTokens = group.Sum(message => message.CacheReadTokens),
                CacheCreationTokens = group.Sum(message => message.CacheCreationTokens),
            })
            .ToListAsync(cancellationToken);
    }

    public Task<List<AiModelTokens>> GetModelTokens(
        int workspaceId,
        DateTime fromUtc,
        CancellationToken cancellationToken = default)
    {
        return BilledMessages(workspaceId, fromUtc)
            .GroupBy(message => message.Model)
            .Select(group => new AiModelTokens
            {
                Model = group.Key,
                InputTokens = group.Sum(message => message.InputTokens),
                OutputTokens = group.Sum(message => message.OutputTokens),
                CacheReadTokens = group.Sum(message => message.CacheReadTokens),
                CacheCreationTokens = group.Sum(message => message.CacheCreationTokens),
            })
            .ToListAsync(cancellationToken);
    }

    public Task<List<AiConversationCount>> GetConversationCounts(
        int workspaceId,
        DateTime fromUtc,
        CancellationToken cancellationToken = default)
    {
        return BilledMessages(workspaceId, fromUtc)
            .GroupBy(message => message.Conversation.UserId)
            .Select(group => new AiConversationCount(
                group.Key,
                group.Select(message => message.ConversationId).Distinct().Count()))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<AiMessage> BilledMessages(int workspaceId, DateTime fromUtc)
    {
        return Context.AiMessages
            .AsNoTracking()
            .Where(message =>
                message.Conversation.WorkspaceId == workspaceId &&
                !message.Conversation.IsDeleted &&
                message.CreatedAt >= fromUtc);
    }

    public async Task<PagedResponse<AiWorkspaceConversationViewModel>> GetPageForWorkspace(
        int workspaceId,
        PageRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = Entities
            .AsNoTracking()
            .Where(conversation => conversation.WorkspaceId == workspaceId && !conversation.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);
        var pagination = request.GetPagination();

        var conversations = await ProjectWorkspaceConversations(SortWorkspaceConversations(query, request))
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var items = conversations
            .Select(conversation => conversation with { Usage = conversation.Usage.WithCost(conversation.Model) })
            .ToList();

        return new PagedResponse<AiWorkspaceConversationViewModel>(items, pagination.Page, pagination.PageSize, totalCount);
    }

    private static IQueryable<AiWorkspaceConversationViewModel> ProjectWorkspaceConversations(IQueryable<AiConversation> query)
    {
        return query.Select(conversation => new AiWorkspaceConversationViewModel
        {
            Id = conversation.Id,
            Title = conversation.Title,
            UserId = conversation.UserId,
            UserDisplayName = string.IsNullOrEmpty(conversation.User.Firstname) && string.IsNullOrEmpty(conversation.User.Lastname)
                ? conversation.User.UserName!
                : conversation.User.Firstname + " " + conversation.User.Lastname,
            UserPictureUrl = conversation.User.PictureUrl,
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
        });
    }

    // Cost is not sortable because it is derived from the model price list after the
    // page has been read.
    private static IQueryable<AiConversation> SortWorkspaceConversations(IQueryable<AiConversation> query, PageRequest request)
    {
        var isDescending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        IOrderedQueryable<AiConversation> Order<TKey>(Expression<Func<AiConversation, TKey>> key)
        {
            return isDescending ? query.OrderByDescending(key) : query.OrderBy(key);
        }

        return request.SortBy?.ToLowerInvariant() switch
        {
            "title" => Order(conversation => conversation.Title),
            "user" => Order(conversation => string.IsNullOrEmpty(conversation.User.Firstname) && string.IsNullOrEmpty(conversation.User.Lastname)
                ? conversation.User.UserName!
                : conversation.User.Firstname + " " + conversation.User.Lastname),
            "messagecount" => Order(conversation => conversation.MessageCount),
            "tokens" => Order(conversation => conversation.Messages.Sum(message =>
                message.InputTokens + message.OutputTokens + message.CacheReadTokens + message.CacheCreationTokens)),
            "lastmessageat" => Order(conversation => conversation.LastMessageAt),
            _ => query.OrderByDescending(conversation => conversation.LastMessageAt),
        };
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
