using System.Security.Claims;
using System.Threading.Tasks;

using Netptune.Core.Entities;

namespace Netptune.Core.Services
{
    public interface IIdentityService
    {
        void BindHubUser(ClaimsPrincipal user);

        Task<AppUser> GetCurrentUser();

        Task<string> GetCurrentUserEmail();

        Task<string> GetCurrentUserId();
    }
}
