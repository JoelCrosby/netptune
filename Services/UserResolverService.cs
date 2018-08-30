using DataPlane.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace DataPlane.Services
{
    public class UserResolverService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IHttpContextAccessor _context;

        public UserResolverService(IHttpContextAccessor context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public string GetUser()
        {
            var user = _context.HttpContext.User;
            return _userManager.GetUserId(user);
        }

    }
}
