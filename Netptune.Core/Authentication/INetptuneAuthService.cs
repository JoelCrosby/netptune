using System.Threading.Tasks;

using Netptune.Core.Authentication.Models;
using Netptune.Core.Models;

namespace Netptune.Core.Authentication
{
    public interface INetptuneAuthService
    {
        Task<ServiceResult<AuthenticationTicket>> LogIn(TokenRequest model);

        Task<ServiceResult<AuthenticationTicket>> Register(RegisterRequest model);
    }
}
