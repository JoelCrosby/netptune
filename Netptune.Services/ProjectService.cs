using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Repositories;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Models;
using Netptune.Models.Relationships;
using Netptune.Models.Requests;
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

        public Task<ProjectViewModel> AddProject(AddProjectRequest request, AppUser user)
        {
            return UnitOfWork.Transaction(async () =>
            {
                var project = new Project
                {
                    Name = request.Name,
                    Description = request.Description,
                    CreatedByUserId = user.Id,
                    OwnerId = user.Id,
                    RepositoryUrl = request.RepositoryUrl
                };

                project.ProjectUsers.Add(new ProjectUser
                {
                    ProjectId = project.Id,
                    UserId = user.Id
                });

                var workspace = await UnitOfWork.Workspaces.GetBySlug(request.Workspace);

                workspace.Projects.Add(project);

                await UnitOfWork.CompleteAsync();

                return await GetProjectViewModel(project.Id);
            });
        }

        public async Task<ProjectViewModel> DeleteProject(int id, AppUser user)
        {
            var project = await ProjectRepository.GetAsync(id);
            
            if (project is null) return null;

            project.IsDeleted = true;
            project.DeletedByUserId = user.Id;

            await UnitOfWork.CompleteAsync();

            return await GetProjectViewModel(project);
        }
        
        public async Task<ProjectViewModel> GetProject(int id)
        {
            var result = await ProjectRepository.GetAsync(id);

            return await GetProjectViewModel(result);
        }

        public Task<List<ProjectViewModel>> GetProjects(string workspaceSlug)
        {
            return ProjectRepository.GetProjects(workspaceSlug);
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
