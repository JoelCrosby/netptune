using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AutoMapper;

using Flurl;

using Netptune.Core.Entities;
using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Services
{
    public class UserService : IUserService
    {
        private readonly INetptuneUnitOfWork UnitOfWork;
        private readonly IMapper Mapper;
        private readonly IEmailService Email;
        private readonly IHostingService Hosting;
        private readonly IUserRepository UserRepository;
        private readonly IWorkspaceRepository WorkspaceRepository;

        public UserService(
            INetptuneUnitOfWork unitOfWork,
            IMapper mapper,
            IEmailService email,
            IHostingService hosting)
        {
            UnitOfWork = unitOfWork;
            Mapper = mapper;
            Email = email;
            Hosting = hosting;
            UserRepository = unitOfWork.Users;
            WorkspaceRepository = unitOfWork.Workspaces;
        }

        public async Task<UserViewModel> Get(string userId)
        {
            var user = await UserRepository.GetAsync(userId);

            if (user is null) return null;

            return MapUser(user);
        }

        public async Task<UserViewModel> GetByEmail(string email)
        {
            var user = await UserRepository.GetByEmail(email);

            if (user is null) return null;

            return MapUser(user);
        }

        public async Task<List<UserViewModel>> GetWorkspaceUsers(string workspaceSlug)
        {
            var users = await UserRepository.GetWorkspaceUsers(workspaceSlug);

            return MapUsers(users);
        }

        public async Task<ClientResponse> InviteUserToWorkspace(string emailAddress, int workspaceId)
        {
            var workspace = await WorkspaceRepository.GetAsync(workspaceId);

            if (workspace is null)
            {
                return ClientResponse.Failed("workspace not found");
            }

            var user = await UserRepository.GetByEmail(emailAddress);

            if (user is null)
            {
                return ClientResponse.Failed("user not found");
            }

            var existing = await UserRepository.IsUserInWorkspace(user.Id, workspaceId);

            if (existing)
            {
                return ClientResponse.Failed("User is already a member of the workspace");
            }

            var result = await UserRepository.InviteUserToWorkspace(user.Id, workspaceId);

            if (result is null) throw new Exception();

            await UnitOfWork.CompleteAsync();

            await SendUserInviteEmail(user, workspace);

            return ClientResponse.Success();
        }

        public async Task<UserViewModel> Update(AppUser user)
        {
            var updatedUser = await UserRepository.GetAsync(user.Id);

            if (updatedUser is null) return null;
            
            updatedUser.PhoneNumber = user.PhoneNumber;
            updatedUser.Firstname = user.Firstname;
            updatedUser.Lastname = user.Lastname;

            await UnitOfWork.CompleteAsync();

            return MapUser(updatedUser);
        }

        private Task SendUserInviteEmail(AppUser user, Workspace workspace)
        {
            var uri = Hosting.ClientOrigin
                .AppendPathSegments("app", workspace.Slug)
                .SetQueryParam("userId", user.Id, true)
                .SetQueryParam("refer", "invite");

            return Email.Send(new SendEmailModel
            {
                ToAddress = user.Email,
                ToDisplayName = user.GetDisplayName(),
                Name = user.Firstname,
                Action = "Got to the Workspace",
                Link = uri,
                Message = $"Hi you've been invited to join the {workspace} in Netptune.",
                Subject = "Netptune workspace invitation.",
                PreHeader = $"Hi you've been invited to join the {workspace} in Netptune."
            });
        }

        private UserViewModel MapUser(AppUser user)
        {
            return Mapper.Map<AppUser, UserViewModel>(user);
        }

        private List<UserViewModel> MapUsers(List<AppUser> users)
        {
            return Mapper.Map<List<AppUser>, List<UserViewModel>>(users);
        }
    }
}
