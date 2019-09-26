using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Models;
using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Models;
using Netptune.Services.Common;

namespace Netptune.Services
{
    public class WorkspaceService : ServiceBase, IWorkspaceService
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

            if (result == null) return BadRequest<Workspace>();

            await _unitOfWork.CompleteAsync();

            return Ok(result);
        }

        public async Task<ServiceResult<Workspace>> DeleteWorkspace(int id)
        {
            var result = await _workspaceRepository.DeleteWorkspace(id);

            if (result == null) return BadRequest<Workspace>();

            await _unitOfWork.CompleteAsync();

            return Ok(result);
        }

        public async Task<ServiceResult<Workspace>> GetWorkspace(int id)
        {
            var result = await _workspaceRepository.GetAsync(id);

            if (result == null) return BadRequest<Workspace>();

            return Ok(result);
        }


        public async Task<ServiceResult<IEnumerable<Workspace>>> GetWorkspaces(AppUser user)
        {
            var result = await _workspaceRepository.GetWorkspaces(user);

            if (result == null) return BadRequest<IEnumerable<Workspace>>();

            return Ok(result);
        }

        public async Task<ServiceResult<Workspace>> UpdateWorkspace(Workspace workspace, AppUser user)
        {
            var result = await _workspaceRepository.UpdateWorkspace(workspace, user);

            if (result == null) return BadRequest<Workspace>();

            await _unitOfWork.CompleteAsync();

            return Ok(result);
        }
    }
}
