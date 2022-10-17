using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Flurl;

using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Messaging;
using Netptune.Core.Models.Authentication;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Repositories;
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

    public async Task<List<WorkspaceUserViewModel>?> GetWorkspaceUsers()
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceKey);

        if (workspace is null) return null;

        var users = await UserRepository.GetWorkspaceUsers(workspaceKey, true);

        return MapWorkspaceUsers(users, workspace.OwnerId!);
    }

    public async Task<List<UserViewModel>> GetAll()
    {
        var users = await UserRepository.GetAllAsync();

        return MapUsers(users);
    }

    public Task<ClientResponse> InviteUserToWorkspace(string userId)
    {
        return InviteUsersToWorkspace(new[] { userId });
    }

    public async Task<ClientResponse> InviteUsersToWorkspace(
        IEnumerable<string> emailAddresses, bool onlyNewUsers = false)
    {
        var workspaceKey = Identity.GetWorkspaceKey();

        var emailList = emailAddresses.Select(email => email.Trim().Normalize()).ToHashSet();
        var workspace = await WorkspaceRepository.GetBySlug(workspaceKey, true);

        if (workspace is null)
        {
            return ClientResponse.Failed("workspace not found");
        }

        if (emailList.Count == 0)
        {
            return ClientResponse.Failed("no email addresses provided");
        }

        async Task<HashSet<string>> InviteExistingUsersDirectly()
        {
            var users = await UserRepository.GetByEmailRange(emailList, true);
            var userIds = users.ConvertAll(user => user.Id);
            var existing = await UserRepository.IsUserInWorkspaceRange(userIds, workspace.Id);
            var newUserIds = userIds.Except(existing).ToList();

            var result = await UserRepository.InviteUsersToWorkspace(newUserIds, workspace.Id);

            if (result is null) throw new Exception();

            await UnitOfWork.CompleteAsync();

            var userEmails = users.Select(user => user.NormalizedEmail);
            return emailList.Where(email => !userEmails.Contains(email)).ToHashSet();
        }

        var usersToInvite = await InviteExistingUsersDirectly();

        await SendUserInviteEmails(usersToInvite, workspace);

        return ClientResponse.Success();
    }

    public async Task<ClientResponse> RemoveUsersFromWorkspace(IEnumerable<string> emailAddresses)
    {
        var workspaceKey = Identity.GetWorkspaceKey();

        var emailList = emailAddresses.ToList();
        var workspace = await WorkspaceRepository.GetBySlug(workspaceKey, true);

        if (workspace is null)
        {
            return ClientResponse.Failed("workspace not found");
        }

        if (emailList.Count == 0)
        {
            return ClientResponse.Failed("no email addresses provided");
        }

        var users = await UserRepository.GetByEmailRange(emailList, true);
        var userIds = users.ConvertAll(user => user.Id);

        if (userIds.Count == 1 && userIds.Contains(workspace.OwnerId!))
        {
            return ClientResponse.Failed("cannot remove thew owner of the workspace");
        }

        foreach (var userId in userIds)
        {
            if (userId == workspace.OwnerId) continue;

            WorkspaceUserCache.Remove(new WorkspaceUserKey
            {
                UserId = userId,
                WorkspaceKey = workspaceKey,
            });
        }

        await UserRepository.RemoveUsersFromWorkspace(userIds, workspace.Id);

        await UnitOfWork.CompleteAsync();

        return ClientResponse.Success();
    }

    public async Task<ClientResponse<UserViewModel>> Update(AppUser user)
    {
        var updatedUser = await UserRepository.GetAsync(user.Id);

        if (updatedUser is null) return ClientResponse<UserViewModel>.NotFound;

        updatedUser.PhoneNumber = user.PhoneNumber;
        updatedUser.Firstname = user.Firstname;
        updatedUser.Lastname = user.Lastname;
        updatedUser.PictureUrl = user.PictureUrl;

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

            InviteCache.Create(key,
                new WorkspaceInvite
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
                SendTo = new SendTo
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

    private static List<UserViewModel> MapUsers(IEnumerable<AppUser> users)
    {
        return users.Select(user => user.ToViewModel()).ToList();
    }

    private static List<WorkspaceUserViewModel> MapWorkspaceUsers
        (IEnumerable<AppUser> users, string workspaceOwnerId)
    {
        return users
            .Select(user => user.ToWorkspaceViewModel(workspaceOwnerId))
            .ToList();
    }
}
