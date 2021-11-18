using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Netptune.App.Utility;
using Netptune.Core.Authorization;
using Netptune.Core.Services;
using Netptune.Core.Storage;
using Netptune.Core.Utilities;

namespace Netptune.App.Controllers;

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
    [DisableFormValueModelBinding]
    public async Task<IActionResult> UploadProfilePicture()
    {
        var file = Request.Form.Files[0];

        if (file.Length > 50 * 1024 * 1024)
        {
            return BadRequest("Request file size exceeds maximum of 50MB.");
        }

        var userId = await Identity.GetCurrentUserId();
        var extension = Path.GetExtension(file.FileName);
        var key = Path.Join(PathConstants.ProfilePicturePath, $"{userId}-{UniqueIdBuilder.Generate(userId)}{extension}");

        var fileStream = file.OpenReadStream();

        var result = await StorageService.UploadFileAsync(fileStream, key);

        var user = await Identity.GetCurrentUser();

        user.PictureUrl = result.Payload?.Uri;

        await UserService.Update(user);

        return Ok(result);
    }

    [HttpPost]
    [Route("media")]
    [Authorize(Policy = NetptunePolicies.Workspace)]
    [DisableFormValueModelBinding]
    public async Task<IActionResult> UploadMedia()
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var file = Request.Form.Files[0];

        if (file.Length > 50 * 1024 * 1024)
        {
            return BadRequest("Request file size exceeds maximum of 50MB.");
        }

        var userId = await Identity.GetCurrentUserId();
        var extension = Path.GetExtension(file.FileName);
        var key = Path.Join(PathConstants.MediaPath(workspaceKey), $"{UniqueIdBuilder.Generate(userId)}{extension}");

        var fileStream = file.OpenReadStream();

        var result = await StorageService.UploadFileAsync(fileStream, key);

        return Ok(result);
    }
}