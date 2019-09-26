using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Models;
using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Models;
using Netptune.Models.Relationships;
using Netptune.Services.Common;

namespace Netptune.Services
{
    public class UserService : ServiceBase, IUserService
    {
        private readonly INetptuneUnitOfWork _unitOfWork;
        private readonly IUserRepository _userRepository;
        private readonly IWorkspaceRepository _workspaceRepository;

        public UserService(INetptuneUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _userRepository = unitOfWork.Users;
            _workspaceRepository = unitOfWork.Workspaces;
        }

        public async Task<ServiceResult<AppUser>> Get(string userId)
        {
            var result = await _userRepository.GetAsync(userId);

            if (result == null) return NotFound<AppUser>();

            return Ok(result);
        }

        public async Task<ServiceResult<AppUser>> GetByEmail(string email)
        {
            var result = await _userRepository.GetByEmail(email);

            if (result == null) return NotFound<AppUser>();

            return Ok(result);
        }

        public async Task<ServiceResult<IList<AppUser>>> GetWorkspaceUsers(int workspaceId)
        {
            var results = await _userRepository.GetWorkspaceUsers(workspaceId);

            return Ok(results);
        }

        public async Task<ServiceResult<WorkspaceAppUser>> InviteUserToWorkspace(string userId, int workspaceId)
        {
            var user = await _userRepository.GetAsync(userId);
            var workspace = await _workspaceRepository.GetAsync(workspaceId);

            if (user == null)
            {
                return NotFound<WorkspaceAppUser>("user not found");
            }

            if (workspace == null)
            {
                return NotFound<WorkspaceAppUser>("workspace not found");
            }

            if (await _userRepository.IsUserInWorkspace(userId, workspaceId))
            {
                return BadRequest<WorkspaceAppUser>("User is already a member of the workspace");
            }

            var result = await _userRepository.InviteUserToWorkspace(userId, workspaceId);

            if (result == null) return NotFound<WorkspaceAppUser>();

            await _unitOfWork.CompleteAsync();

            return Ok(result);
        }

        public async Task<ServiceResult<AppUser>> Update(AppUser user, string userId)
        {
            var result = await _userRepository.Update(user, userId);

            if (result == null) return NotFound<AppUser>();

            await _unitOfWork.CompleteAsync();

            return Ok(result);
        }
    }
}
