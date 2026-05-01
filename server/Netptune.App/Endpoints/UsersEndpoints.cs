using Mediator;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Services.Users.Commands;
using Netptune.Services.Users.Queries;

namespace Netptune.App.Endpoints;

public static class UsersEndpoints
{
    public static RouteGroupBuilder MapUsersEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("users");

        group.MapGet("/", HandleGetWorkspaceUsers).RequireAuthorization(NetptunePermissions.Members.Read);
        group.MapGet("/{id}", HandleGetUser);
        group.MapPut("/{id}", HandleUpdateUser).RequireAuthorization(NetptunePermissions.Members.UpdateProfile);
        group.MapPost("/invite", HandleInvite).RequireAuthorization(NetptunePermissions.Members.Invite);
        group.MapPost("/remove", HandleRemoveUserFromWorkspace).RequireAuthorization(NetptunePermissions.Members.Remove);
        group.MapPost("/toggle-permission", HandleTogglePermission).RequireAuthorization(NetptunePermissions.Members.UpdatePermission);
        group.MapGet("/get-by-email", HandleGetUserByEmail);
        group.MapGet("/all", HandleGetAll);

        return group;
    }

    public static async Task<IResult> HandleGetWorkspaceUsers(IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWorkspaceUsersQuery(), cancellationToken);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetUser(IMediator mediator, string id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUserQuery(id), cancellationToken);

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleUpdateUser(IMediator mediator, UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateUserCommand(request), cancellationToken);

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleInvite(IMediator mediator, InviteUsersRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new InviteUsersToWorkspaceCommand(request.EmailAddresses), cancellationToken);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleRemoveUserFromWorkspace(IMediator mediator, InviteUsersRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RemoveUsersFromWorkspaceCommand(request.EmailAddresses), cancellationToken);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetUserByEmail(IMediator mediator, string email,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUserByEmailQuery(email), cancellationToken);

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetAll(IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAllUsersQuery(), cancellationToken);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleTogglePermission(IMediator mediator, ToggleUserPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ToggleUserPermissionCommand(request), cancellationToken);

        if (!result.IsSuccess) return Results.BadRequest(result.Message);

        return Results.Ok(result);
    }
}
