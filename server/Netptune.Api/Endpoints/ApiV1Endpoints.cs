using Microsoft.AspNetCore.Authorization;

using Netptune.Identity.Authentication;

namespace Netptune.Api.Endpoints;

public static class ApiV1Endpoints
{
    public static RouteGroupBuilder MapApiV1Endpoints(this RouteGroupBuilder group)
    {
        group.WithTags("API v1")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = AuthenticationSchemes.ApiKey,
            });

        group.MapWorkspaceEndpoints();
        group.MapProjectsEndpoints();
        group.MapAssigneesEndpoints();
        group.MapBoardsEndpoints();
        group.MapBoardGroupsEndpoints();
        group.MapSprintsEndpoints();
        group.MapStatusesEndpoints();
        group.MapTagsEndpoints();
        group.MapRelationsEndpoints();
        group.MapTasksEndpoints();
        group.MapCommentsEndpoints();
        group.MapSearchEndpoints();
        group.MapReportingEndpoints();

        return group;
    }
}
