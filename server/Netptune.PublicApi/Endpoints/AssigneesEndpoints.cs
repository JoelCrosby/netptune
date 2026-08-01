using Mediator;

using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Users;
using Netptune.Handlers.Users.Queries;

namespace Netptune.PublicApi.Endpoints;

public static class AssigneesEndpoints
{
    public static RouteGroupBuilder MapAssigneesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/assignees", GetAssignees)
            .WithSummary("List task assignees")
            .WithDescription("Returns the integration-safe identities that can be assigned to tasks in the credential's workspace.")
            .RequireAuthorization(NetptunePermissions.Members.Read);

        return group;
    }

    private static async Task<Ok<PagedResponse<AssigneeViewModel>>> GetAssignees(
        IMediator mediator,
        [AsParameters] AssigneeFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAssigneesQuery(filter), cancellationToken);

        return TypedResults.Ok(result.Payload);
    }
}
