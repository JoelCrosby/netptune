using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Handlers.Users.Queries;

public sealed record GetAllUsersQuery(PageRequest? Page = null) : IRequest<List<UserViewModel>>;

public sealed class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, List<UserViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public GetAllUsersQueryHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public async ValueTask<List<UserViewModel>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await UnitOfWork.Users.GetUsers(cancellationToken, request.Page);
        return users.ConvertAll(u => u.ToViewModel());
    }
}
