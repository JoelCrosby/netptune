using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Services.Users.Commands;

public sealed record UpdateUserCommand(UpdateUserRequest Request) : IRequest<ClientResponse<UserViewModel>>;

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, ClientResponse<UserViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public UpdateUserCommandHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public async ValueTask<ClientResponse<UserViewModel>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var updatedUser = await UnitOfWork.Users.GetAsync(request.Request.Id!, cancellationToken: cancellationToken);

        if (updatedUser is null) return ClientResponse<UserViewModel>.NotFound;

        updatedUser.Firstname = request.Request.Firstname ?? updatedUser.Firstname;
        updatedUser.Lastname = request.Request.Lastname ?? updatedUser.Lastname;
        updatedUser.PictureUrl = request.Request.PictureUrl ?? updatedUser.PictureUrl;

        await UnitOfWork.CompleteAsync(cancellationToken);

        return updatedUser.ToViewModel();
    }
}
