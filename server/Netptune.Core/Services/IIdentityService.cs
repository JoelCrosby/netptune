using Netptune.Core.Entities;

namespace Netptune.Core.Services;

public interface IIdentityService
{
    Task<AppUser> GetCurrentUser();

    string GetCurrentUserId();

    string? TryGetCurrentUserId();

    bool IsSignedIn();

    string GetCurrentUserEmail();

    string GetUserName();

    string? GetPictureUrl();

    string GetProviderKey();

    string? GetProviderUsername();

    bool IsProviderEmailVerified();

    string GetWorkspaceKey();

    string? TryGetWorkspaceKey();

    Task<int> GetWorkspaceId();

    bool IsRestrictedByCredential(string permission);
}
