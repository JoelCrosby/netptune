using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Netptune.JobServer.Areas.Identity.Pages.Account.Manage
{
    public class EmailModel : PageModel
    {
        private readonly IEmailSender EmailSender;
        private readonly UserManager<IdentityUser> UserManager;

        public EmailModel(
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender)
        {
            UserManager = userManager;
            EmailSender = emailSender;
        }

        public string Username { get; set; }

        public string Email { get; set; }

        public bool IsEmailConfirmed { get; set; }

        [TempData] public string StatusMessage { get; set; }

        [BindProperty] public InputModel Input { get; set; }

        private async Task LoadAsync(IdentityUser user)
        {
            var email = await UserManager.GetEmailAsync(user);
            Email = email;

            Input = new InputModel { NewEmail = email };

            IsEmailConfirmed = await UserManager.IsEmailConfirmedAsync(user);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await UserManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{UserManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await UserManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{UserManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var email = await UserManager.GetEmailAsync(user);
            if (Input.NewEmail != email)
            {
                var userId = await UserManager.GetUserIdAsync(user);
                var code = await UserManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmailChange",
                    null,
                    new { userId, email = Input.NewEmail, code },
                    Request.Scheme);
                await EmailSender.SendEmailAsync(
                    Input.NewEmail,
                    "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                StatusMessage = "Confirmation link to change email sent. Please check your email.";
                return RedirectToPage();
            }

            StatusMessage = "Your email is unchanged.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var user = await UserManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{UserManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var userId = await UserManager.GetUserIdAsync(user);
            var email = await UserManager.GetEmailAsync(user);
            var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                null,
                new { area = "Identity", userId, code },
                Request.Scheme);
            await EmailSender.SendEmailAsync(
                email,
                "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            StatusMessage = "Verification email sent. Please check your email.";
            return RedirectToPage();
        }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "New email")]
            public string NewEmail { get; set; }
        }
    }
}
