using Mediator;
using Netptune.Core.Models.Audit;
using Netptune.Core.Models.Files;

namespace Netptune.Services.Audit.Queries;

public sealed record ExportAuditLogQuery(AuditLogFilter Filter) : IRequest<FileResponse>;
