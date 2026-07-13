using Mediator;
using Netptune.Core.Cache;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Workspaces.Commands;

public sealed record DeleteWorkspacePermanentCommand(string Key) : IRequest<ClientResponse>;

public sealed class DeleteWorkspacePermanentCommandHandler : IRequestHandler<DeleteWorkspacePermanentCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IWorkspaceUserCache Cache;

    public DeleteWorkspacePermanentCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IWorkspaceUserCache cache)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Cache = cache;
    }

    public async ValueTask<ClientResponse> Handle(DeleteWorkspacePermanentCommand request, CancellationToken cancellationToken)
    {
        var workspace = await UnitOfWork.Workspaces.GetBySlug(request.Key, cancellationToken: cancellationToken);

        if (workspace is null) return ClientResponse.NotFound;

        var userId = Identity.GetCurrentUserId();
        var workspaceId = workspace.Id;

        Cache.Remove(new() { UserId = userId, WorkspaceKey = workspace.Slug });

        // The teardown order lives on the repository — every workspace-scoped foreign key is
        // Restrict, so the rows have to come out children-first or the workspace row will not budge.
        await UnitOfWork.Transaction(() => UnitOfWork.Workspaces.DeleteWorkspacePermanent(workspaceId, cancellationToken));

        return ClientResponse.Success;
    }
}
