using Microsoft.AspNetCore.Mvc;
using Netptune.Services.Enums;

namespace Netptune.Services.Models
{
    public sealed class ServiceResult<TResult>
    {
        public string Message { get; }

        public ServiceResultStatus Status { get; }

        public TResult Result { get; }

        private ServiceResult(ServiceResultStatus status)
        {
            Status = status;
        }

        private ServiceResult(string message, ServiceResultStatus status)
        {
            Message = message;
            Status = status;
        }

        private ServiceResult(string message, ServiceResultStatus status, TResult result)
        {
            Message = message;
            Status = status;
            Result = result;
        }

        public static ServiceResult<TResult> NotFound(string message = "Not found")
            => new ServiceResult<TResult>(message, ServiceResultStatus.NotFound);

        public static ServiceResult<TResult> BadRequest(string message = "Bad Request")
            => new ServiceResult<TResult>(message, ServiceResultStatus.BadRequest);

        public static ServiceResult<TResult> Unauthorized(string message = "Unauthorized")
            => new ServiceResult<TResult>(message, ServiceResultStatus.Unauthorized);

        public static ServiceResult<TResult> Ok(TResult result, string message = "Ok")
            => new ServiceResult<TResult>(message, ServiceResultStatus.Ok, result);

        public static ServiceResult<TResult> Ok(string message = "Ok")
            => new ServiceResult<TResult>(message, ServiceResultStatus.Ok);

        public static ServiceResult<TResult> NoContent()
            => new ServiceResult<TResult>(ServiceResultStatus.NoContent);

        public IActionResult ToRestResult()
        {
            switch (Status)
            {
                case ServiceResultStatus.NotFound:
                    return new NotFoundObjectResult(Message);
                case ServiceResultStatus.BadRequest:
                    return new BadRequestObjectResult(Message);
                case ServiceResultStatus.Unauthorized:
                    return new UnauthorizedObjectResult(Message);
                case ServiceResultStatus.Ok:
                    return new OkObjectResult(Result);
                case ServiceResultStatus.NoContent:
                    return new NoContentResult();
            }

            return new StatusCodeResult(500);
        }
    }
}
