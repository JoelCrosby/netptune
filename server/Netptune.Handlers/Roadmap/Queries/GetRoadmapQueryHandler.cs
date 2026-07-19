using Mediator;

using Netptune.Core.Models.Roadmap;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Reporting;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Roadmap;

namespace Netptune.Handlers.Roadmap.Queries;

public sealed record GetRoadmapQuery(RoadmapFilter Filter) : IRequest<ClientResponse<RoadmapViewModel>>;

public sealed class GetRoadmapQueryHandler : IRequestHandler<GetRoadmapQuery, ClientResponse<RoadmapViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IReportingScopeResolver ScopeResolver;

    public GetRoadmapQueryHandler(INetptuneUnitOfWork unitOfWork, IReportingScopeResolver scopeResolver)
    {
        UnitOfWork = unitOfWork;
        ScopeResolver = scopeResolver;
    }

    public async ValueTask<ClientResponse<RoadmapViewModel>> Handle(
        GetRoadmapQuery request,
        CancellationToken cancellationToken)
    {
        var validationError = Validate(request.Filter);

        if (validationError is not null)
        {
            return ClientResponse<RoadmapViewModel>.Failed(validationError);
        }

        var scope = await ScopeResolver.Resolve(cancellationToken);

        if (scope is null)
        {
            return ClientResponse<RoadmapViewModel>.NotFound;
        }

        var containsUnknownProject = request.Filter.ProjectIds.Any(id => !scope.CanAccessProject(id));

        if (containsUnknownProject)
        {
            return ClientResponse<RoadmapViewModel>
                .Failed("One or more projects are outside the current workspace scope");
        }

        try
        {
            var roadmap = await UnitOfWork.Roadmaps.GetRoadmap(scope, request.Filter, cancellationToken);

            return ClientResponse<RoadmapViewModel>.Success(roadmap);
        }
        catch (InvalidRoadmapFilterException exception)
        {
            return ClientResponse<RoadmapViewModel>.Failed(exception.Message);
        }
    }

    private static string? Validate(RoadmapFilter filter)
    {
        if (filter.From > filter.To)
        {
            return "Roadmap start date must be on or before its end date";
        }

        var inclusiveDayCount = filter.To.DayNumber - filter.From.DayNumber + 1;

        return inclusiveDayCount > 366
            ? "Roadmap range cannot exceed 366 days"
            : null;
    }
}
