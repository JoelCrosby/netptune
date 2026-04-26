using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Workspaces.Queries.GetAllWorkspaces;

public sealed class GetAllWorkspacesQueryHandler : IRequestHandler<GetAllWorkspacesQuery, List<Workspace>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public GetAllWorkspacesQueryHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public ValueTask<List<Workspace>> Handle(GetAllWorkspacesQuery request, CancellationToken cancellationToken)
    {
        return new ValueTask<List<Workspace>>(UnitOfWork.Workspaces.GetAllAsync());
    }
}
