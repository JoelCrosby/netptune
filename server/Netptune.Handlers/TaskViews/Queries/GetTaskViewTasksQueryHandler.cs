using System.Text.Json;

using Mediator;

using Netptune.Core.Encoding;
using Netptune.Core.Repositories;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Query.Views;

namespace Netptune.Handlers.TaskViews.Queries;

public sealed record GetTaskViewTasksRequest
{
    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }
}

public sealed record GetTaskViewTasksQuery(string Slug, GetTaskViewTasksRequest Request) : IRequest<ClientResponse<TaskViewResultViewModel>>;

public sealed class GetTaskViewTasksQueryHandler : IRequestHandler<GetTaskViewTasksQuery, ClientResponse<TaskViewResultViewModel>>
{
    private readonly ITaskViewRepository TaskViews;
    private readonly IIdentityService Identity;
    private readonly TaskViewQueryRunner Runner;

    public GetTaskViewTasksQueryHandler(IIdentityService identity, ITaskViewRepository taskViews, TaskViewQueryRunner runner)
    {
        Identity = identity;
        TaskViews = taskViews;
        Runner = runner;
    }

    public async ValueTask<ClientResponse<TaskViewResultViewModel>> Handle(GetTaskViewTasksQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var view = await TaskViews.GetBySlug(request.Slug, workspaceId, true, cancellationToken);

        if (view is null)
        {
            return ClientResponse<TaskViewResultViewModel>.NotFound;
        }

        var userId = Identity.GetCurrentUserId();
        var isVisible = view.IsShared || view.CreatedByUserId == userId;

        if (!isVisible)
        {
            return ClientResponse<TaskViewResultViewModel>.NotFound;
        }

        var definition = view.Definition.Deserialize<TaskViewDefinition>(JsonOptions.Default);

        if (definition is null)
        {
            return ClientResponse<TaskViewResultViewModel>.Failed("This view's definition could not be read.");
        }

        var input = request.Request;
        var display = definition.Display;
        var queryRequest = new TaskViewQueryRequest
        {
            Query = definition.Query,
            Page = input.Page,
            PageSize = input.PageSize ?? display.PageSize,
            SortBy = input.SortBy ?? display.SortBy,
            SortDirection = input.SortDirection ?? display.SortDirection,
        };
        var result = await Runner.Run(queryRequest, cancellationToken);

        return result;
    }
}
