using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Models;
using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Models;

namespace Netptune.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly INetptuneUnitOfWork _unitOfWork;

        public ProjectService(INetptuneUnitOfWork unitOfWork)
        {
            _projectRepository = unitOfWork.Projects;
            _unitOfWork = unitOfWork;
        }

        public async Task<ServiceResult<Project>> AddProject(Project project, AppUser user)
        {
            var result = await _projectRepository.AddProject(project, user);

            if (result == null) return ServiceResult<Project>.BadRequest();

            await _unitOfWork.CompleteAsync();

            return ServiceResult<Project>.Ok(result);
        }

        public async Task<ServiceResult<Project>> DeleteProject(int id)
        {
            var result = await _projectRepository.DeleteProject(id);

            if (result == null) return ServiceResult<Project>.BadRequest();

            await _unitOfWork.CompleteAsync();

            return ServiceResult<Project>.Ok(result);
        }

        public async Task<ServiceResult<Project>> GetProject(int id)
        {
            var result = await _projectRepository.GetProject(id);

            if (result == null) return ServiceResult<Project>.BadRequest();

            return ServiceResult<Project>.Ok(result);
        }

        public async Task<ServiceResult<IEnumerable<Project>>> GetProjects(int workspaceId)
        {
            var result = await _projectRepository.GetProjects(workspaceId);

            if (result == null) return ServiceResult<IEnumerable<Project>>.BadRequest();

            return ServiceResult<IEnumerable<Project>>.Ok(result);
        }

        public async Task<ServiceResult<Project>> UpdateProject(Project project, AppUser user)
        {
            var result = await _projectRepository.UpdateProject(project, user);

            if (result == null) return ServiceResult<Project>.BadRequest();

            await _unitOfWork.CompleteAsync();

            return ServiceResult<Project>.Ok(result);
        }
    }
}
