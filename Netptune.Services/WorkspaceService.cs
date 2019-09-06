using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Models;
using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Models;

namespace Netptune.Services
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly INetptuneUnitOfWork _unitOfWork;
        private readonly IWorkspaceRepository _workspaceRepository;

        public WorkspaceService(INetptuneUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _workspaceRepository = unitOfWork.Workspaces;
        }

        public async Task<ServiceResult<Workspace>> AddWorkspace(Workspace workspace, AppUser user)
        {
            var result = await _workspaceRepository.AddWorkspace(workspace, user);

            if (result == null) return ServiceResult<Workspace>.BadRequest();

            await _unitOfWork.CompleteAsync();

            return ServiceResult<Workspace>.Ok(result);
        }

        public async Task<ServiceResult<Workspace>> DeleteWorkspace(int id)
        {
            var result = await _workspaceRepository.DeleteWorkspace(id);

            if (result == null) return ServiceResult<Workspace>.BadRequest();

            await _unitOfWork.CompleteAsync();

            return ServiceResult<Workspace>.Ok(result);
        }

        public async Task<ServiceResult<Workspace>> GetWorkspace(int id)
        {
            var result = await _workspaceRepository.GetAsync(id);

            if (result == null) return ServiceResult<Workspace>.BadRequest();

            return ServiceResult<Workspace>.Ok(result);
        }


        public async Task<ServiceResult<IEnumerable<Workspace>>> GetWorkspaces(AppUser user)
        {
            var result = await _workspaceRepository.GetWorkspaces(user);

            if (result == null) return ServiceResult<IEnumerable<Workspace>>.BadRequest();

            return ServiceResult<IEnumerable<Workspace>>.Ok(result);
        }

        public async Task<ServiceResult<Workspace>> UpdateWorkspace(Workspace workspace, AppUser user)
        {
            var result = await _workspaceRepository.UpdateWorkspace(workspace, user);

            if (result == null) return ServiceResult<Workspace>.BadRequest();

            await _unitOfWork.CompleteAsync();

            return ServiceResult<Workspace>.Ok(result);
        }
    }
}
