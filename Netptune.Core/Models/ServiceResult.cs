using Netptune.Core.Enums;

namespace Netptune.Core.Models
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

        public static ServiceResult<TResult> Ok<TResult>(string message = "Ok")
            => new ServiceResult<TResult>(message, ServiceResultStatus.Ok);

        public static ServiceResult<TResult> NoContent()
            => new ServiceResult<TResult>(ServiceResultStatus.NoContent);
    }
}
