using Mediator;

using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Realtime;
using Netptune.Core.ViewModels.Projects;
using Netptune.Handlers.Projects.Commands;
using Netptune.Handlers.Projects.Queries;
using Netptune.Api.Configuration;
using Netptune.Api.Requests;

namespace Netptune.Api.Endpoints;

public static class ProjectsEndpoints
{
    public static RouteGroupBuilder MapProjectsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/projects", GetProjects)
            .WithSummary("List projects")
            .WithDescription("Returns projects in the credential's workspace.")
            .RequireAuthorization(NetptunePermissions.Projects.Read);

        group.MapGet("/projects/{key}", GetProject)
            .WithSummary("Get a project")
            .WithDescription("Returns a project by its key, the short prefix that starts every task key in it.")
            .RequireAuthorization(NetptunePermissions.Projects.Read);

        group.MapPost("/projects", CreateProject)
            .WithSummary("Create a project")
            .WithDescription("Creates a project in the credential's workspace.")
            .RequireAuthorization(NetptunePermissions.Projects.Create)
            .Broadcasts(WorkspaceEventScopes.Project);

        group.MapPatch("/projects/{id:int}", UpdateProject)
            .WithSummary("Update a project")
            .WithDescription("Updates the supplied fields on an existing project.")
            .RequireAuthorization(NetptunePermissions.Projects.Update)
            .Broadcasts(WorkspaceEventScopes.Project);

        group.MapDelete("/projects/{id:int}", DeleteProject)
            .WithSummary("Delete a project")
            .WithDescription("Deletes a project along with the boards and tasks belonging to it.")
            .RequireAuthorization(NetptunePermissions.Projects.Delete)
            .Broadcasts(WorkspaceEventScopes.Project, WorkspaceEventScopes.Board, WorkspaceEventScopes.Task);

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

    private static async Task<Results<Ok<ProjectViewModel>, NotFound>> GetProject(
        IMediator mediator,
        string key,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProjectQuery(key), cancellationToken);

        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<Results<Created<ProjectViewModel>, NotFound, BadRequest<ClientResponse<ProjectViewModel>>>> CreateProject(
        IMediator mediator,
        AddProjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateProjectCommand(request), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        if (!result.IsSuccess)
        {
            return TypedResults.BadRequest(result);
        }

        return TypedResults.Created($"/api/v1/projects/{result.Payload!.Key}", result.Payload);
    }

    private static async Task<Results<Ok<ProjectViewModel>, NotFound, BadRequest<ClientResponse<ProjectViewModel>>>> UpdateProject(
        IMediator mediator,
        int id,
        PublicUpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var updateRequest = request.ToRequest(id);
        var result = await mediator.Send(new UpdateProjectCommand(updateRequest), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.Ok(result.Payload) : TypedResults.BadRequest(result);
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ClientResponse>>> DeleteProject(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteProjectCommand(id), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
    }
}
