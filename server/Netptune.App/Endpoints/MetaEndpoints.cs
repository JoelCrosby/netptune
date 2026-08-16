using System.ComponentModel.DataAnnotations;

using Netptune.App.Utility;
using Netptune.Core.Exceptions;
using Netptune.Core.Services;

namespace Netptune.App.Endpoints;

public static class MetaEndpoints
{
    public static RouteGroupBuilder MapMetaEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("meta");

        // The login page renders the build number before anyone has signed in.
        group.MapGet("/build-info", HandleGetBuildInfo).AllowAnonymous();

        group.MapGet("/uri-meta-info", HandleGetUriMetaInfo).RequireAuthorization();

        return group;
    }

    public static IResult HandleGetBuildInfo(BuildInfo buildInfo)
    {
        var gitHash = buildInfo.GetBuildInfo();

        return Results.Ok(gitHash);
    }

    public static async Task<IResult> HandleGetUriMetaInfo(
        IWebService webService,
        [Required] string url)
    {
        try
        {
            var result = await webService.GetMetaDataFromUrl(url);

            return Results.Ok(result);
        }
        catch (UrlEgressBlockedException exception)
        {
            return Results.BadRequest(exception.Message);
        }
    }
}
