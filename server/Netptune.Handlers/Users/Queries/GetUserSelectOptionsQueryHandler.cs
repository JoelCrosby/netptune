using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Handlers.Users.Queries;

public sealed record GetUserSelectOptionsQuery(UserSelectFilter Filter)
    : IRequest<ClientResponse<PagedResponse<UserSelectOptionViewModel>>>;

public sealed class GetUserSelectOptionsQueryHandler
    : IRequestHandler<GetUserSelectOptionsQuery, ClientResponse<PagedResponse<UserSelectOptionViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetUserSelectOptionsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<UserSelectOptionViewModel>>> Handle(
        GetUserSelectOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var result = await UnitOfWork.Users.GetUserSelectOptionsPaged(workspaceId, request.Filter, cancellationToken);

        var response = new PagedResponse<UserSelectOptionViewModel>(
            [.. result.Results],
            result.CurrentPage,
            result.PageSize,
            result.RowCount);

        return ClientResponse<PagedResponse<UserSelectOptionViewModel>>.Success(response);
    }
}
