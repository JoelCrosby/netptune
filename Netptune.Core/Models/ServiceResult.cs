using Netptune.Core.Enums;

namespace Netptune.Core.Models
{
    public sealed class ServiceResult<TResult>
    {
        public string Message { get; }

        public ServiceResultStatus Status { get; }

        public TResult Result { get; }

        public ServiceResult(ServiceResultStatus status)
        {
            Status = status;
        }

        public ServiceResult(string message, ServiceResultStatus status)
        {
            Message = message;
            Status = status;
        }

        public ServiceResult(string message, ServiceResultStatus status, TResult result)
        {
            Message = message;
            Status = status;
            Result = result;
        }
    }
}
