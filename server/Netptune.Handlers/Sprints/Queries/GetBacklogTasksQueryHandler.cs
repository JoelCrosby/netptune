using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Handlers.Sprints.Queries;

public sealed record GetBacklogTasksQuery(TaskFilter Filter) : IRequest<ClientResponse<PagedResponse<TaskViewModel>>>;

public sealed class GetBacklogTasksQueryHandler : IRequestHandler<GetBacklogTasksQuery, ClientResponse<PagedResponse<TaskViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetBacklogTasksQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<PagedResponse<TaskViewModel>>> Handle(GetBacklogTasksQuery request, CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();

        // Backlog tasks are, by definition, tasks not assigned to any sprint.
        // Force NoSprint regardless of what the client sends so the endpoint
        // can never be used to page over sprint-assigned tasks.
        var filter = new TaskFilter
        {
            ProjectId = request.Filter.ProjectId,
            Search = request.Filter.Search,
            Tags = request.Filter.Tags,
            StatusIds = request.Filter.StatusIds,
            StatusCategories = request.Filter.StatusCategories,
            Assignees = request.Filter.Assignees,
            Page = request.Filter.Page,
            PageSize = request.Filter.PageSize,
            SortBy = request.Filter.SortBy,
            SortDirection = request.Filter.SortDirection,
            NoSprint = true,
        };

        return await UnitOfWork.Tasks.GetTasksAsync(workspaceKey, filter, true, cancellationToken);
    }
}
