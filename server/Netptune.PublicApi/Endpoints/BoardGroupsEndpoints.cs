using Mediator;

using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Core.Authorization;
using Netptune.Core.ViewModels.Boards;
using Netptune.Handlers.BoardGroups.Queries;

namespace Netptune.PublicApi.Endpoints;

public static class BoardGroupsEndpoints
{
    public static RouteGroupBuilder MapBoardGroupsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/board-groups", GetBoardGroups)
            .WithSummary("List board groups")
            .WithDescription(
                "Returns the board columns in the credential's workspace, with the board and project each belongs to. "
                + "Use the returned id as boardGroupId when moving a task between columns.")
            .RequireAuthorization(NetptunePermissions.BoardGroups.Read);

        return group;
    }

    private static async Task<Ok<List<BoardGroupOptionViewModel>>> GetBoardGroups(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBoardGroupOptionsQuery(), cancellationToken);

        return TypedResults.Ok(result);
    }
}
