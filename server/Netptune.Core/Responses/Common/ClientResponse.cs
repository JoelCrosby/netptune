namespace Netptune.Core.Responses.Common
{
    public class ClientResponse
    {
        protected ClientResponse()
        {
        }

        public bool IsSuccess { get; protected set; }

        public string Message { get; protected set; }

        public static ClientResponse Success(string message = null)
        {
            return new()
            {
                IsSuccess = true,
                Message = message,
            };
        }

        public static ClientResponse Failed(string message = null)
        {
            return new()
            {
                IsSuccess = false,
                Message = message,
            };
        }
    }

    public class ClientResponse<TPayload> : ClientResponse
    {
        protected ClientResponse()
        {
        }

        public TPayload Payload { get; set; }

        public static new ClientResponse<TPayload> Success(string message = null)
        {
            return new()
            {
                IsSuccess = true,
                Message = message,
            };
        }

        public static new ClientResponse<TPayload> Failed(string message = null)
        {
            return new()
            {
                IsSuccess = false,
                Message = message,
            };
        }

        public static ClientResponse<TPayload> Success(TPayload payload, string message = null)
        {
            return new()
            {
                IsSuccess = true,
                Message = message,
                Payload = payload,
            };
        }

        public static ClientResponse<TPayload> Failed(TPayload payload, string message = null)
        {
            return new()
            {
                IsSuccess = false,
                Message = message,
                Payload = payload,
            };
        }
    }
}
