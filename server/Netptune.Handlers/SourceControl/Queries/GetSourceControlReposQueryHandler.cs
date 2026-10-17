using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.SourceControl;

namespace Netptune.Handlers.SourceControl.Queries;

public sealed record GetSourceControlReposQuery(int ConnectionId)
    : IRequest<ClientResponse<List<SourceControlRepoViewModel>>>;

public sealed class GetSourceControlReposQueryHandler
    : IRequestHandler<GetSourceControlReposQuery, ClientResponse<List<SourceControlRepoViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetSourceControlReposQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<List<SourceControlRepoViewModel>>> Handle(
        GetSourceControlReposQuery query,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var connection = await UnitOfWork.SourceControlConnections.GetOwned(
            query.ConnectionId,
            workspaceId,
            cancellationToken);

        if (connection is null)
        {
            return ClientResponse<List<SourceControlRepoViewModel>>.NotFound;
        }

        var repos = await UnitOfWork.SourceControlRepos.GetForConnection(connection.Id, cancellationToken);

        var viewModels = repos
            .OrderBy(repo => repo.FullName, StringComparer.OrdinalIgnoreCase)
            .Select(SourceControlMapping.ToViewModel)
            .ToList();

        return ClientResponse<List<SourceControlRepoViewModel>>.Success(viewModels);
    }
}
