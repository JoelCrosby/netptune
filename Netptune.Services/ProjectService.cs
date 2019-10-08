using System.Collections.Generic;
using System.Threading.Tasks;

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

        public Task<ProjectViewModel> AddProject(Project project, AppUser user)
        {
            return _unitOfWork.Transaction(async () =>
            {
                var result = await _projectRepository.AddProject(project, user);

                await _unitOfWork.CompleteAsync();

                return await GetProjectViewModel(result.Id);
            });
        }

        public async Task<ProjectViewModel> DeleteProject(int id)
        {
            var result = await _projectRepository.DeleteProject(id);

            await _unitOfWork.CompleteAsync();

            return await GetProjectViewModel(result);
        }

        public async Task<ProjectViewModel> GetProject(int id)
        {
            var result = await _projectRepository.GetProject(id);

            return await GetProjectViewModel(result);
        }

        public Task<List<ProjectViewModel>> GetProjects(int workspaceId)
        {
            return _projectRepository.GetProjects(workspaceId);
        }

        public async Task<ProjectViewModel> UpdateProject(Project project, AppUser user)
        {
            var result = await _projectRepository.UpdateProject(project, user);

            await _unitOfWork.CompleteAsync();

            return await GetProjectViewModel(result);
        }

        private Task<ProjectViewModel> GetProjectViewModel(Project project)
        {
            return _projectRepository.GetProjectViewModel(project.Id);
        }

        private Task<ProjectViewModel> GetProjectViewModel(int projectId)
        {
            return _projectRepository.GetProjectViewModel(projectId);
        }
    }
}
