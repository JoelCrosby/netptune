using Netptune.Core.Authorization;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Workspace;

namespace Netptune.Services.Workspaces.Commands;

internal static class WorkspaceFactory
{
    internal static Task<ClientResponse<WorkspaceViewModel>> CreateAsync(AddWorkspaceRequest request, AppUser user, INetptuneUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
    {
        return unitOfWork.Transaction(async () =>
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

            var workspace = await unitOfWork.Workspaces.AddAsync(entity);

            await unitOfWork.CompleteAsync(cancellationToken);

            var permissions = WorkspaceRolePermissions
                .GetDefaultPermissions(WorkspaceRole.Owner)
                .ToList();

            workspace.WorkspaceUsers.Add(new WorkspaceAppUser
            {
                UserId = user.Id,
                WorkspaceId = workspace.Id,
                Role = WorkspaceRole.Owner,
                Permissions = permissions,
            });

            var projectKey = await unitOfWork.Projects.GenerateProjectKey(workspace.Slug, workspace.Id);

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

            await unitOfWork.CompleteAsync(cancellationToken);

            return ClientResponse<WorkspaceViewModel>.Success(workspace.ToViewModel());
        });
    }
}
