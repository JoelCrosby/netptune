using Microsoft.AspNetCore.Authorization;

using Netptune.Identity.Authentication;

namespace Netptune.PublicApi.Endpoints;

public static class PublicApiV1Endpoints
{
    public static RouteGroupBuilder MapPublicApiV1Endpoints(this RouteGroupBuilder group)
    {
        group.WithTags("Public API v1")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = AuthenticationSchemes.ApiKey,
            });

        group.MapProjectsEndpoints();
        group.MapStatusesEndpoints();
        group.MapTasksEndpoints();

        return group;
    }
}
