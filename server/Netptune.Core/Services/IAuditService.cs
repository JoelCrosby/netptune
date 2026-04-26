using Netptune.Core.Models.Audit;
using Netptune.Core.Models.Files;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Audit;

namespace Netptune.Core.Services;

public interface IAuditService
{
    Task<ClientResponse<AuditLogPage>> GetAuditLog(AuditLogFilter filter);

    Task<FileResponse> ExportAuditLog(AuditLogFilter filter);

    Task<ClientResponse> AnonymiseUser(string userId);
}
