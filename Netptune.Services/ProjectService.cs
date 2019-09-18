using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Models;
using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Models;
using Netptune.Models.VeiwModels.Projects;

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

        public async Task<ServiceResult<ProjectViewModel>> AddProject(Project project, AppUser user)
        {
            return await _unitOfWork.Transaction(async () =>
            {
                var result = await _projectRepository.AddProject(project, user);

                await _unitOfWork.CompleteAsync();

                return await GetProjectViewModel(result.Id);
            });
        }

        public async Task<ServiceResult<ProjectViewModel>> DeleteProject(int id)
        {
            var result = await _projectRepository.DeleteProject(id);

            if (result == null) return ServiceResult<ProjectViewModel>.BadRequest();

            await _unitOfWork.CompleteAsync();

            return await GetProjectViewModel(result);
        }

        public async Task<ServiceResult<ProjectViewModel>> GetProject(int id)
        {
            var result = await _projectRepository.GetProject(id);

            if (result == null) return ServiceResult<ProjectViewModel>.BadRequest();

            return await GetProjectViewModel(result);
        }

        public async Task<ServiceResult<IEnumerable<ProjectViewModel>>> GetProjects(int workspaceId)
        {
            var result = await _projectRepository.GetProjects(workspaceId);

            if (result == null) return ServiceResult<IEnumerable<ProjectViewModel>>.BadRequest();

            return ServiceResult<IEnumerable<ProjectViewModel>>.Ok(result);
        }

        public async Task<ServiceResult<ProjectViewModel>> UpdateProject(Project project, AppUser user)
        {
            var result = await _projectRepository.UpdateProject(project, user);

            if (result == null) return ServiceResult<ProjectViewModel>.BadRequest();

            await _unitOfWork.CompleteAsync();

            return await GetProjectViewModel(result);
        }

        private async Task<ServiceResult<ProjectViewModel>> GetProjectViewModel(Project project)
        {
            var viewModel = await _projectRepository.GetProjectViewModel(project.Id);

            return ServiceResult<ProjectViewModel>.Ok(viewModel);
        }

        private async Task<ServiceResult<ProjectViewModel>> GetProjectViewModel(int projectId)
        {
            var viewModel = await _projectRepository.GetProjectViewModel(projectId);

            return ServiceResult<ProjectViewModel>.Ok(viewModel);
        }
    }
}
