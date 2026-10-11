using Mediator;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

using Netptune.Api.Configuration;
using Netptune.Api.Requests;
using Netptune.Core.Authorization;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Realtime;
using Netptune.Core.ViewModels.Pages;
using Netptune.Handlers.Pages.Commands;
using Netptune.Handlers.Pages.Queries;
using Netptune.Handlers.Tags.Queries;

namespace Netptune.Api.Endpoints;

public static class PagesEndpoints
{
    private const string MarkdownBody =
        "body is markdown. Mention other entities inline with [[task:KEY-1|label]], [[page:ID|label]], "
        + "[[user:ID|label]], [[project:ID|label]], [[sprint:ID|label]] or [[board:ID|label]]; "
        + "the label is the text shown and the part before it identifies the target.";

    private const string RevisionContract =
        "Send the revision from the page you last read. When someone else has saved since, nothing is written "
        + "and the response is 409 Conflict whose payload is the current page, so re-read it, merge and retry "
        + "with its revision.";

    private const string OthersPagesInside =
        "Without pages.delete_any, a page holding pages somebody else created is refused with 400 and the "
        + "message that only they or an admin can archive, restore or delete them.";

    public static RouteGroupBuilder MapPagesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/pages", GetPageTree)
            .WithSummary("List the page tree")
            .WithDescription(
                "Returns every live page and folder in the credential's workspace as a flat list. Rebuild the tree "
                + "from parentId and order siblings by sortOrder. Bodies are not included; read a page by id for "
                + "its body. kind is 0 for a folder and 1 for a document; status is 0 for draft and 1 for published.")
            .RequireAuthorization(NetptunePermissions.Pages.Read);

        group.MapGet("/pages/archived", GetArchivedPages)
            .WithSummary("List archived pages")
            .WithDescription(
                "Returns the pages that have been archived and can still be restored through "
                + "POST /pages/{id}/restore or deleted for good through DELETE /pages/{id}/permanent.")
            .RequireAuthorization(NetptunePermissions.Pages.Read);

        group.MapGet("/pages/{id:int}", GetPage)
            .WithSummary("Get a page")
            .WithDescription(
                "Returns a live page by its numeric identifier, with its markdown body, tags, contributors and "
                + "the revision to send back when saving.")
            .RequireAuthorization(NetptunePermissions.Pages.Read);

        group.MapGet("/pages/by-slug/{slug}", GetPageBySlug)
            .WithSummary("Get a page by its slug")
            .WithDescription(
                "Returns a live page by its slug, as it appears in the page's URL. A slug the page had before it "
                + "was renamed still resolves to it.")
            .RequireAuthorization(NetptunePermissions.Pages.Read);

        group.MapPost("/pages", CreatePage)
            .WithSummary("Create a page")
            .WithDescription(
                "Creates a document (kind 1, the default) or a folder (kind 0) in the credential's workspace. "
                + "parentId nests it under an existing page, and projectId files it in a project's space. "
                + "templateKey starts a document from a template (meeting-notes, rfc, runbook, retro, spec, or "
                + "from-sprint with sprintId for release notes); an explicit title or body overrides the template's. "
                + "from-sprint also needs the sprints.read and tasks.read scopes, and is refused with 400 "
                + "without them. "
                + "Folders have no body. "
                + MarkdownBody)
            .RequireAuthorization(NetptunePermissions.Pages.Create)
            .Broadcasts(WorkspaceEventScopes.Page);

        group.MapPut("/pages/{id:int}/content", SavePageContent)
            .WithSummary("Save a page's title and body")
            .WithDescription(
                "Replaces the title and body of a page and records a new version. "
                + RevisionContract
                + " "
                + MarkdownBody)
            .Produces<ClientResponse<PageViewModel>>(StatusCodes.Status409Conflict)
            .RequireAuthorization(NetptunePermissions.Pages.Update)
            .Broadcasts(WorkspaceEventScopes.Page);

        group.MapPatch("/pages/{id:int}", UpdatePage)
            .WithSummary("Update a page's status and tags")
            .WithDescription(
                "Updates the supplied fields on a page. status is 0 for draft and 1 for published. tags names the "
                + "workspace tags the page should carry and replaces the ones it has; every name must already exist "
                + "in the workspace (create them with POST /tags), an unknown name is refused with 400, and an "
                + "empty list clears them. Sending tags needs tags.assign, and at most "
                + $"{ResolveTagIdsByNameQueryHandler.MaximumNames} names are accepted.")
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(NetptunePermissions.Pages.Update)
            .Broadcasts(WorkspaceEventScopes.Page);

        group.MapPost("/pages/{id:int}/move", MovePage)
            .WithSummary("Move a page")
            .WithDescription(
                "Moves a page, with everything beneath it, under parentId (null for the top level) at sortOrder. "
                + "Pick a sortOrder between the neighbours' values from GET /pages to place it between them. "
                + "A page cannot be moved inside itself or into another project's space.")
            .RequireAuthorization(NetptunePermissions.Pages.Update)
            .Broadcasts(WorkspaceEventScopes.Page);

        group.MapDelete("/pages/{id:int}", ArchivePage)
            .WithSummary("Archive a page")
            .WithDescription(
                "Archives a page and every page beneath it. Archiving is reversible through "
                + "POST /pages/{id}/restore. Archiving a page somebody else created additionally needs "
                + "pages.delete_any. "
                + OthersPagesInside)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(NetptunePermissions.Pages.Delete)
            .Broadcasts(WorkspaceEventScopes.Page);

        group.MapPost("/pages/{id:int}/restore", RestorePage)
            .WithSummary("Restore an archived page")
            .WithDescription(
                "Restores an archived page and the pages archived with it. Restoring a page somebody else created "
                + "additionally needs pages.delete_any. "
                + OthersPagesInside)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(NetptunePermissions.Pages.Delete)
            .Broadcasts(WorkspaceEventScopes.Page);

        group.MapDelete("/pages/{id:int}/permanent", DeleteArchivedPage)
            .WithSummary("Delete an archived page for good")
            .WithDescription(
                "Permanently deletes an archived page and the archived pages beneath it. This cannot be "
                + "undone; prefer DELETE /pages/{id}, which archives. Deleting a page somebody else created "
                + "additionally needs pages.delete_any. "
                + OthersPagesInside)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(NetptunePermissions.Pages.Delete)
            .Broadcasts(WorkspaceEventScopes.Page);

        group.MapGet("/pages/{id:int}/versions", GetPageVersions)
            .WithSummary("List a page's versions")
            .WithDescription("Returns the saved versions of a page, newest first, without their bodies.")
            .RequireAuthorization(NetptunePermissions.Pages.Read);

        group.MapGet("/pages/{id:int}/versions/{versionId:int}", GetPageVersion)
            .WithSummary("Get a page version")
            .WithDescription("Returns one saved version of a page, with the title and markdown body it had.")
            .RequireAuthorization(NetptunePermissions.Pages.Read);

        group.MapPost("/pages/{id:int}/versions/{versionId:int}/restore", RestorePageVersion)
            .WithSummary("Restore a page version")
            .WithDescription(
                "Saves a version's title and body as the page's current content, recording a new version. "
                + "Optionally supply the revision query parameter (?revision=N); when someone has saved since that "
                + "revision, nothing is written and "
                + "the response is 409 Conflict whose payload is the current page.")
            .Produces<ClientResponse<PageViewModel>>(StatusCodes.Status409Conflict)
            .RequireAuthorization(NetptunePermissions.Pages.Update)
            .Broadcasts(WorkspaceEventScopes.Page);

        group.MapGet("/pages/{id:int}/links", GetPageLinks)
            .WithSummary("Get a page's links")
            .WithDescription(
                "Returns the tasks and pages a page mentions in its body, and the pages and tasks that link back "
                + "to it.")
            .RequireAuthorization(NetptunePermissions.Pages.Read);

        return group;
    }

    private static async Task<Ok<List<PageTreeNodeViewModel>>> GetPageTree(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPageTreeQuery(), cancellationToken);

        return TypedResults.Ok(result.Payload);
    }

    private static async Task<Ok<List<ArchivedPageViewModel>>> GetArchivedPages(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetArchivedPagesQuery(), cancellationToken);

        return TypedResults.Ok(result.Payload);
    }

    private static async Task<Results<Ok<PageViewModel>, NotFound>> GetPage(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPageByIdQuery(id), cancellationToken);

        return result.IsNotFound ? TypedResults.NotFound() : TypedResults.Ok(result.Payload);
    }

    private static async Task<Results<Ok<PageViewModel>, NotFound>> GetPageBySlug(
        IMediator mediator,
        string slug,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPageQuery(slug), cancellationToken);

        return result.IsNotFound ? TypedResults.NotFound() : TypedResults.Ok(result.Payload);
    }

    private static async Task<Results<Created<PageViewModel>, BadRequest<ClientResponse<PageViewModel>>>> CreatePage(
        IMediator mediator,
        CreatePageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreatePageCommand(request), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.BadRequest(result);
        }

        return TypedResults.Created($"/api/v1/pages/{result.Payload!.Id}", result.Payload);
    }

    private static async Task<Results<Ok<PageViewModel>, NotFound, Conflict<ClientResponse<PageViewModel>>, BadRequest<ClientResponse<PageViewModel>>>> SavePageContent(
        IMediator mediator,
        int id,
        SavePageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SavePageCommand(id, request), cancellationToken);

        return ToPageResult(result);
    }

    private static async Task<Results<Ok<PageViewModel>, NotFound, BadRequest<ClientResponse<PageViewModel>>, ForbidHttpResult>> UpdatePage(
        IMediator mediator,
        IAuthorizationService authorization,
        HttpContext http,
        int id,
        PublicUpdatePageRequest request,
        CancellationToken cancellationToken)
    {
        var setsTags = request.Tags is not null;

        if (setsTags)
        {
            var canAssignTags = await authorization.AuthorizeAsync(http.User, NetptunePermissions.Tags.Assign);

            if (!canAssignTags.Succeeded)
            {
                return TypedResults.Forbid();
            }
        }

        var resolution = setsTags
            ? await mediator.Send(new ResolveTagIdsByNameQuery(request.Tags!), cancellationToken)
            : null;

        if (resolution?.Error is { } tagError)
        {
            return TypedResults.BadRequest(ClientResponse<PageViewModel>.Failed(tagError));
        }

        var update = new UpdatePageRequest { Status = request.Status, TagIds = resolution?.Ids };
        var result = await mediator.Send(new UpdatePageCommand(id, update), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.Ok(result.Payload) : TypedResults.BadRequest(result);
    }

    private static async Task<Results<Ok<PageViewModel>, NotFound, BadRequest<ClientResponse<PageViewModel>>>> MovePage(
        IMediator mediator,
        int id,
        MovePageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new MovePageCommand(id, request), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        return result.IsSuccess ? TypedResults.Ok(result.Payload) : TypedResults.BadRequest(result);
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, BadRequest<ClientResponse>>> ArchivePage(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ArchivePageCommand(id), cancellationToken);

        return ToNoContentResult(result);
    }

    private static async Task<Results<Ok<PageViewModel>, NotFound, ForbidHttpResult, BadRequest<ClientResponse<PageViewModel>>>> RestorePage(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RestorePageCommand(id), cancellationToken);

        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        if (result.IsForbidden)
        {
            return TypedResults.Forbid();
        }

        return result.IsSuccess ? TypedResults.Ok(result.Payload) : TypedResults.BadRequest(result);
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult, BadRequest<ClientResponse>>> DeleteArchivedPage(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteArchivedPageCommand(id), cancellationToken);

        return ToNoContentResult(result);
    }

    private static async Task<Results<Ok<List<PageVersionSummaryViewModel>>, NotFound>> GetPageVersions(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPageVersionsQuery(id), cancellationToken);

        return result.IsNotFound ? TypedResults.NotFound() : TypedResults.Ok(result.Payload);
    }

    private static async Task<Results<Ok<PageVersionViewModel>, NotFound>> GetPageVersion(
        IMediator mediator,
        int id,
        int versionId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPageVersionQuery(id, versionId), cancellationToken);

        return result.IsNotFound ? TypedResults.NotFound() : TypedResults.Ok(result.Payload);
    }

    private static async Task<Results<Ok<PageViewModel>, NotFound, Conflict<ClientResponse<PageViewModel>>, BadRequest<ClientResponse<PageViewModel>>>> RestorePageVersion(
        IMediator mediator,
        int id,
        int versionId,
        int? revision,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RestorePageVersionCommand(id, versionId, revision), cancellationToken);

        return ToPageResult(result);
    }

    private static async Task<Results<Ok<PageLinksViewModel>, NotFound>> GetPageLinks(
        IMediator mediator,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPageLinksQuery(id), cancellationToken);

        return result.IsNotFound ? TypedResults.NotFound() : TypedResults.Ok(result.Payload);
    }

    private static Results<Ok<PageViewModel>, NotFound, Conflict<ClientResponse<PageViewModel>>, BadRequest<ClientResponse<PageViewModel>>> ToPageResult(
        ClientResponse<PageViewModel> result)
    {
        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        if (result.IsConflict)
        {
            return TypedResults.Conflict(result);
        }

        return result.IsSuccess ? TypedResults.Ok(result.Payload) : TypedResults.BadRequest(result);
    }

    private static Results<NoContent, NotFound, ForbidHttpResult, BadRequest<ClientResponse>> ToNoContentResult(ClientResponse result)
    {
        if (result.IsNotFound)
        {
            return TypedResults.NotFound();
        }

        if (result.IsForbidden)
        {
            return TypedResults.Forbid();
        }

        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest(result);
    }
}
