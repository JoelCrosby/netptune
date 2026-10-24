using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Models.Usage;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Statuses.Commands;

public sealed record DeleteStatusCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteStatusCommandHandler : IRequestHandler<DeleteStatusCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public DeleteStatusCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteStatusCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return ClientResponse.NotFound;

        var status = await UnitOfWork.Statuses.GetInWorkspace(request.Id, workspaceId.Value, cancellationToken: cancellationToken);

        if (status is null) return ClientResponse.NotFound;

        if (status.IsSystem) return ClientResponse.Failed(AppText.ErrorSystemStatusesCannotDeleted);

        var usage = await UnitOfWork.Statuses.GetUsage(status.Id, workspaceId.Value, cancellationToken);
        var isInUse = usage.TaskCount > 0 || usage.Projects.Count > 0;

        if (isInUse)
        {
            return ClientResponse.Failed(DescribeUsage(usage));
        }

        status.Delete(Identity.GetCurrentUserId());
        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = status.Id;
            options.EntityType = EntityType.Status;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }

    private static string DescribeUsage(StatusUsage usage)
    {
        var parts = new List<string>();

        if (usage.TaskCount > 0)
        {
            var tasks = usage.TaskCount == 1
                ? AppText.StatusUsedByOneTask
                : Localized.Format(AppText.StatusUsedByTasks, usage.TaskCount);

            parts.Add(tasks);
        }

        if (usage.Projects.Count > 0)
        {
            var projects = usage.Projects.Count == 1
                ? AppText.StatusUsedByOneProject
                : Localized.Format(AppText.StatusUsedByProjects, usage.Projects.Count);

            parts.Add(projects);
        }

        var description = parts.Count == 2
            ? Localized.Format(AppText.ListPair, parts[0], parts[1])
            : string.Concat(parts);

        return Localized.Format(AppText.ErrorStatusUsedCannotDeleted, description);
    }
}
