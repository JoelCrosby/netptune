using System.Threading.Tasks;

using Netptune.Core.Authentication.Models;
using Netptune.Core.Entities;
using Netptune.Core.Models.Authentication;

namespace Netptune.Core.Authentication
{
    public interface INetptuneAuthService
    {
        Task<LoginResult> LogIn(TokenRequest model);

        Task<RegisterResult> Register(RegisterRequest model);

        Task<RegisterResult> ConfirmEmail(string userId, string code);

        Task<RegisterResult> ConfirmEmail(AppUser appUser, string code);
    }
}
