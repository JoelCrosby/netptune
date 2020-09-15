using System.Threading.Tasks;

using Netptune.Core.Hubs;

namespace Netptune.Core.Services
{
    public interface IUserConnectionService
    {
        Task<UserConnection> Add(string connectionId);

        Task<UserConnection> Get(string connectionId);

        Task Remove(string connectionId);
    }
}
