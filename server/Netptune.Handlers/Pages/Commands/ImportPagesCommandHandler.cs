using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Pages;
using Netptune.Core.Models.Search;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Pages.Commands;

public sealed record ImportPagesRequest
{
    public int? ParentId { get; init; }

    public int? ProjectId { get; init; }
}

public sealed record ImportPagesResult
{
    public int Created { get; init; }

    public List<string> Skipped { get; init; } = [];

    public string? FirstSlug { get; init; }
}

public sealed record ImportPagesCommand(ImportPagesRequest Request, PageImportRead Upload) : IRequest<ClientResponse<ImportPagesResult>>;

public sealed class ImportPagesCommandHandler : IRequestHandler<ImportPagesCommand, ClientResponse<ImportPagesResult>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;
    private readonly IEventPublisher EventPublisher;

    public ImportPagesCommandHandler(
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

    public async ValueTask<ClientResponse<ImportPagesResult>> Handle(ImportPagesCommand request, CancellationToken cancellationToken)
    {
        var upload = request.Upload;

        if (upload.Error is not null)
        {
            return ClientResponse<ImportPagesResult>.Failed(upload.Error);
        }

        if (upload.Files.Count == 0)
        {
            return ClientResponse<ImportPagesResult>.Failed("There were no markdown files to import.");
        }

        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var placementRequest = new PagePlacementRequest(request.Request.ParentId, request.Request.ProjectId, workspaceId);
        var placement = await PagePlacement.Resolve(UnitOfWork, placementRequest, cancellationToken);

        if (placement.Error is not null)
        {
            return ClientResponse<ImportPagesResult>.Failed(placement.Error);
        }

        var nodes = PageImportPlan.Build(upload.Files);
        var firstSortOrder = await UnitOfWork.Pages.GetNextSortOrder(workspaceId, request.Request.ParentId, cancellationToken);
        var origin = new ImportOrigin
        {
            Placement = placement,
            WorkspaceId = workspaceId,
            UserId = userId,
            FirstSortOrder = firstSortOrder,
        };
        var pages = CreatePages(nodes, origin);

        await UnitOfWork.Transaction(async () =>
        {
            await UnitOfWork.Pages.AddRangeAsync(pages, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);

            var versions = pages.Select(page => ToFirstVersion(page, userId)).ToList();

            await UnitOfWork.PageVersions.AddRangeAsync(versions, cancellationToken);
            await UnitOfWork.CompleteAsync(cancellationToken);
        });

        var pageIds = pages.ConvertAll(page => page.Id);

        await EventPublisher.IndexPages(pageIds, workspaceKey);

        Activity.LogMany(options =>
        {
            options.EntityIds = pageIds;
            options.EntityType = EntityType.Page;
            options.Type = ActivityType.Create;
        });

        var opens = pages.Count == 1 ? pages[0] : pages.FirstOrDefault(page => page.Parent is null);

        return ClientResponse<ImportPagesResult>.Success(new ImportPagesResult
        {
            Created = pages.Count,
            Skipped = upload.Skipped,
            FirstSlug = opens?.Slug,
        });
    }

    private static List<Page> CreatePages(List<PageImportNode> nodes, ImportOrigin origin)
    {
        var pagesByKey = new Dictionary<string, Page>(StringComparer.OrdinalIgnoreCase);
        var siblingCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodes)
        {
            var importedParent = node.ParentKey is null ? null : pagesByKey[node.ParentKey];
            var siblingKey = node.ParentKey ?? string.Empty;
            var position = siblingCounts.GetValueOrDefault(siblingKey);
            var sortOrder = node.ParentKey is null ? origin.FirstSortOrder + position : position + 1;
            var isDocument = node.Kind == PageKind.Document;
            var body = isDocument ? node.Body ?? string.Empty : null;

            siblingCounts[siblingKey] = position + 1;

            var page = new Page
            {
                Kind = node.Kind,
                // The placement parent was read untracked, so only its id is set; imported parents are new entities.
                Parent = importedParent,
                ParentId = importedParent is null ? origin.Placement.Parent?.Id : null,
                ProjectId = origin.Placement.ProjectId,
                Title = node.Title,
                Slug = PageSlug.Create(node.Title, body),
                Body = body,
                SortOrder = sortOrder,
                Status = PageStatus.Draft,
                Revision = 1,
                WorkspaceId = origin.WorkspaceId,
                CreatedByUserId = origin.UserId,
                OwnerId = origin.UserId,
            };

            pagesByKey[node.Key] = page;
        }

        return [.. pagesByKey.Values];
    }

    private static PageVersion ToFirstVersion(Page page, string userId)
    {
        var snapshot = new PageVersionSnapshot
        {
            PageId = page.Id,
            Revision = page.Revision,
            Title = page.Title,
            Body = page.Body,
            UserId = userId,
        };

        return PageVersionRecorder.ToVersion(snapshot, DateTime.UtcNow);
    }

    private sealed record ImportOrigin
    {
        public required PagePlacementResult Placement { get; init; }

        public int WorkspaceId { get; init; }

        public required string UserId { get; init; }

        public double FirstSortOrder { get; init; }
    }
}
