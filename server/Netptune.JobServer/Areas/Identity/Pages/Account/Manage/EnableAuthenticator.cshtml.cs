using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Netptune.JobServer.Areas.Identity.Pages.Account.Manage
{
    public class EnableAuthenticatorModel : PageModel
    {
        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
        private readonly ILogger<EnableAuthenticatorModel> Logger;
        private readonly UrlEncoder UrlEncoder;
        private readonly UserManager<IdentityUser> UserManager;

        public EnableAuthenticatorModel(
            UserManager<IdentityUser> userManager,
            ILogger<EnableAuthenticatorModel> logger,
            UrlEncoder urlEncoder)
        {
            UserManager = userManager;
            Logger = logger;
            UrlEncoder = urlEncoder;
        }

        public string SharedKey { get; set; }

        public string AuthenticatorUri { get; set; }

        [TempData] public string[] RecoveryCodes { get; set; }

        [TempData] public string StatusMessage { get; set; }

        [BindProperty] public InputModel Input { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await UserManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{UserManager.GetUserId(User)}'.");
            }

            await LoadSharedKeyAndQrCodeUriAsync(user);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await UserManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{UserManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadSharedKeyAndQrCodeUriAsync(user);
                return Page();
            }

            // Strip spaces and hypens
            var verificationCode = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

            var is2FaTokenValid = await UserManager.VerifyTwoFactorTokenAsync(
                user, UserManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2FaTokenValid)
            {
                ModelState.AddModelError("Input.Code", "Verification code is invalid.");
                await LoadSharedKeyAndQrCodeUriAsync(user);
                return Page();
            }

            await UserManager.SetTwoFactorEnabledAsync(user, true);
            var userId = await UserManager.GetUserIdAsync(user);
            Logger.LogInformation("User with ID '{UserId}' has enabled 2FA with an authenticator app.", userId);

            StatusMessage = "Your authenticator app has been verified.";

            if (await UserManager.CountRecoveryCodesAsync(user) == 0)
            {
                var recoveryCodes = await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                RecoveryCodes = recoveryCodes.ToArray();
                return RedirectToPage("./ShowRecoveryCodes");
            }

            return RedirectToPage("./TwoFactorAuthentication");
        }

        private async Task LoadSharedKeyAndQrCodeUriAsync(IdentityUser user)
        {
            // Load the authenticator key & QR code URI to display on the form
            var unformattedKey = await UserManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await UserManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await UserManager.GetAuthenticatorKeyAsync(user);
            }

            SharedKey = FormatKey(unformattedKey);

            var email = await UserManager.GetEmailAsync(user);
            AuthenticatorUri = GenerateQrCodeUri(email, unformattedKey);
        }

        private static string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            var currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(' ');
                currentPosition += 4;
            }

            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey[currentPosition..]);
            }

            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            return string.Format(
                AuthenticatorUriFormat,
                UrlEncoder.Encode("Netptune.Authentication"),
                UrlEncoder.Encode(email),
                unformattedKey);
        }

        public class InputModel
        {
            [Required]
            [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.",
                MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Verification Code")]
            public string Code { get; set; }
        }
    }
}
