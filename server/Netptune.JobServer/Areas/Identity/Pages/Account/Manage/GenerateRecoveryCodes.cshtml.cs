using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Netptune.JobServer.Areas.Identity.Pages.Account.Manage
{
    public class GenerateRecoveryCodesModel : PageModel
    {
        private readonly ILogger<GenerateRecoveryCodesModel> Logger;
        private readonly UserManager<IdentityUser> UserManager;

        public GenerateRecoveryCodesModel(
            UserManager<IdentityUser> userManager,
            ILogger<GenerateRecoveryCodesModel> logger)
        {
            UserManager = userManager;
            Logger = logger;
        }

        [TempData] public string[] RecoveryCodes { get; set; }

        [TempData] public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await UserManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{UserManager.GetUserId(User)}'.");
            }

            var isTwoFactorEnabled = await UserManager.GetTwoFactorEnabledAsync(user);

            if (isTwoFactorEnabled)
            {
                return Page();
            }

            var userId = await UserManager.GetUserIdAsync(user);

            throw new InvalidOperationException(
                $"Cannot generate recovery codes for user with ID '{userId}' because they do not have 2FA enabled.");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await UserManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{UserManager.GetUserId(User)}'.");
            }

            var isTwoFactorEnabled = await UserManager.GetTwoFactorEnabledAsync(user);
            var userId = await UserManager.GetUserIdAsync(user);

            if (!isTwoFactorEnabled)
            {
                throw new InvalidOperationException(
                    $"Cannot generate recovery codes for user with ID '{userId}' as they do not have 2FA enabled.");
            }

            var recoveryCodes = await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            RecoveryCodes = recoveryCodes.ToArray();

            Logger.LogInformation("User with ID '{UserId}' has generated new 2FA recovery codes.", userId);
            StatusMessage = "You have generated new recovery codes.";

            return RedirectToPage("./ShowRecoveryCodes");
        }
    }
}
