using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Encoding;
using Netptune.Core.Services;

namespace Netptune.App.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StorageController : ControllerBase
    {
        private readonly IStorageService StorageService;

        public StorageController(IStorageService storageService)
        {
            StorageService = storageService;
        }

        [HttpPost]
        public async Task<IActionResult> Upload()
        {
            var file = Request.Form.Files.FirstOrDefault();

            if (file is null) return BadRequest();

            var key = file.FileName.ToUrlSlug();
            var fileStream = file.OpenReadStream();

            await StorageService.UploadFileAsync(fileStream, key);

            return Ok();
        }
    }
}
