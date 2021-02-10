using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Netptune.JobServer.Areas.Identity.Pages.Account.Manage
{
    public class PersonalDataModel : PageModel
    {
        private readonly UserManager<IdentityUser> UserManager;

        public PersonalDataModel(UserManager<IdentityUser> userManager)
        {
            UserManager = userManager;
        }

        public async Task<IActionResult> OnGet()
        {
            var user = await UserManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{UserManager.GetUserId(User)}'.");
            }

            return Page();
        }
    }
}
