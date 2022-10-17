using System.Threading.Tasks;

using Netptune.Core.Entities;

namespace Netptune.Core.Services;

public interface IIdentityService
{
    Task<AppUser> GetCurrentUser();

    Task<string> GetCurrentUserEmail();

    Task<string> GetCurrentUserId();

    string GetUserId();

    string GetUserEmail();

    string GetUserName();

    string GetPictureUrl();

    string GetWorkspaceKey();

    Task<int> GetWorkspaceId();
}
