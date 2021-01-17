using System;
using System.Linq;
using System.Security.Authentication;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Services;

namespace Netptune.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly IUserCache Cache;
        private readonly IHttpContextAccessor ContextAccessor;

        public IdentityService(IUserCache cache, IHttpContextAccessor contextAccessor)
        {
            Cache = cache;
            ContextAccessor = contextAccessor;
        }

        public string GetUserId() => GetClaimValue(ClaimTypes.NameIdentifier);

        public string GetUserEmail() => GetClaimValue(ClaimTypes.Email);

        public string GetUserName()
        {
            if (GetClaimValue("urn:github:name") is var githubUserName && githubUserName is {})
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
            return Cache.Get(GetUserId());
        }

        public async Task<string> GetCurrentUserEmail()
        {
            var claimEmail = GetUserEmail();

            if (!string.IsNullOrEmpty(claimEmail))
            {
                return claimEmail;
            }

            var user = await GetCurrentUser();
            return user.Email;
        }

        public Task<string> GetCurrentUserId()
        {
            return Task.FromResult(GetUserId());
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
}
