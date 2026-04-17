namespace Netptune.Core.Responses.Common;

public readonly struct ClientResponse
{
    public bool IsSuccess { get; init; }
    public string? Message { get; init; }
    private readonly ResponseType ResponseType;

    // ReSharper disable once UnusedMember.Global
    public ClientResponse(bool isSuccess, string? message, ResponseType responseType =  ResponseType.Default)
    {
        IsSuccess = isSuccess;
        Message = message;
        ResponseType = responseType;
    }

    public bool IsNotFound => ResponseType == ResponseType.NotFound;

    public static readonly ClientResponse Success = new ()
    {
        IsSuccess = true,
    };

    public static ClientResponse Failed(string? message = null)
    {
        return new(false, message);
    }

    public static readonly ClientResponse NotFound = new(false, null,  ResponseType.NotFound);
}

public readonly struct ClientResponse<TPayload>
{
    public bool IsSuccess { get; init; }
    public TPayload? Payload { get; init; }
    public string? Message { get; init; }
    public ResponseType ResponseType { get; init; }

    private ClientResponse(
        bool isSuccess,
        TPayload? payload = default,
        string? message = null,
        ResponseType? responseType = null)
    {
        this.IsSuccess = isSuccess;
        this.Payload = payload;
        this.Message = message;
        this.ResponseType = responseType ?? ResponseType.Default;
    }

    public bool IsNotFound => ResponseType == ResponseType.NotFound;

    public static ClientResponse<TPayload> Success(string? message = null)
    {
        return new(true, message: message);
    }

    public static ClientResponse<TPayload> Failed(string? message = null)
    {
        return new(false, default, message);
    }

    public static ClientResponse<TPayload> Success(TPayload payload, string? message = null)
    {
        return new(true, payload, message);
    }

    public static ClientResponse<TPayload> Failed(TPayload payload, string? message = null)
    {
        return new(false, payload, message);
    }

    public static readonly ClientResponse<TPayload> NotFound = new (false, default, null, ResponseType.NotFound);

    public static implicit operator ClientResponse<TPayload>(TPayload payload) => new (isSuccess: true, payload: payload, message: null);
}
