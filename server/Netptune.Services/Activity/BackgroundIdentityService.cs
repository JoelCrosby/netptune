using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Services;

namespace Netptune.Services.Activity;

public sealed class BackgroundIdentityService : IIdentityService
{
    private readonly IActorContext Actor;
    private readonly IUserCache UserCache;

    public BackgroundIdentityService(IActorContext actor, IUserCache userCache)
    {
        Actor = actor;
        UserCache = userCache;
    }

    public string GetCurrentUserId() => RequireActor().UserId;

    public string? TryGetCurrentUserId() => Actor.Current?.UserId;

    public string GetWorkspaceKey() => RequireActor().WorkspaceKey;

    public string? TryGetWorkspaceKey() => Actor.Current?.WorkspaceKey;

    public Task<int> GetWorkspaceId() => Task.FromResult(RequireActor().WorkspaceId);

    public string GetProviderKey() => RequireActor().UserId;

    public async Task<AppUser> GetCurrentUser()
    {
        var userId = RequireActor().UserId;
        var user = await UserCache.Get(userId);

        if (user is null)
        {
            throw new InvalidOperationException($"User '{userId}' could not be read from the cache.");
        }

        return user;
    }

    public string GetCurrentUserEmail()
    {
        var user = RequireCurrentUser();

        return user.Email ?? user.UserName!;
    }

    public string GetUserName()
    {
        var user = RequireCurrentUser();
        var displayName = $"{user.Firstname} {user.Lastname}".Trim();

        return displayName.Length > 0 ? displayName : user.UserName!;
    }

    public string? GetPictureUrl() => RequireCurrentUser().PictureUrl;

    private ActorIdentity RequireActor()
    {
        var actor = Actor.Current;

        if (actor is null)
        {
            throw new InvalidOperationException(
                "No actor is in scope. Background work must run inside IActorContext.Begin.");
        }

        return actor;
    }

    private AppUser RequireCurrentUser()
    {
        return GetCurrentUser().GetAwaiter().GetResult();
    }
}
