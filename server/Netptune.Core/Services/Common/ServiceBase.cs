using Netptune.Core.Responses.Common;

namespace Netptune.Core.Services.Common
{
    public abstract class ServiceBase<TResult>
    {
        protected virtual ClientResponse<TResult> Success(TResult payload, string message = null)
        {
            return ClientResponse<TResult>.Success(payload, message);
        }

        protected virtual ClientResponse<TType> Success<TType>(TType payload, string message = null)
        {
            return ClientResponse<TType>.Success(payload, message);
        }

        protected virtual ClientResponse<TResult> Success(string message = null)
        {
            return ClientResponse<TResult>.Success(message);
        }

        protected virtual ClientResponse<TType> Success<TType>(string message = null)
        {
            return ClientResponse<TType>.Success(message);
        }

        protected virtual ClientResponse<TResult> Failed(string message, TResult payload)
        {
            return ClientResponse<TResult>.Failed(payload, message);
        }

        protected virtual ClientResponse<TType> Failed<TType>(string message, TType payload)
        {
            return ClientResponse<TType>.Failed(payload, message);
        }

        protected virtual ClientResponse<TResult> Failed(TResult payload, string message = null)
        {
            return ClientResponse<TResult>.Failed(payload, message);
        }

        protected virtual ClientResponse<TType> Failed<TType>(TType payload, string message = null)
        {
            return ClientResponse<TType>.Failed(payload, message);
        }

        protected virtual ClientResponse<TResult> Failed(string message = null)
        {
            return ClientResponse<TResult>.Failed(message);
        }

        protected virtual ClientResponse<TType> Failed<TType>(string message = null)
        {
            return ClientResponse<TType>.Failed(message);
        }
    }
}
