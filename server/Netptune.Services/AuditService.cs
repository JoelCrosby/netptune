using System.Globalization;
using System.Text;

using CsvHelper;

using Netptune.Core.Models.Audit;
using Netptune.Core.Models.Files;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Audit;

namespace Netptune.Services;

public class AuditService : IAuditService
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public AuditService(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async Task<ClientResponse<AuditLogPage>> GetAuditLog(AuditLogFilter filter)
    {
        filter.WorkspaceId = await Identity.GetWorkspaceId();

        var page = await UnitOfWork.ActivityLogs.GetAuditLog(filter);

        return page;
    }

    public async Task<ClientResponse<List<AuditActivityPoint>>> GetActivitySummary(AuditLogFilter filter)
    {
        filter.WorkspaceId = await Identity.GetWorkspaceId();

        var points = await UnitOfWork.ActivityLogs.GetActivitySummary(filter);

        return points;
    }

    public async Task<FileResponse> ExportAuditLog(AuditLogFilter filter)
    {
        filter.WorkspaceId = await Identity.GetWorkspaceId();

        // Cap to 12 months to prevent unbounded exports
        var ceiling = DateTime.UtcNow;
        var floor = ceiling.AddMonths(-12);

        filter.From ??= floor;
        filter.To ??= ceiling;

        if (filter.From < floor) filter.From = floor;

        var rows = await UnitOfWork.ActivityLogs.GetAuditLogForExport(filter);

        var stream = await ToAuditCsvStream(rows);

        var workspaceKey = Identity.GetWorkspaceKey();
        var filename = $"Netptune-Audit-Export_{workspaceKey}-{DateTime.UtcNow:yy-MMM-dd-HH-mm}.csv";

        return new FileResponse
        {
            Stream = stream,
            ContentType = "text/csv",
            Filename = filename,
        };
    }

    public async Task<ClientResponse> AnonymiseUser(string userId)
    {
        var workspaceId = await Identity.GetWorkspaceId();

        await UnitOfWork.ActivityLogs.AnonymiseUser(userId, workspaceId);

        return ClientResponse.Success;
    }

    private static async Task<MemoryStream> ToAuditCsvStream(IEnumerable<AuditLogViewModel> rows)
    {
        var memoryStream = new MemoryStream();
        var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        var flat = rows.Select(r => new
        {
            r.Id,
            r.OccurredAt,
            r.UserId,
            r.UserDisplayName,
            Type = r.Type.ToString(),
            EntityType = r.EntityType.ToString(),
            r.EntityId,
            r.WorkspaceSlug,
            r.ProjectSlug,
            r.BoardSlug,
            Meta = r.Meta?.RootElement.ToString() ?? string.Empty,
        });

        await csv.WriteRecordsAsync(flat);
        await writer.FlushAsync();

        memoryStream.Seek(0, SeekOrigin.Begin);

        return memoryStream;
    }
}
