using Mediator;

using Netptune.Core.Exceptions;
using Netptune.Core.Models.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Queries;

public sealed record FindPagesByTextQuery(PageTextSearch Search) : IRequest<ClientResponse<PagedResponse<PageBodyViewModel>>>;

public sealed class FindPagesByTextQueryHandler : IRequestHandler<FindPagesByTextQuery, ClientResponse<PagedResponse<PageBodyViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public FindPagesByTextQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<PageBodyViewModel>>> Handle(FindPagesByTextQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();

        try
        {
            var pages = await UnitOfWork.Pages.FindByText(workspaceId, request.Search, cancellationToken);

            return ClientResponse<PagedResponse<PageBodyViewModel>>.Success(pages);
        }
        catch (InvalidTextPatternException exception)
        {
            return ClientResponse<PagedResponse<PageBodyViewModel>>.Failed(exception.Message);
        }
    }
}
