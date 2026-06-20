using Mediator;

using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Statuses;

namespace Netptune.Handlers.Statuses.Commands;

public sealed record UpdateStatusCommand(UpdateStatusRequest Request) : IRequest<ClientResponse<StatusViewModel>>;

public sealed class UpdateStatusCommandHandler : IRequestHandler<UpdateStatusCommand, ClientResponse<StatusViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public UpdateStatusCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<StatusViewModel>> Handle(UpdateStatusCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return ClientResponse<StatusViewModel>.NotFound;

        var status = await UnitOfWork.Statuses.GetInWorkspace(request.Request.Id, workspaceId.Value, cancellationToken: cancellationToken);

        if (status is null) return ClientResponse<StatusViewModel>.NotFound;

        var key = request.Request.Name.Trim().ToUrlSlug();
        if (string.IsNullOrWhiteSpace(key)) return ClientResponse<StatusViewModel>.Failed("Status name must contain at least one valid character.");

        if (await UnitOfWork.Statuses.KeyExists(workspaceId.Value, status.EntityType, key, status.Id, cancellationToken))
        {
            return ClientResponse<StatusViewModel>.Failed("Status name should be unique.");
        }

        status.Name = request.Request.Name.Trim();
        status.Key = key;
        status.Description = request.Request.Description?.Trim();
        status.Color = request.Request.Color?.Trim();
        status.Category = request.Request.Category;

        await UnitOfWork.CompleteAsync(cancellationToken);

        var result = await UnitOfWork.Statuses.GetViewModel(status.Id, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = status.Id;
            options.EntityType = EntityType.Status;
            options.Type = ActivityType.Modify;
        });

        return ClientResponse<StatusViewModel>.Success(result!);
    }
}
