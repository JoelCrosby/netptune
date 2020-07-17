using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;

using Netptune.Core.Authentication.Models;
using Netptune.Core.Entities;
using Netptune.Core.Models.Authentication;

namespace Netptune.Core.Authentication
{
    public interface INetptuneAuthService
    {
        Task<LoginResult> LogIn(TokenRequest model);

        Task<RegisterResult> Register(RegisterRequest model);

        Task<IdentityResult> ConfirmEmail(string userId, string code);

        Task<IdentityResult> ConfirmEmail(AppUser appUser, string code);
    }
}
