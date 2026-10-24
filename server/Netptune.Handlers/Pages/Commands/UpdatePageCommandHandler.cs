using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Models.Search;
using Netptune.Core.Relationships;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;

namespace Netptune.Handlers.Pages.Commands;

public sealed record UpdatePageCommand(int Id, UpdatePageRequest Request) : IRequest<ClientResponse<PageViewModel>>;

public sealed class UpdatePageCommandHandler : IRequestHandler<UpdatePageCommand, ClientResponse<PageViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;

    public UpdatePageCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IActivityLogger activity,
        IEventPublisher eventPublisher)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
        EventPublisher = eventPublisher;
    }

    public async ValueTask<ClientResponse<PageViewModel>> Handle(UpdatePageCommand request, CancellationToken cancellationToken)
    {
        var input = request.Request;
        var hasUnknownStatus = input.Status.HasValue && !Enum.IsDefined(input.Status.Value);

        if (hasUnknownStatus)
        {
            return ClientResponse<PageViewModel>.Failed(AppText.ErrorUnknownPageStatus);
        }

        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var page = await UnitOfWork.Pages.GetLiveWithTags(request.Id, workspaceId, cancellationToken);

        if (page is null)
        {
            return ClientResponse<PageViewModel>.NotFound;
        }

        if (input.TagIds is { } tagIds)
        {
            var distinctIds = tagIds.Distinct().ToList();

            // A tag deleted while the page was open still comes back in the client's set; dropping ids that are not
            // live in this workspace keeps that from failing every later edit, and keeps foreign ids out.
            var requestedIds = await UnitOfWork.Tags.GetExistingIds(distinctIds, workspaceId, cancellationToken);
            var kept = page.PageTags.Where(pageTag => requestedIds.Contains(pageTag.TagId)).ToList();
            var keptIds = kept.Select(pageTag => pageTag.TagId).ToHashSet();
            var added = requestedIds
                .Where(tagId => !keptIds.Contains(tagId))
                .Select(tagId => new PageTag { PageId = page.Id, TagId = tagId });

            page.PageTags = [.. kept, .. added];
        }

        if (input.Status is { } status)
        {
            page.Status = status;
        }

        page.ModifiedByUserId = userId;

        await UnitOfWork.CompleteAsync(cancellationToken);
        await EventPublisher.IndexPages([page.Id], workspaceKey);

        var viewModel = await UnitOfWork.Pages.GetViewModel(page.Id, workspaceId, userId, cancellationToken);

        if (viewModel is null)
        {
            return ClientResponse<PageViewModel>.NotFound;
        }

        var statusChanged = input.Status.HasValue;
        var activityType = statusChanged ? ActivityType.ModifyStatus : ActivityType.Modify;

        Activity.Log(options =>
        {
            options.EntityId = page.Id;
            options.EntityType = EntityType.Page;
            options.Type = activityType;
        });

        return ClientResponse<PageViewModel>.Success(viewModel);
    }
}
