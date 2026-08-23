using Mediator;

using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Queries;

public sealed record GetAiConversationChangeSetsQuery(Guid ConversationId) : IRequest<List<AiChangeSetViewModel>>;

public sealed class GetAiConversationChangeSetsQueryHandler
    : IRequestHandler<GetAiConversationChangeSetsQuery, List<AiChangeSetViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiUndoCatalog UndoCatalog;

    public GetAiConversationChangeSetsQueryHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiUndoCatalog undoCatalog)
    {
        UndoCatalog = undoCatalog;
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<AiChangeSetViewModel>> Handle(
        GetAiConversationChangeSetsQuery query,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();
        var changeSets = await UnitOfWork.AiChangeSets.GetForConversation(
            query.ConversationId,
            userId,
            workspaceId,
            cancellationToken);

        if (changeSets.Count == 0)
        {
            return [];
        }

        var changeSetIds = changeSets.Select(changeSet => changeSet.Id).ToList();
        var changes = await UnitOfWork.AiChangeSets.GetChanges(changeSetIds, cancellationToken);
        var changesByChangeSet = changes
            .GroupBy(change => change.ChangeSetId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var taskIds = AiChangeSetMapper.CollectTaskIds(changes);
        var tasks = await UnitOfWork.Tasks.GetTaskViewModels(taskIds, cancellationToken);
        var models = new List<AiChangeSetViewModel>(changeSets.Count);

        foreach (var changeSet in changeSets)
        {
            var hasChanges = changesByChangeSet.TryGetValue(changeSet.Id, out var changeSetChanges);
            var model = AiChangeSetMapper.ToViewModel(
                changeSet,
                hasChanges ? changeSetChanges! : [],
                tasks,
                UndoCatalog);

            models.Add(model);
        }

        return models;
    }
}
