using Netptune.Core.Models.Pages;

namespace Netptune.Core.Services.Pages;

public interface IPageVisibilityService
{
    Task<PublicPageScope> GetScope(int workspaceId, CancellationToken cancellationToken = default);
}
