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

    public static async Task<IResult> HandleGetWorkspaceUsers(IMediator mediator)
    {
        var result = await mediator.Send(new GetWorkspaceUsersQuery());

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetUser(IMediator mediator, string id)
    {
        var result = await mediator.Send(new GetUserQuery(id));

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleUpdateUser(IMediator mediator, UpdateUserRequest request)
    {
        var result = await mediator.Send(new UpdateUserCommand(request));

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleInvite(IMediator mediator, InviteUsersRequest request)
    {
        var result = await mediator.Send(new InviteUsersToWorkspaceCommand(request.EmailAddresses));

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleRemoveUserFromWorkspace(IMediator mediator, InviteUsersRequest request)
    {
        var result = await mediator.Send(new RemoveUsersFromWorkspaceCommand(request.EmailAddresses));

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetUserByEmail(IMediator mediator, string email)
    {
        var result = await mediator.Send(new GetUserByEmailQuery(email));

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetAll(IMediator mediator)
    {
        var result = await mediator.Send(new GetAllUsersQuery());

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleTogglePermission(IMediator mediator, ToggleUserPermissionRequest request)
    {
        var result = await mediator.Send(new ToggleUserPermissionCommand(request));

        if (!result.IsSuccess) return Results.BadRequest(result.Message);

        return Results.Ok(result);
    }
}
