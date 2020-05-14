using Netptune.Core.Entities;

using System.Threading.Tasks;

namespace Netptune.Core.Services
{
    public interface IIdentityService
    {
        Task<AppUser> GetCurrentUser();

        Task<string> GetCurrentUserEmail();

        Task<string> GetCurrentUserId();
    }
}