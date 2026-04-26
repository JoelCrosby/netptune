using Mediator;
using Netptune.Core.Models.Audit;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Audit;

namespace Netptune.Services.Audit.Queries;

public sealed record GetActivitySummaryQuery(AuditLogFilter Filter) : IRequest<ClientResponse<List<AuditActivityPoint>>>;
