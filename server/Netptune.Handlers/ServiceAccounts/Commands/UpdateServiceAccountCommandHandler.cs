using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Requests.ServiceAccounts;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ServiceAccounts;

namespace Netptune.Handlers.ServiceAccounts.Commands;

public sealed record UpdateServiceAccountCommand(int ServiceAccountId, UpdateServiceAccountRequest Request)
    : IRequest<ClientResponse<ServiceAccountViewModel>>;

public sealed class UpdateServiceAccountCommandHandler
    : IRequestHandler<UpdateServiceAccountCommand, ClientResponse<ServiceAccountViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;

    public UpdateServiceAccountCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IWorkspacePermissionCache permissionCache)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        PermissionCache = permissionCache;
    }

    public async ValueTask<ClientResponse<ServiceAccountViewModel>> Handle(
        UpdateServiceAccountCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;
        var name = request.Name.Trim();

        if (name.Length is < 2 or > 128)
        {
            return ClientResponse<ServiceAccountViewModel>.Failed("Service account name must be between 2 and 128 characters.");
        }

        var currentUser = await Identity.GetCurrentUser();

        if (currentUser.UserType != AppUserType.User)
        {
            return ClientResponse<ServiceAccountViewModel>.Forbidden;
        }

        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var currentPermissions = await PermissionCache.GetUserPermissions(currentUser.Id, workspaceKey);

        if (currentPermissions is null)
        {
            return ClientResponse<ServiceAccountViewModel>.Forbidden;
        }

        var serviceAccount = await UnitOfWork.ServiceAccounts.GetForManagement(
            command.ServiceAccountId,
            workspaceId,
            cancellationToken);

        if (serviceAccount is null)
        {
            return ClientResponse<ServiceAccountViewModel>.NotFound;
        }

        if (serviceAccount.DisabledAt is not null)
        {
            return ClientResponse<ServiceAccountViewModel>.Failed("A disabled service account cannot be updated.");
        }

        var permissions = request.Permissions
            .Select(permission => permission.Trim())
            .Where(permission => permission.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (permissions.Count == 0)
        {
            return ClientResponse<ServiceAccountViewModel>.Failed("A service account needs at least one permission.");
        }

        var allowedPermissions = currentPermissions.Role == WorkspaceRole.Owner
            ? NetptunePermissions.All
            : currentPermissions.Permissions;

        if (permissions.Any(permission => !NetptunePermissions.All.Contains(permission)
                                          || !allowedPermissions.Contains(permission)))
        {
            return ClientResponse<ServiceAccountViewModel>.Failed("A service account cannot be granted permissions its owner does not have.");
        }

        var ownerIds = request.OwnerUserIds
            .Append(currentUser.Id)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var owners = await UnitOfWork.Users.IsUserInWorkspaceRange(ownerIds, workspaceId, cancellationToken);

        if (owners.Count != ownerIds.Count || owners.Any(owner => owner.UserType != AppUserType.User))
        {
            return ClientResponse<ServiceAccountViewModel>.Failed("Every service account owner must be a human member of the workspace.");
        }

        var updatedAt = DateTime.UtcNow;

        return await UnitOfWork.Transaction(async () =>
        {
            serviceAccount.User.Firstname = name;
            serviceAccount.Description = request.Description?.Trim();

            await UnitOfWork.WorkspaceUsers.SetUserPermissions(
                serviceAccount.UserId,
                workspaceId,
                permissions,
                cancellationToken);

            TrimCredentialScopes(serviceAccount, permissions);
            SyncOwners(serviceAccount, ownerIds, updatedAt);

            await UnitOfWork.CompleteAsync(cancellationToken);

            PermissionCache.Remove(new WorkspaceUserKey
            {
                UserId = serviceAccount.UserId,
                WorkspaceKey = workspaceKey,
            });

            return ClientResponse<ServiceAccountViewModel>.Success(new ServiceAccountViewModel
            {
                Id = serviceAccount.Id,
                UserId = serviceAccount.UserId,
                Name = name,
                Description = serviceAccount.Description,
                CreatedAt = serviceAccount.CreatedAt,
                DisabledAt = serviceAccount.DisabledAt,
                OwnerUserIds = ownerIds,
                Permissions = permissions,
            });
        });
    }

    private static void TrimCredentialScopes(ServiceAccount serviceAccount, List<string> permissions)
    {
        var granted = permissions.ToHashSet(StringComparer.Ordinal);

        foreach (var credential in serviceAccount.Credentials)
        {
            var scopes = credential.Scopes.Where(granted.Contains).ToList();

            if (scopes.Count == credential.Scopes.Count)
            {
                continue;
            }

            credential.Scopes.Clear();
            credential.Scopes.AddRange(scopes);
        }
    }

    private static void SyncOwners(ServiceAccount serviceAccount, List<string> ownerIds, DateTime updatedAt)
    {
        var requestedOwners = ownerIds.ToHashSet(StringComparer.Ordinal);
        var currentOwners = serviceAccount.Owners
            .Select(owner => owner.UserId)
            .ToHashSet(StringComparer.Ordinal);
        var removedOwners = serviceAccount.Owners
            .Where(owner => !requestedOwners.Contains(owner.UserId))
            .ToList();

        foreach (var owner in removedOwners)
        {
            serviceAccount.Owners.Remove(owner);
        }

        var addedOwnerIds = requestedOwners.Where(ownerId => !currentOwners.Contains(ownerId));

        foreach (var ownerId in addedOwnerIds)
        {
            serviceAccount.Owners.Add(new ServiceAccountOwner
            {
                ServiceAccountId = serviceAccount.Id,
                UserId = ownerId,
                CreatedAt = updatedAt,
            });
        }
    }
}
