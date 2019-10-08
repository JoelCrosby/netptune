using System.Collections.Generic;
using System.Threading.Tasks;

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

        public async Task<Workspace> AddWorkspace(Workspace workspace, AppUser user)
        {
            var result = await _workspaceRepository.AddWorkspace(workspace, user);

            await _unitOfWork.CompleteAsync();

            return result;
        }

        public async Task<Workspace> DeleteWorkspace(int id)
        {
            var result = await _workspaceRepository.DeleteWorkspace(id);

            await _unitOfWork.CompleteAsync();

            return result;
        }

        public Task<Workspace> GetWorkspace(int id)
        {
            return _workspaceRepository.GetAsync(id);
        }


        public Task<List<Workspace>> GetWorkspaces(AppUser user)
        {
            return _workspaceRepository.GetWorkspaces(user);
        }

        public async Task<Workspace> UpdateWorkspace(Workspace workspace, AppUser user)
        {
            var result = await _workspaceRepository.UpdateWorkspace(workspace, user);

            await _unitOfWork.CompleteAsync();

            return result;
        }
    }
}
