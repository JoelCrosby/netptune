using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Cache;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Workspace;

namespace Netptune.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService IdentityService;
    private readonly IWorkspaceUserCache Cache;
    private readonly IWorkspaceRepository WorkspaceRepository;

    public WorkspaceService(INetptuneUnitOfWork unitOfWork, IIdentityService identityService, IWorkspaceUserCache cache)
    {
        UnitOfWork = unitOfWork;
        IdentityService = identityService;
        Cache = cache;
        WorkspaceRepository = unitOfWork.Workspaces;
    }

    public async Task<ClientResponse<WorkspaceViewModel>> Create(AddWorkspaceRequest request)
    {
        var user = await IdentityService.GetCurrentUser();
        return await Create(request, user);
    }

    public Task<ClientResponse<WorkspaceViewModel>> CreateNewUserWorkspace(AddWorkspaceRequest request, AppUser newUser)
    {
        return Create(request, newUser);
    }

    private Task<ClientResponse<WorkspaceViewModel>> Create(AddWorkspaceRequest request, AppUser user)
    {
        return UnitOfWork.Transaction(async () =>
        {
            var entity = new Workspace
            {
                Name = request.Name,
                Description = request.Description,
                CreatedByUserId = user.Id,
                OwnerId = user.Id,
                Slug = request.Slug.ToUrlSlug(),
                MetaInfo = request.MetaInfo,
            };

            var workspace = await WorkspaceRepository.AddAsync(entity);

            await UnitOfWork.CompleteAsync();

            workspace.WorkspaceUsers.Add(new()
            {
                UserId = user.Id,
                WorkspaceId = workspace.Id,
            });

            var projectKey = await UnitOfWork.Projects.GenerateProjectKey(workspace.Slug, workspace.Id);

            var project = Project.Create(new()
            {
                Name = request.Name,
                Description = request.Description,
                Key = projectKey,
                UserId = user.Id,
                WorkspaceId = workspace.Id,
                MetaInfo = new()
                {
                    Color = request.MetaInfo.Color,
                },
            });

            workspace.Projects.Add(project);

            await UnitOfWork.CompleteAsync();

            var result = workspace.ToViewModel();

            return ClientResponse<WorkspaceViewModel>.Success(result);
        });
    }

    public async Task<ClientResponse> Delete(string key)
    {
        var workspace = await WorkspaceRepository.GetBySlug(key);

        if (workspace is null)
        {
            return ClientResponse.NotFound;
        }

        var userId = await IdentityService.GetCurrentUserId();

        Cache.Remove(new()
        {
            UserId = userId,
            WorkspaceKey = workspace.Slug,
        });

        workspace.Delete(userId);

        await UnitOfWork.CompleteAsync();

        return ClientResponse.Success();
    }

    public async Task<ClientResponse> DeletePermanent(string key)
    {
        var workspace = await WorkspaceRepository.GetBySlug(key);

        if (workspace is null)
        {
            return ClientResponse.NotFound;
        }

        var userId = await IdentityService.GetCurrentUserId();

        Cache.Remove(new()
        {
            UserId = userId,
            WorkspaceKey = workspace.Slug,
        });

        await UnitOfWork.Transaction(async () =>
        {
            var u = UnitOfWork;
            var workspaceId = workspace.Id;

            var taskIds = await u.Tasks.GetAllIdsInWorkspace(workspaceId, true);
            await u.ProjectTasksInGroups.DeleteAllByTaskId(taskIds);
            await u.ProjectTaskTags.DeleteAllByTaskId(taskIds);

            await u.Tags.DeleteAllInWorkspace(workspaceId);
            await u.Comments.DeleteAllInWorkspace(workspaceId);
            await u.Tasks.DeleteAllInWorkspace(workspaceId);
            await u.BoardGroups.DeleteAllInWorkspace(workspaceId);
            await u.Boards.DeleteAllInWorkspace(workspaceId);
            await u.Projects.DeleteAllInWorkspace(workspaceId);

            await u.Workspaces.DeletePermanent(workspaceId);

            await UnitOfWork.CompleteAsync();
        });

        return ClientResponse.Success();
    }

    public Task<Workspace?> GetWorkspace(string slug)
    {
        return WorkspaceRepository.GetBySlug(slug);
    }

    public async Task<List<Workspace>> GetUserWorkspaces()
    {
        var userId = await IdentityService.GetCurrentUserId();

        return await WorkspaceRepository.GetUserWorkspaces(userId);
    }

    public Task<List<Workspace>> GetAll()
    {
        return WorkspaceRepository.GetAllAsync();
    }

    public async Task<ClientResponse<Workspace>> UpdateWorkspace(Workspace workspace)
    {
        var user = await IdentityService.GetCurrentUser();

        if (workspace is null) throw new ArgumentNullException(nameof(workspace));

        var result = await WorkspaceRepository.GetAsync(workspace.Id);

        if (result is null)
        {
            return ClientResponse<Workspace>.NotFound;
        }

        result.Name = workspace.Name;
        result.Description = workspace.Description;
        result.ModifiedByUserId = user.Id;
        result.MetaInfo = workspace.MetaInfo;

        if (workspace.IsDeleted)
        {
            result.Delete(user.Id);
        }

        result.UpdatedAt = DateTime.UtcNow;

        await UnitOfWork.CompleteAsync();

        return ClientResponse<Workspace>.Success(result);
    }

    public async Task<ClientResponse<IsSlugUniqueResponse>> IsSlugUnique(string slug)
    {
        var slugLower = slug.ToUrlSlug();
        var exists = await WorkspaceRepository.Exists(slugLower);

        return ClientResponse<IsSlugUniqueResponse>.Success(new IsSlugUniqueResponse
        {
            Request = slug,
            Slug = slugLower,
            IsUnique = !exists,
        });
    }
}
