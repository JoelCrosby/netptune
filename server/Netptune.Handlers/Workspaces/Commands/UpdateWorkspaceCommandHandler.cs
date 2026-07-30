using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Workspaces.Commands;

public sealed record UpdateWorkspaceCommand(UpdateWorkspaceRequest Request) : IRequest<ClientResponse<Workspace>>;

public sealed class UpdateWorkspaceCommandHandler : IRequestHandler<UpdateWorkspaceCommand, ClientResponse<Workspace>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IEventRecordWriter EventRecords;

    public UpdateWorkspaceCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IEventRecordWriter eventRecords)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        EventRecords = eventRecords;
    }

    public async ValueTask<ClientResponse<Workspace>> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var result = await UnitOfWork.Workspaces.GetBySlug(request.Request.Slug!, cancellationToken: cancellationToken);

        if (result is null)
        {
            return ClientResponse<Workspace>.NotFound;
        }

        var changedFields = GetChangedFields(result, request.Request);

        result.Name = request.Request.Name ?? result.Name;
        result.Description = request.Request.Description ?? result.Description;
        result.ModifiedByUserId = userId;
        result.MetaInfo = request.Request.MetaInfo ?? result.MetaInfo;
        result.IsPublic = request.Request.IsPublic ?? result.IsPublic;
        result.PublicPermissions = ResolvePublicPermissions(result, request.Request);
        result.UpdatedAt = DateTime.UtcNow;

        if (changedFields.Count > 0)
        {
            await EventRecords.Append(new EventWriteRequest<WorkspaceSettingsChangedPayload>
            {
                WorkspaceId = result.Id,
                EventKey = EventKeys.WorkspaceSettingsChanged,
                SubjectType = EventEntityTypes.From(EntityType.Workspace),
                SubjectId = result.Id.ToString(),
                Payload = new WorkspaceSettingsChangedPayload
                {
                    Fields = changedFields,
                },
            }, cancellationToken);
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse<Workspace>.Success(result);
    }

    private static List<string>? ResolvePublicPermissions(Workspace workspace, UpdateWorkspaceRequest request)
    {
        if (request.PublicPermissions is not null)
        {
            var requestedSelection = NetptunePermissions.ResolvePublicPermissions(request.PublicPermissions);

            return [.. requestedSelection];
        }

        var needsDefaultSelection = workspace.IsPublic && workspace.PublicPermissions is null;

        if (needsDefaultSelection)
        {
            return [.. NetptunePermissions.PublicReadable];
        }

        return workspace.PublicPermissions;
    }

    private static List<string> GetChangedFields(Workspace workspace, UpdateWorkspaceRequest request)
    {
        var fields = new List<string>();

        if (request.Name is not null && request.Name != workspace.Name)
        {
            fields.Add("name");
        }

        if (request.Description is not null && request.Description != workspace.Description)
        {
            fields.Add("description");
        }

        if (request.MetaInfo is not null && request.MetaInfo.Color != workspace.MetaInfo?.Color)
        {
            fields.Add("appearance");
        }

        if (request.IsPublic.HasValue && request.IsPublic.Value != workspace.IsPublic)
        {
            fields.Add("visibility");
        }

        if (request.PublicPermissions is not null)
        {
            var requestedSelection = NetptunePermissions.ResolvePublicPermissions(request.PublicPermissions);
            var currentSelection = NetptunePermissions.ResolvePublicPermissions(workspace.PublicPermissions);
            var publicAccessChanged = !requestedSelection.SetEquals(currentSelection);

            if (publicAccessChanged)
            {
                fields.Add("public_access");
            }
        }

        return fields;
    }
}
