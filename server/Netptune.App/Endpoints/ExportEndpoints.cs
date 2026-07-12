using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Models.Activity;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.Services.Export;

namespace Netptune.App.Endpoints;

public static class ExportEndpoints
{
    public static RouteGroupBuilder MapExportEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("export");

        group.MapGet("/tasks/export-workspace", HandleExportWorkspaceTasks)
            .RequireAuthorization(NetptunePermissions.Tasks.Export);
        group.MapGet("/tasks/export-board/{boardId}", HandleExportBoardTasks)
            .RequireAuthorization(NetptunePermissions.Tasks.Export);

        return group;
    }

    public static async Task<IResult> HandleExportWorkspaceTasks(
        ITaskExportService taskExportService,
        IActivityLogger activity,
        IIdentityService identity)
    {
        var result = await taskExportService.ExportWorkspaceTasks();

        await LogExportRequested(activity, identity, "workspace-tasks");

        return Results.File(result.Stream, result.ContentType, result.Filename);
    }

    public static async Task<IResult> HandleExportBoardTasks(
        ITaskExportService taskExportService,
        IActivityLogger activity,
        IIdentityService identity,
        string boardId)
    {
        var result = await taskExportService.ExportBoardTasks(boardId);

        await LogExportRequested(activity, identity, "board-tasks", boardId);

        return Results.File(result.Stream, result.ContentType, result.Filename);
    }

    internal static async Task LogExportRequested(
        IActivityLogger activity,
        IIdentityService identity,
        string exportType,
        string? scope = null)
    {
        var workspaceId = await identity.GetWorkspaceId();

        activity.LogWith<ExportRequestedMeta>(options =>
        {
            options.EntityId = workspaceId;
            options.WorkspaceId = workspaceId;
            options.EntityType = EntityType.Workspace;
            options.Type = ActivityType.ExportRequested;
            options.Meta = new()
            {
                ExportType = exportType,
                Scope = scope,
            };
        });
    }
}
