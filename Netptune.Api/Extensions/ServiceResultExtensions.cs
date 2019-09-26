using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Enums;
using Netptune.Core.Models;

namespace Netptune.Api.Extensions
{
    public static class ServiceResultExtensions
    {
        public static IActionResult ToRestResult<TResult>(this ServiceResult<TResult> serviceResult)
        {
            return serviceResult.Status switch
            {
                ServiceResultStatus.NotFound => new NotFoundObjectResult(serviceResult.Message),
                ServiceResultStatus.BadRequest => new BadRequestObjectResult(serviceResult.Message),
                ServiceResultStatus.Unauthorized => new UnauthorizedObjectResult(serviceResult.Message),
                ServiceResultStatus.Ok => new OkObjectResult(serviceResult.Result),
                ServiceResultStatus.NoContent => new NoContentResult(),

                _ => new StatusCodeResult(500),
            };
        }
    }
}
