using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Netptune.JobServer.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> SignInManager;
        private readonly UserManager<IdentityUser> UserManager;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public Task OnGetAsync(string returnUrl = null)
        {
            if (!GetIsAuthenticated())
            {
                Response.Redirect("/account/login");
                return Task.CompletedTask;
            }

            ReturnUrl = returnUrl;

            return Task.CompletedTask;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            if (!GetIsAuthenticated())
            {
                return Redirect("/account/login");
            }

            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = new IdentityUser
            {
                UserName = Input.Email,
                Email = Input.Email,
            };

            var result = await UserManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                await SignInManager.SignInAsync(user, false);

                return LocalRedirect(returnUrl);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private bool GetIsAuthenticated()
        {
            return Request.HttpContext.User.Identity?.IsAuthenticated ?? false;
        }
    }
}
