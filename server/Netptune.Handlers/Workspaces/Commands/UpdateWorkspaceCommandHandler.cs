using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Requests;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Workspaces.Commands;

public sealed record UpdateWorkspaceCommand(UpdateWorkspaceRequest Request) : IRequest<ClientResponse<UpdateWorkspaceResponse>>;

public sealed class UpdateWorkspaceCommandHandler : IRequestHandler<UpdateWorkspaceCommand, ClientResponse<UpdateWorkspaceResponse>>
{
    public const int MinimumSlugLength = 4;

    private const string IdentifierTakenMessage = "Identifier is already taken";
    private const string IdentifierInvalidMessage = "Identifier must be at least 4 characters and contain letters or numbers";

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IEventRecordWriter EventRecords;
    private readonly IWorkspaceUserCache WorkspaceUsers;
    private readonly IWorkspacePermissionCache WorkspacePermissions;
    private readonly IWorkspaceCache WorkspaceCache;

    public UpdateWorkspaceCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IEventRecordWriter eventRecords,
        IWorkspaceUserCache workspaceUsers,
        IWorkspacePermissionCache workspacePermissions,
        IWorkspaceCache workspaceCache)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        EventRecords = eventRecords;
        WorkspaceUsers = workspaceUsers;
        WorkspacePermissions = workspacePermissions;
        WorkspaceCache = workspaceCache;
    }

    public async ValueTask<ClientResponse<UpdateWorkspaceResponse>> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var result = await UnitOfWork.Workspaces.GetBySlug(request.Request.Slug!, cancellationToken: cancellationToken);

        if (result is null)
        {
            return ClientResponse<UpdateWorkspaceResponse>.NotFound;
        }

        var previousSlug = result.Slug;
        var requestedSlug = ResolveRequestedSlug(request.Request.NewSlug, previousSlug);

        if (requestedSlug.IsInvalid)
        {
            return ClientResponse<UpdateWorkspaceResponse>.Failed(IdentifierInvalidMessage);
        }

        if (requestedSlug.IsRename)
        {
            var slugTaken = await UnitOfWork.Workspaces.Exists(requestedSlug.Slug!, cancellationToken);

            if (slugTaken)
            {
                return ClientResponse<UpdateWorkspaceResponse>.Failed(IdentifierTakenMessage);
            }
        }

        var changedFields = GetChangedFields(result, request.Request);

        if (requestedSlug.IsRename)
        {
            changedFields.Add("identifier");
            result.Slug = requestedSlug.Slug!;
        }

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

        if (requestedSlug.IsRename)
        {
            await ForgetWorkspaceUnder(result.Id, previousSlug, cancellationToken);
        }

        var response = new UpdateWorkspaceResponse
        {
            Workspace = result,
            PreviousSlug = requestedSlug.IsRename ? previousSlug : null,
        };

        return ClientResponse<UpdateWorkspaceResponse>.Success(response);
    }

    private async Task ForgetWorkspaceUnder(int workspaceId, string slug, CancellationToken cancellationToken)
    {
        WorkspaceCache.Remove(slug);

        var memberIds = await UnitOfWork.Users.GetWorkspaceUserIds(workspaceId, cancellationToken);

        foreach (var memberId in memberIds)
        {
            var key = new WorkspaceUserKey
            {
                WorkspaceKey = slug,
                UserId = memberId,
            };

            WorkspaceUsers.Remove(key);
            WorkspacePermissions.Remove(key);
        }
    }

    private static RequestedSlug ResolveRequestedSlug(string? newSlug, string currentSlug)
    {
        if (string.IsNullOrWhiteSpace(newSlug))
        {
            return RequestedSlug.Unchanged;
        }

        var normalised = newSlug.ToUrlSlug();

        if (normalised == currentSlug)
        {
            return RequestedSlug.Unchanged;
        }

        if (normalised.Length < MinimumSlugLength)
        {
            return RequestedSlug.Invalid;
        }

        return new RequestedSlug { Slug = normalised, IsRename = true };
    }

    private sealed record RequestedSlug
    {
        public static readonly RequestedSlug Unchanged = new();

        public static readonly RequestedSlug Invalid = new() { IsInvalid = true };

        public string? Slug { get; init; }

        public bool IsRename { get; init; }

        public bool IsInvalid { get; init; }
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
