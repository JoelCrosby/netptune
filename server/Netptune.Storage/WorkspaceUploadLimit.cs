using Netptune.Core.Storage;
using Netptune.Core.UnitOfWork;

namespace Netptune.Storage;

public static class WorkspaceUploadLimit
{
    public static async Task<long> Resolve(INetptuneUnitOfWork unitOfWork, int workspaceId, CancellationToken cancellationToken)
    {
        var configured = await unitOfWork.Workspaces.GetMaxUploadBytes(workspaceId, cancellationToken);

        return UploadLimits.Clamp(configured ?? UploadLimits.DefaultMaxUploadBytes);
    }
}
