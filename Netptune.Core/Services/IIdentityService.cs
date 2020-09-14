using System.Threading.Tasks;

using Netptune.Core.Entities;

namespace Netptune.Core.Services
{
    public interface IIdentityService
    {
        Task<AppUser> GetCurrentUser();

        Task<string> GetCurrentUserEmail();

        Task<string> GetCurrentUserId();
    }
}
