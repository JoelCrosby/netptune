namespace Netptune.Core.Responses.Common;

public class ClientResponse
{
    public bool IsSuccess { get; init; }

    public string? Message { get; init; }

    public ResponseType ResponseType { get; init; } = ResponseType.Default;

    public bool IsNotFound => ResponseType == ResponseType.NotFound;

    public static ClientResponse Success(string? message = null)
    {
        return new()
        {
            IsSuccess = true,
            Message = message,
        };
    }

    public static ClientResponse Failed(string? message = null)
    {
        return new()
        {
            IsSuccess = false,
            Message = message,
        };
    }

    public static readonly ClientResponse NotFound = new()
    {
        IsSuccess = false,
        ResponseType = ResponseType.NotFound,
    };
}

public class ClientResponse<TPayload> : ClientResponse
{
    public TPayload? Payload { get; init; }

    public static new ClientResponse<TPayload> Success(string? message = null)
    {
        return new()
        {
            IsSuccess = true,
            Message = message,
        };
    }

    public static new ClientResponse<TPayload> Failed(string? message = null)
    {
        return new()
        {
            IsSuccess = false,
            Message = message,
        };
    }

    public static ClientResponse<TPayload> Success(TPayload payload, string? message = null)
    {
        return new()
        {
            IsSuccess = true,
            Message = message,
            Payload = payload,
        };
    }

    public static ClientResponse<TPayload> Failed(TPayload payload, string? message = null)
    {
        return new()
        {
            IsSuccess = false,
            Message = message,
            Payload = payload,
        };
    }

    public static readonly new ClientResponse<TPayload> NotFound = new()
    {
        IsSuccess = false,
        ResponseType = ResponseType.NotFound,
    };
}
