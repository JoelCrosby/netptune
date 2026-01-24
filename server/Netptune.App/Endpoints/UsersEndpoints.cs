using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services;

namespace Netptune.App.Endpoints;

public static class UsersEndpoints
{
    public static RouteGroupBuilder Map(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("users");

        group.MapGet("/", HandleGetWorkspaceUsers).RequireAuthorization(NetptunePolicies.Workspace);
        group.MapGet("/{id}", HandleGetUser);
        group.MapPut("/{id}", HandleUpdateUser);
        group.MapPost("/invite", HandleInvite).RequireAuthorization(NetptunePolicies.Workspace);
        group.MapPost("/remove", HandleRemoveUserFromWorkspace).RequireAuthorization(NetptunePolicies.Workspace);
        group.MapGet("/get-by-email", HandleGetUserByEmail);
        group.MapGet("/all", HandleGetAll);

        return group;
    }

    public static async Task<IResult> HandleGetWorkspaceUsers(
        IUserService userService)
    {
        var result = await userService.GetWorkspaceUsers();

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetUser(
        IUserService userService,
        string id)
    {
        var result = await userService.Get(id);

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleUpdateUser(
        IUserService userService,
        UpdateUserRequest request)
    {
        var result = await userService.Update(request);

        if (result.IsNotFound) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleInvite(
        IUserService userService,
        InviteUsersRequest request)
    {
        var result = await userService.InviteUsersToWorkspace(request.EmailAddresses);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleRemoveUserFromWorkspace(
        IUserService userService,
        InviteUsersRequest request)
    {
        var result = await userService.RemoveUsersFromWorkspace(request.EmailAddresses);

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetUserByEmail(
        IUserService userService,
        string email)
    {
        var result = await userService.GetByEmail(email);

        if (result is null) return Results.NotFound();

        return Results.Ok(result);
    }

    public static async Task<IResult> HandleGetAll(
        IUserService userService)
    {
        var result = await userService.GetAll();

        return Results.Ok(result);
    }
}
