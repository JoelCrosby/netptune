using Mediator;

using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Statuses;

namespace Netptune.Handlers.Statuses.Commands;

public sealed record CreateStatusCommand(CreateStatusRequest Request) : IRequest<ClientResponse<StatusViewModel>>;

public sealed class CreateStatusCommandHandler : IRequestHandler<CreateStatusCommand, ClientResponse<StatusViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public CreateStatusCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse<StatusViewModel>> Handle(CreateStatusCommand request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey, cancellationToken);

        if (workspaceId is null) return ClientResponse<StatusViewModel>.NotFound;

        var key = request.Request.Name.Trim().ToUrlSlug();
        if (string.IsNullOrWhiteSpace(key)) return ClientResponse<StatusViewModel>.Failed("Status name must contain at least one valid character.");

        if (await UnitOfWork.Statuses.KeyExists(workspaceId.Value, request.Request.EntityType, key, cancellationToken: cancellationToken))
        {
            return ClientResponse<StatusViewModel>.Failed("Status name should be unique.");
        }

        var statuses = await UnitOfWork.Statuses.GetViewModelsForWorkspace(workspaceId.Value, request.Request.EntityType, cancellationToken);
        var sortOrder = statuses.Count == 0 ? 0 : statuses.Max(status => status.SortOrder) + 1;

        var status = new Status
        {
            WorkspaceId = workspaceId.Value,
            OwnerId = Identity.GetCurrentUserId(),
            EntityType = request.Request.EntityType,
            Name = request.Request.Name.Trim(),
            Key = key,
            Description = request.Request.Description?.Trim(),
            Color = request.Request.Color?.Trim(),
            Category = request.Request.Category,
            SortOrder = sortOrder,
        };

        await UnitOfWork.Statuses.AddAsync(status, cancellationToken);

        await UnitOfWork.CompleteAsync(cancellationToken);

        var result = await UnitOfWork.Statuses.GetViewModel(status.Id, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = status.Id;
            options.EntityType = EntityType.Status;
            options.Type = ActivityType.Create;
        });

        return ClientResponse<StatusViewModel>.Success(result!);
    }
}
