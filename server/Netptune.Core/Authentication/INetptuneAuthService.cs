using System.Threading.Tasks;

using Netptune.Core.Authentication.Models;
using Netptune.Core.Entities;
using Netptune.Core.Models.Authentication;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;

namespace Netptune.Core.Authentication;

public interface INetptuneAuthService
{
    Task<LoginResult> LogIn(TokenRequest model);

    Task<LoginResult> LogInViaProvider();

    Task<RegisterResult> Register(RegisterRequest model);

    Task<RegisterResult> ConfirmEmail(string userId, string code);

    Task<RegisterResult> ConfirmEmail(AppUser appUser, string code);

    Task<ClientResponse> RequestPasswordReset(RequestPasswordResetRequest request);

    Task<LoginResult> ResetPassword(ResetPasswordRequest request);

    Task<ClientResponse> ChangePassword(ChangePasswordRequest request);

    Task<CurrentUserResponse> CurrentUser();

    Task<WorkspaceInvite> ValidateInviteCode(string code);
}