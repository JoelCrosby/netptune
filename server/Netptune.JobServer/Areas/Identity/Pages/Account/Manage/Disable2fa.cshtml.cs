using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Netptune.JobServer.Areas.Identity.Pages.Account.Manage
{
    public class Disable2faModel : PageModel
    {
        private readonly ILogger<Disable2faModel> Logger;
        private readonly UserManager<IdentityUser> UserManager;

        public Disable2faModel(
            UserManager<IdentityUser> userManager,
            ILogger<Disable2faModel> logger)
        {
            UserManager = userManager;
            Logger = logger;
        }

        [TempData] public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var user = await UserManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{UserManager.GetUserId(User)}'.");
            }

            if (!await UserManager.GetTwoFactorEnabledAsync(user))
            {
                throw new InvalidOperationException(
                    $"Cannot disable 2FA for user with ID '{UserManager.GetUserId(User)}' as it's not currently enabled.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await UserManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{UserManager.GetUserId(User)}'.");
            }

            var disable2FaResult = await UserManager.SetTwoFactorEnabledAsync(user, false);
            if (!disable2FaResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unexpected error occurred disabling 2FA for user with ID '{UserManager.GetUserId(User)}'.");
            }

            Logger.LogInformation("User with ID '{UserId}' has disabled 2fa.", UserManager.GetUserId(User));
            StatusMessage = "2fa has been disabled. You can reenable 2fa when you setup an authenticator app";
            return RedirectToPage("./TwoFactorAuthentication");
        }
    }
}
