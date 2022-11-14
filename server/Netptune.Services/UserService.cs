using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Flurl;

using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
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
    private readonly IInviteCache InviteCache;
    private readonly IUserRepository UserRepository;
    private readonly IWorkspaceRepository WorkspaceRepository;

    public UserService(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IEmailService email,
        IHostingService hosting,
        IWorkspaceUserCache workspaceUserCache,
        IInviteCache inviteCache)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Email = email;
        Hosting = hosting;
        WorkspaceUserCache = workspaceUserCache;
        InviteCache = inviteCache;
        UserRepository = unitOfWork.Users;
        WorkspaceRepository = unitOfWork.Workspaces;
    }

    public async Task<UserViewModel?> Get(string userId)
    {
        var user = await UserRepository.GetAsync(userId, true);

        return user?.ToViewModel();
    }

    public async Task<UserViewModel?> GetByEmail(string email)
    {
        var user = await UserRepository.GetByEmail(email, true);

        return user?.ToViewModel();
    }

    public async Task<List<UserViewModel>> GetAll()
    {
        var users = await UserRepository.GetAllAsync();

        return MapUsers(users);
    }

    public async Task<List<WorkspaceUserViewModel>?> GetWorkspaceUsers()
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceKey);

        if (workspace is null) return null;

        var users = await UserRepository.GetWorkspaceUsers(workspaceKey, true);

        return MapWorkspaceUsers(users, workspace.OwnerId!);
    }

    public async Task<ClientResponse<InviteUserResponse>> InviteUsersToWorkspace(IEnumerable<string> emails)
    {
        var workspaceKey = Identity.GetWorkspaceKey();

        var emailList = emails.Select(email => email.Trim().Normalize()).ToHashSet();
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

        var existingUserEmails = existingUsers.Select(user => user.Email!.Normalize());
        var usersToInvite = emailList.Where(email => !existingUserEmails.Contains(email)).ToHashSet();

        await SendUserInviteEmails(usersToInvite, workspace);

        return ClientResponse<InviteUserResponse>.Success(new InviteUserResponse
        {
            Emails = usersToInvite.ToList(),
        });
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

        return ClientResponse<RemoveUsersWorkspaceResponse>.Success(new RemoveUsersWorkspaceResponse
        {
            Emails = removeUserEmails,
        });
    }

    public async Task<ClientResponse<UserViewModel>> Update(UpdateUserRequest request)
    {
        var updatedUser = await UserRepository.GetAsync(request.Id!);

        if (updatedUser is null) return ClientResponse<UserViewModel>.NotFound;

        updatedUser.Firstname = request.Firstname ?? updatedUser.Firstname;
        updatedUser.Lastname = request.Lastname ?? updatedUser.Lastname;
        updatedUser.PictureUrl = request.PictureUrl ?? updatedUser.PictureUrl;

        await UnitOfWork.CompleteAsync();

        var result = updatedUser.ToViewModel();

        return ClientResponse<UserViewModel>.Success(result);
    }

    private Task SendUserInviteEmails(IEnumerable<string> emails, Workspace workspace)
    {
        var emailModels = emails.Select(email =>
        {
            var key = Guid.NewGuid()
                .ToString()
                .Replace("-", "")
                .ToLowerInvariant();

            InviteCache.Create(key, new()
            {
                Email = email,
                WorkspaceId = workspace.Id,
            });

            var uri = Hosting.ClientOrigin
                .AppendPathSegments("auth", "register")
                .SetQueryParam("code", key, true)
                .SetQueryParam("refer", "invite");

            return new SendEmailModel
            {
                SendTo = new()
                {
                    Address = email,
                    DisplayName = email,
                },
                Name = email,
                Action = "Go to Workspace",
                Link = uri,
                Reason = "workspace invite",
                Message = $"Hi you've been invited to join the {workspace.Name} workspace in Netptune.",
                Subject = "Netptune workspace invitation.",
                PreHeader = $"Hi you've been invited to join the {workspace.Name} in Netptune.",
                RawTextContent = $"Hi you've been invited to join the {workspace.Name} in Netptune. Click the link below to start. \n\n {uri}",
            };
        });

        return Email.Send(emailModels);
    }

    private static List<UserViewModel> MapUsers(List<AppUser> users)
    {
        return users.ConvertAll(user => user.ToViewModel());
    }

    private static List<WorkspaceUserViewModel> MapWorkspaceUsers(List<AppUser> users, string workspaceOwnerId)
    {
        return users.ConvertAll(user => user.ToWorkspaceViewModel(workspaceOwnerId));
    }
}
