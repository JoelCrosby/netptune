using System;
using System.Linq;
using System.Security.Authentication;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Services;

namespace Netptune.Services;

public class IdentityService : IIdentityService
{
    private readonly IUserCache UserCache;
    private readonly IWorkspaceCache WorkspaceCache;
    private readonly IHttpContextAccessor ContextAccessor;

    public IdentityService(IUserCache userCache, IWorkspaceCache workspaceCache, IHttpContextAccessor contextAccessor)
    {
        UserCache = userCache;
        WorkspaceCache = workspaceCache;
        ContextAccessor = contextAccessor;
    }

    public string GetCurrentUserId() => GetClaimValue(ClaimTypes.NameIdentifier);

    public string GetCurrentUserEmail() => GetClaimValue(ClaimTypes.Email);

    public string GetUserName()
    {
        if (GetClaimValue("urn:github:name") is {} githubUserName)
        {
            return githubUserName;
        }

        return GetClaimValue(ClaimTypes.Name);
    }

    public string GetPictureUrl()
    {
        return GetClaimValue("Provider-Picture-Url");
    }

    public Task<AppUser> GetCurrentUser()
    {
        return UserCache.Get(GetCurrentUserId())!;
    }

    public string GetWorkspaceKey()
    {
        var context = ContextAccessor.HttpContext;

        if (context is null)
        {
            throw new Exception("HttpContext was null");
        }

        if (context.Request.Headers.TryGetValue("workspace", out var workspace))
        {
            return workspace;
        }

        throw new Exception("Client request did not contain a 'workspace' header.");
    }

    public async Task<int> GetWorkspaceId()
    {
        var id = await WorkspaceCache.GetIdBySlug(GetWorkspaceKey());

        if (id is null)
        {
            throw new Exception("Failed to get workspace from cache.");
        }

        return id.Value;
    }

    private string GetClaimValue(string type)
    {
        var claimsPrincipal = ContextAccessor.HttpContext?.User;

        var claim = claimsPrincipal?.Claims.FirstOrDefault(c => c.Type == type);
        var result = claim?.Value;

        if (result is null)
        {
            throw new AuthenticationException($"user does not have value for claim of type {type}");
        }

        return result;
    }
}
