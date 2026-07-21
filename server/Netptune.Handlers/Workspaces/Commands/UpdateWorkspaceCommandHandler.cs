using Mediator;

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

        return fields;
    }
}
