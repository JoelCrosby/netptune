using Mediator;

using Microsoft.AspNetCore.Identity;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Relationships;
using Netptune.Core.Requests.ServiceAccounts;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ServiceAccounts;

namespace Netptune.Handlers.ServiceAccounts.Commands;

public sealed record CreateServiceAccountCommand(CreateServiceAccountRequest Request)
    : IRequest<ClientResponse<ServiceAccountViewModel>>;

public sealed class CreateServiceAccountCommandHandler
    : IRequestHandler<CreateServiceAccountCommand, ClientResponse<ServiceAccountViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IWorkspacePermissionCache PermissionCache;
    private readonly UserManager<AppUser> UserManager;

    public CreateServiceAccountCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IWorkspacePermissionCache permissionCache,
        UserManager<AppUser> userManager)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        PermissionCache = permissionCache;
        UserManager = userManager;
    }

    public async ValueTask<ClientResponse<ServiceAccountViewModel>> Handle(
        CreateServiceAccountCommand command,
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

        var permissions = request.Permissions
            .Select(permission => permission.Trim())
            .Where(permission => permission.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

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

        var accountUserId = Guid.NewGuid().ToString();
        var internalName = $"svc-{workspaceId}-{Guid.NewGuid():N}";
        var serviceUser = new AppUser
        {
            Id = accountUserId,
            UserType = AppUserType.ServiceAccount,
            Firstname = name,
            Lastname = string.Empty,
            UserName = internalName,
            Email = $"{internalName}@service.netptune.invalid",
            EmailConfirmed = true,
            LockoutEnabled = true,
        };

        var createdAt = DateTime.UtcNow;
        var serviceAccount = new ServiceAccount
        {
            UserId = accountUserId,
            WorkspaceId = workspaceId,
            Description = request.Description?.Trim(),
            CreatedByUserId = currentUser.Id,
            CreatedAt = createdAt,
        };

        var result = await UnitOfWork.Transaction(async () =>
        {
            var identityResult = await UserManager.CreateAsync(serviceUser);

            if (!identityResult.Succeeded)
            {
                return ClientResponse<ServiceAccountViewModel>.Failed(
                    string.Join(", ", identityResult.Errors.Select(error => error.Description)));
            }

            await UnitOfWork.ServiceAccounts.AddAsync(serviceAccount, cancellationToken);
            await UnitOfWork.ServiceAccounts.AddMembership(new WorkspaceAppUser
            {
                WorkspaceId = workspaceId,
                UserId = accountUserId,
                Role = WorkspaceRole.Member,
                Permissions = permissions,
            }, cancellationToken);
            await UnitOfWork.ServiceAccounts.AddOwners(ownerIds.Select(ownerId => new ServiceAccountOwner
            {
                ServiceAccount = serviceAccount,
                UserId = ownerId,
                CreatedAt = createdAt,
            }), cancellationToken);

            await UnitOfWork.CompleteAsync(cancellationToken);

            return ClientResponse<ServiceAccountViewModel>.Success(new ServiceAccountViewModel
            {
                Id = serviceAccount.Id,
                UserId = accountUserId,
                Name = name,
                Description = serviceAccount.Description,
                CreatedAt = createdAt,
                OwnerUserIds = ownerIds,
                Permissions = permissions,
            });
        });

        return result;
    }
}
