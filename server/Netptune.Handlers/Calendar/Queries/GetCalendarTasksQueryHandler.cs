using Mediator;

using Netptune.Core.Models.Roadmap;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Reporting;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Roadmap;

namespace Netptune.Handlers.Calendar.Queries;

public sealed record GetCalendarTasksQuery(CalendarTaskFilter Filter)
    : IRequest<ClientResponse<PagedResponse<RoadmapTaskViewModel>>>;

public sealed class GetCalendarTasksQueryHandler
    : IRequestHandler<GetCalendarTasksQuery, ClientResponse<PagedResponse<RoadmapTaskViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IReportingScopeResolver ScopeResolver;

    public GetCalendarTasksQueryHandler(INetptuneUnitOfWork unitOfWork, IReportingScopeResolver scopeResolver)
    {
        UnitOfWork = unitOfWork;
        ScopeResolver = scopeResolver;
    }

    public async ValueTask<ClientResponse<PagedResponse<RoadmapTaskViewModel>>> Handle(
        GetCalendarTasksQuery request,
        CancellationToken cancellationToken)
    {
        var scope = await ScopeResolver.Resolve(cancellationToken);

        if (scope is null)
        {
            return ClientResponse<PagedResponse<RoadmapTaskViewModel>>.NotFound;
        }

        var projectId = request.Filter.ProjectId;
        var containsUnknownProject = projectId.HasValue && !scope.CanAccessProject(projectId.Value);

        if (containsUnknownProject)
        {
            return ClientResponse<PagedResponse<RoadmapTaskViewModel>>
                .Failed("The project is outside the current workspace scope");
        }

        try
        {
            var tasks = await UnitOfWork.Roadmaps.GetCalendarTasks(scope, request.Filter, cancellationToken);

            return ClientResponse<PagedResponse<RoadmapTaskViewModel>>.Success(tasks);
        }
        catch (InvalidRoadmapFilterException exception)
        {
            return ClientResponse<PagedResponse<RoadmapTaskViewModel>>.Failed(exception.Message);
        }
    }
}
