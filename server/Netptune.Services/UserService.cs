using Flurl;

using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.Users;
using Netptune.Core.Extensions;
using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Services;

public class UserService : IUserService
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IEmailService Email;
    private readonly IHostingService Hosting;
    private readonly IWorkspaceUserCache WorkspaceUserCache;
    private readonly IWorkspacePermissionCache PermissionCache;
    private readonly IInviteCache InviteCache;
    private readonly IUserRepository UserRepository;
    private readonly IWorkspaceRepository WorkspaceRepository;
    private readonly IActivityLogger Activity;

    public UserService(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IEmailService email,
        IHostingService hosting,
        IWorkspaceUserCache workspaceUserCache,
        IInviteCache inviteCache,
        IWorkspacePermissionCache permissionCache,
        IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Email = email;
        Hosting = hosting;
        WorkspaceUserCache = workspaceUserCache;
        InviteCache = inviteCache;
        UserRepository = unitOfWork.Users;
        WorkspaceRepository = unitOfWork.Workspaces;
        PermissionCache = permissionCache;
        Activity = activity;
    }

    public async Task<UserViewModel?> Get(string userId)
    {
        var user = await UserRepository.GetAsync(userId, true);
        return await ToViewModel(user);
    }

    public async Task<UserViewModel?> GetByEmail(string email)
    {
        var user = await UserRepository.GetByEmail(email, true);
        return await ToViewModel(user);
    }

    public async Task<List<UserViewModel>> GetAll()
    {
        var users = await UserRepository.GetAllAsync();

        return MapUsers(users);
    }

    public async Task<List<WorkspaceUserViewModel>> GetWorkspaceUsers()
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceAppUsers = await UserRepository.GetWorkspaceAppUsers(workspaceKey, true);

        if (workspaceAppUsers.Count == 0) return [];

        return workspaceAppUsers.ConvertAll(user => user.ToWorkspaceViewModel());
    }

    public async Task<ClientResponse<InviteUserResponse>> InviteUsersToWorkspace(IEnumerable<string> emails)
    {
        var workspaceKey = Identity.GetWorkspaceKey();

        var emailList = emails.Select(e => e.Trim().IdentityNormalize()).ToHashSet();
        var workspace = await WorkspaceRepository.GetBySlug(workspaceKey, true);

        if (workspace is null)
        {
            return ClientResponse<InviteUserResponse>.Failed("workspace not found");
        }

        if (emailList.Count == 0)
        {
            return ClientResponse<InviteUserResponse>.Failed("no email addresses provided");
        }

        var users = await UserRepository.GetByEmailRange(emailList, true);
        var userIds = users.ConvertAll(user => user.Id);
        var existingUsers = await UserRepository.IsUserInWorkspaceRange(userIds, workspace.Id);
        var newUserIds = userIds.Except(existingUsers.Select(x => x.Id)).ToHashSet();

        await UserRepository.InviteUsersToWorkspace(newUserIds, workspace.Id);
        await UnitOfWork.CompleteAsync();

        var existingUserEmails = existingUsers.Select(user => user.Email!.IdentityNormalize()).ToHashSet();
        var usersToInvite = emailList.Where(email => !existingUserEmails.Contains(email)).Select(e => e.ToLowerInvariant()).ToHashSet();

        await SendUserInviteEmails(usersToInvite, workspace);

        Activity.LogWith<UserMembershipActivityMeta>(options =>
        {
            options.EntityId = workspace.Id;
            options.WorkspaceId = workspace.Id;
            options.EntityType = EntityType.Workspace;
            options.Type = ActivityType.Invite;
            options.Meta = new UserMembershipActivityMeta
            {
                Emails = usersToInvite.ToList(),
            };
        });

        return new InviteUserResponse
        {
            Emails = usersToInvite.ToList(),
        };
    }

    public async Task<ClientResponse<RemoveUsersWorkspaceResponse>> RemoveUsersFromWorkspace(IEnumerable<string> emails)
    {
        var workspaceKey = Identity.GetWorkspaceKey();

        var emailList = emails.ToList();
        var workspace = await WorkspaceRepository.GetBySlug(workspaceKey, true);

        if (workspace is null)
        {
            return ClientResponse<RemoveUsersWorkspaceResponse>.Failed("workspace not found");
        }

        if (emailList.Count == 0)
        {
            return ClientResponse<RemoveUsersWorkspaceResponse>.Failed("no email addresses provided");
        }

        var users = await UserRepository.GetByEmailRange(emailList, true);
        var userIds = users.ConvertAll(user => user.Id);

        if (userIds.Count == 1 && userIds.Contains(workspace.OwnerId!))
        {
            return ClientResponse<RemoveUsersWorkspaceResponse>.Failed("cannot remove thew owner of the workspace");
        }

        foreach (var userId in userIds)
        {
            if (userId == workspace.OwnerId) continue;

            WorkspaceUserCache.Remove(new ()
            {
                UserId = userId,
                WorkspaceKey = workspaceKey,
            });
        }

        var removed = await UserRepository.RemoveUsersFromWorkspace(userIds, workspace.Id);
        var removeUserEmails = removed.Select(x => x.User.Email!).ToList();

        await UnitOfWork.CompleteAsync();

        Activity.LogWith<UserMembershipActivityMeta>(options =>
        {
            options.EntityId = workspace.Id;
            options.WorkspaceId = workspace.Id;
            options.EntityType = EntityType.Workspace;
            options.Type = ActivityType.Remove;
            options.Meta = new UserMembershipActivityMeta
            {
                Emails = removeUserEmails,
            };
        });

        return new RemoveUsersWorkspaceResponse
        {
            Emails = removeUserEmails,
        };
    }

    public async Task<ClientResponse<UserViewModel>> Update(UpdateUserRequest request)
    {
        var updatedUser = await UserRepository.GetAsync(request.Id!);

        if (updatedUser is null) return ClientResponse<UserViewModel>.NotFound;

        updatedUser.Firstname = request.Firstname ?? updatedUser.Firstname;
        updatedUser.Lastname = request.Lastname ?? updatedUser.Lastname;
        updatedUser.PictureUrl = request.PictureUrl ?? updatedUser.PictureUrl;

        await UnitOfWork.CompleteAsync();

        return updatedUser.ToViewModel();
    }

    public async Task<ClientResponse<List<string>>> ToggleUserPermission(ToggleUserPermissionRequest request)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspace = await WorkspaceRepository.GetBySlug(workspaceKey, true);

        if (workspace is null)
        {
            return ClientResponse<List<string>>.Failed("workspace not found");
        }

        var workspaceUser = await UnitOfWork.WorkspaceUsers.GetUserPermissions(request.UserId, workspaceKey, false);

        if (workspaceUser is null)
        {
            return ClientResponse<List<string>>.Failed("user is not a member of this workspace");
        }

        var permissions = workspaceUser.Permissions;
        bool granted;

        if (permissions.Contains(request.Permission))
        {
            permissions.Remove(request.Permission);
            granted = false;
        }
        else
        {
            permissions.Add(request.Permission);
            granted = true;
        }

        await UnitOfWork.WorkspaceUsers.SetUserPermissions(request.UserId, workspace.Id, permissions);
        await UnitOfWork.CompleteAsync();

        PermissionCache.Remove(new()
        {
            UserId = request.UserId,
            WorkspaceKey = workspaceKey,
        });

        Activity.LogWith<UserPermissionActivityMeta>(options =>
        {
            options.EntityId = workspace.Id;
            options.WorkspaceId = workspace.Id;
            options.EntityType = EntityType.Workspace;
            options.Type = ActivityType.PermissionChanged;
            options.Meta = new UserPermissionActivityMeta
            {
                TargetUserId = request.UserId,
                Permission = request.Permission,
                Granted = granted,
            };
        });

        return ClientResponse<List<string>>.Success([.. permissions]);
    }

    private Task SendUserInviteEmails(IEnumerable<string> emails, Workspace workspace)
    {
        var emailList = emails.ToList();

        var key = Guid.NewGuid()
            .ToString()
            .Replace("-", "")
            .ToLowerInvariant();

        foreach (var email in emailList)
        {
            InviteCache.Create(key, new()
            {
                Email = email,
                WorkspaceId = workspace.Id,
            });
        }


        var uri = Hosting.ClientOrigin
            .AppendPathSegments("auth", "register")
            .SetQueryParam("code", key, true)
            .SetQueryParam("refer", "invite");

        var sendTo = emailList.Select(e => new SendTo
        {
            Address = e,
            DisplayName = e,
        }).ToList();

        var message = new SendMultipleEmailModel
        {
            SendTo = sendTo,
            Name = "-name-",
            Action = "Go to Workspace",
            Link = uri,
            Reason = "workspace invite",
            Message = "Hi you've been invited to join the -workspace- workspace in Netptune.",
            Subject = "Netptune workspace invitation.",
            PreHeader = "Hi you've been invited to join the -workspace- in Netptune.",
            RawTextContent = $"Hi you've been invited to join the -workspace- in Netptune. Click the link below to start. \n\n {uri}",
        };

        return Email.Send(message);
    }

    private static List<UserViewModel> MapUsers(List<AppUser> users)
    {
        return users.ConvertAll(user => user.ToViewModel());
    }

    private async Task<UserViewModel?> ToViewModel(AppUser? user)
    {
        if (user is null) return null;

        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceUser = await PermissionCache.GetUserPermissions(user.Id, workspaceKey);

        if (workspaceUser is null) return null;

        return user.ToViewModel(workspaceUser.Permissions);
    }
}
