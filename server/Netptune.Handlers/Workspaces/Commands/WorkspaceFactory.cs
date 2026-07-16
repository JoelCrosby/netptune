using Netptune.Core.Authorization;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Onboarding.Templates;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Workspace;
using Netptune.Handlers.Onboarding.Templates;

namespace Netptune.Handlers.Workspaces.Commands;

internal static class WorkspaceFactory
{
    internal static Task<ClientResponse<WorkspaceViewModel>> CreateAsync(
        AddWorkspaceRequest request,
        AppUser user,
        INetptuneUnitOfWork unitOfWork,
        long storageLimitBytes,
        CancellationToken cancellationToken = default)
    {
        return unitOfWork.Transaction(async () =>
        {
            var template = WorkspaceSetupTemplateCatalog.Find(request.TemplateKey);

            if (template is null)
            {
                return ClientResponse<WorkspaceViewModel>.Failed("Setup template not found");
            }

            var entity = new Workspace
            {
                Name = request.Name,
                Description = request.Description,
                CreatedByUserId = user.Id,
                OwnerId = user.Id,
                Slug = request.Slug.ToUrlSlug(),
                MetaInfo = request.MetaInfo,
                StorageLimitBytes = Math.Max(0, storageLimitBytes),
            };

            var workspace = await unitOfWork.Workspaces.AddAsync(entity, cancellationToken);

            await unitOfWork.CompleteAsync(cancellationToken);
            await WorkspaceSetupTemplateApplicator.MergeWorkspaceDefaultsAsync(
                template,
                workspace.Id,
                user.Id,
                unitOfWork,
                cancellationToken);

            var defaultStatus = await unitOfWork.Statuses.GetTaskStatusByKey(
                                    workspace.Id,
                                    WorkspaceSetupTemplateCatalog.NewStatusKey,
                                    cancellationToken)
                                ?? await unitOfWork.Statuses.GetFirstTaskStatus(workspace.Id, cancellationToken);

            if (defaultStatus is null) return ClientResponse<WorkspaceViewModel>.Failed("Default task status not found");

            var boardGroups = await WorkspaceSetupTemplateApplicator.ResolveBoardGroupsAsync(
                template,
                workspace.Id,
                unitOfWork,
                cancellationToken);

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

            var projectKey = await unitOfWork.Projects.GenerateProjectKey(workspace.Slug, workspace.Id, cancellationToken);

            var project = Project.Create(new()
            {
                Name = request.Name,
                Description = request.Description,
                Key = projectKey,
                UserId = user.Id,
                WorkspaceId = workspace.Id,
                DefaultStatusId = defaultStatus.Id,
                BoardGroups = boardGroups,
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
