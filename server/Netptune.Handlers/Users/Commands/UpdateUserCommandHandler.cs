using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Handlers.Users.Commands;

public sealed record UpdateUserCommand(UpdateUserRequest Request) : IRequest<ClientResponse<UserViewModel>>;

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, ClientResponse<UserViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public UpdateUserCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<UserViewModel>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = Identity.GetCurrentUserId();
        var isSelf = string.Equals(request.Request.Id, currentUserId, StringComparison.Ordinal);

        if (!isSelf)
        {
            return ClientResponse<UserViewModel>.NotFound;
        }

        var updatedUser = await UnitOfWork.Users.GetAsync(currentUserId, cancellationToken: cancellationToken);

        if (updatedUser is null) return ClientResponse<UserViewModel>.NotFound;

        updatedUser.Firstname = request.Request.Firstname ?? updatedUser.Firstname;
        updatedUser.Lastname = request.Request.Lastname ?? updatedUser.Lastname;
        updatedUser.PictureUrl = request.Request.PictureUrl ?? updatedUser.PictureUrl;

        await UnitOfWork.CompleteAsync(cancellationToken);

        return updatedUser.ToViewModel();
    }
}
