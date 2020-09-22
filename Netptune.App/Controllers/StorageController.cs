using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Services;
using Netptune.Core.Storage;

namespace Netptune.App.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StorageController : ControllerBase
    {
        private readonly IStorageService StorageService;
        private readonly IIdentityService Identity;
        private readonly IUserService UserService;

        public StorageController(IStorageService storageService, IIdentityService identity, IUserService userService)
        {
            StorageService = storageService;
            Identity = identity;
            UserService = userService;
        }

        [HttpPost]
        [Route("profile-picture")]
        public async Task<IActionResult> UploadProfilePicture()
        {
            var file = Request.Form.Files.FirstOrDefault();

            if (file is null) return BadRequest();

            var userId = await Identity.GetCurrentUserId();
            var extension = Path.GetExtension(file.FileName);
            var key = Path.Join(PathConstants.ProfilePicturePath, $"{userId}{extension}");

            var fileStream = file.OpenReadStream();

            var result = await StorageService.UploadFileAsync(fileStream, key);

            var user = await Identity.GetCurrentUser();

            user.PictureUrl = result.Payload?.Uri;

            await UserService.Update(user);

            return Ok(result);
        }
    }
}
