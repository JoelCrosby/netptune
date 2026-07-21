using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Services;
using Netptune.Core.Services.Export;
using Netptune.Core.UnitOfWork;

namespace Netptune.App.Endpoints;

public static class ExportEndpoints
{
    internal sealed record ExportAuditDetails(string ExportType, string? Scope = null);

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
        IEventRecordWriter eventRecords,
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        CancellationToken cancellationToken)
    {
        var result = await taskExportService.ExportWorkspaceTasks();

        await LogExportRequested(
            eventRecords,
            unitOfWork,
            identity,
            new ExportAuditDetails("workspace-tasks"),
            cancellationToken);

        return Results.File(result.Stream, result.ContentType, result.Filename);
    }

    public static async Task<IResult> HandleExportBoardTasks(
        ITaskExportService taskExportService,
        IEventRecordWriter eventRecords,
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        string boardId,
        CancellationToken cancellationToken)
    {
        var result = await taskExportService.ExportBoardTasks(boardId);

        await LogExportRequested(
            eventRecords,
            unitOfWork,
            identity,
            new ExportAuditDetails("board-tasks", boardId),
            cancellationToken);

        return Results.File(result.Stream, result.ContentType, result.Filename);
    }

    internal static async Task LogExportRequested(
        IEventRecordWriter eventRecords,
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        ExportAuditDetails details,
        CancellationToken cancellationToken)
    {
        var workspaceId = await identity.GetWorkspaceId();

        await eventRecords.Append(new EventWriteRequest<ExportRequestedPayload>
        {
            WorkspaceId = workspaceId,
            EventKey = EventKeys.ExportRequested,
            SubjectType = EventEntityTypes.From(EntityType.Workspace),
            SubjectId = workspaceId.ToString(),
            Payload = new ExportRequestedPayload
            {
                ExportType = details.ExportType,
                Scope = details.Scope,
            },
        }, cancellationToken);

        await unitOfWork.CompleteAsync(cancellationToken);
    }
}
