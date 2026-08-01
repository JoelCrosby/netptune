using Mediator;

using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.ViewModels.Projects;
using Netptune.Handlers.Projects.Queries;

namespace Netptune.PublicApi.Endpoints;

public static class ProjectsEndpoints
{
    public static RouteGroupBuilder MapProjectsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/projects", GetProjects)
            .WithSummary("List projects")
            .WithDescription("Returns projects in the credential's workspace.")
            .RequireAuthorization(NetptunePermissions.Projects.Read);

        return group;
    }

    private static async Task<Ok<List<ProjectViewModel>>> GetProjects(
        IMediator mediator,
        [AsParameters] PageRequest page,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProjectsQuery(page), cancellationToken);
        return TypedResults.Ok(result);
    }
}
