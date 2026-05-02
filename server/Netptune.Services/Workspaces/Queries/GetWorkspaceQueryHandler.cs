using Mediator;
using Netptune.Core.Entities;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Workspaces.Queries;

public sealed record GetWorkspaceQuery(string Slug) : IRequest<Workspace?>;

public sealed class GetWorkspaceQueryHandler : IRequestHandler<GetWorkspaceQuery, Workspace?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public GetWorkspaceQueryHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public ValueTask<Workspace?> Handle(GetWorkspaceQuery request, CancellationToken cancellationToken)
    {
        return new ValueTask<Workspace?>(UnitOfWork.Workspaces.GetBySlug(request.Slug, cancellationToken: cancellationToken));
    }
}
