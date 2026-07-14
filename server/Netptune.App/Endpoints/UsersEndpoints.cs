using Mediator;
using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Handlers.Users.Commands;
using Netptune.Handlers.Users.Queries;

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
        group.MapPost("/resend-invite", HandleResendInvite).RequireAuthorization(NetptunePermissions.Members.Invite);
        group.MapPost("/remove", HandleRemoveUserFromWorkspace).RequireAuthorization(NetptunePermissions.Members.Remove);
        group.MapPost("/toggle-permission", HandleTogglePermission).RequireAuthorization(NetptunePermissions.Members.UpdatePermission);
        group.MapPut("/role", HandleUpdateRole).RequireAuthorization(NetptunePermissions.Members.UpdateRole);
        group.MapGet("/get-by-email", HandleGetUserByEmail);
        group.MapGet("/all", HandleGetAll);

        return group;
    }

    public static async Task<IResult> HandleGetWorkspaceUsers(
        IMediator mediator,
        [AsParameters] PageRequest page,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWorkspaceUsersQuery(page), cancellationToken);

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

    public static async Task<IResult> HandleGetAll(
        IMediator mediator,
        [AsParameters] PageRequest page,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAllUsersQuery(page), cancellationToken);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleTogglePermission(IMediator mediator, ToggleUserPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ToggleUserPermissionCommand(request), cancellationToken);

        if (!result.IsSuccess) return Results.BadRequest(result.Message);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleUpdateRole(
        IMediator mediator,
        UpdateWorkspaceRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateWorkspaceRoleCommand(request), cancellationToken);

        if (!result.IsSuccess) return Results.BadRequest(result.Message);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleResendInvite(IMediator mediator, InviteUsersRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.EmailAddresses.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(email)) return Results.BadRequest("no email provided");

        var result = await mediator.Send(new ResendWorkspaceInviteCommand(email), cancellationToken);

        if (!result.IsSuccess) return Results.BadRequest(result.Message);

        return Results.Ok(result);
    }
}
