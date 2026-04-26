using Mediator;

using Netptune.Core.Models.Audit;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Audit;

namespace Netptune.Services.Audit.Queries.GetAuditLog;

public sealed record GetAuditLogQuery(AuditLogFilter Filter) : IRequest<ClientResponse<AuditLogPage>>;
