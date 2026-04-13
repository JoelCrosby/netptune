using Netptune.Core.Entities;

namespace Netptune.Core.Services;

public interface IPublicWorkspaceService
{
    Task<Workspace?> GetPublicWorkspace(string slug);
}
