using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Netptune.App.Utility;
using Netptune.Core.Services;

namespace Netptune.App.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MetaController : ControllerBase
    {
        private readonly IWebService WebService;
        private readonly BuildInfo BuildInfo;

        public MetaController(IWebService webService ,BuildInfo buildInfo)
        {
            WebService = webService;
            BuildInfo = buildInfo;
        }

        [Route("build-info")]
        public IActionResult GetBuildInfo()
        {
            var gitHash = BuildInfo.GetBuildInfo();

            return Ok(gitHash);
        }

        [AllowAnonymous]
        [Route("uri-meta-info")]
        public async Task<IActionResult> GetUriMetaInfo([Required] string url)
        {
            var meta = await WebService.GetMetaDataFromUrl(url);

            return Ok(new
            {
                Success = 1,
                Meta = meta,
            });
        }
    }
}
