using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Flags;

namespace Netptune.Handlers.Flags.Queries;

public sealed record GetTaskFlagsQuery(int TaskId) : IRequest<ClientResponse<List<TaskFlagViewModel>>>;

public sealed class GetTaskFlagsQueryHandler
    : IRequestHandler<GetTaskFlagsQuery, ClientResponse<List<TaskFlagViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetTaskFlagsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<List<TaskFlagViewModel>>> Handle(
        GetTaskFlagsQuery request,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var flags = await UnitOfWork.Flags.GetActiveTaskFlags(request.TaskId, workspaceId, cancellationToken);

        return ClientResponse<List<TaskFlagViewModel>>.Success(flags);
    }
}
