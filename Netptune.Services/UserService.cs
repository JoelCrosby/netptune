using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AutoMapper;

using Netptune.Core.Entities;
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
        private readonly IUserRepository UserRepository;
        private readonly IWorkspaceRepository WorkspaceRepository;

        public UserService(INetptuneUnitOfWork unitOfWork, IMapper mapper)
        {
            UnitOfWork = unitOfWork;
            Mapper = mapper;
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

        public async Task<ClientResponse> InviteUserToWorkspace(string userId, int workspaceId)
        {
            var user = await UserRepository.GetAsync(userId);
            var workspace = await WorkspaceRepository.GetAsync(workspaceId);

            // TODO: Replace exceptions with return result type.

            if (user is null)
            {
                ClientResponse.Failed("user not found");
            }

            if (workspace is null)
            {
                ClientResponse.Failed("workspace not found");
            }

            if (await UserRepository.IsUserInWorkspace(userId, workspaceId))
            {
                ClientResponse.Failed("User is already a member of the workspace");
            }

            var result = await UserRepository.InviteUserToWorkspace(userId, workspaceId);

            if (result is null) throw new Exception();

            await UnitOfWork.CompleteAsync();

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
