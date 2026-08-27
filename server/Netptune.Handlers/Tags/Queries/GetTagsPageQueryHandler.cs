using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Handlers.Tags.Queries;

public sealed record GetTagsPageQuery(TagFilter Filter) : IRequest<ClientResponse<PagedResponse<TagViewModel>>>;

public sealed class GetTagsPageQueryHandler : IRequestHandler<GetTagsPageQuery, ClientResponse<PagedResponse<TagViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetTagsPageQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<TagViewModel>>> Handle(GetTagsPageQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null)
        {
            return ClientResponse<PagedResponse<TagViewModel>>.NotFound;
        }

        var page = await UnitOfWork.Tags.GetPageForWorkspace(workspaceId.Value, request.Filter, cancellationToken);

        return ClientResponse<PagedResponse<TagViewModel>>.Success(page);
    }
}
