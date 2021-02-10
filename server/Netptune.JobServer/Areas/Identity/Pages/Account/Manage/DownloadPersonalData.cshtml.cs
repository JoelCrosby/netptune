using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Netptune.JobServer.Areas.Identity.Pages.Account.Manage
{
    public class DownloadPersonalDataModel : PageModel
    {
        private readonly ILogger<DownloadPersonalDataModel> Logger;
        private readonly UserManager<IdentityUser> UserManager;

        public DownloadPersonalDataModel(
            UserManager<IdentityUser> userManager,
            ILogger<DownloadPersonalDataModel> logger)
        {
            UserManager = userManager;
            Logger = logger;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await UserManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{UserManager.GetUserId(User)}'.");
            }

            Logger.LogInformation("User with ID '{UserId}' asked for their personal data.",
                UserManager.GetUserId(User));

            // Only include personal data for download
            var personalData = new Dictionary<string, string>();
            var personalDataProps = typeof(IdentityUser).GetProperties().Where(
                prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
            foreach (var p in personalDataProps)
            {
                personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
            }

            var logins = await UserManager.GetLoginsAsync(user);
            foreach (var l in logins)
            {
                personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
            }

            Response.Headers.Add("Content-Disposition", "attachment; filename=PersonalData.json");
            return new FileContentResult(JsonSerializer.SerializeToUtf8Bytes(personalData), "application/json");
        }
    }
}
