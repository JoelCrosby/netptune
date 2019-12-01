
using Netptune.Core.UnitOfWork;

namespace Netptune.Services
{
    public class BoardService
    {
        protected readonly INetptuneUnitOfWork UnitOfWork;

        public BoardService(INetptuneUnitOfWork unitOfWork)
        {
            UnitOfWork = unitOfWork;
        }
    }
}
