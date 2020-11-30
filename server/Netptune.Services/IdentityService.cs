using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

using Netptune.Core.Entities;
using Netptune.Core.Services;

namespace Netptune.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<AppUser> UserManager;
        private readonly IHttpContextAccessor ContextAccessor;

        public IdentityService(UserManager<AppUser> userManager, IHttpContextAccessor contextAccessor)
        {
            UserManager = userManager;
            ContextAccessor = contextAccessor;
        }

        public Task<AppUser> GetCurrentUser()
        {
            var user = ContextAccessor.HttpContext?.User;

            return UserManager.GetUserAsync(user);
        }

        public async Task<string> GetCurrentUserEmail()
        {
            var user = await GetCurrentUser();

            return await UserManager.GetEmailAsync(user);
        }

        public async Task<string> GetCurrentUserId()
        {
            var user = await GetCurrentUser();

            return await UserManager.GetUserIdAsync(user);
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
    }
}
