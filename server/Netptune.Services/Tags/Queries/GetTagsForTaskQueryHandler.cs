using Mediator;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Services.Tags.Queries;

public sealed record GetTagsForTaskQuery(string SystemId) : IRequest<List<TagViewModel>?>;

public sealed class GetTagsForTaskQueryHandler : IRequestHandler<GetTagsForTaskQuery, List<TagViewModel>?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetTagsForTaskQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<TagViewModel>?> Handle(GetTagsForTaskQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var taskId = await UnitOfWork.Tasks.GetTaskInternalId(request.SystemId, workspaceKey);

        if (taskId is null) return null;

        return await UnitOfWork.Tags.GetViewModelsForTask(taskId.Value, true);
    }
}
