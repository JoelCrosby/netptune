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
        private readonly INetptuneUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IUserRepository _userRepository;
        private readonly IWorkspaceRepository _workspaceRepository;

        public UserService(INetptuneUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userRepository = unitOfWork.Users;
            _workspaceRepository = unitOfWork.Workspaces;
        }

        public Task<AppUser> Get(string userId)
        {
            return _userRepository.GetAsync(userId);
        }

        public Task<AppUser> GetByEmail(string email)
        {
            return _userRepository.GetByEmail(email);
        }

        public async Task<List<UserViewModel>> GetWorkspaceUsers(int workspaceId)
        {
            var users = await _userRepository.GetWorkspaceUsers(workspaceId);

            return _mapper.Map<List<AppUser>, List<UserViewModel>>(users);
        }

        public async Task<WorkspaceAppUser> InviteUserToWorkspace(string userId, int workspaceId)
        {
            var user = await _userRepository.GetAsync(userId);
            var workspace = await _workspaceRepository.GetAsync(workspaceId);

            // TODO: Replace exceptions with return result type.

            if (user == null)
            {
                throw new Exception("user not found");
            }

            if (workspace == null)
            {
                throw new Exception("workspace not found");
            }

            if (await _userRepository.IsUserInWorkspace(userId, workspaceId))
            {
                throw new Exception("User is already a member of the workspace");
            }

            var result = await _userRepository.InviteUserToWorkspace(userId, workspaceId);

            if (result == null) throw new Exception();

            await _unitOfWork.CompleteAsync();

            return result;
        }

        public async Task<AppUser> Update(AppUser user, string userId)
        {
            var result = await _userRepository.Update(user, userId);

            await _unitOfWork.CompleteAsync();

            return result;
        }
    }
}
