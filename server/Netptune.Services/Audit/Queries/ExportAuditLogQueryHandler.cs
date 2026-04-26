using System.Globalization;
using System.Text;
using CsvHelper;
using Mediator;
using Netptune.Core.Models.Files;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Audit;
using Netptune.Core.Models.Audit;

namespace Netptune.Services.Audit.Queries;

public sealed record ExportAuditLogQuery(AuditLogFilter Filter) : IRequest<FileResponse>;

public sealed class ExportAuditLogQueryHandler : IRequestHandler<ExportAuditLogQuery, FileResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public ExportAuditLogQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<FileResponse> Handle(ExportAuditLogQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter;
        filter.WorkspaceId = await Identity.GetWorkspaceId();

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
