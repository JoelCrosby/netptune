using Mediator;

using Netptune.Core.Models.Roadmap;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Reporting;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Roadmap;

namespace Netptune.Handlers.Roadmap.Queries;

public sealed record GetUnscheduledRoadmapTasksQuery(RoadmapUnscheduledTaskFilter Filter)
    : IRequest<ClientResponse<PagedResponse<RoadmapTaskViewModel>>>;

public sealed class GetUnscheduledRoadmapTasksQueryHandler
    : IRequestHandler<GetUnscheduledRoadmapTasksQuery, ClientResponse<PagedResponse<RoadmapTaskViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IReportingScopeResolver ScopeResolver;

    public GetUnscheduledRoadmapTasksQueryHandler(
        INetptuneUnitOfWork unitOfWork,
        IReportingScopeResolver scopeResolver)
    {
        UnitOfWork = unitOfWork;
        ScopeResolver = scopeResolver;
    }

    public async ValueTask<ClientResponse<PagedResponse<RoadmapTaskViewModel>>> Handle(
        GetUnscheduledRoadmapTasksQuery request,
        CancellationToken cancellationToken)
    {
        var scope = await ScopeResolver.Resolve(cancellationToken);

        if (scope is null)
        {
            return ClientResponse<PagedResponse<RoadmapTaskViewModel>>.NotFound;
        }

        var containsUnknownProject = request.Filter.ProjectIds.Any(id => !scope.CanAccessProject(id));

        if (containsUnknownProject)
        {
            return ClientResponse<PagedResponse<RoadmapTaskViewModel>>
                .Failed("One or more projects are outside the current workspace scope");
        }

        try
        {
            var tasks = await UnitOfWork.Roadmaps.GetUnscheduledTasks(scope, request.Filter, cancellationToken);

            return ClientResponse<PagedResponse<RoadmapTaskViewModel>>.Success(tasks);
        }
        catch (InvalidRoadmapFilterException exception)
        {
            return ClientResponse<PagedResponse<RoadmapTaskViewModel>>.Failed(exception.Message);
        }
    }
}
