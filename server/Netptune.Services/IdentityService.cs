using System;
using System.Diagnostics.CodeAnalysis;
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
    private readonly IHttpContextAccessor Context;

    public IdentityService(IUserCache userCache, IWorkspaceCache workspaceCache, IHttpContextAccessor context)
    {
        UserCache = userCache;
        WorkspaceCache = workspaceCache;
        Context = context;
    }

    public string GetCurrentUserId() => GetClaimValue(ClaimTypes.NameIdentifier);

    public string GetCurrentUserEmail() => GetClaimValue(ClaimTypes.Email);

    public string GetUserName()
    {
        if (TryGetClaimValue("urn:github:name", out var githubUserName))
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
        var context = Context.HttpContext;

        if (context is null)
        {
            throw new Exception("HttpContext was null");
        }

        if (context.Request.Headers.TryGetValue("workspace", out var workspace))
        {
            return workspace!;
        }

        throw new Exception("request context did not contain a 'workspace' header.");
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

    private bool TryGetClaimValue(string type, [MaybeNullWhen(false)] out string value)
    {
        var claimsPrincipal = Context.HttpContext?.User;

        var claim = claimsPrincipal?.Claims.FirstOrDefault(c => c.Type == type);
        var result = claim?.Value;

        if (result is null)
        {
            value = default;
            return false;
        }

        value = result;
        return true;
    }

    private string GetClaimValue(string type)
    {
        var claimsPrincipal = Context.HttpContext?.User;

        var claim = claimsPrincipal?.Claims.FirstOrDefault(c => c.Type == type);
        var result = claim?.Value;

        if (result is null)
        {
            throw new AuthenticationException($"user does not have value for claim of type {type}");
        }

        return result;
    }
}
