using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services;
using Netptune.Core.ViewModels.Activity;

namespace Netptune.App.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = NetptunePolicies.Workspace)]
    public class ActivityController : ControllerBase
    {
        private readonly IActivityService ActivityService;

        public ActivityController(IActivityService activityService)
        {
            ActivityService = activityService;
        }

        [AllowAnonymous]
        [HttpGet("{entityType}/{entityId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(List<ActivityViewModel>))]
        public async Task<IActionResult> Get(EntityType entityType, int entityId)
        {
            var result = await ActivityService.GetActivities(entityType, entityId);

            return Ok(result);
        }
    }
}
