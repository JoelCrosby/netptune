using Netptune.Core.Responses.Common;

namespace Netptune.App.Utility;

public static class ClientResponseExtensions
{
    public static IResult ToResult(this ClientResponse response)
    {
        if (response.IsNotFound)
        {
            return Results.NotFound(response);
        }

        if (response.IsForbidden)
        {
            return Results.Forbid();
        }

        if (!response.IsSuccess)
        {
            return Results.BadRequest(response);
        }

        return Results.Ok(response);
    }

    public static IResult ToResult<TPayload>(this ClientResponse<TPayload> response)
    {
        var failure = TryGetFailureResult(response);

        return failure ?? Results.Ok(response);
    }

    public static IResult ToPayloadResult<TPayload>(this ClientResponse<TPayload> response)
    {
        var failure = TryGetFailureResult(response);

        return failure ?? Results.Ok(response.Payload);
    }

    public static IResult ToNoContentResult(this ClientResponse response)
    {
        if (response.IsNotFound)
        {
            return Results.NotFound(response);
        }

        if (response.IsForbidden)
        {
            return Results.Forbid();
        }

        if (!response.IsSuccess)
        {
            return Results.BadRequest(response);
        }

        return Results.NoContent();
    }

    public static IResult ToNoContentResult<TPayload>(this ClientResponse<TPayload> response)
    {
        var failure = TryGetFailureResult(response);

        return failure ?? Results.NoContent();
    }

    private static IResult? TryGetFailureResult<TPayload>(ClientResponse<TPayload> response)
    {
        if (response.IsNotFound)
        {
            return Results.NotFound(response);
        }

        if (response.IsForbidden)
        {
            return Results.Forbid();
        }

        if (!response.IsSuccess)
        {
            return Results.BadRequest(response);
        }

        return null;
    }
}
