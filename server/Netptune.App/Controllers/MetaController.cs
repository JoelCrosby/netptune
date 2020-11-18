using Microsoft.AspNetCore.Mvc;

using Netptune.App.Utility;

namespace Netptune.App.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MetaController : ControllerBase
    {
        private readonly BuildInfo BuildInfo;

        public MetaController(BuildInfo buildInfo)
        {
            BuildInfo = buildInfo;
        }

        [Route("build-info")]
        public IActionResult GetBuildInfo()
        {
            var gitHash = BuildInfo.GetBuildInfo();

            return Ok(gitHash);
        }
    }
}
