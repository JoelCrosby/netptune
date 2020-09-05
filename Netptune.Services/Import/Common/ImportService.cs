using Netptune.Core.Responses.Common;

namespace Netptune.Services.Import.Common
{
    public abstract class ImportService<TResult>
    {
        protected virtual ClientResponse<TResult> Success(TResult payload, string message = null)
        {
            return ClientResponse<TResult>.Success(payload, message);
        }

        protected virtual ClientResponse<TResult> Success(string message = null)
        {
            return ClientResponse<TResult>.Success(message);
        }

        protected virtual ClientResponse<TResult> Failed(string message, TResult payload)
        {
            return ClientResponse<TResult>.Failed(payload, message);
        }

        protected virtual ClientResponse<TResult> Failed(TResult payload, string message = null)
        {
            return ClientResponse<TResult>.Failed(payload, message);
        }

        protected virtual ClientResponse<TResult> Failed(string message = null)
        {
            return ClientResponse<TResult>.Failed(message);
        }
    }
}
