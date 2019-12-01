using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Models;
using Netptune.Models.Relationships;
using Netptune.Models.ViewModels.Projects;

namespace Netptune.Services
{
    public class ProjectService : IProjectService
    {
        protected readonly IProjectRepository ProjectRepository;
        protected readonly INetptuneUnitOfWork UnitOfWork;

        public ProjectService(INetptuneUnitOfWork unitOfWork)
        {
            ProjectRepository = unitOfWork.Projects;
            UnitOfWork = unitOfWork;
        }

        public Task<ProjectViewModel> AddProject(Project project, AppUser user)
        {
            return UnitOfWork.Transaction(async () =>
            {
                project.CreatedByUserId = user.Id;
                project.OwnerId = user.Id;

                var userRelationship = new ProjectUser
                {
                    ProjectId = project.Id,
                    UserId = user.Id
                };

                project.ProjectUsers.Add(userRelationship);

                var workspace = await UnitOfWork.Workspaces.GetAsync(project.WorkspaceId);

                workspace.Projects.Add(project);

                await UnitOfWork.CompleteAsync();

                return await GetProjectViewModel(project.Id);
            });
        }

        public async Task<ProjectViewModel> DeleteProject(int id)
        {
            var result = await ProjectRepository.DeleteProject(id);

            await UnitOfWork.CompleteAsync();

            return await GetProjectViewModel(result);
        }

        public async Task<ProjectViewModel> GetProject(int id)
        {
            var result = await ProjectRepository.GetProject(id);

            return await GetProjectViewModel(result);
        }

        public Task<List<ProjectViewModel>> GetProjects(int workspaceId)
        {
            return ProjectRepository.GetProjects(workspaceId);
        }

        public async Task<ProjectViewModel> UpdateProject(Project project, AppUser user)
        {
            var result = await ProjectRepository.GetAsync(project.Id);

            if (result is null) return null;
            
            result.Name = project.Name;
            result.Description = project.Description;
            result.ModifiedByUserId = user.Id;

            await UnitOfWork.CompleteAsync();

            return await GetProjectViewModel(result);
        }

        private Task<ProjectViewModel> GetProjectViewModel(Project project)
        {
            return ProjectRepository.GetProjectViewModel(project.Id);
        }

        private Task<ProjectViewModel> GetProjectViewModel(int projectId)
        {
            return ProjectRepository.GetProjectViewModel(projectId);
        }
    }
}
