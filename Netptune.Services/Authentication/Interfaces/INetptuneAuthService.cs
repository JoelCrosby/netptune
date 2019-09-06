using System.Threading.Tasks;

using Netptune.Core.Models;
using Netptune.Services.Authentication.Models;

namespace Netptune.Services.Authentication.Interfaces
{
    public interface INetptuneAuthService
    {
        Task<ServiceResult<AuthenticationTicket>> LogIn(TokenRequest model);

        Task<ServiceResult<AuthenticationTicket>> Register(RegisterRequest model);
    }
}
