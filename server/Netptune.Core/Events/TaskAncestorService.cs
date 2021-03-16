using System.Collections.Generic;
using System.Threading.Tasks;

using Netptune.Core.Entities;
using Netptune.Core.UnitOfWork;

namespace Netptune.Core.Events
{
    public class TaskAncestorService : IAncestorService<ProjectTask>
    {
        private readonly INetptuneUnitOfWork UnitOfWork;

        public TaskAncestorService(INetptuneUnitOfWork unitOfWork)
        {
            UnitOfWork = unitOfWork;
        }

        public Task<List<int>> GetAncestors(int entityId)
        {
             return UnitOfWork.Tasks.GetAncestors(entityId);
        }
    }
}
