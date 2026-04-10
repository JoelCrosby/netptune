using Netptune.Core.Responses.Common;

namespace Netptune.Core.Services.Common;

public abstract class ServiceBase<TResult> where TResult : class
{
    protected ClientResponse<TResult> Success(TResult payload, string? message = null)
    {
        return ClientResponse<TResult>.Success(payload, message);
    }

    protected ClientResponse<TResult> Success<TType>(TResult payload, string? message = null)
    {
        return ClientResponse<TResult>.Success(payload, message);
    }

    protected ClientResponse<TResult> Success(string? message = null)
    {
        return ClientResponse<TResult>.Success(message);
    }

    protected ClientResponse<TResult> Success<TType>(string? message = null)
    {
        return ClientResponse<TResult>.Success(message);
    }

    protected ClientResponse<TResult> Failed(string message, TResult payload)
    {
        return ClientResponse<TResult>.Failed(payload, message);
    }

    protected ClientResponse<TResult> Failed(TResult payload, string? message = null)
    {
        return ClientResponse<TResult>.Failed(payload, message);
    }

    protected ClientResponse<TResult> Failed(string? message = null)
    {
        return ClientResponse<TResult>.Failed(message);
    }

    protected ClientResponse<TResult> NotFound()
    {
        return ClientResponse<TResult>.NotFound;
    }
}
