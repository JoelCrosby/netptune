using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Export;

namespace Netptune.App.Endpoints;

public static class ExportEndpoints
{
    public static RouteGroupBuilder Map(this RouteGroupBuilder builder)
    {
        var group = builder.MapGroup("export")
            .RequireAuthorization(NetptunePolicies.Workspace);

        group.MapGet("/tasks/export-workspace", HandleExportWorkspaceTasks);
        group.MapGet("/tasks/export-board/{boardId}", HandleExportBoardTasks);

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
