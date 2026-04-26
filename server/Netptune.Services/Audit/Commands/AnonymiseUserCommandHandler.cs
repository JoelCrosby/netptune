using Mediator;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Audit.Commands;

public sealed record AnonymiseUserCommand(string UserId) : IRequest<ClientResponse>;

public sealed class AnonymiseUserCommandHandler : IRequestHandler<AnonymiseUserCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public AnonymiseUserCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(AnonymiseUserCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();

        await UnitOfWork.ActivityLogs.AnonymiseUser(request.UserId, workspaceId);

        return ClientResponse.Success;
    }
}
