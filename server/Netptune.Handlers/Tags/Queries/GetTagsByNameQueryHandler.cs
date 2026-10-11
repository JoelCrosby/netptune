using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Handlers.Tags.Queries;

public sealed record GetTagsByNameQuery(IReadOnlyCollection<string> Names) : IRequest<List<TagViewModel>>;

public sealed class GetTagsByNameQueryHandler : IRequestHandler<GetTagsByNameQuery, List<TagViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetTagsByNameQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<List<TagViewModel>> Handle(GetTagsByNameQuery request, CancellationToken cancellationToken)
    {
        if (request.Names.Count == 0)
        {
            return [];
        }

        var workspaceId = await Identity.GetWorkspaceId();

        return await UnitOfWork.Tags.GetViewModelsByNames(workspaceId, request.Names, cancellationToken);
    }
}
