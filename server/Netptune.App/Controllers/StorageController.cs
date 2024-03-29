using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Netptune.App.Utility;
using Netptune.Core.Authorization;
using Netptune.Core.Services;
using Netptune.Core.Storage;
using Netptune.Core.Utilities;

namespace Netptune.App.Controllers;

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
    [DisableFormValueModelBinding]
    public async Task<IActionResult> UploadProfilePicture()
    {
        var file = Request.Form.Files.FirstOrDefault();

        if (file is null)
        {
            return BadRequest("Import File must be provided. Only one file can be uploaded at a time.");
        }

        if (file.Length > 50 * 1024 * 1024)
        {
            return BadRequest("Request file size exceeds maximum of 50MB.");
        }

        var userId = Identity.GetCurrentUserId();
        var extension = Path.GetExtension(file.FileName);
        var key = Path.Join(PathConstants.ProfilePicturePath, $"{userId}-{UniqueIdBuilder.Generate(userId)}{extension}");

        var fileStream = file.OpenReadStream();

        var result = await StorageService.UploadFileAsync(fileStream, key, key);

        if (!result.IsSuccess || result.Payload is null)
        {
            return BadRequest();
        }

        await UserService.Update(new ()
        {
            Id = userId,
            PictureUrl = result.Payload.Uri,
        });

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

        var userId = Identity.GetCurrentUserId();
        var extension = Path.GetExtension(file.FileName);
        var key = Path.Join(PathConstants.MediaPath(workspaceKey), $"{UniqueIdBuilder.Generate(userId)}{extension}");

        var fileStream = file.OpenReadStream();

        var result = await StorageService.UploadFileAsync(fileStream, file.FileName, key);

        return Ok(result);
    }
}
