using Netptune.Core.Enums;
using Netptune.Core.Models;

namespace Netptune.Services.Common
{
    public abstract class ServiceBase
    {
        public static ServiceResult<TResult> NotFound<TResult>(string message = "Not found")
        {
            return new ServiceResult<TResult>(message, ServiceResultStatus.NotFound);
        }

        public static ServiceResult<TResult> BadRequest<TResult>(string message = "Bad Request")
        {
            return new ServiceResult<TResult>(message, ServiceResultStatus.BadRequest);
        }

        public static ServiceResult<TResult> Unauthorized<TResult>(string message = "Unauthorized")
        {
            return new ServiceResult<TResult>(message, ServiceResultStatus.Unauthorized);
        }

        public static ServiceResult<TObject> Ok<TObject>(TObject result, string message = "Ok")
        {
            return new ServiceResult<TObject>(message, ServiceResultStatus.Ok, result);
        }

        public static ServiceResult<TResult> Ok<TResult>(string message = "Ok")
        {
            return new ServiceResult<TResult>(message, ServiceResultStatus.Ok);
        }

        public static ServiceResult<TResult> NoContent<TResult>()
        {
            return new ServiceResult<TResult>(ServiceResultStatus.NoContent);
        }
    }
}
