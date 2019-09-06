using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Enums;
using Netptune.Core.Models;

namespace Netptune.Api.Extensions
{
    public static class ServiceResultExtensions
    {
        public static IActionResult ToRestResult<TResult>(this ServiceResult<TResult> serviceResult)
        {
            switch (serviceResult.Status)
            {
                case ServiceResultStatus.NotFound:
                    return new NotFoundObjectResult(serviceResult.Message);
                case ServiceResultStatus.BadRequest:
                    return new BadRequestObjectResult(serviceResult.Message);
                case ServiceResultStatus.Unauthorized:
                    return new UnauthorizedObjectResult(serviceResult.Message);
                case ServiceResultStatus.Ok:
                    return new OkObjectResult(serviceResult.Result);
                case ServiceResultStatus.NoContent:
                    return new NoContentResult();
            }

            return new StatusCodeResult(500);
        }
    }
}
