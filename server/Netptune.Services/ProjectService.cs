using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Services;

public class ProjectService : ServiceBase<ProjectViewModel>, IProjectService
{
    private readonly IProjectRepository ProjectRepository;
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService IdentityService;
    private readonly IActivityLogger Activity;

    public ProjectService(INetptuneUnitOfWork unitOfWork, IIdentityService identityService, IActivityLogger activity)
    {
        ProjectRepository = unitOfWork.Projects;
        UnitOfWork = unitOfWork;
        IdentityService = identityService;
        Activity = activity;
    }

    public Task<ClientResponse<ProjectViewModel>> Create(AddProjectRequest request)
    {
        return UnitOfWork.Transaction(async () =>
        {
            var workspaceId = IdentityService.GetWorkspaceKey();
            var workspace = await UnitOfWork.Workspaces.GetBySlug(workspaceId);

            if (workspace is null)
            {
                return NotFound();
            }

            var user = await IdentityService.GetCurrentUser();
            var projectKey = await UnitOfWork.Projects.GenerateProjectKey(request.Name, workspace.Id);

            var project = Project.Create(new ()
            {
                Name = request.Name,
                Description = request.Description,
                Key = projectKey,
                UserId = user.Id,
                WorkspaceId = workspace.Id,
                RepositoryUrl = request.RepositoryUrl,
                MetaInfo = request.MetaInfo,
            });

            workspace.Projects.Add(project);

            await UnitOfWork.CompleteAsync();

            var result = await ProjectRepository.GetProjectViewModel(project.Id);

            if (result is null)
            {
                return Failed("Project not found");
            }

            Activity.Log(options =>
            {
                options.EntityId = project.Id;
                options.EntityType = EntityType.Project;
                options.Type = ActivityType.Create;
            });

            return result;
        });
    }

    public async Task<ClientResponse> Delete(int id)
    {
        var project = await ProjectRepository.GetAsync(id);
        var userId = IdentityService.GetCurrentUserId();

        if (project is null)
        {
            return ClientResponse.NotFound;
        }

        project.Delete(userId);

        await UnitOfWork.CompleteAsync();

        Activity.Log(options =>
        {
            options.EntityId = project.Id;
            options.EntityType = EntityType.Project;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }

    public async Task<ProjectViewModel?> GetProject(string projectKey)
    {
        var workspaceKey = IdentityService.GetWorkspaceKey();
        var workspaceId = await UnitOfWork.Workspaces.GetIdBySlug(workspaceKey);

        if (workspaceId is null) return null;

        return await ProjectRepository.GetProjectViewModel(projectKey, workspaceId.Value);
    }

    public Task<List<ProjectViewModel>> GetProjects()
    {
        var workspaceId = IdentityService.GetWorkspaceKey();
        return ProjectRepository.GetProjects(workspaceId);
    }

    public async Task<ClientResponse<ProjectViewModel>> Update(UpdateProjectRequest request)
    {
        var project = await ProjectRepository.GetWithIncludes(request.Id!.Value);
        var user = await IdentityService.GetCurrentUser();

        if (project is null)
        {
            return NotFound();
        }

        project.Name = request.Name ?? project.Name;
        project.Description = request.Description ?? project.Description;
        project.RepositoryUrl = request.RepositoryUrl ?? project.RepositoryUrl;
        project.Key = request.Key ?? project.Key;
        project.ModifiedByUserId = user.Id;

        await UnitOfWork.CompleteAsync();

        Activity.Log(options =>
        {
            options.EntityId = project.Id;
            options.EntityType = EntityType.Project;
            options.Type = ActivityType.Modify;
        });

        return project.ToViewModel();
    }
}
