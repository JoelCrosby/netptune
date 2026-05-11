using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Handlers.Tags.Queries;

public sealed record GetTagsForWorkspaceQuery(PageRequest? Page = null) : IRequest<List<TagViewModel>?>;

public sealed class GetTagsForWorkspaceQueryHandler : IRequestHandler<GetTagsForWorkspaceQuery, List<TagViewModel>?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetTagsForWorkspaceQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<TagViewModel>?> Handle(GetTagsForWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return null;

        return await UnitOfWork.Tags.GetViewModelsForWorkspace(workspaceId.Value, cancellationToken, request.Page);
    }
}
