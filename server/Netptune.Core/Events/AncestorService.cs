using System.Threading.Tasks;

using Netptune.Core.Models.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Core.Events;

public class AncestorService : IAncestorService
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public AncestorService(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public Task<ActivityAncestors> GetTaskAncestors(int entityId)
    {
        return UnitOfWork.Tasks.GetAncestors(entityId);
    }
}