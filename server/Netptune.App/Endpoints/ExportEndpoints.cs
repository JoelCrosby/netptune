using Netptune.Core.Authorization;
using Netptune.Core.Services.Export;

namespace Netptune.App.Endpoints;

public static class ExportEndpoints
{
    public static RouteGroupBuilder MapExportEndpoints(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("export");

        group.MapGet("/tasks/export-workspace", HandleExportWorkspaceTasks)
            .RequireAuthorization(NetptunePermissions.Export.Tasks);
        group.MapGet("/tasks/export-board/{boardId}", HandleExportBoardTasks)
            .RequireAuthorization(NetptunePermissions.Export.Tasks);

        return group;
    }

    public static async Task<IResult> HandleExportWorkspaceTasks(
        ITaskExportService taskExportService)
    {
        var result = await taskExportService.ExportWorkspaceTasks();

        return Results.File(result.Stream, result.ContentType, result.Filename);
    }

    public static async Task<IResult> HandleExportBoardTasks(
        ITaskExportService taskExportService,
        string boardId)
    {
        var result = await taskExportService.ExportBoardTasks(boardId);

        return Results.File(result.Stream, result.ContentType, result.Filename);
    }
}
