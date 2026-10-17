using Mediator;

using Netptune.Core.Services;
using Netptune.Core.SourceControl;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.SourceControl;

namespace Netptune.Handlers.SourceControl.Queries;

public sealed record GetSourceControlConnectionsQuery : IRequest<List<SourceControlConnectionViewModel>>;

public sealed class GetSourceControlConnectionsQueryHandler
    : IRequestHandler<GetSourceControlConnectionsQuery, List<SourceControlConnectionViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly ISourceControlProviderRegistry Registry;

    public GetSourceControlConnectionsQueryHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        ISourceControlProviderRegistry registry)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Registry = registry;
    }

    public async ValueTask<List<SourceControlConnectionViewModel>> Handle(
        GetSourceControlConnectionsQuery query,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var connections = await UnitOfWork.SourceControlConnections.GetForWorkspace(workspaceId, cancellationToken);
        var linkedRepos = await UnitOfWork.SourceControlRepos.GetLinkedForWorkspace(workspaceId, cancellationToken);

        var linkedCounts = linkedRepos
            .GroupBy(repo => repo.ConnectionId)
            .ToDictionary(group => group.Key, group => group.Count());

        return connections
            .Select(connection =>
            {
                var linkedCount = linkedCounts.GetValueOrDefault(connection.Id);

                return SourceControlConnectionMapper.ToViewModel(connection, Registry, linkedCount);
            })
            .ToList();
    }
}
