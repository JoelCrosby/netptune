using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AutoMapper;

using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Models;
using Netptune.Models.Relationships;
using Netptune.Models.ViewModels.Users;

namespace Netptune.Services
{
    public class UserService : IUserService
    {
        protected readonly INetptuneUnitOfWork UnitOfWork;
        protected readonly IMapper Mapper;
        protected readonly IUserRepository UserRepository;
        protected readonly IWorkspaceRepository WorkspaceRepository;

        public UserService(INetptuneUnitOfWork unitOfWork, IMapper mapper)
        {
            UnitOfWork = unitOfWork;
            Mapper = mapper;
            UserRepository = unitOfWork.Users;
            WorkspaceRepository = unitOfWork.Workspaces;
        }

        public Task<AppUser> Get(string userId)
        {
            return UserRepository.GetAsync(userId);
        }

        public Task<AppUser> GetByEmail(string email)
        {
            return UserRepository.GetByEmail(email);
        }

        public async Task<List<UserViewModel>> GetWorkspaceUsers(int workspaceId)
        {
            var users = await UserRepository.GetWorkspaceUsers(workspaceId);

            return Mapper.Map<List<AppUser>, List<UserViewModel>>(users);
        }

        public async Task<WorkspaceAppUser> InviteUserToWorkspace(string userId, int workspaceId)
        {
            var user = await UserRepository.GetAsync(userId);
            var workspace = await WorkspaceRepository.GetAsync(workspaceId);

            // TODO: Replace exceptions with return result type.

            if (user == null)
            {
                throw new Exception("user not found");
            }

            if (workspace == null)
            {
                throw new Exception("workspace not found");
            }

            if (await UserRepository.IsUserInWorkspace(userId, workspaceId))
            {
                throw new Exception("User is already a member of the workspace");
            }

            var result = await UserRepository.InviteUserToWorkspace(userId, workspaceId);

            if (result == null) throw new Exception();

            await UnitOfWork.CompleteAsync();

            return result;
        }

        public async Task<AppUser> Update(AppUser user, string userId)
        {
            var updatedUser = await UserRepository.GetAsync(user.Id);

            if (updatedUser is null)
            {
                return null;
            }

            updatedUser.PhoneNumber = user.PhoneNumber;
            updatedUser.Firstname = user.Firstname;
            updatedUser.Lastname = user.Lastname;

            await UnitOfWork.CompleteAsync();

            return updatedUser;
        }
    }
}
