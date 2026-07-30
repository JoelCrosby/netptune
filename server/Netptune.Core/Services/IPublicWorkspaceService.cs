using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Users;
using Netptune.Core.ViewModels.Workspace;

namespace Netptune.Core.Services;

public interface IPublicWorkspaceService
{
    Task<PublicWorkspaceViewModel?> GetPublicWorkspace(string slug, CancellationToken cancellationToken = default);

    Task<PagedResponse<AssigneeViewModel>?> GetPublicWorkspaceMembers(string slug, AssigneeFilter filter, CancellationToken cancellationToken = default);
}
