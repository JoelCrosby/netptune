using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Netptune.App.Utility;
using Netptune.Core.Services;

namespace Netptune.App.Endpoints;

public static class MetaEndpoints
{
    public static RouteGroupBuilder Map(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("meta");

        group.MapGet("/build-info", HandleGetBuildInfo);
        group.MapGet("/uri-meta-info", HandleGetUriMetaInfo).AllowAnonymous();

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
        var result = await webService.GetMetaDataFromUrl(url);

        return Results.Ok(result);
    }
}
