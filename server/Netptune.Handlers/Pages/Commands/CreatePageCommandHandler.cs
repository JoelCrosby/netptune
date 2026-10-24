using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Models.Pages;
using Netptune.Core.Models.Search;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Commands;

public sealed record CreatePageCommand(CreatePageRequest Request) : IRequest<ClientResponse<PageViewModel>>;

public sealed class CreatePageCommandHandler : IRequestHandler<CreatePageCommand, ClientResponse<PageViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;
    private readonly IWorkspacePermissionCache PermissionCache;

    public CreatePageCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IActivityLogger activity,
        IEventPublisher eventPublisher,
        IWorkspacePermissionCache permissionCache)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
        EventPublisher = eventPublisher;
        PermissionCache = permissionCache;
    }

    public async ValueTask<ClientResponse<PageViewModel>> Handle(CreatePageCommand request, CancellationToken cancellationToken)
    {
        var input = request.Request;
        var isKnownKind = Enum.IsDefined(input.Kind);

        if (!isKnownKind)
        {
            return ClientResponse<PageViewModel>.Failed(AppText.ErrorUnknownPageKind);
        }

        var start = await ResolveTemplate(input, cancellationToken);

        if (start.Error is not null)
        {
            return ClientResponse<PageViewModel>.Failed(start.Error);
        }

        var title = PageTitle.Normalise(input.Title ?? start.Title);
        var titleTooLong = PageTitle.IsTooLong(title);

        if (titleTooLong)
        {
            return ClientResponse<PageViewModel>.Failed(PageTitle.TooLongMessage);
        }

        var requestedBody = input.Body ?? start.Body;
        var isFolder = input.Kind == PageKind.Folder;
        var hasBody = !string.IsNullOrEmpty(requestedBody);

        if (isFolder && hasBody)
        {
            return ClientResponse<PageViewModel>.Failed(AppText.ErrorFoldersNoBody);
        }

        var bodyTooLarge = PageBody.IsTooLarge(requestedBody);

        if (bodyTooLarge)
        {
            return ClientResponse<PageViewModel>.Failed(PageBody.TooLargeMessage);
        }

        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var placementRequest = new PagePlacementRequest(input.ParentId, input.ProjectId, workspaceId);
        var placement = await PagePlacement.Resolve(UnitOfWork, placementRequest, cancellationToken);

        if (placement.Error is not null)
        {
            return ClientResponse<PageViewModel>.Failed(placement.Error);
        }

        var sortOrder = await UnitOfWork.Pages.GetNextSortOrder(workspaceId, input.ParentId, cancellationToken);
        var isDocument = input.Kind == PageKind.Document;
        var body = isDocument ? requestedBody ?? string.Empty : null;

        var page = new Page
        {
            Kind = input.Kind,
            ParentId = input.ParentId,
            ProjectId = placement.ProjectId,
            Title = title,
            Slug = PageSlug.Create(title, body),
            Body = body,
            SortOrder = sortOrder,
            Status = PageStatus.Draft,
            Revision = 1,
            WorkspaceId = workspaceId,
            CreatedByUserId = userId,
            OwnerId = userId,
        };

        await UnitOfWork.Transaction(async () =>
        {
            await UnitOfWork.Pages.AddAsync(page, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);

            var firstVersion = new PageVersionSnapshot
            {
                PageId = page.Id,
                Revision = page.Revision,
                Title = page.Title,
                Body = page.Body,
                UserId = userId,
            };

            var version = PageVersionRecorder.ToVersion(firstVersion, DateTime.UtcNow);

            await UnitOfWork.PageVersions.AddAsync(version, cancellationToken);

            if (hasBody)
            {
                var content = new PageContentUpdate
                {
                    PageId = page.Id,
                    WorkspaceId = workspaceId,
                    WorkspaceKey = workspaceKey,
                    ExpectedRevision = page.Revision,
                    Title = page.Title,
                    Slug = page.Slug,
                    Body = body,
                    UserId = userId,
                };

                await PageLinkRebuilder.Rebuild(UnitOfWork, content, cancellationToken);
            }

            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        await EventPublisher.IndexPages([page.Id], workspaceKey);

        var viewModel = await UnitOfWork.Pages.GetViewModel(page.Id, workspaceId, userId, cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = page.Id;
            options.EntityType = EntityType.Page;
            options.Type = ActivityType.Create;
        });

        return ClientResponse<PageViewModel>.Success(viewModel!);
    }

    private async Task<PageStart> ResolveTemplate(CreatePageRequest input, CancellationToken cancellationToken)
    {
        var key = input.TemplateKey;

        if (key is null)
        {
            return new PageStart(null, null, null);
        }

        if (input.Kind != PageKind.Document)
        {
            return new PageStart(null, null, AppText.ErrorTemplatesMakeDocumentsNotFolders);
        }

        var isFromSprint = string.Equals(key, PageTemplates.FromSprint, StringComparison.Ordinal);

        if (isFromSprint)
        {
            return await ResolveReleaseNotes(input.SprintId, cancellationToken);
        }

        var template = PageTemplates.Find(key);

        if (template is null)
        {
            return new PageStart(null, null, PageTemplates.UnknownMessage);
        }

        return new PageStart(template.Title, template.Body, null);
    }

    private async Task<PageStart> ResolveReleaseNotes(int? sprintId, CancellationToken cancellationToken)
    {
        if (!sprintId.HasValue)
        {
            return new PageStart(null, null, AppText.ErrorChooseSprintWriteReleaseNotes);
        }

        var canReadSprint = await CanReadSprintTasks();

        if (!canReadSprint)
        {
            return new PageStart(null, null, AppText.ErrorYouNeedAccessSprintsTasksWriteRelease);
        }

        var workspaceKey = Identity.GetWorkspaceKey();
        var sprint = await UnitOfWork.Sprints.GetSprintDetailAsync(workspaceKey, sprintId.Value, cancellationToken);

        if (sprint is null)
        {
            return new PageStart(null, null, AppText.ErrorSprintDoesNotExist);
        }

        var completed = sprint.Tasks
            .Where(task => task.StatusCategory == StatusCategory.Done)
            .Select(task => new ReleaseNoteTask(task.SystemId, task.Name))
            .ToList();
        var title = PageTemplates.ReleaseNotesTitle(sprint.Name);
        var body = PageTemplates.ReleaseNotes(sprint.Name, completed);

        return new PageStart(title, body, null);
    }

    private async Task<bool> CanReadSprintTasks()
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceKey = Identity.GetWorkspaceKey();
        var permissions = await PermissionCache.GetUserPermissions(userId, workspaceKey);
        var granted = permissions?.Permissions ?? [];
        var memberCanRead = granted.Contains(
            NetptunePermissions.Sprints.Read) && granted.Contains(NetptunePermissions.Tasks.Read);
        var credentialRestricts = Identity.IsRestrictedByCredential(NetptunePermissions.Sprints.Read)
            || Identity.IsRestrictedByCredential(NetptunePermissions.Tasks.Read);

        return memberCanRead && !credentialRestricts;
    }

    private sealed record PageStart(string? Title, string? Body, string? Error);
}
