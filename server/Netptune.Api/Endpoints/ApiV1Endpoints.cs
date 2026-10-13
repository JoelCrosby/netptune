using Microsoft.AspNetCore.Authorization;

using Netptune.Api.Configuration;
using Netptune.Identity.Authentication;

namespace Netptune.Api.Endpoints;

public static class ApiV1Endpoints
{
    public static RouteGroupBuilder MapApiV1Endpoints(this RouteGroupBuilder group)
    {
        group.RequireAuthorization(new AuthorizeAttribute
        {
            AuthenticationSchemes = AuthenticationSchemes.ApiKey,
        });

        Section(group, ApiTags.Workspace).MapWorkspaceEndpoints();
        Section(group, ApiTags.Projects).MapProjectsEndpoints();
        Section(group, ApiTags.Assignees).MapAssigneesEndpoints();
        Section(group, ApiTags.Boards).MapBoardsEndpoints();
        Section(group, ApiTags.BoardGroups).MapBoardGroupsEndpoints();
        Section(group, ApiTags.Sprints).MapSprintsEndpoints();
        Section(group, ApiTags.Statuses).MapStatusesEndpoints();
        Section(group, ApiTags.Tags).MapTagsEndpoints();
        Section(group, ApiTags.Relations).MapRelationsEndpoints();
        Section(group, ApiTags.Tasks).MapTasksEndpoints();
        Section(group, ApiTags.Comments).MapCommentsEndpoints();
        Section(group, ApiTags.Pages).MapPagesEndpoints();
        Section(group, ApiTags.Search).MapSearchEndpoints();
        Section(group, ApiTags.Reports).MapReportingEndpoints();

        return group;
    }

    private static RouteGroupBuilder Section(RouteGroupBuilder group, ApiTag tag)
    {
        return group.MapGroup(string.Empty).WithTags(tag.Name);
    }
}
