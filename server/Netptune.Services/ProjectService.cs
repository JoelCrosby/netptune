using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository ProjectRepository;
        private readonly INetptuneUnitOfWork UnitOfWork;
        private readonly IIdentityService IdentityService;

        public ProjectService(INetptuneUnitOfWork unitOfWork, IIdentityService identityService)
        {
            ProjectRepository = unitOfWork.Projects;
            UnitOfWork = unitOfWork;
            IdentityService = identityService;
        }

        public Task<ProjectViewModel> AddProject(AddProjectRequest request)
        {
            return UnitOfWork.Transaction(async () =>
            {
                var workspaceId = IdentityService.GetWorkspaceKey();
                var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceId);

                if (workspace is null) return null;

                var user = await IdentityService.GetCurrentUser();

                var projectKey = await UnitOfWork.Projects.GenerateProjectKey(request.Name, workspace.Id);

                var project = Project.Create(new CreateProjectOptions
                {
                    Name = request.Name,
                    Description = request.Description,
                    Key = projectKey,
                    User = user,
                    WorkspaceId = workspace.Id,
                    RepositoryUrl = request.RepositoryUrl
                });

                workspace.Projects.Add(project);

                await UnitOfWork.CompleteAsync();

                return await ProjectRepository.GetProjectViewModel(project.Id);
            });
        }

        public async Task<ClientResponse> Delete(int id)
        {
            var project = await ProjectRepository.GetAsync(id);
            var userId = await IdentityService.GetCurrentUserId();

            if (project is null || userId is null) return null;

            project.Delete(userId);

            await UnitOfWork.CompleteAsync();

            return ClientResponse.Success();
        }

        public Task<ProjectViewModel> GetProject(int id)
        {
            return ProjectRepository.GetProjectViewModel(id);
        }

        public Task<List<ProjectViewModel>> GetProjects()
        {
            var workspaceId = IdentityService.GetWorkspaceKey();
            return ProjectRepository.GetProjects(workspaceId);
        }

        public async Task<ProjectViewModel> UpdateProject(Project project)
        {
            var result = await ProjectRepository.GetAsync(project.Id);
            var user = await IdentityService.GetCurrentUser();

            if (result is null) return null;

            result.Name = project.Name;
            result.Description = project.Description;
            result.ModifiedByUserId = user.Id;

            await UnitOfWork.CompleteAsync();

            return await ProjectRepository.GetProjectViewModel(result.Id);
        }
    }
}
