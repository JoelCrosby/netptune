using System.Threading.Tasks;
using Netptune.Entities.Authentication;
using Netptune.Services.Models;

namespace Netptune.Services.Authentication.Interfaces
{
    public interface INetptuneAuthService
    {
        Task<ServiceResult<AuthenticationTicket>> LogIn(TokenRequest model);

        Task<ServiceResult<AuthenticationTicket>> Register(TokenRequest model);
    }
}
