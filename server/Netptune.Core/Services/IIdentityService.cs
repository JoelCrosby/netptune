using System.Threading.Tasks;

using Netptune.Core.Entities;

namespace Netptune.Core.Services;

public interface IIdentityService
{
    Task<AppUser> GetCurrentUser();

    string GetCurrentUserId();

    string GetCurrentUserEmail();

    string GetUserName();

    string GetPictureUrl();

    string GetWorkspaceKey();

    Task<int> GetWorkspaceId();
}
